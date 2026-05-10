using TMPro;
using UnityEngine;

public class TimeDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    private float time = 0;
    void Update()
    {
        time += Time.deltaTime;
        text.text = time.ToString("0.00");
    }
}
