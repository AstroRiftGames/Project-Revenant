using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillData))]
[CanEditMultipleObjects]
public class SkillDataEditor : Editor
{
    private SerializedProperty _skillId;
    private SerializedProperty _displayName;
    private SerializedProperty _description;
    private SerializedProperty _icon;
    
    // Targeting
    private SerializedProperty _primaryTargetRequirement;
    private SerializedProperty _impactTargetRequirement;
    private SerializedProperty _targetSelectionMode;
    private SerializedProperty _targetFallbackMode;
    
    // Delivery
    private SerializedProperty _trajectory;
    private SerializedProperty _impactPattern;
    private SerializedProperty _impactCenterMode;
    private SerializedProperty _skillExecutionMode;
    
    // Composition
    private SerializedProperty _compositionEffects;
    private SerializedProperty _compositionModifierKinds;
    private SerializedProperty _compositionModifierData;
    private SerializedProperty _compositionState;
    

    
    // Parameters
    private SerializedProperty _castTime;
    private SerializedProperty _rangeInCells;
    private SerializedProperty _radiusInCells;
    private SerializedProperty _lineLengthInCells;
    private SerializedProperty _maxTargets;

    private void OnEnable()
    {
        _skillId = serializedObject.FindProperty("_skillId");
        _displayName = serializedObject.FindProperty("_displayName");
        _description = serializedObject.FindProperty("_description");
        _icon = serializedObject.FindProperty("_icon");
        
        _primaryTargetRequirement = serializedObject.FindProperty("_primaryTargetRequirement");
        _impactTargetRequirement = serializedObject.FindProperty("_impactTargetRequirement");
        _targetSelectionMode = serializedObject.FindProperty("_targetSelectionMode");
        _targetFallbackMode = serializedObject.FindProperty("_targetFallbackMode");
        
        _trajectory = serializedObject.FindProperty("_trajectory");
        _impactPattern = serializedObject.FindProperty("_impactPattern");
        _impactCenterMode = serializedObject.FindProperty("_impactCenterMode");
        _skillExecutionMode = serializedObject.FindProperty("_skillExecutionMode");
        
        _compositionEffects = serializedObject.FindProperty("_compositionEffects");
        _compositionModifierKinds = serializedObject.FindProperty("_compositionModifierKinds");
        _compositionModifierData = serializedObject.FindProperty("_compositionModifierData");
        _compositionState = serializedObject.FindProperty("_compositionState");
        

        
        _castTime = serializedObject.FindProperty("_castTime");
        _rangeInCells = serializedObject.FindProperty("_rangeInCells");
        _radiusInCells = serializedObject.FindProperty("_radiusInCells");
        _lineLengthInCells = serializedObject.FindProperty("_lineLengthInCells");
        _maxTargets = serializedObject.FindProperty("_maxTargets");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. HEADER & METADATA
        GUILayout.Label("Skill Metadata", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(_skillId);
        EditorGUILayout.PropertyField(_displayName);
        EditorGUILayout.PropertyField(_description);
        EditorGUILayout.PropertyField(_icon);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 2. TARGETING & DELIVERY
        GUILayout.Label("Targeting & Delivery", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(_primaryTargetRequirement);
        EditorGUILayout.PropertyField(_impactTargetRequirement);
        EditorGUILayout.PropertyField(_targetSelectionMode);
        EditorGUILayout.PropertyField(_targetFallbackMode);
        EditorGUILayout.PropertyField(_trajectory);
        EditorGUILayout.PropertyField(_impactPattern);
        EditorGUILayout.PropertyField(_impactCenterMode);
        EditorGUILayout.PropertyField(_skillExecutionMode);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 3. SKILL COMPOSITION
        GUILayout.Label("Skill Composition", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        // Show helpbox based on state
        if (!_compositionState.hasMultipleDifferentValues)
        {
            SkillCompositionState stateVal = (SkillCompositionState)_compositionState.enumValueIndex;
            switch (stateVal)
            {
                case SkillCompositionState.Official:
                    EditorGUILayout.HelpBox("Esta skill cumple el contrato oficial de diseño y es apta para producción.", MessageType.Info);
                    break;
                case SkillCompositionState.DebugOnly:
                    EditorGUILayout.HelpBox("Esta skill está marcada como DebugOnly. No debe asignarse a criaturas reales en producción.", MessageType.Warning);
                    break;
                case SkillCompositionState.Provisional:
                    EditorGUILayout.HelpBox("Esta skill está en un estado Provisional.", MessageType.Info);
                    break;
                case SkillCompositionState.NonOfficial:
                    EditorGUILayout.HelpBox("Esta skill está marcada como NonOfficial. Usa efectos fuera del documento oficial.", MessageType.Error);
                    break;
                case SkillCompositionState.RequiresDesignDecision:
                    EditorGUILayout.HelpBox("Esta skill requiere una decisión de diseño antes de ser apta para producción.", MessageType.Warning);
                    break;
            }
        }

        EditorGUILayout.PropertyField(_compositionState, new GUIContent("State"));

        var skill = (SkillData)target;
        var productionSupportIssues = SkillProductionSupportCatalog.GetProductionSupportIssues(skill);
        for (int i = 0; i < productionSupportIssues.Count; i++)
        {
            EditorGUILayout.HelpBox(productionSupportIssues[i], MessageType.Warning);
        }
        
        // Group visually: Effects
        GUILayout.Label("Effects", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(_compositionEffects, new GUIContent("Effects List"));

        // Group visually: Modifiers
        GUILayout.Label("Modifiers", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(_compositionModifierKinds, new GUIContent("Modifier Kinds"));
        if (_compositionModifierKinds.isArray && _compositionModifierKinds.arraySize > 0)
        {
            EditorGUILayout.PropertyField(_compositionModifierData, new GUIContent("Modifier Parameters"));
        }

        EditorGUILayout.Space(5);
        
        // Buttons
        if (GUILayout.Button("Validate This Skill", GUILayout.Height(30)))
        {
            var violations = SkillCompositionEditorTools.ValidateSkill(skill);
            if (violations.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Success", $"Skill '{skill.name}' is valid and matches all metadata rules!", "OK");
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Skill '{skill.name}' has {violations.Count} validation violations:");
                sb.AppendLine();
                foreach (var v in violations)
                {
                    sb.AppendLine($"- {v.Message}");
                }
                EditorUtility.DisplayDialog("Validation Violations", sb.ToString(), "OK");
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);



        EditorGUILayout.Space(5);

        // 5. PARAMETERS
        GUILayout.Label("Parameters", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(_castTime);
        EditorGUILayout.PropertyField(_rangeInCells);
        EditorGUILayout.PropertyField(_radiusInCells);
        EditorGUILayout.PropertyField(_lineLengthInCells);
        EditorGUILayout.PropertyField(_maxTargets);
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
