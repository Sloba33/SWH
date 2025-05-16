

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Obstacles;
public class GoalSlot : MonoBehaviour
{
    public ObstacleType obstacleType;
    public Sprite sprite;
    public int amount;
    public TextMeshProUGUI Text;
    public Image img;
    public int count;

    private void Start()
    {
        SetGoal();
    }
    public void SetGoal()
    {
        img.sprite = sprite;
        Text.text = "0/" + amount;
    }
    public void IncreaseCount()
    {
        count++;
        Text.text = count + "/" + amount;
    }
}
