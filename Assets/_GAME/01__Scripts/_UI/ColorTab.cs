using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ColorTab : MonoBehaviour
{
    public GameObject panel;
    public Button button;
    public ColorTabManager colorTabManager;


    private void Start()
    {
        button.onClick.AddListener(() =>
                   {
                       colorTabManager.SelectTab(panel);
                       CharacterManager.Instance.PlayClick();
                   });
    }
}
