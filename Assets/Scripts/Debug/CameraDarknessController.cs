
using UnityEngine;
using UnityEngine.UI;

public class CameraDarknessController : MonoBehaviour
{
    [SerializeField] private Slider darknessSlider;
    [SerializeField] private Image darkFilter;

    private void Start()
    {
        darknessSlider.minValue = 0f;
        darknessSlider.maxValue = 1f;
        darknessSlider.value = 0f;

        UpdateFilter(0f);
        darknessSlider.onValueChanged.AddListener(UpdateFilter);
    }

    private void UpdateFilter(float value)
    {
        Color color = darkFilter.color;
        color.a = value;
        darkFilter.color = color;
    }

    private void OnDestroy()
    {
        if (darknessSlider != null)
        {
            darknessSlider.onValueChanged.RemoveListener(UpdateFilter);
        }
    }
}