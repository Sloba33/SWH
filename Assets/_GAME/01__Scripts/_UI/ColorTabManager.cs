using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorTabManager : MonoBehaviour
{
    public List<ColorTab> colorTabs = new();
    public List<GameObject> colorPanels = new();
    public Transform content;
    private void Start()
    {
        for (int i = 0; i < content.childCount; i++)
        {
            Debug.Log("Adding tabs");
            colorTabs.Add(content.GetChild(i).GetComponent<ColorTab>());
            colorPanels.Add(colorTabs[i].panel);
            colorTabs[i].colorTabManager = this;
           
        }
        colorTabs[0].button.onClick.Invoke();
    }
    public void SelectTab(GameObject panel)
    {
        foreach (GameObject pan in colorPanels)
        {
            if (pan == panel)
            {
                pan.SetActive(true);
            }
            else
                pan.SetActive(false);
        }
    }

}
