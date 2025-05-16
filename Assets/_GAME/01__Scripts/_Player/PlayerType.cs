using UnityEngine;

public class PlayerType : MonoBehaviour
{
    public PlayerVersion playerVersion;
    public static GameObject playerPrefab;

}
public enum PlayerVersion
{
    Standard, Green, Red, Female
}
