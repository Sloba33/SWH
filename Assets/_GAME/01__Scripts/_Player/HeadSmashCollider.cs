using UnityEngine;

public class HeadSmashCollider : MonoBehaviour
{
    public PlayerController playerController;
    private Player player;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInParent<Animator>();
        player = GetComponentInParent<Player>();
        playerController = GetComponentInParent<PlayerController>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.transform.CompareTag("Obstacle"))
        {
            Debug.Log("Current helemt durabilaty : " + player.helmet.helmetDurability);
            if (playerController._isJumping && player.helmet.helmetDurability > 0)
            {
                Debug.Log("Helmet is present, destroying obstacle");
                Obstacle obs = other.transform.GetComponent<Obstacle>();
                if (obs != null && obs.isFalling)
                {
                    if (player.helmet != null || player.helmet.gameObject.activeSelf)
                    {
                        player.helmet.DamageHelmet(1);
                        GameObject originalObject = obs.transform.gameObject;
                        // QuestRotator.Instance.UpdateQuestProgress(QuestType.Headbutt);

                        originalObject.GetComponent<Obstacle>().ParticleDestroy();
                    }
                    else
                    {
                        player.Die(other.transform);
                    }
                }
            }
            else
            {
                Debug.Log("Helmet is not present, killing player");
                player.Die(other.transform);
            }
            // else if (!player.helmet.gameObject.activeSelf && player.helmet.helmetDurability <= 0)
            // {
            //     Debug.Log("Helmet destroyed");
            //     player.Die(other.transform);
            // }
        }

    }
}
