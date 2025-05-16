using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public class Quest : MonoBehaviour
{
    public QuestData questData;
    public Image questIcon;
    public TextMeshProUGUI fillText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI titleText;

    public Image fillBar;
    // public Quest()
    // {
    //     Debug.Log("Constructoring");
    //     questIcon.sprite = questData.questIcon;
    //     float progress = questData.currentAmount;
    //     fillText.text = "" + progress + "/" + questData.requiredAmount;
    //     fillBar.fillAmount = progress / questData.requiredAmount;
    //     xpText.text = questData.xpReward.ToString();
    //     titleText.text = questData.questName;
    // }
    private void Start()
    {
        Debug.Log("Starting");
        // Debug.Log("Image name :" + questData.questIcon.name);

        // questIcon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(questData.questIconPath);
        float progress = questData.currentAmount;
        fillText.text = "" + progress + "/" + questData.requiredAmount;
        fillBar.fillAmount = progress / questData.requiredAmount;
        xpText.text = questData.xpReward.ToString();
        titleText.text = questData.questName;
    }
}
