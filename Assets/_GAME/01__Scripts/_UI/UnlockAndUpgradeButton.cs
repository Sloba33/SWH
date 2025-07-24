using TetraCreations.Attributes.Editor;
using UnityEngine;

public class UnlockAndUpgradeButton : MonoBehaviour
{
    private Button button;
    public UnlockAndUpgradeButtonType buttonType;
}
public enum UnlockAndUpgradeButtonType
{
    Unlock,
    Ad,
    Upgrade,
}