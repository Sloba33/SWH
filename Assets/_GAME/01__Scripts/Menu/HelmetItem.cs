using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.UI;
public class HelmetItem : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject helmetObject;
    public HelmetItemManager helmetItemManager;
    public HelmetType helmetType;
    public Helmet helmet;
    public Button button;
    public int id;
    public GameObject lockImage;
    public GameObject notificationImage;
    public Image helmetImage;
    public Color lockedColor, unlockedColor;
    public UIShadow uiShadow;
    public int helmetPrice;
    public bool unlocked;
    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
        id = transform.GetSiblingIndex();
        uiShadow = GetComponent<UIShadow>();
        unlocked = PlayerPrefs.GetInt(helmetType.ToString()) == 1;
        Debug.Log("Helmet name : " + helmetType.ToString() + " unlocked " + unlocked);
        Debug.Log("ID :" + helmet.name);
        button.onClick.AddListener(() =>
               {
                   helmetItemManager.SelectHelmet(this);
               });
        if (unlocked)
        {
            if (PlayerPrefs.GetInt(helmetType.ToString() + "_clicked") == 0)
            {
                if (id != 0)
                {

                    Debug.Log("Item clicked? :" + PlayerPrefs.GetInt(helmetType + "_clicked"));
                    notificationImage.SetActive(true);
                    Debug.Log("Enabling notification iamge for helmet " + this.name);
                }

            }
            else notificationImage.SetActive(false);
            LockHelmet(false);

        }
        else
        {
            LockHelmet(true);
        }
        if(id==0) notificationImage.SetActive(false);

    }
    public void LockHelmet(bool flag)
    {
        if (lockImage != null)
            lockImage.SetActive(flag);
        helmetImage.color = flag ? lockedColor : unlockedColor;
        if (PlayerPrefs.GetInt(helmetType.ToString() + "_clicked") == 0 && unlocked)
        {
            notificationImage.SetActive(true);
        }
        else notificationImage.SetActive(false);
    }

}
