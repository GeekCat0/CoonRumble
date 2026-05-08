using System;
using TMPro;
using UnityEngine;

public class SpeedDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private Rigidbody playerBody;

    private void Update()
    {
        speedText.text = "Speed: " + Mathf.Round(Mathf.Abs(playerBody.linearVelocity.x) + Mathf.Abs(playerBody.linearVelocity.z)).ToString();
    }
}
