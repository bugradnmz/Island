using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FramesPerSecond: MonoBehaviour
{
    private Text fpsText;
    private float deltaTime;

    private void Start()
    {
        fpsText = GetComponent<Text>();
    }

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        
        if (fps > 45)
            fpsText.color = Color.green;
        else if (25 <= fps & fps <= 45)
            fpsText.color = Color.red + Color.yellow;
        else if (fps < 25)
            fpsText.color = Color.red;

        fpsText.text = Mathf.Ceil(fps).ToString();
    }
}
