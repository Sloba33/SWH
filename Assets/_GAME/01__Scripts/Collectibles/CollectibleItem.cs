using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CollectibleItem : MonoBehaviour
{
    [SerializeField] public Sprite collectibleSprite;
    public AudioClip collectSound;
    public bool isConsumable;


    public abstract void Collect(PlayerController player);

    protected void PlayCollectSound(GameObject objectToKill)
    {
        if (collectSound != null)
        {   
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
            Destroy(objectToKill, collectSound.length);
        }
    }


}
