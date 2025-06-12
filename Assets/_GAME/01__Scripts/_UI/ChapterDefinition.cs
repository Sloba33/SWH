// ChapterDefinition.cs
using System.Collections.Generic;
using UnityEngine;

// This attribute allows you to create new ChapterDefinition assets via Assets > Create > Scene Data > Chapter Definition
[CreateAssetMenu(fileName = "NewChapterDefinition", menuName = "Scene Data/Chapter Definition")]
public class ChapterDefinition : ScriptableObject
{
    public string chapterName;
    public List<Level> levelsInChapter; // A list of Level ScriptableObjects belonging to this chapter
    public Sprite chapterSprite;
}