using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NameSelector : MonoBehaviour
{
    public TMP_InputField inputField;
    public TextMeshProUGUI playerName;
    public const string errorLength = "Name must contain at least 3 characters!";
    public const string errorInvalidCharacter = "Name can only contain letters and numbers";
    public TextMeshProUGUI errorField;
    public void SetName()
    {
        string PlayersName = inputField.text;
        if (!PlayersName.All(Char.IsLetterOrDigit))
        {
            errorField.text = errorInvalidCharacter;
        }
        else if (PlayersName.Length < 3)
        {
            errorField.text = errorLength;
        }
        else
        {
            PlayerPrefs.SetString("playerName", PlayersName);
            SceneLoader sceneLoader = FindObjectOfType<SceneLoader>();
            if (sceneLoader == null)
            {
                Debug.Log("No scene loader"); return;
            }

            else
            {
                StartCoroutine(sceneLoader.LoadLevelWithIndex(1));
                Debug.Log("Loading");

            }

        }
    }
}
