using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Scene Data/Level")]
public class Level : ScriptableObject
{
    public int levelNumber; 
    

    
    public int sceneBuildIndex; 
    
    [HideInInspector]
    public string sceneName;

    private void OnEnable()
    {
        sceneBuildIndex = levelNumber + 2;
        Debug.Log("Scene Build Index set to: " + sceneBuildIndex + " for level number: " + levelNumber);

    }
}