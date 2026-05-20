

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
        GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, GetComponent<RectTransform>().sizeDelta.y/5);
        for (int i = 0; i < transform.childCount; i++)
        {
            Image img = transform.GetChild(i).GetComponent<Image>();
            if (img != null) img.enabled = false;
        }
    }

    // Renders this row invisible while keeping its full size in the layout. Used for the local
    // player's own row: it reserves the slot in the list, and the floating personal-rank element
    // is drawn in its place (see Leaderboard.LateUpdate).
    public void SetHidden(bool hidden)
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = hidden ? 0f : 1f;
    }
}
