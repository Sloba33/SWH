using UnityEngine;
using DG.Tweening;
public class BonusMatchXP : MonoBehaviour
{
    private LevelGoal levelGoal;
    private Player player;
    void Start()
    {
        levelGoal = FindFirstObjectByType<LevelGoal>();
        if (levelGoal != null)
        {
            levelGoal.bonusXpMultiplier += 1;
        }
    }
    void OnEnable()
    {
        player= FindFirstObjectByType<Player>();

        transform.position= player.transform.position + new Vector3(0, 0.5f, 0);
        transform.DOScale(1.5f, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            transform.DOScale(0, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
            {
                Destroy(gameObject);
            }).Play();
        }).Play();
    }
}
