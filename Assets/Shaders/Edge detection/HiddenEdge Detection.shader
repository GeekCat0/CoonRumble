Shader "Hidden/Edge Detection"
{
    Properties
    {
        _OutlineThickness ("Outline Thickness", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)

        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 50
        _NoiseStrength ("Noise Strength", Float) = 1

        _DepthThreshold ("Depth Treshold", Float) = 200
        _NormalThreshold ("Normal Treshold", Float) = 4
        _LuminanceThreshold ("Luminance Treshold", Float) = 1

        _OutlineFadeStart ("Outline Fade Start", Float) = 15
        _OutlineFadeEnd ("Outline Fade End", Float) = 25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
        }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass 
        {
            Name "EDGE DETECTION OUTLINE"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl" // needed to sample scene depth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl" // needed to sample scene normals
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl" // needed to sample scene color/luminance

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            float _NoiseScale;
            float _NoiseStrength;

            float _DepthThreshold;
            float _NormalThreshold;
            float _LuminanceThreshold;

            float _OutlineThickness;
            float4 _OutlineColor;

            float _OutlineFadeStart;
            float _OutlineFadeEnd;

            #pragma vertex Vert // vertex shader is provided by the Blit.hlsl include
            #pragma fragment frag

            // Edge detection kernel that works by taking the sum of the squares of the differences between diagonally adjacent pixels (Roberts Cross).
            float RobertsCross(float3 samples[4])
            {
                const float3 difference_1 = samples[1] - samples[2];
                const float3 difference_2 = samples[0] - samples[3];
                return sqrt(dot(difference_1, difference_1) + dot(difference_2, difference_2));
            }

            // The same kernel logic as above, but for a single-value instead of a vector3.
            float RobertsCross(float samples[4])
            {
                const float difference_1 = samples[1] - samples[2];
                const float difference_2 = samples[0] - samples[3];
                return sqrt(difference_1 * difference_1 + difference_2 * difference_2);
            }
            
            // Helper function to sample scene normals remapped from [-1, 1] range to [0, 1].
            float3 SampleSceneNormalsRemapped(float2 uv)
            {
                return SampleSceneNormals(uv) * 0.5 + 0.5;
            }

            // Helper function to sample scene luminance.
            float SampleSceneLuminance(float2 uv)
            {
                float3 color = SampleSceneColor(uv);
                return color.r * 0.3 + color.g * 0.59 + color.b * 0.11;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                // Screen-space coordinates which we will use to sample.
                float2 uv = IN.texcoord;
                float referenceHeight = 1080.0;
                float resolutionScale = _ScreenParams.y / referenceHeight;

                float scaledThickness = _OutlineThickness * resolutionScale;

                float2 texel_size = float2(1.0 / _ScreenParams.x,
                                           1.0 / _ScreenParams.y);

                float2 offset = texel_size * scaledThickness;

                // Sample scene depth
                float rawDepth = SampleSceneDepth(uv);

                // Convert to linear depth
                float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                // Reconstruct clip space position
                float4 clipPos;
                clipPos.xy = uv * 2.0 - 1.0;
                clipPos.z = rawDepth;
                clipPos.w = 1.0;

                // Convert to view space
                float4 viewPos = mul(UNITY_MATRIX_I_P, clipPos);
                viewPos /= viewPos.w;

                // Convert to world space
                float3 worldPos = mul(UNITY_MATRIX_I_V, float4(viewPos.xyz, 1.0)).xyz;

                float distanceToCamera = distance(worldPos, _WorldSpaceCameraPos);

                float fade = 1.0 - smoothstep(_OutlineFadeStart,
                               _OutlineFadeEnd,
                               distanceToCamera);

                float2 noiseUV = worldPos.xz * _NoiseScale;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                // Center noise around 0
                noise = noise * 2.0 - 1.0;
                
                float distortionStrength = _NoiseStrength; 
                float centeredNoise = noise * 2.0 - 1.0;
                float depthFade = saturate(1.0 / (linearDepth * 0.1));
                float2 distortion = centeredNoise * offset * _NoiseStrength * depthFade;

                float2 uvs[4];

                uvs[0] = uv + float2(-offset.x,  offset.y) + distortion; // top left
                uvs[1] = uv + float2( offset.x,  offset.y) + distortion; // top right
                uvs[2] = uv + float2(-offset.x, -offset.y) + distortion; // bottom left
                uvs[3] = uv + float2( offset.x, -offset.y) + distortion; // bottom right 
                
                float3 normal_samples[4];
                float depth_samples[4], luminance_samples[4];
                
                for (int i = 0; i < 4; i++) {
                    depth_samples[i] = SampleSceneDepth(uvs[i]);
                    normal_samples[i] = SampleSceneNormalsRemapped(uvs[i]);
                    luminance_samples[i] = SampleSceneLuminance(uvs[i]);
                }
                
                // Apply edge detection kernel on the samples to compute edges.
                float edge_depth = RobertsCross(depth_samples);
                float edge_normal = RobertsCross(normal_samples);
                float edge_luminance = RobertsCross(luminance_samples);
                
                // Threshold the edges (discontinuity must be above certain threshold to be counted as an edge). The sensitivities are hardcoded here.
                float depth_threshold = 1 / _DepthThreshold;

                // derivative of depth in screen space
                float depthDerivative = fwidth(rawDepth);

                // scale factor you can tweak
                float adaptiveThreshold = depth_threshold + depthDerivative * 2.0;

                // smooth transition instead of hard step
                edge_depth = smoothstep(adaptiveThreshold,
                                        adaptiveThreshold * 1.5,
                                        edge_depth);
                
                float normal_threshold = 1 / _NormalThreshold;
                edge_normal = smoothstep(normal_threshold, normal_threshold * 2.0, edge_normal);
                
                float luminance_threshold = 1 / _LuminanceThreshold;
                edge_luminance = smoothstep(luminance_threshold, luminance_threshold * 2.0, edge_luminance);
                
                // Combine the edges from depth/normals/luminance using the max operator.
                float edge = max(edge_depth, max(edge_normal, edge_luminance));

                float2 px = offset;

                float edgeBlur = 0;
                edgeBlur += edge;
                edgeBlur += SampleSceneDepth(uv + px);
                edgeBlur += SampleSceneDepth(uv - px);
                edgeBlur += SampleSceneDepth(uv + float2(px.x, -px.y));
                edgeBlur += SampleSceneDepth(uv + float2(-px.x, px.y));
                edgeBlur *= 0.2;
                
                // Break up the edge using noise
                float noisyEdge = edge * lerp(1.0, noise, 0.15f);
                float finalEdge = noisyEdge * fade;
                
                return finalEdge * _OutlineColor;
            }
            ENDHLSL
        }
    }
}