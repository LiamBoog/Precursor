using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private ExpMovingAverageFloat fps;
    
    private void Update()
    {
        fps.AddSample(1f / Time.deltaTime, Time.deltaTime);
        text.text = $"{fps.Value:0.0} fps";
    }
}
