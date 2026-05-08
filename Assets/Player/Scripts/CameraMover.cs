using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [SerializeField] private Transform cameraPosition;
    private void Update()
    {
        transform.position = cameraPosition.position;
    }
}
