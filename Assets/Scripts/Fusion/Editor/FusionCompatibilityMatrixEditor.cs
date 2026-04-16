#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(FusionCompatibilityMatrix))]
public class FusionCompatibilityMatrixEditor : Editor
{
    private FusionCompatibilityMatrix _target;

    private void OnEnable()
    {
        _target = (FusionCompatibilityMatrix)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.Label("Fusion Compatibility Matrix", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        UnitFaction[] factions = (UnitFaction[])System.Enum.GetValues(typeof(UnitFaction));
        
        EditorGUI.BeginChangeCheck();

        GUILayout.BeginHorizontal();
        GUILayout.Space(85);
        for(int i = 1; i < factions.Length; i++)
        {
            string colName = factions[i].ToString();
            string shortName = colName.Substring(0, Mathf.Min(3, colName.Length));
            GUILayout.Label(shortName, EditorStyles.boldLabel, GUILayout.Width(45));
        }
        GUILayout.EndHorizontal();

        for(int i = 1; i < factions.Length; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(factions[i].ToString(), EditorStyles.boldLabel, GUILayout.Width(80));

            for(int j = 1; j < factions.Length; j++)
            {
                if (j >= i)
                {
                    UnitFaction factionInRow = factions[i];
                    UnitFaction factionInCol = factions[j];
                    
                    bool isCurrentlyCompatible = _target.IsCompatible(factionInRow, factionInCol);
                    UnitFaction currentDominant = _target.GetDominantFaction(factionInRow, factionInCol);

                    GUILayout.BeginVertical(GUILayout.Width(45));
                    
                    bool newCompatible = EditorGUILayout.Toggle(isCurrentlyCompatible, GUILayout.Width(20));

                    UnitFaction newDominant = currentDominant;
                    if (newCompatible)
                    {
                        if (newDominant != factionInRow && newDominant != factionInCol)
                            newDominant = factionInRow;

                        GUI.backgroundColor = (newDominant == factionInRow) ? Color.cyan : Color.yellow;
                        
                        string domName = newDominant.ToString();
                        string shortDom = domName.Substring(0, Mathf.Min(3, domName.Length));
                        
                        if (GUILayout.Button(shortDom, GUILayout.Width(35)))
                        {
                            newDominant = (newDominant == factionInRow) ? factionInCol : factionInRow;
                        }
                        GUI.backgroundColor = Color.white;
                    }

                    GUILayout.EndVertical();

                    if (newCompatible != isCurrentlyCompatible || newDominant != currentDominant)
                    {
                        UpdateMappingData(factionInRow, factionInCol, newCompatible, newDominant);
                    }
                }
                else
                {
                    GUILayout.Space(49);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_target);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("INSTRUCCIONES:\n1. Marca el Checkbox para hacer compatibles dos facciones.\n2. Haz clic en el botón de abajo (ej. 'Hum', 'Orc') para intercambiar cuál de las dos es la Dominante.", MessageType.Info);
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        GUILayout.Label("Raw Configuration Values", EditorStyles.boldLabel);
        DrawDefaultInspector();
    }

    private void UpdateMappingData(UnitFaction f1, UnitFaction f2, bool isCompatible, UnitFaction dominant)
    {
        SerializedProperty listProp = serializedObject.FindProperty("_compatibilityMappings");
        if (listProp == null) return;
        
        for (int i = 0; i < listProp.arraySize; i++)
        {
            SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);
            SerializedProperty factionsProp = elementProp.FindPropertyRelative("Factions");
            SerializedProperty fAProp = factionsProp.FindPropertyRelative("FactionA");
            SerializedProperty fBProp = factionsProp.FindPropertyRelative("FactionB");

            UnitFaction mapFA = (UnitFaction)fAProp.enumValueIndex;
            UnitFaction mapFB = (UnitFaction)fBProp.enumValueIndex;

            if ((mapFA == f1 && mapFB == f2) || (mapFA == f2 && mapFB == f1))
            {
                if (!isCompatible)
                {
                    listProp.DeleteArrayElementAtIndex(i);
                    return;
                }
                else
                {
                    elementProp.FindPropertyRelative("IsCompatible").boolValue = true;
                    elementProp.FindPropertyRelative("DominantFaction").enumValueIndex = (int)dominant; 
                    return;
                }
            }
        }

        if (isCompatible)
        {
            int index = listProp.arraySize;
            listProp.InsertArrayElementAtIndex(index);
            SerializedProperty newElement = listProp.GetArrayElementAtIndex(index);
            
            newElement.FindPropertyRelative("Factions").FindPropertyRelative("FactionA").enumValueIndex = (int)f1;
            newElement.FindPropertyRelative("Factions").FindPropertyRelative("FactionB").enumValueIndex = (int)f2;
            newElement.FindPropertyRelative("IsCompatible").boolValue = true;
            newElement.FindPropertyRelative("DominantFaction").enumValueIndex = (int)((dominant == UnitFaction.None) ? f1 : dominant);
        }
    }
}
#endif

