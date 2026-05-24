using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Transform currentCheckpoint;

    public void SetCheckpoint(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
    }
    public Transform GetCheckpoint()
    {
        return currentCheckpoint;
    }
}
