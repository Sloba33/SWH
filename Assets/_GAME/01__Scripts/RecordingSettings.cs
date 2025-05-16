using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEditor;
using Cinemachine;

public class RecordingSettings : MonoBehaviour
{
    [SerializeField] private CinemachineDollyCart cmdc;
    [SerializeField] private CinemachineVirtualCamera cmvc;
    public List<Image> images = new List<Image>();
    public GameObject cameraObj;
    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.G))
        // {
        //     TurnImagesOff();
        // }
        if (cmdc != null)
        {
            if (cmdc.m_Position >= 0.99f && cmvc != null) cmvc.enabled = false;
        }

    }
    public bool EnabledAtStart;
    private void Start()
    {
        // cmdc = GetComponent<CinemachineDollyCart>();
        cmvc = GetComponent<CinemachineVirtualCamera>();
        // for (int i = 0; i < images.Count; i++)
        // {
        //     images[i].enabled = EnabledAtStart;
        // }
    }
    public void TurnImagesOff()
    {
        for (int i = 0; i < images.Count; i++)
        {
            images[i].enabled = !images[i].enabled;
        }
    }
    public void TurnCamOff()
    {
        this.gameObject.SetActive(false);
    }
}
