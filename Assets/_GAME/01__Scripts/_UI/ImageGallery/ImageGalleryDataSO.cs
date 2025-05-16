using System.Collections;
using System.Collections.Generic;

using UnityEngine;
[CreateAssetMenu(fileName = "ImageGalleryData", menuName = "ImageGallery/ImageGalleryData", order = 1)]
public class ImageGalleryDataSO : ScriptableObject
{
    public List<RewardData> rewards = new();

    public List<SpriteAssignment> spriteAssignments = new();

    private Dictionary<RewardType, Sprite> backgroundSprites = new Dictionary<RewardType, Sprite>(); private Dictionary<RewardType, Sprite> rewardSprites = new Dictionary<RewardType, Sprite>();
    private void OnEnable()
    {
        BuildSpriteDictionary();
        BuildBackgroundDictionary();

        foreach (RewardData reward in rewards)
        {
            if (rewardSprites.ContainsKey(reward.rewardType))
            {
                reward.rewardSprite = rewardSprites[reward.rewardType];
            }
            if (backgroundSprites.ContainsKey(reward.rewardType))
            {
                reward.backgroundSprite = backgroundSprites[reward.rewardType];
            }
        }
    }

    private void BuildSpriteDictionary()
    {
        rewardSprites.Clear();
        foreach (SpriteAssignment assignment in spriteAssignments)
        {
            if (!rewardSprites.ContainsKey(assignment.rewardType))
            {
                rewardSprites[assignment.rewardType] = assignment.rewardSprite;

            }
            else
            {
                Debug.LogWarning($"Duplicate RewardType {assignment.rewardType} in Sprite Assignments.");
            }
        }
    }
    private void BuildBackgroundDictionary()
    {
        backgroundSprites.Clear();
        foreach (SpriteAssignment assignment in spriteAssignments)
        {
            if (!backgroundSprites.ContainsKey(assignment.rewardType))
            {
                backgroundSprites[assignment.rewardType] = assignment.backgroundSprite;
            }
            else
            {
                Debug.LogWarning($"Duplicate RewardType {assignment.rewardType} in Sprite Assignments.");
            }
        }
    }
    [System.Serializable]
    public class RewardData
    {
        public int amount;
        public string description;
        public RewardType rewardType;
        public Sprite rewardSprite;
        public Sprite backgroundSprite;
    }
    public enum RewardType
    {
        Coins, Cash, Gems, XP, Token
    }
    [System.Serializable]
    public class SpriteAssignment
    {
        public RewardType rewardType;
        public Sprite rewardSprite;
        public Sprite backgroundSprite; // Added background sprite
    }
}
