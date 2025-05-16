using UnityEngine;
using UnityEditor;
using Obstacles;
public enum AudioCategory { Obstacle, Player, UI }
public enum ObstacleActionType { Destruction, Move }

[CreateAssetMenu(fileName = "New Audio Clip", menuName = "Audio/Audio Clip")]
public class AudioClipSO : ScriptableObject
{
    public AudioClip clip;
    [Range(0, 1)] public float volume = 1f;
    public AudioCategory category;

    [ConditionalField("category", AudioCategory.Obstacle)]
    public ObstacleAudioType type;
    [ConditionalField("category", AudioCategory.Obstacle)]
    public ObstacleActionType actionType;

    [ConditionalField("category", AudioCategory.Player, AudioCategory.UI)]
    public string identifier; // For player and UI sounds
}

// Custom attribute to show/hide fields based on conditions
public class ConditionalFieldAttribute : PropertyAttribute
{
    public string conditionalFieldName;
    public object[] conditionalValues;

    public ConditionalFieldAttribute(string conditionalFieldName, params object[] conditionalValues)
    {
        this.conditionalFieldName = conditionalFieldName;
        this.conditionalValues = conditionalValues;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
public class ConditionalFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalFieldAttribute condAttr = attribute as ConditionalFieldAttribute;
        SerializedProperty conditionProperty = property.serializedObject.FindProperty(condAttr.conditionalFieldName);

        if (conditionProperty != null)
        {
            bool showField = false;
            foreach (object value in condAttr.conditionalValues)
            {
                if (conditionProperty.propertyType == SerializedPropertyType.Enum)
                {
                    showField |= conditionProperty.enumValueIndex == (int)value;
                }
                // Add more type checks if needed
            }

            if (showField)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else if (conditionProperty.propertyType == SerializedPropertyType.Enum &&
                     conditionProperty.enumValueIndex == (int)AudioCategory.Obstacle &&
                     property.name == "type")
            {
                // Show the field but disable it
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.EndDisabledGroup();
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalFieldAttribute condAttr = attribute as ConditionalFieldAttribute;
        SerializedProperty conditionProperty = property.serializedObject.FindProperty(condAttr.conditionalFieldName);

        if (conditionProperty != null)
        {
            bool showField = false;
            foreach (object value in condAttr.conditionalValues)
            {
                if (conditionProperty.propertyType == SerializedPropertyType.Enum)
                {
                    showField |= conditionProperty.enumValueIndex == (int)value;
                }
                // Add more type checks if needed
            }

            if (!showField)
            {
                return 0f;
            }
        }

        return base.GetPropertyHeight(property, label);
    }
}
#endif