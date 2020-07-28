using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    
    TextMeshProUGUI TextComponent;
    float MinFPS = float.MaxValue;
    float MaxFPS = float.MinValue;

    private void Awake()
    {
        TextComponent = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        float FPS = 1 / Time.unscaledDeltaTime;
        if (FPS < MinFPS && FPS!=0) MinFPS = FPS;
        if (FPS > MaxFPS) MaxFPS = FPS;
        TextComponent.text = MinFPS.ToString("000.") + ", " + MaxFPS.ToString("000.") + ", " + FPS.ToString("000.");
    }

}
