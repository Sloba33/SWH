using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class TutorialDialogue : MonoBehaviour
{
    public GameObject dialoguePanel;
    public List<Dialogue> dialogues = new();
    public Dialogue dialogue;
    public void ToggleDialogue(DialogueType dialogueType)
    {
        foreach (Dialogue dialogue in dialogues)
        {

            dialogue.dialogueObject.SetActive(false);

        }
        foreach (Dialogue dialogue in dialogues)
        {
            if (dialogue.dialogueType == dialogueType)
                dialogue.dialogueObject.SetActive(true);

        }

    }
}
public enum DialogueType
{
    Joystick, Pull, Jump, HitDown, Hit, BombUniversal, BombColored, WellDone, GoodJob, Continue
}
