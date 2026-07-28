using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TrophyRoadMilestone))]
public class TrophyRoadMilestoneDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get properties
        SerializedProperty trophyReq = property.FindPropertyRelative("trophyRequirement");
        SerializedProperty reward = property.FindPropertyRelative("reward");
        
        // Calculate rects with proper Y positioning
        Rect foldoutRect = new Rect(position.x, position.y, 20, EditorGUIUtility.singleLineHeight);
        Rect labelRect = new Rect(position.x + 20, position.y, 120, EditorGUIUtility.singleLineHeight);
        Rect trophyRect = new Rect(position.x + 140, position.y, 60, EditorGUIUtility.singleLineHeight);
        Rect rewardRect = new Rect(position.x + 210, position.y, position.width - 220, EditorGUIUtility.singleLineHeight);
        
        // Draw the foldout arrow
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none);
        
        // --- COLOR BY REWARD TYPE FOR COLLAPSED VIEW ---
        SerializedProperty rewardType = reward.FindPropertyRelative("rewardType");
        TrophyRewardType currentRewardType = (TrophyRewardType)rewardType.enumValueIndex;
        
        Color originalColor = GUI.color;
        Color originalContentColor = GUI.contentColor;
        
        // Set color based on reward type (for the collapsed view)
        if (currentRewardType.ToString().Contains("Money"))
        {
            GUI.color = Color.green;
            GUI.contentColor = Color.green;
        }
        else if (currentRewardType.ToString().Contains("Coins") || currentRewardType.ToString().Contains("Coin"))
        {
            GUI.color = Color.yellow;
            GUI.contentColor = Color.yellow;
        }
        else if (currentRewardType.ToString().Contains("Gems") || currentRewardType.ToString().Contains("Gem"))
        {
            GUI.color = new Color(0.7f, 0.3f, 0.9f); // Lighter purple
            GUI.contentColor = new Color(0.7f, 0.3f, 0.9f);
        }
        else
        {
            // Default color (white) for non-currency rewards
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
        }
        
        // Draw label with reward type color
        EditorGUI.LabelField(labelRect, "Trophies:");
        
        // Draw the trophy requirement field (uncolored by value now)
        trophyReq.intValue = EditorGUI.IntField(trophyRect, GUIContent.none, trophyReq.intValue);
        
        // Reset color
        GUI.color = originalColor;
        GUI.contentColor = originalContentColor;
        
        // --- COLOR THE REWARD TYPE DROPDOWN (same color logic) ---
        if (currentRewardType.ToString().Contains("Money"))
        {
            GUI.color = Color.green;
            GUI.contentColor = Color.green;
        }
        else if (currentRewardType.ToString().Contains("Coins") || currentRewardType.ToString().Contains("Coin"))
        {
            GUI.color = Color.yellow;
            GUI.contentColor = Color.yellow;
        }
        else if (currentRewardType.ToString().Contains("Gems") || currentRewardType.ToString().Contains("Gem"))
        {
            GUI.color = new Color(0.7f, 0.3f, 0.9f); // Lighter purple
            GUI.contentColor = new Color(0.7f, 0.3f, 0.9f);
        }
        else
        {
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
        }
        
        // Draw reward type (dropdown) with color
        EditorGUI.PropertyField(rewardRect, reward, GUIContent.none);
        
        // Reset color
        GUI.color = originalColor;
        GUI.contentColor = originalContentColor;
        
        // If expanded, draw the child properties with proper Y positioning
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            // Get reward sub-properties
            SerializedProperty amount = reward.FindPropertyRelative("amount");
            SerializedProperty description = reward.FindPropertyRelative("description");
            
            // Calculate Y positions for each property
            float yOffset = position.y + EditorGUIUtility.singleLineHeight + 2;
            
            // --- COLOR THE REWARD TYPE IN THE EXPANDED VIEW ---
            Rect rewardTypeRect = new Rect(position.x + 20, yOffset, position.width - 20, EditorGUIUtility.singleLineHeight);
            
            if (currentRewardType.ToString().Contains("Money"))
            {
                GUI.color = Color.green;
                GUI.contentColor = Color.green;
            }
            else if (currentRewardType.ToString().Contains("Coins") || currentRewardType.ToString().Contains("Coin"))
            {
                GUI.color = Color.yellow;
                GUI.contentColor = Color.yellow;
            }
            else if (currentRewardType.ToString().Contains("Gems") || currentRewardType.ToString().Contains("Gem"))
            {
                GUI.color = new Color(0.7f, 0.3f, 0.9f); // Lighter purple
                GUI.contentColor = new Color(0.7f, 0.3f, 0.9f);
            }
            else
            {
                GUI.color = Color.white;
                GUI.contentColor = Color.white;
            }
            
            // Draw reward type with color
            EditorGUI.PropertyField(rewardTypeRect, rewardType);
            
            // Reset color
            GUI.color = originalColor;
            GUI.contentColor = originalContentColor;
            
            // Draw amount (no color)
            Rect amountRect = new Rect(position.x + 20, yOffset + EditorGUIUtility.singleLineHeight + 2, position.width - 20, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(amountRect, amount);
            
            // Draw description (no color)
            Rect descRect = new Rect(position.x + 20, yOffset + (EditorGUIUtility.singleLineHeight + 2) * 2, position.width - 20, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(descRect, description);
            
            EditorGUI.indentLevel--;
        }
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
        {
            // Height for: main line + 3 child properties + spacing
            return EditorGUIUtility.singleLineHeight * 4 + 6;
        }
        else
        {
            // Height for just the main line
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif