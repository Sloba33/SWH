using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Scene Data/Level")]
public class Level : ScriptableObject
{
    public int levelNumber;
    public int sceneBuildIndex;
    public string sceneName;
}