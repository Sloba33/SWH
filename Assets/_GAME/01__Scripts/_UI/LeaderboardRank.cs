

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
public class LeaderboardRank : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI trophiesText;
    public TextMeshProUGUI rankText;


    public void Clear()
    {
        nameText.text = "-";
        trophiesText.text = "-";
        rankText.text = "-";
    }
    public void Set(string playerName, int trophies, int rank)
    {
        nameText.text = playerName;
        trophiesText.text = trophies.ToString();
        rankText.text = rank.ToString() + ".";
    }
    public void SetAsExtra()
    {
        nameText.text = "";
        trophiesText.text = "";
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
