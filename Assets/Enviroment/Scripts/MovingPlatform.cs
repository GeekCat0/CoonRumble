using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static UnityEngine.GraphicsBuffer;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform platform;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed;
    [SerializeField] private bool teleporting = false;

    private Vector3 targetPosition;
    private PlayerControler playerControler;

    private void Start()
    {
        playerControler = FindAnyObjectByType<PlayerControler>();
        targetPosition = pointA.position;
    }

    private void LateUpdate()
    {
        platform.position = Vector3.MoveTowards(platform.transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(platform.position, targetPosition) < 0.01f)
        {
            if (teleporting)
            {
                platform.position = pointB.position;
            }
            else
                targetPosition = targetPosition == pointA.position ? pointB.position : pointA.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            playerControler.SetPlatform(platform);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            playerControler.LeavePlatform();
        }
    }
}
