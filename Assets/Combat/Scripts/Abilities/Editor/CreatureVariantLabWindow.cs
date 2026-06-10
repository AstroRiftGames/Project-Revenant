using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreatureVariantLabWindow : EditorWindow
{
    [MenuItem("Tools/Creatures/Creature Variant Lab")]
    public static void ShowWindow()
    {
        GetWindow<CreatureVariantLabWindow>("Creature Variant Lab");
    }

    // Enums for UI
    private enum LabEffectKind
    {
        Damage,
        Heal,
        Shield,
        Stun,
        Slow,
        PoisonBurn,
        Haste,
        StrengthBuff,
        HealOverTime,
        Summon,
        Knockback
    }

    private enum LabModifierKind
    {
        None,
        Splash,
        Penetrating,
        Bounce,
        Explosive,
        Persistent,
        Periodic
    }

    // A. Unit Setup State
    private UnitFaction _faction = UnitFaction.Human;
    private UnitRole _role = UnitRole.DPS;
    private UnitTeam _team = UnitTeam.Ally;
    private GameObject _prefabOverride;
    private UnitData _templateOverride;

    // B. Skill Composition Setup State
    private ImpactPattern _delivery = ImpactPattern.Direct;
    private PrimaryTargetRequirement _primaryTarget = PrimaryTargetRequirement.Hostile;
    private LabEffectKind _effect = LabEffectKind.Damage;
    private LabModifierKind _modifier = LabModifierKind.None;

    // Parameter States
    private int _value = 10;
    private float _duration = 3f;
    private float _interval = 1f;
    private int _maxStacks = 1;
    private int _radiusInCells = 1;
    private int _rangeInCells = 3;
    private int _lineLengthInCells = 5;
    private int _knockbackCells = 2;
    private UnitData _summonedUnit;
    private SummonAnchorMode _summonAnchorMode = SummonAnchorMode.AroundImpactCenter;
    private int _bounceMaxBounces = 3;
    private int _bounceRangeInCells = 2;
    private int _explosiveRadiusInCells = 2;
    private bool _explosiveIncludePrimaryImpactTarget = true;
    private bool _bounceCanBounceToPrimaryTargetAgain = false;

    // Status definitions cache
    private List<StatusEffectDefinition> _statusDefinitions = new List<StatusEffectDefinition>();
    private string[] _statusNames = new string[0];
    private int _selectedStatusIndex = 0;
    private StatusEffectDefinition _statusDefinition;

    // Save state
    private string _saveSkillName = "";

    // Validation State
    private List<string> _validationErrors = new List<string>();
    private List<string> _validationWarnings = new List<string>();

    // Scroll & layout
    private Vector2 _scrollPosition;
    private bool _showUnitSetup = true;
    private bool _showSkillSetup = true;
    private bool _showValidation = true;
    private bool _showTestActions = true;
    private bool _showSaveActions = true;

    private void OnEnable()
    {
        LoadStatusDefinitions();
        ValidateComposition();
    }

    private void LoadStatusDefinitions()
    {
        _statusDefinitions.Clear();
        string[] guids = AssetDatabase.FindAssets("t:StatusEffectDefinition");
        List<string> names = new List<string> { "None" };
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sd = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>(path);
            if (sd != null)
            {
                _statusDefinitions.Add(sd);
                names.Add(sd.name);
            }
        }
        _statusNames = names.ToArray();
        
        // Auto-select match
        if (_statusDefinition != null)
        {
            int index = _statusDefinitions.IndexOf(_statusDefinition);
            _selectedStatusIndex = index >= 0 ? index + 1 : 0;
        }
        else
        {
            _selectedStatusIndex = 0;
        }
    }

    private void OnGUI()
    {
        // Styling
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;
        headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.7f, 1f) : new Color(0.1f, 0.4f, 0.8f);

        GUIStyle sectionTitleStyle = new GUIStyle(EditorStyles.foldout);
        sectionTitleStyle.fontStyle = FontStyle.Bold;
        sectionTitleStyle.fontSize = 12;

        GUILayout.Space(10);
        GUILayout.Label("Creature Variant Lab", headerStyle);
        GUILayout.Label("Espacio de pruebas y laboratorio de composición de habilidades", EditorStyles.miniLabel);
        GUILayout.Space(5);

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        // ==========================================
        // A. UNIT SETUP
        // ==========================================
        _showUnitSetup = EditorGUILayout.BeginFoldoutHeaderGroup(_showUnitSetup, "A. Unit Setup", sectionTitleStyle);
        if (_showUnitSetup)
        {
            EditorGUILayout.BeginVertical("box");
            
            _faction = (UnitFaction)EditorGUILayout.EnumPopup("Faction", _faction);
            // Restrict faction to Human or Orc
            if (_faction != UnitFaction.Human && _faction != UnitFaction.Orc)
            {
                _faction = UnitFaction.Human;
            }

            _role = (UnitRole)EditorGUILayout.EnumPopup("Role", _role);
            _team = (UnitTeam)EditorGUILayout.EnumPopup("Team para Test", _team);

            GUILayout.Space(5);
            // Auto-resolved preview
            GameObject resolvedPrefab = GetDefaultPrefab(_faction, _role);
            UnitData resolvedTemplate = GetBaseTemplate(_faction, _role);

            EditorGUILayout.LabelField("Auto-resolved Defaults:", EditorStyles.miniBoldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Default Prefab", resolvedPrefab, typeof(GameObject), false);
            EditorGUILayout.ObjectField("Default Template", resolvedTemplate, typeof(UnitData), false);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);
            _prefabOverride = (GameObject)EditorGUILayout.ObjectField("Prefab Override (Manual)", _prefabOverride, typeof(GameObject), false);
            _templateOverride = (UnitData)EditorGUILayout.ObjectField("Template Override (Manual)", _templateOverride, typeof(UnitData), false);

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        GUILayout.Space(10);

        // ==========================================
        // B. SKILL COMPOSITION SETUP
        // ==========================================
        _showSkillSetup = EditorGUILayout.BeginFoldoutHeaderGroup(_showSkillSetup, "B. Skill Composition Setup", sectionTitleStyle);
        if (_showSkillSetup)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginChangeCheck();

            _delivery = (ImpactPattern)EditorGUILayout.EnumPopup("Delivery", _delivery);
            _primaryTarget = (PrimaryTargetRequirement)EditorGUILayout.EnumPopup("Primary Target Requirement", _primaryTarget);
            _effect = (LabEffectKind)EditorGUILayout.EnumPopup("Effect", _effect);
            _modifier = (LabModifierKind)EditorGUILayout.EnumPopup("Modifier", _modifier);

            GUILayout.Space(10);
            GUILayout.Label("Skill Parameters", EditorStyles.boldLabel);
            DrawHorizontalLine();
            GUILayout.Space(5);

            // Conditional parameter rendering
            // 1. Value
            if (_effect == LabEffectKind.Damage || _effect == LabEffectKind.Heal || _effect == LabEffectKind.Shield || _effect == LabEffectKind.HealOverTime)
            {
                _value = EditorGUILayout.IntField("Damage / Heal / Shield Value", _value);
            }

            // 2. Duration / Interval / Max Stacks for Status
            bool usesStatus = (_effect == LabEffectKind.Stun || _effect == LabEffectKind.Slow || 
                               _effect == LabEffectKind.PoisonBurn || _effect == LabEffectKind.Haste || 
                               _effect == LabEffectKind.StrengthBuff || _effect == LabEffectKind.HealOverTime ||
                               _effect == LabEffectKind.Shield);
            if (usesStatus)
            {
                _duration = EditorGUILayout.FloatField("Duration (s)", _duration);
                if (_effect == LabEffectKind.PoisonBurn || _effect == LabEffectKind.HealOverTime)
                {
                    _interval = EditorGUILayout.FloatField("Tick Interval (s)", _interval);
                }
                _maxStacks = EditorGUILayout.IntField("Max Stacks", _maxStacks);
            }

            // Status definition dropdown
            bool needsStatusAsset = (_effect == LabEffectKind.Stun || _effect == LabEffectKind.Slow || 
                                     _effect == LabEffectKind.PoisonBurn || _effect == LabEffectKind.Haste || 
                                     _effect == LabEffectKind.StrengthBuff || _effect == LabEffectKind.HealOverTime);
            if (needsStatusAsset)
            {
                if (_statusNames.Length > 0)
                {
                    int prevIndex = _selectedStatusIndex;
                    _selectedStatusIndex = EditorGUILayout.Popup("Status Effect Definition", _selectedStatusIndex, _statusNames);
                    if (_selectedStatusIndex > 0 && _selectedStatusIndex <= _statusDefinitions.Count)
                    {
                        _statusDefinition = _statusDefinitions[_selectedStatusIndex - 1];
                    }
                    else
                    {
                        _statusDefinition = null;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No StatusEffectDefinition assets found in project.", MessageType.Warning);
                }
            }

            // 3. Radius for Area
            if (_delivery == ImpactPattern.Area)
            {
                _radiusInCells = EditorGUILayout.IntField("Radius in Cells", _radiusInCells);
            }

            // 4. Line length for Line
            if (_delivery == ImpactPattern.Line)
            {
                _lineLengthInCells = EditorGUILayout.IntField("Line Length in Cells", _lineLengthInCells);
            }

            // 5. Range (all except self target)
            if (_primaryTarget != PrimaryTargetRequirement.Self)
            {
                _rangeInCells = EditorGUILayout.IntField("Range in Cells", _rangeInCells);
            }

            // 6. Knockback
            if (_effect == LabEffectKind.Knockback)
            {
                _knockbackCells = EditorGUILayout.IntField("Knockback Cells", _knockbackCells);
            }

            // 7. Summon parameters
            if (_effect == LabEffectKind.Summon)
            {
                _summonedUnit = (UnitData)EditorGUILayout.ObjectField("Summoned Unit Data", _summonedUnit, typeof(UnitData), false);
                _summonAnchorMode = (SummonAnchorMode)EditorGUILayout.EnumPopup("Summon Anchor Mode", _summonAnchorMode);
            }

            // 8. Modifier parameters
            if (_modifier == LabModifierKind.Bounce)
            {
                _bounceMaxBounces = EditorGUILayout.IntField("Max Bounces", _bounceMaxBounces);
                _bounceRangeInCells = EditorGUILayout.IntField("Bounce Range (Cells)", _bounceRangeInCells);
                _bounceCanBounceToPrimaryTargetAgain = EditorGUILayout.Toggle("Can Bounce To Primary Again", _bounceCanBounceToPrimaryTargetAgain);
            }
            else if (_modifier == LabModifierKind.Explosive)
            {
                _explosiveRadiusInCells = EditorGUILayout.IntField("Explosion Radius (Cells)", _explosiveRadiusInCells);
                _explosiveIncludePrimaryImpactTarget = EditorGUILayout.Toggle("Include Primary Target", _explosiveIncludePrimaryImpactTarget);
            }

            if (EditorGUI.EndChangeCheck())
            {
                ValidateComposition();
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        GUILayout.Space(10);

        // ==========================================
        // C. VALIDATION PREVIEW
        // ==========================================
        _showValidation = EditorGUILayout.BeginFoldoutHeaderGroup(_showValidation, "C. Validation Preview", sectionTitleStyle);
        if (_showValidation)
        {
            EditorGUILayout.BeginVertical("box");

            if (_validationErrors.Count == 0 && _validationWarnings.Count == 0)
            {
                GUI.backgroundColor = Color.green;
                EditorGUILayout.HelpBox("✔ Composición válida y lista para pruebas runtime.", MessageType.Info);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                if (_validationErrors.Count > 0)
                {
                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                    foreach (var err in _validationErrors)
                    {
                        EditorGUILayout.HelpBox("❌ ERROR: " + err, MessageType.Error);
                    }
                    GUI.backgroundColor = Color.white;
                }

                if (_validationWarnings.Count > 0)
                {
                    GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
                    foreach (var warn in _validationWarnings)
                    {
                        EditorGUILayout.HelpBox("⚠ ADVERTENCIA: " + warn, MessageType.Warning);
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        GUILayout.Space(10);

        // ==========================================
        // D. TEST ACTIONS
        // ==========================================
        _showTestActions = EditorGUILayout.BeginFoldoutHeaderGroup(_showTestActions, "D. Test Actions", sectionTitleStyle);
        if (_showTestActions)
        {
            EditorGUILayout.BeginVertical("box");

            bool inPlayMode = EditorApplication.isPlaying;
            if (!inPlayMode)
            {
                EditorGUILayout.HelpBox("Entra en Play Mode en 'TestMapScene' para habilitar las acciones de Spawn/Prueba.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(!inPlayMode);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("Spawn Test Unit As Ally", GUILayout.Height(30)))
            {
                SpawnTestUnit(UnitTeam.Ally);
            }
            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("Spawn Test Unit As Enemy", GUILayout.Height(30)))
            {
                SpawnTestUnit(UnitTeam.Enemy);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Opponent Dummy", GUILayout.Height(24)))
            {
                SpawnDummy();
            }
            if (GUILayout.Button("Clear Spawned Test Units", GUILayout.Height(24)))
            {
                ClearSpawnedUnits();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Spawned Unit", GUILayout.Height(22)))
            {
                SelectLastSpawnedUnit();
            }
            if (GUILayout.Button("Log Runtime State", GUILayout.Height(22)))
            {
                LogRuntimeState();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);
            if (GUILayout.Button("Clear Lab Temp Assets (Disk Cleanup)", GUILayout.Height(20)))
            {
                ClearLabTempAssets();
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        GUILayout.Space(10);

        // ==========================================
        // E. SAVE ACTIONS
        // ==========================================
        _showSaveActions = EditorGUILayout.BeginFoldoutHeaderGroup(_showSaveActions, "E. Save Actions", sectionTitleStyle);
        if (_showSaveActions)
        {
            EditorGUILayout.BeginVertical("box");

            // Input for name without role prefix
            _saveSkillName = EditorGUILayout.TextField("Skill Name (No Role Prefix)", _saveSkillName);
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find Matching Generated Variant", GUILayout.Height(25)))
            {
                UnitData matched = FindMatchingVariant();
                if (matched != null)
                {
                    Debug.Log($"[Variant Lab] Found matching generated variant: '{matched.name}' at path '{AssetDatabase.GetAssetPath(matched)}'");
                    EditorUtility.DisplayDialog("Match Found", $"Se encontró variante coincidente:\n\n{matched.name}\n\nUbicación: {AssetDatabase.GetAssetPath(matched)}", "OK");
                }
                else
                {
                    Debug.Log("[Variant Lab] No matching generated variant found.");
                    EditorUtility.DisplayDialog("No Match", "No existe ninguna variante generada activa que coincida con esta composición.", "OK");
                }
            }

            if (GUILayout.Button("Ping Matching Variant", GUILayout.Height(25)))
            {
                UnitData matched = FindMatchingVariant();
                if (matched != null)
                {
                    EditorGUIUtility.PingObject(matched);
                    UnityEditor.Selection.activeObject = matched;
                }
                else
                {
                    EditorUtility.DisplayDialog("No Match", "No existe ninguna variante generada coincidente para pinguear.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            bool saveAllowed = _validationErrors.Count == 0 && !string.IsNullOrEmpty(_saveSkillName);
            EditorGUI.BeginDisabledGroup(!saveAllowed);
            GUI.backgroundColor = saveAllowed ? new Color(0.3f, 0.7f, 1f) : Color.white;
            if (GUILayout.Button("Save As Generated Variant", GUILayout.Height(30)))
            {
                SaveAsGeneratedVariant(_saveSkillName);
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            if (string.IsNullOrEmpty(_saveSkillName))
            {
                EditorGUILayout.HelpBox("Ingresa un nombre de habilidad (ej: DirectDamage) para habilitar el guardado persistente.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        GUILayout.Space(10);
        
        // F. Legacy maintenance options (Generate / Validate variants)
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Legacy Actions (Catalog Maintenance):", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate/Regenerate All Variants", GUILayout.Height(20)))
        {
            CreatureGeneratorTools.GenerateSkillVariants();
        }
        if (GUILayout.Button("Validate All Variants", GUILayout.Height(20)))
        {
            CreatureGeneratorTools.ValidateSkillVariants();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.EndScrollView();
    }

    private void DrawHorizontalLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    // ==========================================
    // VALIDATION LOGIC
    // ==========================================
    private void ValidateComposition()
    {
        _validationErrors.Clear();
        _validationWarnings.Clear();

        // 1. Prefab & template checks
        GameObject prefab = _prefabOverride != null ? _prefabOverride : GetDefaultPrefab(_faction, _role);
        if (prefab == null)
        {
            _validationErrors.Add("No se pudo resolver el prefab base.");
        }
        else if (prefab.GetComponent<Unit>() == null)
        {
            _validationErrors.Add($"El prefab '{prefab.name}' no tiene el componente Unit.");
        }

        UnitData template = _templateOverride != null ? _templateOverride : GetBaseTemplate(_faction, _role);
        if (template == null)
        {
            _validationErrors.Add("No se pudo resolver la plantilla base (UnitData).");
        }

        // 2. Role compatibility check
        bool roleMatch = false;
        switch (_role)
        {
            case UnitRole.DPS:
                roleMatch = (_delivery == ImpactPattern.Direct || _delivery == ImpactPattern.Line);
                break;
            case UnitRole.Tank:
                roleMatch = (_delivery == ImpactPattern.Area);
                break;
            case UnitRole.Support:
                roleMatch = (_delivery == ImpactPattern.Direct || _delivery == ImpactPattern.Area);
                break;
        }
        if (!roleMatch)
        {
            _validationErrors.Add($"El patrón de impacto {_delivery} no es compatible con el rol {_role} (AdvancedPatternNotProductionReady).");
        }

        // 3. Parameter checks
        if (_effect == LabEffectKind.Shield && _value <= 0)
        {
            _validationErrors.Add("El efecto Shield requiere un valor mayor a 0.");
        }
        if (_effect == LabEffectKind.Shield && _duration <= 0f)
        {
            _validationErrors.Add("El efecto Shield requiere una duración mayor a 0.");
        }

        bool isStatus = (_effect == LabEffectKind.Stun || _effect == LabEffectKind.Slow || 
                         _effect == LabEffectKind.PoisonBurn || _effect == LabEffectKind.Haste || 
                         _effect == LabEffectKind.StrengthBuff || _effect == LabEffectKind.HealOverTime);
        if (isStatus)
        {
            if (_statusDefinition == null)
            {
                _validationErrors.Add("Se requiere un StatusEffectDefinition para los efectos de estado.");
            }
            else
            {
                StatusEffectType expectedType = StatusEffectType.Stun;
                if (_effect == LabEffectKind.Slow) expectedType = StatusEffectType.StatModifierDebuff;
                else if (_effect == LabEffectKind.PoisonBurn) expectedType = StatusEffectType.DamageOverTime;
                else if (_effect == LabEffectKind.HealOverTime) expectedType = StatusEffectType.HealOverTime;
                else if (_effect == LabEffectKind.Haste || _effect == LabEffectKind.StrengthBuff) expectedType = StatusEffectType.StatModifierBuff;

                if (_statusDefinition.EffectType != expectedType)
                {
                    _validationWarnings.Add($"El subtipo del StatusEffectDefinition '{_statusDefinition.EffectType}' no coincide del todo con el efecto seleccionado '{_effect}'.");
                }
            }
        }

        if (_effect == LabEffectKind.Summon)
        {
            if (_summonedUnit == null)
            {
                _validationErrors.Add("El efecto Summon requiere asignar un SummonedUnit.");
            }
            _validationWarnings.Add("El efecto Summon está marcado como no listo para producción (SummonNotProductionReady).");
        }

        // Modifier warnings
        if (_modifier == LabModifierKind.Bounce || _modifier == LabModifierKind.Explosive)
        {
            _validationWarnings.Add($"El modificador {_modifier} está marcado como no listo para producción (ModifierNotProductionReady).");
        }
        if (_modifier == LabModifierKind.Persistent || _modifier == LabModifierKind.Periodic)
        {
            _validationWarnings.Add($"El modificador {_modifier} no tiene implementación en runtime (NoRuntimeImplementation).");
        }

        // Rule 7/8 warnings
        if (isStatus || _effect == LabEffectKind.Shield)
        {
            if (_modifier != LabModifierKind.Persistent && _duration > 0f)
            {
                _validationWarnings.Add("[Rule 7] El efecto tiene una duración persistente pero le falta el modificador 'Persistent'.");
            }
        }
        if (isStatus && _interval > 0f)
        {
            if (_modifier != LabModifierKind.Periodic)
            {
                _validationWarnings.Add("[Rule 8] El efecto tiene ticks periódicos pero le falta el modificador 'Periodic'.");
            }
        }
    }

    // ==========================================
    // ASSETS MANAGEMENT & GENERATION
    // ==========================================
    private UnitData GetBaseTemplate(UnitFaction faction, UnitRole role)
    {
        string path = "";
        if (faction == UnitFaction.Human)
        {
            if (role == UnitRole.DPS) path = "Assets/Core/Data/Scriptable Objects/Allies/HumanSlayerData.asset";
            else if (role == UnitRole.Tank) path = "Assets/Core/Data/Scriptable Objects/Allies/HumanBruteData.asset";
            else if (role == UnitRole.Support) path = "Assets/Core/Data/Scriptable Objects/Allies/HumanShamanData.asset";
        }
        else if (faction == UnitFaction.Orc)
        {
            if (role == UnitRole.DPS) path = "Assets/Core/Data/Scriptable Objects/Allies/OrcSlayerData.asset";
            else if (role == UnitRole.Tank) path = "Assets/Core/Data/Scriptable Objects/Allies/OrcBruteData.asset";
            else if (role == UnitRole.Support) path = "Assets/Core/Data/Scriptable Objects/Allies/OrcShamanData.asset";
        }

        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<UnitData>(path);
    }

    private GameObject GetDefaultPrefab(UnitFaction faction, UnitRole role)
    {
        string path = $"Assets/Combat/Prefabs/Creatures/Playable/{faction}_{role}.prefab";
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private void CreateAssetDirectories(string path)
    {
        string dir = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }
    }

    private void ConfigureSkillProperties(SkillData skill, string assetName, string displayName, SkillCompositionState state)
    {
        SerializedObject skillSO = new SerializedObject(skill);
        skillSO.FindProperty("_skillId").stringValue = assetName;
        skillSO.FindProperty("_displayName").stringValue = displayName;
        skillSO.FindProperty("_description").stringValue = "Skill autogenerada en el laboratorio.";

        skillSO.FindProperty("_primaryTargetRequirement").enumValueIndex = (int)_primaryTarget;
        skillSO.FindProperty("_impactTargetRequirement").enumValueIndex = (int)(_primaryTarget == PrimaryTargetRequirement.Ally || _primaryTarget == PrimaryTargetRequirement.Self ? ImpactTargetRequirement.Ally : ImpactTargetRequirement.Hostile);
        skillSO.FindProperty("_targetSelectionMode").enumValueIndex = (int)(_primaryTarget == PrimaryTargetRequirement.Ally || _primaryTarget == PrimaryTargetRequirement.Self ? TargetSelectionMode.AllyLowestHealth : TargetSelectionMode.RoleBasedOffensive);
        skillSO.FindProperty("_targetFallbackMode").enumValueIndex = (int)TargetFallbackMode.Retarget;

        skillSO.FindProperty("_trajectory").enumValueIndex = (int)SkillTrajectory.Hitscan;
        skillSO.FindProperty("_impactPattern").enumValueIndex = (int)_delivery;
        skillSO.FindProperty("_impactCenterMode").enumValueIndex = (int)(_delivery == ImpactPattern.Direct ? ImpactCenterMode.PrimaryTarget : ImpactCenterMode.TargetCell);
        skillSO.FindProperty("_skillExecutionMode").enumValueIndex = (int)SkillExecutionMode.Instant;

        // Effects setup
        SerializedProperty effectsProp = skillSO.FindProperty("_compositionEffects");
        effectsProp.arraySize = 1;
        SerializedProperty effectProp = effectsProp.GetArrayElementAtIndex(0);

        SkillEffectKind kind = SkillEffectKind.Damage;
        switch (_effect)
        {
            case LabEffectKind.Damage: kind = SkillEffectKind.Damage; break;
            case LabEffectKind.Heal: kind = SkillEffectKind.Heal; break;
            case LabEffectKind.Shield: kind = SkillEffectKind.Shield; break;
            case LabEffectKind.Stun: kind = SkillEffectKind.Stun; break;
            case LabEffectKind.Slow: kind = SkillEffectKind.Slow; break;
            case LabEffectKind.PoisonBurn: kind = SkillEffectKind.PoisonBurn; break;
            case LabEffectKind.Haste: kind = SkillEffectKind.Haste; break;
            case LabEffectKind.StrengthBuff: kind = SkillEffectKind.StrengthBuff; break;
            case LabEffectKind.HealOverTime: kind = SkillEffectKind.Heal; break;
            case LabEffectKind.Summon: kind = SkillEffectKind.Summon; break;
            case LabEffectKind.Knockback: kind = SkillEffectKind.Knockback; break;
        }

        effectProp.FindPropertyRelative("_effectKind").enumValueIndex = (int)kind;
        effectProp.FindPropertyRelative("_value").intValue = _value;
        effectProp.FindPropertyRelative("_duration").floatValue = _duration;
        effectProp.FindPropertyRelative("_interval").floatValue = _interval;
        effectProp.FindPropertyRelative("_maxStacks").intValue = _maxStacks;
        effectProp.FindPropertyRelative("_summonedUnit").objectReferenceValue = _summonedUnit;
        effectProp.FindPropertyRelative("_statusDefinition").objectReferenceValue = _statusDefinition;
        effectProp.FindPropertyRelative("_summonAnchorMode").enumValueIndex = (int)_summonAnchorMode;

        // Modifiers setup
        if (_modifier != LabModifierKind.None)
        {
            SerializedProperty modifierKindsProp = skillSO.FindProperty("_compositionModifierKinds");
            modifierKindsProp.arraySize = 1;
            
            SkillModifierKind modKind = SkillModifierKind.Splash;
            switch (_modifier)
            {
                case LabModifierKind.Splash: modKind = SkillModifierKind.Splash; break;
                case LabModifierKind.Penetrating: modKind = SkillModifierKind.Penetrating; break;
                case LabModifierKind.Bounce: modKind = SkillModifierKind.Bounce; break;
                case LabModifierKind.Explosive: modKind = SkillModifierKind.Explosive; break;
                case LabModifierKind.Persistent: modKind = SkillModifierKind.Persistent; break;
                case LabModifierKind.Periodic: modKind = SkillModifierKind.Periodic; break;
            }
            modifierKindsProp.GetArrayElementAtIndex(0).enumValueIndex = (int)modKind;

            SerializedProperty modifierDataProp = skillSO.FindProperty("_compositionModifierData");
            modifierDataProp.arraySize = 1;
            SerializedProperty modDataProp = modifierDataProp.GetArrayElementAtIndex(0);
            modDataProp.FindPropertyRelative("_modifierKind").enumValueIndex = (int)modKind;
            modDataProp.FindPropertyRelative("_bounceMaxBounces").intValue = _bounceMaxBounces;
            modDataProp.FindPropertyRelative("_bounceRangeInCells").intValue = _bounceRangeInCells;
            modDataProp.FindPropertyRelative("_bounceCanBounceToPrimaryTargetAgain").boolValue = _bounceCanBounceToPrimaryTargetAgain;
            modDataProp.FindPropertyRelative("_explosiveRadiusInCells").intValue = _explosiveRadiusInCells;
            modDataProp.FindPropertyRelative("_explosiveIncludePrimaryImpactTarget").boolValue = _explosiveIncludePrimaryImpactTarget;
        }
        else
        {
            skillSO.FindProperty("_compositionModifierKinds").arraySize = 0;
            skillSO.FindProperty("_compositionModifierData").arraySize = 0;
        }

        skillSO.FindProperty("_compositionState").enumValueIndex = (int)state;

        // Parameters
        skillSO.FindProperty("_rangeInCells").intValue = _rangeInCells;
        skillSO.FindProperty("_radiusInCells").intValue = _radiusInCells;
        skillSO.FindProperty("_lineLengthInCells").intValue = _lineLengthInCells;
        skillSO.FindProperty("_maxTargets").intValue = 10;

        skillSO.ApplyModifiedProperties();
    }

    private void ConfigureUnitProperties(UnitData unit, SkillData skill, GameObject prefab, UnitData template)
    {
        SerializedObject unitSO = new SerializedObject(unit);
        unitSO.FindProperty("unitPrefab").objectReferenceValue = prefab;
        unitSO.FindProperty("team").enumValueIndex = (int)_team;
        unitSO.FindProperty("role").enumValueIndex = (int)_role;
        unitSO.FindProperty("faction").enumValueIndex = (int)_faction;
        unitSO.FindProperty("combatStyle").enumValueIndex = (int)template.combatStyle;
        unitSO.FindProperty("targetingMode").enumValueIndex = (int)template.targetingMode;
        unitSO.FindProperty("sprite").objectReferenceValue = template.sprite;
        unitSO.FindProperty("tileSize").intValue = template.tileSize;

        // Copy stats
        SerializedProperty statsProp = unitSO.FindProperty("stats");
        statsProp.FindPropertyRelative("maxHealth").intValue = template.stats.maxHealth;
        statsProp.FindPropertyRelative("attackDamage").intValue = template.stats.attackDamage;
        statsProp.FindPropertyRelative("attackCooldown").floatValue = template.stats.attackCooldown;
        statsProp.FindPropertyRelative("attackRangeInCells").intValue = template.stats.attackRangeInCells;
        statsProp.FindPropertyRelative("preferredDistanceInCells").intValue = template.stats.preferredDistanceInCells;
        statsProp.FindPropertyRelative("accuracy").floatValue = template.stats.accuracy;
        statsProp.FindPropertyRelative("evasion").floatValue = template.stats.evasion;
        statsProp.FindPropertyRelative("defense").intValue = template.stats.defense;
        statsProp.FindPropertyRelative("moveSpeed").floatValue = template.stats.moveSpeed;
        statsProp.FindPropertyRelative("visionRange").floatValue = template.stats.visionRange;

        unitSO.FindProperty("unitId").stringValue = $"{template.unitId}_{skill.name}";
        unitSO.FindProperty("displayName").stringValue = $"{template.displayName} ({skill.DisplayName})";
        unitSO.FindProperty("skill").objectReferenceValue = skill;

        unitSO.ApplyModifiedProperties();
    }

    private void ClearLabTempAssets()
    {
        string tempFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/_LabTemp";
        if (AssetDatabase.IsValidFolder(tempFolder))
        {
            AssetDatabase.DeleteAsset(tempFolder);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Creature Variant Lab] Cleanup: Temporary lab assets deleted.");
            EditorUtility.DisplayDialog("Cleanup Complete", "Temporales de laboratorio limpiados con éxito.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Cleanup", "No hay temporales que limpiar.", "OK");
        }
    }

    // ==========================================
    // SPAWNING & TESTING LOGIC
    // ==========================================
    private void SpawnTestUnit(UnitTeam team)
    {
        ValidateComposition();
        if (_validationErrors.Count > 0)
        {
            EditorUtility.DisplayDialog("No se puede spawnear", "Corrige los errores bloqueantes antes de spawnear.", "OK");
            return;
        }

        var debugTool = FindFirstObjectByType<CreatureCombatDebugTool>();
        if (debugTool == null)
        {
            EditorUtility.DisplayDialog("Error de Escena", "No se encontró CreatureCombatDebugTool en la escena. Abre 'TestMapScene' y asegúrate de que esté configurada.", "OK");
            return;
        }

        RoomGrid grid = debugTool.ContextGrid;
        RoomContext ctx = debugTool.ContextRoom;
        if (grid == null || ctx == null)
        {
            EditorUtility.DisplayDialog("Error de Configuración", "Grid o Context no inicializados en el CreatureCombatDebugTool.", "OK");
            return;
        }

        // 1. Create temporary assets on disk
        string tempFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/_LabTemp";
        CreateAssetDirectories(tempFolder + "/dummy.asset");

        string skillPath = $"{tempFolder}/LabTemp_Skill.asset";
        string unitPath = $"{tempFolder}/LabTemp_UnitData.asset";

        AssetDatabase.DeleteAsset(skillPath);
        AssetDatabase.DeleteAsset(unitPath);

        SkillData tempSkill = ScriptableObject.CreateInstance<SkillData>();
        ConfigureSkillProperties(tempSkill, "LabTemp_Skill", $"LabTemp_{_role}_{_delivery}", SkillCompositionState.Provisional);
        AssetDatabase.CreateAsset(tempSkill, skillPath);

        UnitData tempUnit = ScriptableObject.CreateInstance<UnitData>();
        GameObject prefab = _prefabOverride != null ? _prefabOverride : GetDefaultPrefab(_faction, _role);
        UnitData template = _templateOverride != null ? _templateOverride : GetBaseTemplate(_faction, _role);
        ConfigureUnitProperties(tempUnit, tempSkill, prefab, template);
        AssetDatabase.CreateAsset(tempUnit, unitPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2. Resolve target cell
        Vector3Int cell = Vector3Int.zero;
        bool cellFound = false;
        for (int r = 0; r < 12 && !cellFound; r++)
        {
            for (int dx = -r; dx <= r && !cellFound; dx++)
            {
                for (int dy = -r; dy <= r && !cellFound; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                        continue;
                    Vector3Int candidate = new Vector3Int(dx, dy, 0);
                    if (grid.IsCellWalkable(candidate, null))
                    {
                        cell = candidate;
                        cellFound = true;
                    }
                }
            }
        }

        // 3. Instantiate and configure
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ctx.transform);
        instance.name = $"{_faction}_{_role}_LabSpawned";
        instance.transform.position = grid.CellToWorld(cell);

        Unit unit = instance.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogError("[Variant Lab] Prefab missing Unit component.");
            DestroyImmediate(instance);
            return;
        }

        // Setup components via reflection/SO
        var unitSO = new SerializedObject(unit);
        unitSO.FindProperty("_unitData").objectReferenceValue = tempUnit;
        unitSO.ApplyModifiedProperties();

        SkillCaster skillCaster = unit.GetComponent<SkillCaster>();
        if (skillCaster != null)
        {
            var casterSO = new SerializedObject(skillCaster);
            casterSO.FindProperty("_overrideSkill").objectReferenceValue = tempSkill;
            casterSO.ApplyModifiedProperties();

            // Clear cache
            typeof(SkillCaster).GetField("_resolvedSkill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(skillCaster, null);
        }

        typeof(Creature).GetMethod("Initialize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(unit, new object[] { tempUnit });

        unit.SetAffiliation(team, unit.Faction);

        RecruitableUnitState lifeState = instance.GetComponent<RecruitableUnitState>();
        if (lifeState != null && lifeState.CurrentState != UnitLifecycleState.Alive)
            lifeState.SetState(UnitLifecycleState.Alive);

        UnitMovement movement = instance.GetComponent<UnitMovement>();
        if (movement != null)
        {
            movement.SetGrid(grid);
            movement.AttachToGridAtCell(grid, cell);
        }

        ctx.RegisterUnit(unit);

        // Add to debug tool's list
        var spawnedList = (List<Unit>)typeof(CreatureCombatDebugTool)
            .GetField("_spawnedUnits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(debugTool);
        spawnedList?.Add(unit);

        // Selection
        UnityEditor.Selection.activeObject = instance;
        EditorGUIUtility.PingObject(instance);

        Debug.Log($"[Creature Variant Lab] Spawned test unit '{instance.name}' ({team}) at cell ({cell.x}, {cell.y}).");
    }

    private void SpawnDummy()
    {
        var debugTool = FindFirstObjectByType<CreatureCombatDebugTool>();
        if (debugTool == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró CreatureCombatDebugTool.", "OK");
            return;
        }

        RoomGrid grid = debugTool.ContextGrid;
        RoomContext ctx = debugTool.ContextRoom;
        if (grid == null || ctx == null) return;

        var dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Combat/Prefabs/Debug/Targets/Debug_Target_Enemy.prefab");
        if (dummyPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "No se pudo cargar: Assets/Combat/Prefabs/Debug/Targets/Debug_Target_Enemy.prefab", "OK");
            return;
        }

        Vector3Int cell = Vector3Int.zero;
        bool cellFound = false;
        for (int r = 1; r < 12 && !cellFound; r++)
        {
            for (int dx = -r; dx <= r && !cellFound; dx++)
            {
                for (int dy = -r; dy <= r && !cellFound; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                        continue;
                    Vector3Int candidate = new Vector3Int(dx, dy, 0);
                    if (grid.IsCellWalkable(candidate, null))
                    {
                        cell = candidate;
                        cellFound = true;
                    }
                }
            }
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab, ctx.transform);
        instance.name = "Debug_Target_Enemy_Spawned";
        instance.transform.position = grid.CellToWorld(cell);

        Unit unit = instance.GetComponent<Unit>();
        if (unit != null)
        {
            unit.SetAffiliation(UnitTeam.Enemy, UnitFaction.None);
            RecruitableUnitState lifeState = instance.GetComponent<RecruitableUnitState>();
            if (lifeState != null && lifeState.CurrentState != UnitLifecycleState.Alive)
                lifeState.SetState(UnitLifecycleState.Alive);

            UnitMovement movement = instance.GetComponent<UnitMovement>();
            if (movement != null)
            {
                movement.SetGrid(grid);
                movement.AttachToGridAtCell(grid, cell);
            }

            ctx.RegisterUnit(unit);

            var spawnedList = (List<Unit>)typeof(CreatureCombatDebugTool)
                .GetField("_spawnedUnits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(debugTool);
            spawnedList?.Add(unit);

            UnityEditor.Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log($"[Creature Variant Lab] Spawned opponent dummy at cell ({cell.x}, {cell.y}).");
        }
    }

    private void ClearSpawnedUnits()
    {
        var debugTool = FindFirstObjectByType<CreatureCombatDebugTool>();
        if (debugTool != null)
        {
            debugTool.ClearSpawned();
        }
    }

    private void SelectLastSpawnedUnit()
    {
        var debugTool = FindFirstObjectByType<CreatureCombatDebugTool>();
        if (debugTool != null && debugTool.SpawnedUnits.Count > 0)
        {
            Unit last = debugTool.SpawnedUnits[debugTool.SpawnedUnits.Count - 1];
            if (last != null)
            {
                UnityEditor.Selection.activeObject = last.gameObject;
                EditorGUIUtility.PingObject(last.gameObject);
            }
        }
    }

    private void LogRuntimeState()
    {
        var debugTool = FindFirstObjectByType<CreatureCombatDebugTool>();
        if (debugTool != null)
        {
            debugTool.LogCurrentState();
        }
    }

    // ==========================================
    // SAVE LOGIC & COMPARISON
    // ==========================================
    private UnitData FindMatchingVariant()
    {
        string[] guids = AssetDatabase.FindAssets("t:UnitData", new[] { "Assets/Core/Data/Scriptable Objects/Creatures/Generated" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Ignore temporary lab assets
            if (path.Contains("/_LabTemp/")) continue;

            UnitData ud = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (ud == null) continue;

            if (ud.faction != _faction || ud.role != _role) continue;
            if (ud.skill == null) continue;

            SkillData sd = ud.skill;
            if (sd.ImpactPattern != _delivery) continue;

            // Compare effects
            if (sd.CompositionEffects == null || sd.CompositionEffects.Length != 1) continue;
            var eff = sd.CompositionEffects[0];
            
            SkillEffectKind expectedKind = SkillEffectKind.Damage;
            switch (_effect)
            {
                case LabEffectKind.Damage: expectedKind = SkillEffectKind.Damage; break;
                case LabEffectKind.Heal: expectedKind = SkillEffectKind.Heal; break;
                case LabEffectKind.Shield: expectedKind = SkillEffectKind.Shield; break;
                case LabEffectKind.Stun: expectedKind = SkillEffectKind.Stun; break;
                case LabEffectKind.Slow: expectedKind = SkillEffectKind.Slow; break;
                case LabEffectKind.PoisonBurn: expectedKind = SkillEffectKind.PoisonBurn; break;
                case LabEffectKind.Haste: expectedKind = SkillEffectKind.Haste; break;
                case LabEffectKind.StrengthBuff: expectedKind = SkillEffectKind.StrengthBuff; break;
                case LabEffectKind.HealOverTime: expectedKind = SkillEffectKind.Heal; break;
                case LabEffectKind.Summon: expectedKind = SkillEffectKind.Summon; break;
                case LabEffectKind.Knockback: expectedKind = SkillEffectKind.Knockback; break;
            }

            if (eff.EffectKind != expectedKind) continue;
            if (eff.Value != _value) continue;
            if (!Mathf.Approximately(eff.Duration, _duration)) continue;
            if (!Mathf.Approximately(eff.Interval, _interval)) continue;
            if (eff.StatusDefinition != _statusDefinition) continue;
            if (eff.SummonedUnit != _summonedUnit) continue;
            if (eff.SummonAnchorMode != _summonAnchorMode) continue;

            // Compare modifiers
            if (_modifier == LabModifierKind.None)
            {
                if (sd.CompositionModifierKinds != null && sd.CompositionModifierKinds.Length > 0) continue;
            }
            else
            {
                if (sd.CompositionModifierKinds == null || sd.CompositionModifierKinds.Length != 1) continue;
                
                SkillModifierKind expectedMod = SkillModifierKind.Splash;
                switch (_modifier)
                {
                    case LabModifierKind.Splash: expectedMod = SkillModifierKind.Splash; break;
                    case LabModifierKind.Penetrating: expectedMod = SkillModifierKind.Penetrating; break;
                    case LabModifierKind.Bounce: expectedMod = SkillModifierKind.Bounce; break;
                    case LabModifierKind.Explosive: expectedMod = SkillModifierKind.Explosive; break;
                    case LabModifierKind.Persistent: expectedMod = SkillModifierKind.Persistent; break;
                    case LabModifierKind.Periodic: expectedMod = SkillModifierKind.Periodic; break;
                }

                if (sd.CompositionModifierKinds[0] != expectedMod) continue;

                if (expectedMod == SkillModifierKind.Bounce || expectedMod == SkillModifierKind.Explosive)
                {
                    if (sd.CompositionModifierData == null || sd.CompositionModifierData.Length == 0) continue;
                    bool modDataMatch = false;
                    foreach (var md in sd.CompositionModifierData)
                    {
                        if (md.ModifierKind == expectedMod)
                        {
                            if (expectedMod == SkillModifierKind.Bounce)
                            {
                                if (md.BounceMaxBounces == _bounceMaxBounces &&
                                    md.BounceRangeInCells == _bounceRangeInCells &&
                                    md.BounceCanBounceToPrimaryTargetAgain == _bounceCanBounceToPrimaryTargetAgain)
                                {
                                    modDataMatch = true;
                                }
                            }
                            else if (expectedMod == SkillModifierKind.Explosive)
                            {
                                if (md.ExplosiveRadiusInCells == _explosiveRadiusInCells &&
                                    md.ExplosiveIncludePrimaryImpactTarget == _explosiveIncludePrimaryImpactTarget)
                                {
                                    modDataMatch = true;
                                }
                            }
                        }
                    }
                    if (!modDataMatch) continue;
                }
            }

            return ud;
        }
        return null;
    }

    private void SaveAsGeneratedVariant(string skillNameWithoutPrefix)
    {
        if (string.IsNullOrEmpty(skillNameWithoutPrefix))
        {
            EditorUtility.DisplayDialog("Error al Guardar", "El nombre de la habilidad no puede estar vacío.", "OK");
            return;
        }

        // Paths
        string skillFolder = $"Assets/Core/Data/Scriptable Objects/Combat/Skills/Role_{_role}";
        string variantFolder = $"Assets/Core/Data/Scriptable Objects/Creatures/Generated/{_faction}/{_role}";

        string skillAssetName = $"{_role}_{skillNameWithoutPrefix}";
        string variantAssetName = $"{_faction}_{_role}_{skillNameWithoutPrefix}";

        string skillPath = $"{skillFolder}/{skillAssetName}.asset";
        string variantPath = $"{variantFolder}/{variantAssetName}.asset";

        // Confirm overwrite
        if (AssetDatabase.LoadAssetAtPath<UnitData>(variantPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Sobrescribir Variante", $"La variante '{variantAssetName}' ya existe.\n\n¿Deseas sobrescribirla?", "Sí, Sobrescribir", "Cancelar"))
            {
                return;
            }
        }

        // Create directory structures
        CreateAssetDirectories(skillPath);
        CreateAssetDirectories(variantPath);

        // 1. Create and Save SkillData
        SkillData finalSkill = AssetDatabase.LoadAssetAtPath<SkillData>(skillPath);
        bool isNewSkill = (finalSkill == null);
        if (isNewSkill)
        {
            finalSkill = ScriptableObject.CreateInstance<SkillData>();
        }
        ConfigureSkillProperties(finalSkill, skillAssetName, skillNameWithoutPrefix, SkillCompositionState.Official);
        
        if (isNewSkill)
        {
            AssetDatabase.CreateAsset(finalSkill, skillPath);
        }
        else
        {
            EditorUtility.SetDirty(finalSkill);
        }

        // 2. Create and Save UnitData
        UnitData finalUnit = AssetDatabase.LoadAssetAtPath<UnitData>(variantPath);
        bool isNewUnit = (finalUnit == null);
        if (isNewUnit)
        {
            finalUnit = ScriptableObject.CreateInstance<UnitData>();
        }

        GameObject prefab = _prefabOverride != null ? _prefabOverride : GetDefaultPrefab(_faction, _role);
        UnitData template = _templateOverride != null ? _templateOverride : GetBaseTemplate(_faction, _role);
        ConfigureUnitProperties(finalUnit, finalSkill, prefab, template);

        if (isNewUnit)
        {
            AssetDatabase.CreateAsset(finalUnit, variantPath);
        }
        else
        {
            EditorUtility.SetDirty(finalUnit);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Creature Variant Lab] Saved variant: '{variantAssetName}' and skill: '{skillAssetName}'.");
        EditorUtility.DisplayDialog("Guardado Completado", $"Variante guardada con éxito:\n\nUnidad: {variantPath}\nHabilidad: {skillPath}", "OK");
    }
}
