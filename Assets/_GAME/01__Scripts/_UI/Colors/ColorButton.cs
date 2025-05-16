using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    public Color color;
    private Button button;
    public int childIndex;
    public ColorButtonManager colorButtonManager;
    public bool isDefault;
    private void Start()
    {
        int childIndex = transform.GetSiblingIndex();
        if (!isDefault) color = transform.GetChild(0).GetComponent<Image>().color;
        button = GetComponent<Button>();
        colorButtonManager = GetComponentInParent<ColorButtonManager>();
        button.onClick.AddListener(() =>
        {
            colorButtonManager.ChangeColor(color, childIndex);
        });
        if (isDefault && colorButtonManager.clothesType == ColorButtonManager.ClothesType.Hat)
            if (CharacterManager.Instance.helmet != null)
                color = CharacterManager.Instance.helmet.defaultColor;
    }

}
