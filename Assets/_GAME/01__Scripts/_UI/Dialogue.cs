using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class Dialogue : MonoBehaviour
{
    public GameObject dialogueObject;
    public DialogueType dialogueType;
    public Transform characterImage;
    private void Start()
    {
        if (characterImage == null || dialogueObject == null) return;
        characterImage.gameObject.SetActive(true);
        // characterImage.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        // characterImage.DOScale(new Vector3(1, 1, 1), 0.3f).Play();

        dialogueObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        dialogueObject.transform.DOScale(new Vector3(1, 1, 1), 0.4f).Play();

    }
}
