
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DigitalRuby.SoundManagerNamespace;
using System.Collections;
using DanielLochner.Assets.SimpleScrollSnap;
using Obstacles;


public class AudioManager : GloballyAccessibleBase<AudioManager>
{
    public AudioDatabaseSO audioDatabase;
    public int poolSize = 20;
    private Dictionary<ObstacleAudioType, AudioClipSO> obstacleDestruction_AudioClips;
    private Dictionary<ObstacleAudioType, AudioClipSO> obstacleMove_AudioClips;

    private Dictionary<string, AudioClipSO> player_AudioClips;
    private Dictionary<string, AudioClipSO> ui_AudioClips;
    private Queue<AudioSource> audioSourcePool;

    public AudioSource[] SoundAudioSources;
    public AudioSource[] MusicAudioSources;
    private float bgmVolume = 0.5f;
    private float sfxVolume = 1f;

    // Properties to get and set volumes
    public float BGMVolume
    {
        get { return bgmVolume; }
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            UpdateBGMVolume();
        }
    }

    public float SFXVolume
    {
        get { return sfxVolume; }
        set
        {
            sfxVolume = Mathf.Clamp01(value);
        }
    }
    private void Start()
    {
        InitializeAudioClips();
        InitializeAudioSourcePool();
        InitializeVolume();
    }
    private float defaultSourceVolume = 0.4f;
    private void UpdateBGMVolume()
    {
        foreach (AudioSource source in MusicAudioSources)
        {
            source.volume = defaultSourceVolume * bgmVolume;
        }
    }

    // New method to update SFX volume
    public void SetBGMVolume(float volume)
    {
        BGMVolume = volume;
        PlayerPrefs.SetFloat("BGM_Volume", volume);
    }

    // New method to set SFX volume (can be called from UI)
    public void SetSFXVolume(float volume)
    {
        SFXVolume = volume;
        PlayerPrefs.SetFloat("SFX_Volume", volume);
    }
    private void InitializeVolume()
    {
        SetBGMVolume(PlayerPrefs.GetFloat("BGM_Volume", 0.5f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFX_Volume", 1f));
    }
    private void InitializeAudioClips()
    {
        obstacleDestruction_AudioClips = new Dictionary<ObstacleAudioType, AudioClipSO>();
        obstacleMove_AudioClips = new Dictionary<ObstacleAudioType, AudioClipSO>();
        player_AudioClips = new Dictionary<string, AudioClipSO>();
        ui_AudioClips = new Dictionary<string, AudioClipSO>();

        foreach (var clip in audioDatabase.audioClips)
        {
            switch (clip.category)
            {
                case AudioCategory.Obstacle:
                    if (clip.actionType == ObstacleActionType.Destruction)
                    {
                        obstacleDestruction_AudioClips[clip.type] = clip;
                    }
                    else if (clip.actionType == ObstacleActionType.Move)
                    {
                        obstacleMove_AudioClips[clip.type] = clip;
                    }
                    break;
                case AudioCategory.Player:
                    player_AudioClips[clip.identifier] = clip;
                    break;
                case AudioCategory.UI:
                    ui_AudioClips[clip.identifier] = clip;
                    break;
            }
        }
    }
    private void InitializeAudioSourcePool()
    {
        audioSourcePool = new Queue<AudioSource>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject audioSourceObj = new GameObject($"PooledAudioSource_{i}");
            audioSourceObj.transform.SetParent(transform);
            AudioSource audioSource = audioSourceObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSourcePool.Enqueue(audioSource);
        }
    }

    public void PlayObstacleSound_Destruction(ObstacleAudioType type, Vector3 position)
    {
        if (!obstacleDestruction_AudioClips.TryGetValue(type, out AudioClipSO clip))
        {
            Debug.LogWarning($"No audio clip found for obstacle destruction type {type}.");
            return;
        }
        // PlaySound3D(clip, position);
        PlaySound2D(clip);
        Debug.Log($"Playing: {clip.name}");
    }

    private AudioSource currentObstacleMoveSource;
    private AudioSource currentFootstepSource;
    private Coroutine obstacleMoveSoundCoroutine;
    private Coroutine footstepSoundCoroutine;
    // intended for looping sounds of obstacles - e.g when the player pushes or pulls an obstacle i'd be looping a sound during the pushing/pulling duration
    public void PlayObstacleSound_Move(ObstacleAudioType type, Vector3 position)
    {
        if (isPlayingObstacleMove)
        {
            // Update the position of the existing footstep sound
            if (currentObstacleMoveSource != null)
            {
                currentObstacleMoveSource.transform.position = position;
            }
            return;
        }
        if (!obstacleMove_AudioClips.TryGetValue(type, out AudioClipSO clip))
        {
            Debug.LogWarning($"No audio clip found for obstacle move type {type}.");
            return;
        }

        StopObstacleSound_Move(); // Stop any currently playing obstacle move sound

        currentObstacleMoveSource = GetAudioSource();
        currentObstacleMoveSource.clip = clip.clip;
        currentObstacleMoveSource.volume = clip.volume * sfxVolume;
        currentObstacleMoveSource.loop = true;
        currentObstacleMoveSource.spatialBlend = 1f;
        currentObstacleMoveSource.transform.position = position;
        isPlayingObstacleMove = true;
        obstacleMoveSoundCoroutine = StartCoroutine(PlayObstacleMoveSound(0.5f)); // 0.5f is the delay between loops, adjust as needed
        Debug.Log("Playing :" + clip.name);
    }
    public void StopObstacleSound_Move()
    {
        if (obstacleMoveSoundCoroutine != null)
        {
            StopCoroutine(obstacleMoveSoundCoroutine);
            obstacleMoveSoundCoroutine = null;
        }

        if (currentObstacleMoveSource != null)
        {
            currentObstacleMoveSource.Stop();
            StartCoroutine(FadeOutAndStop(currentObstacleMoveSource));
            currentObstacleMoveSource = null;
        }
        isPlayingObstacleMove = false;
    }
    private IEnumerator FadeOutAndStop(AudioSource source, float fadeDuration = 0.1f)
    {
        float startVolume = source.volume;

        while (source.volume > 0)
        {
            source.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        source.Stop();
        source.volume = startVolume; // Reset volume for future reuse
        ReturnAudioSourceToPool(source);
    }
    private bool isPlayingFootsteps = false;
    public void StartPlayerFootsteps(Vector3 position)
    {
        if (isPlayingFootsteps)
        {
            // Update the position of the existing footstep sound
            if (currentFootstepSource != null)
            {
                currentFootstepSource.transform.position = position;
            }
            return;
        }

        if (!player_AudioClips.TryGetValue("Footsteps", out AudioClipSO footstepClip))
        {
            Debug.LogWarning("No footstep audio clip found.");
            return;
        }

        currentFootstepSource = GetAudioSource();
        currentFootstepSource.clip = footstepClip.clip;
        currentFootstepSource.volume = footstepClip.volume * sfxVolume;
        currentFootstepSource.loop = false; // Changed to false, we'll handle looping manually
        currentFootstepSource.spatialBlend = 1f;
        currentFootstepSource.transform.position = position;

        isPlayingFootsteps = true;
        footstepSoundCoroutine = StartCoroutine(PlayFootstepSound(0.39f));
        // Debug.Log("Started playing: " + footstepClip.name);
    }

    public void StopPlayerFootsteps()
    {
        if (footstepSoundCoroutine != null)
        {
            StopCoroutine(footstepSoundCoroutine);
            footstepSoundCoroutine = null;
        }

        if (currentFootstepSource != null)
        {
            currentFootstepSource.Stop();
            StartCoroutine(FadeOutAndStop(currentFootstepSource));
            currentFootstepSource = null;
        }

        isPlayingFootsteps = false;
    }
    private bool isPlayingObstacleMove = false;
    private IEnumerator PlayObstacleMoveSound(float delay)
    {
        while (isPlayingObstacleMove)
        {
            currentObstacleMoveSource.Play();
            yield return new WaitForSeconds(currentObstacleMoveSource.clip.length + delay); // 0.3f is the delay between footsteps, adjust as needed
        }
    }

    private IEnumerator PlayFootstepSound(float delay)
    {
        while (isPlayingFootsteps)
        {
            currentFootstepSource.Play();
            yield return new WaitForSeconds(currentFootstepSource.clip.length + delay); // 0.3f is the delay between footsteps, adjust as needed
        }
    }

    public void PlayPlayerSound(string identifier, Vector3 position)
    {
        if (!player_AudioClips.TryGetValue(identifier, out AudioClipSO clip))
        {
            Debug.LogWarning($"No audio clip found for player sound {identifier}.");
            return;
        }
        // Debug.Log("playing : " + identifier);
        PlaySound3D(clip, position);
    }
    public void PlayJumpSound(bool female, Vector3 position)
    {
        string baseIdentifier = female ? "jump_f" : "jump_m";
        int variation = Random.Range(1, 3); // Randomly picks 1 or 2
        string identifier = $"{baseIdentifier}{variation}";

        if (!player_AudioClips.TryGetValue(identifier, out AudioClipSO clip))
        {
            Debug.LogWarning($"No audio clip found for jump sound {identifier}");
            return;
        }
        // Debug.Log("playing : " + identifier);
        PlaySound3D(clip, position);
    }
    public void PlayUISound(string identifier)
    {
        if (!ui_AudioClips.TryGetValue(identifier, out AudioClipSO clip))
        {
            Debug.LogWarning($"No audio clip found for UI sound {identifier}.");
            return;
        }
        // Debug.Log("playing : " + identifier);
        PlaySound2D(clip);
    }
    public IEnumerator PlayUISound(string identifier, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!ui_AudioClips.TryGetValue(identifier, out AudioClipSO clip))
        {
            Debug.LogWarning($"No audio clip found for UI sound {identifier}.");
            yield return null;
        }
        // Debug.Log("playing : " + identifier);
        PlaySound2D(clip);
    }

    private void PlaySound2D(AudioClipSO audioClipSO)
    {
        AudioSource audioSource = GetAudioSource();
        audioSource.clip = audioClipSO.clip;
        audioSource.volume = audioClipSO.volume * sfxVolume;
        audioSource.spatialBlend = 0f;  // Pure 2D sound
        audioSource.Play();

        StartCoroutine(ReturnToPoolWhenFinished(audioSource));
    }
    private void PlaySound3D(AudioClipSO audioClipSO, Vector3 position)
    {
        AudioSource audioSource = GetAudioSource();
        audioSource.clip = audioClipSO.clip;
        audioSource.volume = audioClipSO.volume * sfxVolume;
        audioSource.pitch = 1.0f + Random.Range(-0.05f, 0.05f); // Add pitch variation
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 25f;
        audioSource.transform.position = position;
        audioSource.Play();

        StartCoroutine(ReturnToPoolWhenFinished(audioSource));
    }
    private AudioSource GetAudioSource()
    {
        if (audioSourcePool.Count > 0)
            return audioSourcePool.Dequeue();

        // If pool is empty, create a new AudioSource
        GameObject audioSourceObj = new GameObject("DynamicAudioSource");
        audioSourceObj.transform.SetParent(transform);
        return audioSourceObj.AddComponent<AudioSource>();
    }

    private System.Collections.IEnumerator ReturnToPoolWhenFinished(AudioSource audioSource)
    {
        yield return new WaitForSeconds(audioSource.clip.length);
        audioSource.Stop();
        audioSource.pitch = 1.0f; // Reset pitch
        audioSourcePool.Enqueue(audioSource);
    }
    private void ReturnAudioSourceToPool(AudioSource audioSource)
    {
        audioSource.Stop();
        audioSourcePool.Enqueue(audioSource);
    }

    
    private AudioSource loopedAudioSource, loopedObstacleAudioSource;
    public void PlaySound(int index)
    {
        // int count;
        // if (!int.TryParse(SoundCountTextBox.text, out count))
        // {
        //     count = 1;
        // }
        // while (count-- > 0)
        // {
        //     
        // }
        if (!SoundAudioSources[index].isPlaying)
        {
            SoundAudioSources[index].PlayOneShotSoundManaged(SoundAudioSources[index].clip);
        }
        else
        {
            Debug.Log("Already playing");
            return;
        }
    }
    public void PlaySoundObstacleDestroy(ObstacleAudioType obstacleAudioType)
    {
        if (obstacleAudioType == ObstacleAudioType.Wood)
        {
            // if (!SoundAudioSources[0].isPlaying)
            // {
            SoundAudioSources[0].PlayOneShotSoundManaged(SoundAudioSources[0].clip);
            // }
            // else return;
        }
        else if (obstacleAudioType == ObstacleAudioType.Concrete)
        {
            // if (!SoundAudioSources[1].isPlaying)
            // {
            SoundAudioSources[1].PlayOneShotSoundManaged(SoundAudioSources[1].clip);
            // }
            // else return;
        }
        else Debug.Log("No sound to play");
    }
    public void PlaySoundObstacleMove(ObstacleAudioType obstacleAudioType)
    {
        if (obstacleAudioType == ObstacleAudioType.Wood)
        {
            if (!SoundAudioSources[3].isPlaying)
            {
                SoundAudioSources[3].PlayOneShotSoundManaged(SoundAudioSources[3].clip);
            }
            else return;
        }
        else if (obstacleAudioType == ObstacleAudioType.Concrete)
        {
            if (!SoundAudioSources[2].isPlaying)
            {
                SoundAudioSources[2].PlayOneShotSoundManaged(SoundAudioSources[2].clip);
            }
            else return;
        }
        else Debug.Log("No sound to play");
    }
    public void PlaySoundWithClipping(int index)
    {
        SoundAudioSources[index].PlayOneShotSoundManaged(SoundAudioSources[index].clip);
    }
    public void StopSound(int index)
    {
        SoundManager.StopLoopingSound(SoundAudioSources[index]);
    }

    private void PlayMusic(int index)
    {
        MusicAudioSources[index].PlayLoopingMusicManaged(1.0f, 1.0f, true);
    }
    public void PersistToggleChanged(bool isOn)
    {
        SoundManager.StopSoundsOnLevelLoad = !isOn;
    }

}


