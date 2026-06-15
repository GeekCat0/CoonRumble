using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEnd : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(0);
        }
    }
}
