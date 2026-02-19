using UnityEngine;

public class FirebasePlayerManager : MonoBehaviour
{
    public string name;
    public int playerLevel;
    public int progressLevel;

    public FirebasePlayerManager(string name, int playerLevel, int progressLevel)
    {
        this.name = name;
        this.playerLevel = playerLevel;
        this.progressLevel = progressLevel;
    }
}
