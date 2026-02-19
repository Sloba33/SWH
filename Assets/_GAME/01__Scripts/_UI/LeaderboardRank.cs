

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
public class LeaderboardRank : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI rankText;


    public void Clear()
    {
        nameText.text = "-";
        levelText.text = "-";
        rankText.text = "-";
    }
    public void Set(string playerName, int level, int rank)
    {
        nameText.text = playerName;
        levelText.text = level.ToString();
        rankText.text = rank.ToString() + ".";
    }
    public void SetAsExtra()
    {
        nameText.text = "";
        levelText.text = "";
        rankText.text = "";
        transform.GetComponent<Image>().enabled = false;
        GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, GetComponent<RectTransform>().sizeDelta.y/5);
        for (int i = 0; i < transform.childCount; i++)
        {
            Image img = transform.GetChild(i).GetComponent<Image>();
            if (img != null) img.enabled = false;
        }
    }
}
