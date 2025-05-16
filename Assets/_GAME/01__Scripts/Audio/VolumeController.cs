using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class VolumeController : MonoBehaviour
{
    private Slider slider;
    [SerializeField] TextMeshProUGUI valueText;
    [SerializeField] VolumeControllerType volumeControllerType;

    void Start()
    {
        slider = GetComponent<Slider>();
        OnValueChangedListener();
    }
    private void OnValueChangedListener()
    {
        // Set the initial slider value based on PlayerPrefs
        if (volumeControllerType == VolumeControllerType.SFX)
        {
            slider.value = PlayerPrefs.GetFloat("SFX_Volume", 1f);
            slider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        }
        else
        {
            slider.value = PlayerPrefs.GetFloat("BGM_Volume", 1f);
            slider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
        }

        // Update the text initially
        UpdateValueText(slider.value);

        // Add a listener to update the text dynamically as the slider value changes
        slider.onValueChanged.AddListener(UpdateValueText);
    }

    // Function to update the text value in percentage format
    private void UpdateValueText(float value)
    {
        float percentage = value * 100f;
        valueText.text = percentage.ToString("F0") + "%";
    }
}


[System.Serializable]
enum VolumeControllerType
{
    SFX, BGM
}
