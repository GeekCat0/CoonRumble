using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEnd : MonoBehaviour
{
    [SerializeField] private GameObject endScreen;
    [SerializeField] private TextMeshProUGUI endScore;
    [SerializeField] private TextMeshProUGUI endTime;
    [SerializeField] private float expectedTime = 0f;
    [SerializeField] private int timeMultiplier = 10;
    private LevelManager levelManager;
    private TimeDisplay timeDisplay;
    private int finalScore = 0;

    private void Awake()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
        timeDisplay = FindAnyObjectByType<TimeDisplay>();
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Time.timeScale = 0f;
            if (timeDisplay.GetTime() <= expectedTime)
            {
                finalScore += Mathf.RoundToInt(expectedTime - timeDisplay.GetTime()) * timeMultiplier;
            }
            finalScore += levelManager.GetScore();
            endTime.text = ("Time: " + timeDisplay.GetTime().ToString("0.00"));
            endScore.text = ("Score: " + finalScore);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            endScreen.SetActive(true);
        }
    }
    public void GoToMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(0);
    }
}
