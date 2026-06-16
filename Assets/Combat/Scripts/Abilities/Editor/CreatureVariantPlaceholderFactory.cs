using UnityEditor;
using UnityEngine;

public enum PlaceholderVisualKind
{
    Ally,
    Enemy,
    Dummy
}

public static class CreatureVariantPlaceholderFactory
{
    private const int SpriteSize = 64;
    private const string SortingLayerName = "Characters";

    private static Sprite s_allySprite;
    private static Sprite s_enemySprite;
    private static Sprite s_dummySprite;
    private static Texture2D s_allyTexture;
    private static Texture2D s_enemyTexture;
    private static Texture2D s_dummyTexture;
    private static Material s_placeholderOverlayMaterial;
    private static bool s_sortingLayerChecked;
    private static string s_resolvedSortingLayer;

    public static GameObject CreatePlaceholderUnit(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.SetActive(false);
        if (parent != null) go.transform.SetParent(parent, false);
        AddRequiredComponents(go);
        AddVisual(go, PlaceholderVisualKind.Ally);
        SetupVisualController(go);
        go.SetActive(true);
        return go;
    }

    public static GameObject CreatePlaceholderUnit(string name, Transform parent, PlaceholderVisualKind kind)
    {
        var go = new GameObject(name);
        go.SetActive(false);
        if (parent != null) go.transform.SetParent(parent, false);
        AddRequiredComponents(go);
        AddVisual(go, kind);
        SetupVisualController(go);
        go.SetActive(true);
        return go;
    }

    public static GameObject CreatePlaceholderDummy(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.SetActive(false);
        if (parent != null) go.transform.SetParent(parent, false);
        AddRequiredComponents(go);
        AddVisual(go, PlaceholderVisualKind.Dummy);
        SetupVisualController(go);
        go.SetActive(true);
        return go;
    }

    public static UnitData CreateDummyUnitData()
    {
        UnitData data = ScriptableObject.CreateInstance<UnitData>();
        var so = new SerializedObject(data);
        so.FindProperty("unitId").stringValue = "LabDummy";
        so.FindProperty("displayName").stringValue = "Lab Dummy Target";
        so.FindProperty("team").enumValueIndex = (int)UnitTeam.Enemy;
        so.FindProperty("faction").enumValueIndex = (int)UnitFaction.None;
        so.FindProperty("role").enumValueIndex = (int)UnitRole.DPS;
        so.FindProperty("combatStyle").enumValueIndex = (int)UnitCombatStyle.Melee;
        so.FindProperty("targetingMode").enumValueIndex = (int)UnitTargetingMode.RolePriority;
        var stats = so.FindProperty("stats");
        stats.FindPropertyRelative("maxHealth").intValue = 100;
        stats.FindPropertyRelative("attackDamage").intValue = 0;
        stats.FindPropertyRelative("attackCooldown").floatValue = 2f;
        stats.FindPropertyRelative("attackRangeInCells").intValue = 1;
        stats.FindPropertyRelative("preferredDistanceInCells").intValue = 1;
        stats.FindPropertyRelative("moveSpeed").floatValue = 2f;
        stats.FindPropertyRelative("visionRange").floatValue = 5f;
        so.FindProperty("manaCostToRecruit").intValue = 0;
        so.FindProperty("manaCostToAbsorbSoul").intValue = 0;
        so.ApplyModifiedProperties();
        return data;
    }

    private static void AddRequiredComponents(GameObject go)
    {
        go.AddComponent<Unit>();
        go.AddComponent<SkillCaster>();
    }

    private static void AddVisual(GameObject go, PlaceholderVisualKind kind)
    {
        string layer = ResolveSortingLayer();
        Sprite sprite = GetOrCreatePlaceholderSprite(kind);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingLayerName = layer;
        sr.sortingOrder = 0;

        var overlay = new GameObject("VisualOverlay");
        overlay.transform.SetParent(go.transform, false);
        var overlaySr = overlay.AddComponent<SpriteRenderer>();
        overlaySr.sprite = sprite;
        overlaySr.color = new Color(1f, 1f, 1f, 0.4f);
        overlaySr.sortingLayerName = layer;
        overlaySr.sortingOrder = 1;
    }

    private static void SetupVisualController(GameObject go)
    {
        var vmc = go.GetComponent<UnitVisualMaterialController>();
        if (vmc == null)
            return;

        AssignOverlayMaterial(go);
    }

    private static void AssignOverlayMaterial(GameObject go)
    {
        var vmc = go.GetComponent<UnitVisualMaterialController>();
        if (vmc == null)
            return;

        var so = new SerializedObject(vmc);
        so.FindProperty("_unitVisualMaterial").objectReferenceValue = GetPlaceholderOverlayMaterial();
        so.ApplyModifiedProperties();
    }

    private static Material GetPlaceholderOverlayMaterial()
    {
        if (s_placeholderOverlayMaterial != null)
            return s_placeholderOverlayMaterial;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
            s_placeholderOverlayMaterial = new Material(shader);
        else
            s_placeholderOverlayMaterial = new Material(Shader.Find("Unlit/Texture"));

        s_placeholderOverlayMaterial.name = "LabPlaceholderOverlay";
        return s_placeholderOverlayMaterial;
    }

    private static string ResolveSortingLayer()
    {
        if (s_sortingLayerChecked)
            return s_resolvedSortingLayer;

        s_sortingLayerChecked = true;
        foreach (var layer in SortingLayer.layers)
        {
            if (layer.name == SortingLayerName)
            {
                s_resolvedSortingLayer = SortingLayerName;
                return s_resolvedSortingLayer;
            }
        }

        s_resolvedSortingLayer = "Default";
        return s_resolvedSortingLayer;
    }

    private static Sprite GetOrCreatePlaceholderSprite(PlaceholderVisualKind kind)
    {
        return kind switch
        {
            PlaceholderVisualKind.Ally => GetOrCreateKindSprite(ref s_allySprite, ref s_allyTexture, PlaceholderVisualKind.Ally),
            PlaceholderVisualKind.Enemy => GetOrCreateKindSprite(ref s_enemySprite, ref s_enemyTexture, PlaceholderVisualKind.Enemy),
            PlaceholderVisualKind.Dummy => GetOrCreateKindSprite(ref s_dummySprite, ref s_dummyTexture, PlaceholderVisualKind.Dummy),
            _ => GetOrCreateKindSprite(ref s_allySprite, ref s_allyTexture, PlaceholderVisualKind.Ally),
        };
    }

    private static Sprite GetOrCreateKindSprite(ref Sprite cache, ref Texture2D texCache, PlaceholderVisualKind kind)
    {
        if (cache != null)
            return cache;

        texCache = CreateDiamondTexture(kind);
        cache = Sprite.Create(texCache, new Rect(0, 0, SpriteSize, SpriteSize), new Vector2(0.5f, 0.5f), SpriteSize);
        cache.name = $"LabPlaceholder_{kind}";
        return cache;
    }

    private static Texture2D CreateDiamondTexture(PlaceholderVisualKind kind)
    {
        var tex = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
        var pixels = new Color32[SpriteSize * SpriteSize];

        Color32 fill = kind switch
        {
            PlaceholderVisualKind.Ally => new Color32(70, 160, 220, 230),
            PlaceholderVisualKind.Enemy => new Color32(200, 60, 60, 230),
            PlaceholderVisualKind.Dummy => new Color32(230, 170, 40, 230),
            _ => new Color32(70, 160, 220, 230),
        };

        Color32 border = new Color32(30, 30, 30, 255);
        Color32 transparent = new Color32(0, 0, 0, 0);
        int half = SpriteSize / 2;
        int borderWidth = 2;

        for (int y = 0; y < SpriteSize; y++)
        {
            for (int x = 0; x < SpriteSize; x++)
            {
                int dx = x - half + 1;
                int dy = y - half + 1;

                int diamondDist = Mathf.Abs(dx) + Mathf.Abs(dy);
                int innerHalf = half - 2;

                if (diamondDist > innerHalf + borderWidth)
                {
                    pixels[y * SpriteSize + x] = transparent;
                }
                else if (diamondDist >= innerHalf)
                {
                    pixels[y * SpriteSize + x] = border;
                }
                else
                {
                    pixels[y * SpriteSize + x] = fill;
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }
}