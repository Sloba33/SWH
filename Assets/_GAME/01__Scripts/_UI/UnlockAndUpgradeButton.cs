using System;
using UnityEngine;
using UnityEngine.UI;
public class UnlockAndUpgradeButton : MonoBehaviour
{
    private Button button;
    public UnlockAndUpgradeButtonType buttonType;
    public void SetButtonListener(UnlockAndUpgradeButtonType type, Action action)
    {
        button.onClick.AddListener(() =>
            {
                action();
            });
    }
    public void ClearButton()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}
public enum UnlockAndUpgradeButtonType
{
    Unlock,
    Ad,
    Upgrade,
}