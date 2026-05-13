using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

public class BrightnessSetter : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private float minValue;
    [SerializeField] private float maxValue;

    private ColorAdjustments colorAdjustments;
    private float deltaValue;

    private void Awake()
    {
        volume.profile.TryGet<ColorAdjustments>(out ColorAdjustments adjustments);
        colorAdjustments = adjustments;

        deltaValue = maxValue - minValue;
        SetBrightness();    
    }   
    
    public void SetBrightness()
    {
        float percent = PlayerPrefs.GetFloat("Brightness") / 100f;
        FloatParameter value = new FloatParameter(minValue + percent * deltaValue);
        colorAdjustments.postExposure.SetValue(value);
    }
}
