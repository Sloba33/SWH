using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelEvent : MonoBehaviour
{
    public Dialogue dialogue;
    public GameEvent gameEvent;
    public GameEvent gameEvent_Out;
    public GameObject objectToTurnOff;
    public GameObject objectToTurnOn;
    [SerializeField] bool isTempTrigger;
    public GameObject dialogueBox;
    public TutorialDialogue tutorialDialogue;
    private void Start()
    {
        tutorialDialogue = FindObjectOfType<TutorialDialogue>();
    }
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player") && other.GetType() == typeof(CapsuleCollider))
        {
            Debug.Log("Check");
            if (gameEvent != null)
                gameEvent.Raise(this, this.gameObject);
            if (tutorialDialogue != null && dialogue != null)
            {
                tutorialDialogue.ToggleDialogue(dialogue.dialogueType);
            }
            if (objectToTurnOff != null) objectToTurnOff.SetActive(false);
            if (objectToTurnOn != null) objectToTurnOn.SetActive(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.GetType() == typeof(CapsuleCollider) && isTempTrigger)
        {
            Debug.Log("Check");
            gameEvent_Out.Raise(this, this.gameObject);
        }
    }
}
