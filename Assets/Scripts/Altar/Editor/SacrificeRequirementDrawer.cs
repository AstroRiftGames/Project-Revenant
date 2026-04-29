#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Altar.Data;

namespace Altar.EditorScripts
{
    [CustomPropertyDrawer(typeof(SacrificeRequirement))]
    public class SacrificeRequirementDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float padding = EditorGUIUtility.standardVerticalSpacing;
            float height = EditorGUIUtility.singleLineHeight + padding; // Foldout

            SerializedProperty requiresSpecificUnit = property.FindPropertyRelative("requiresSpecificUnit");
            height += EditorGUI.GetPropertyHeight(requiresSpecificUnit, true) + padding;

            if (requiresSpecificUnit.boolValue)
            {
                SerializedProperty specificUnit = property.FindPropertyRelative("specificUnit");
                height += EditorGUI.GetPropertyHeight(specificUnit, true) + padding;
            }
            else
            {
                SerializedProperty anyFaction = property.FindPropertyRelative("anyFaction");
                height += EditorGUI.GetPropertyHeight(anyFaction, true) + padding;

                if (!anyFaction.boolValue)
                {
                    SerializedProperty requiredFaction = property.FindPropertyRelative("requiredFaction");
                    height += EditorGUI.GetPropertyHeight(requiredFaction, true) + padding;
                }

                SerializedProperty anyRole = property.FindPropertyRelative("anyRole");
                height += EditorGUI.GetPropertyHeight(anyRole, true) + padding;

                if (!anyRole.boolValue)
                {
                    SerializedProperty requiredRole = property.FindPropertyRelative("requiredRole");
                    height += EditorGUI.GetPropertyHeight(requiredRole, true) + padding;
                }
            }

            SerializedProperty amount = property.FindPropertyRelative("amount");
            height += EditorGUI.GetPropertyHeight(amount, true) + padding;

            return height + padding;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
            
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                SerializedProperty requiresSpecificUnit = property.FindPropertyRelative("requiresSpecificUnit");
                float h = EditorGUI.GetPropertyHeight(requiresSpecificUnit, true);
                rect.height = h;
                EditorGUI.PropertyField(rect, requiresSpecificUnit, true);
                rect.y += h + EditorGUIUtility.standardVerticalSpacing;

                if (requiresSpecificUnit.boolValue)
                {
                    SerializedProperty specificUnit = property.FindPropertyRelative("specificUnit");
                    h = EditorGUI.GetPropertyHeight(specificUnit, true);
                    rect.height = h;
                    EditorGUI.PropertyField(rect, specificUnit, true);
                    rect.y += h + EditorGUIUtility.standardVerticalSpacing;
                }
                else
                {
                    SerializedProperty anyFaction = property.FindPropertyRelative("anyFaction");
                    h = EditorGUI.GetPropertyHeight(anyFaction, true);
                    rect.height = h;
                    EditorGUI.PropertyField(rect, anyFaction, true);
                    rect.y += h + EditorGUIUtility.standardVerticalSpacing;

                    if (!anyFaction.boolValue)
                    {
                        SerializedProperty requiredFaction = property.FindPropertyRelative("requiredFaction");
                        h = EditorGUI.GetPropertyHeight(requiredFaction, true);
                        rect.height = h;
                        EditorGUI.PropertyField(rect, requiredFaction, true);
                        rect.y += h + EditorGUIUtility.standardVerticalSpacing;
                    }

                    SerializedProperty anyRole = property.FindPropertyRelative("anyRole");
                    h = EditorGUI.GetPropertyHeight(anyRole, true);
                    rect.height = h;
                    EditorGUI.PropertyField(rect, anyRole, true);
                    rect.y += h + EditorGUIUtility.standardVerticalSpacing;

                    if (!anyRole.boolValue)
                    {
                        SerializedProperty requiredRole = property.FindPropertyRelative("requiredRole");
                        h = EditorGUI.GetPropertyHeight(requiredRole, true);
                        rect.height = h;
                        EditorGUI.PropertyField(rect, requiredRole, true);
                        rect.y += h + EditorGUIUtility.standardVerticalSpacing;
                    }
                }

                SerializedProperty amount = property.FindPropertyRelative("amount");
                h = EditorGUI.GetPropertyHeight(amount, true);
                rect.height = h;
                EditorGUI.PropertyField(rect, amount, true);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
