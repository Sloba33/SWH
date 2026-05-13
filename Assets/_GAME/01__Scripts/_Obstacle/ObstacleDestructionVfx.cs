using System;
using Coherence.Toolkit;
using UnityEngine;

public class ObstacleDestructionVfx : MonoBehaviour
{
    private ParticleSystem particles;
    [NonSerialized, Sync]
    public ObstacleAudioType ObstacleAudioType;
    // public float lifetime = 3f;

    private void Awake()
    {
        particles = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        if (particles != null)
            particles.Play();

        AudioManager.Instance.PlayObstacleSound_Destruction(ObstacleAudioType, transform.position);

        // Destroy(gameObject, lifetime);
    }
}