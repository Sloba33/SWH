using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CollectibleItem : MonoBehaviour
{
    [SerializeField] public Sprite collectibleSprite;
    public AudioClip collectSound;
    public bool isConsumable;
    public bool isCollected = false;

    /// <summary>
    /// Raised when a collectible is actually consumed (all of Collect's guards
    /// passed). The state-replay recorder captures it so the bot-side counterpart
    /// vanishes at the recorded moment. Subclasses call <see cref="RaiseConsumed"/>
    /// at their point of successful consumption.
    /// </summary>
    public static event System.Action<CollectibleItem> CollectibleConsumed;

    protected void RaiseConsumed() => CollectibleConsumed?.Invoke(this);

    /// <summary>
    /// Replay-driven collection: presentation only — hide the collectible, play
    /// the sound, and show the on-ghost visuals (buff particles, helmet repair)
    /// where the subclass has them. No gameplay effects ever run: the ghost's
    /// movement and the level's reactions are baked into the replay's tracks and
    /// events, and a ghost must never touch stats or save state.
    /// </summary>
    /// <param name="ghostPlayer">The replayed bot character, for on-ghost visuals. May be null.</param>
    public virtual void ReplayCollect(Player ghostPlayer)
    {
        if (isCollected) return;
        isCollected = true;
        gameObject.SetActive(false);
    }

    public abstract void Collect(PlayerController player);

    protected void PlayCollectSound(GameObject objectToKill)
    {
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
            Destroy(objectToKill, collectSound.length);
        }
    }

    protected void PlayCollectSound(GameObject objectToKill, float time)
    {
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
            Destroy(objectToKill, time);
        }
    }
}
