using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetSlider : MonoBehaviour
{
    public Slider slider;
    public void SetSliderValue(int value)
    {
        slider.value = value;
    }

    public void SetSliderMax(int max)
    {
        slider.maxValue = max;
        slider.value = max;
    }
}
