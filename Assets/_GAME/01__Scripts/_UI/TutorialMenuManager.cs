using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialMenuManager : MonoBehaviour
{
    public List<GameObject> FirstMenuTutorialObjects = new();
    public List<GameObject> FirstMenuTutorialObjectsToDeactivate = new();

    public GameObject TrophyHint, RewardHint, BackHint, WorkersHint, UpgradeHint, BackHint2, WorkHint;
    public List<GameObject> UIToDisable = new();
    public int firstTutorialStepCounter = 0;
    public int step = 0;
    private void Start()
    {
        Debug.Log("Finished Tutorial INT : " + PlayerPrefs.GetInt("FinishedTutorial", 0));
        if (PlayerPrefs.GetInt("FirstTimeMenu") == 1 && PlayerPrefs.GetInt("FinishedTutorial", 0) == 0)
        {
            Debug.Log("Disabling shit and enabling first item of MenuTutorialObjects");
            for (int i = 0; i < FirstMenuTutorialObjectsToDeactivate.Count; i++)
            {
                FirstMenuTutorialObjectsToDeactivate[i].SetActive(false);
            }
            FirstMenuTutorialObjects[firstTutorialStepCounter].SetActive(true);
            firstTutorialStepCounter++;
            PlayerPrefs.SetInt("FinishedTutorial", 1);
        }
        else if (PlayerPrefs.GetInt("FirstTimeMenu") == 0 && PlayerPrefs.GetInt("FirstTime") == 0)
        {
            Debug.Log("Tutorial Handler : Enabling WORK HINT and disabling everything else");
            for (int i = 0; i < UIToDisable.Count; i++)
            {
                UIToDisable[i].SetActive(false);
            }
            WorkHint.SetActive(true);
        }
    }
    public void EndMenuTutorial()
    {
        PlayerPrefs.SetInt("FirstTimeMenu", 2);
    }
    public void RewardHint_02()
    {
        if (PlayerPrefs.GetInt("FirstTimeMenu") > 1) return;
        if (RewardHint != null && step == 0)
        {
            step++;
            RewardHint.SetActive(true);
            Destroy(TrophyHint, 0.05f);
        }
    }
    public void BackHint_03()
    {
        if (PlayerPrefs.GetInt("FirstTimeMenu") > 1) return;
        if (BackHint != null && step == 1)
        {
            step++;
            BackHint.SetActive(true);
            Destroy(RewardHint, 0.05f);
        }

    }
    public void WorkersHint_04()
    {
        if (PlayerPrefs.GetInt("FirstTimeMenu") > 1) return;
        if (WorkersHint != null && step == 2)
        {
            step++;
            WorkersHint.SetActive(true);
            Destroy(BackHint, 0.05f);
        }
    }
    public void UpgradeHint_05()
    {
        if (PlayerPrefs.GetInt("FirstTimeMenu") > 1) return;
        if (UpgradeHint != null && step == 3)
        {
            step++;
            UpgradeHint.SetActive(true);
            Destroy(WorkersHint, 0.05f);
        }


    }
    public void BackHint2_06()
    {
        if (PlayerPrefs.GetInt("FirstTimeMenu") > 1) return;
        if (BackHint2 != null && step == 4)
        {
            step++;
            BackHint2.SetActive(true);
            Destroy(UpgradeHint, 0.05f);
        }
    }
    public void WorkHint_07()
    {
        if (PlayerPrefs.GetInt("FirstTimeMenu") > 1) return;
        if (WorkHint != null && step == 5)
        {
            step++;
            WorkHint.SetActive(true);

            Destroy(BackHint2, 0.05f);
            
        }
        EndMenuTutorial();
    }
}
