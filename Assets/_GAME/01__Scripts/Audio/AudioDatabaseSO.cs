using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Audio Database", menuName = "Audio/Audio Database")]
public class AudioDatabaseSO : ScriptableObject
{
    public List<AudioClipSO> audioClips = new();
}