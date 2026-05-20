using UnityEditor;
using UnityEngine;

namespace CustomCollections.Editor
{
    [CustomPropertyDrawer(typeof(CustomDictionary<,>.DictionaryItem), true)]
    public class CustomDictionaryItemDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 4f;

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty valueProperty =
                property.FindPropertyRelative("Value");

            float valueHeight =
                EditorGUI.GetPropertyHeight(valueProperty, true);

            return valueHeight + VerticalSpacing + EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty keyProperty =
                property.FindPropertyRelative("Key");

            SerializedProperty valueProperty =
                property.FindPropertyRelative("Value");

            Rect keyRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            Rect valueRect = new Rect(
                position.x,
                position.y + EditorGUIUtility.singleLineHeight + VerticalSpacing,
                position.width,
                EditorGUI.GetPropertyHeight(valueProperty, true));

            EditorGUI.BeginProperty(position, label, property);

            GUI.enabled = false;
            EditorGUI.PropertyField(keyRect, keyProperty);
            GUI.enabled = true;

            EditorGUI.PropertyField(valueRect, valueProperty, true);

            EditorGUI.EndProperty();
        }
    }
}