using UnityEngine.UI;
using UnityEngine;

public class Chapter : MonoBehaviour
{
    public GameObject levelsParent;
    public GameObject levelsPanel;
    public GameObject chaptersPanel;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
              {
                  OpenPanel();
                  CharacterManager.Instance.PlayClick();
              });
    }
    public void OpenPanel()
    {
        levelsPanel.SetActive(true);
        levelsParent.SetActive(true);
        chaptersPanel.SetActive(false);
    }
}
