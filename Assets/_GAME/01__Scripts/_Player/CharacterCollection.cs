using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Characters", menuName = "Characters/CharacterCollection", order = 1)]
public class CharacterCollection : ScriptableObject
{
    public List<GameObject> Characters = new();
    public List<CharacterStats> TokenCharacters = new();
}
