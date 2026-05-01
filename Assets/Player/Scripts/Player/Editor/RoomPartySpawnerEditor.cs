using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomPartySpawner))]
public class RoomPartySpawnerEditor : Editor
{
    private const float PreviewHeight = 220f;
    private const float PreviewPadding = 16f;

    private SerializedProperty _floorManagerProperty;
    private SerializedProperty _partyProperty;
    private SerializedProperty _temporaryFormationProperty;
    private SerializedProperty _slotOffsetsProperty;

    private void OnEnable()
    {
        _floorManagerProperty = serializedObject.FindProperty("_floorManager");
        _partyProperty = serializedObject.FindProperty("_party");
        _temporaryFormationProperty = serializedObject.FindProperty("_temporaryFormation");
        _slotOffsetsProperty = _temporaryFormationProperty.FindPropertyRelative("_slotOffsets");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_floorManagerProperty);
        EditorGUILayout.PropertyField(_partyProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Temporary Formation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_temporaryFormationProperty, includeChildren: true);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset Pattern", GUILayout.Width(120f)))
            {
                ResetPatternToDefault();
            }
        }

        EditorGUILayout.Space();
        DrawFormationPreview();

        serializedObject.ApplyModifiedProperties();
    }

    private void ResetPatternToDefault()
    {
        serializedObject.ApplyModifiedProperties();

        foreach (Object targetObject in targets)
        {
            if (targetObject is not RoomPartySpawner spawner)
                continue;

            Undo.RecordObject(spawner, "Reset Temporary Formation");
            spawner.ResetTemporaryFormationToDefault();
            EditorUtility.SetDirty(spawner);
        }

        serializedObject.Update();
    }

    private void DrawFormationPreview()
    {
        EditorGUILayout.LabelField("Formation Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Forward points upward. The necromancer is anchored at (0,0), and each slot shows its assignment order.", MessageType.None);

        Rect previewRect = GUILayoutUtility.GetRect(0f, PreviewHeight, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(previewRect, new Color(0.12f, 0.12f, 0.12f));

        if (_slotOffsetsProperty == null)
            return;

        Rect contentRect = new(
            previewRect.x + PreviewPadding,
            previewRect.y + PreviewPadding,
            previewRect.width - (PreviewPadding * 2f),
            previewRect.height - (PreviewPadding * 2f));

        CalculateBounds(out int minX, out int maxX, out int minY, out int maxY);

        int columns = Mathf.Max(1, maxX - minX + 1);
        int rows = Mathf.Max(1, maxY - minY + 1);
        float cellSize = Mathf.Min(contentRect.width / columns, contentRect.height / rows);
        float gridWidth = cellSize * columns;
        float gridHeight = cellSize * rows;

        Rect gridRect = new(
            contentRect.x + ((contentRect.width - gridWidth) * 0.5f),
            contentRect.y + ((contentRect.height - gridHeight) * 0.5f),
            gridWidth,
            gridHeight);

        DrawGrid(gridRect, minX, maxX, minY, maxY, cellSize);
        DrawForwardIndicator(gridRect);
    }

    private void CalculateBounds(out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = 0;
        maxX = 0;
        minY = 0;
        maxY = 0;

        for (int i = 0; i < _slotOffsetsProperty.arraySize; i++)
        {
            SerializedProperty element = _slotOffsetsProperty.GetArrayElementAtIndex(i);
            Vector2Int offset = element.vector2IntValue;
            minX = Mathf.Min(minX, offset.x);
            maxX = Mathf.Max(maxX, offset.x);
            minY = Mathf.Min(minY, offset.y);
            maxY = Mathf.Max(maxY, offset.y);
        }

        minX -= 1;
        maxX += 1;
        minY -= 1;
        maxY += 1;
    }

    private void DrawGrid(Rect gridRect, int minX, int maxX, int minY, int maxY, float cellSize)
    {
        Color lineColor = new(1f, 1f, 1f, 0.14f);
        Color originColor = new(0.2f, 0.75f, 1f, 0.85f);
        Color slotColor = new(1f, 0.65f, 0.2f, 0.9f);
        GUIStyle centeredLabel = new(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        for (int y = maxY; y >= minY; y--)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Rect cellRect = GetCellRect(gridRect, minX, maxY, x, y, cellSize);
                EditorGUI.DrawRect(cellRect, new Color(1f, 1f, 1f, 0.035f));
                DrawCellOutline(cellRect, lineColor);
            }
        }

        Rect originRect = GetCellRect(gridRect, minX, maxY, 0, 0, cellSize);
        EditorGUI.DrawRect(originRect, originColor);
        GUI.Label(originRect, "N", centeredLabel);

        for (int i = 0; i < _slotOffsetsProperty.arraySize; i++)
        {
            Vector2Int offset = _slotOffsetsProperty.GetArrayElementAtIndex(i).vector2IntValue;
            Rect slotRect = GetCellRect(gridRect, minX, maxY, offset.x, offset.y, cellSize);
            EditorGUI.DrawRect(slotRect, slotColor);
            GUI.Label(slotRect, i.ToString(), centeredLabel);
        }
    }

    private static Rect GetCellRect(Rect gridRect, int minX, int maxY, int cellX, int cellY, float cellSize)
    {
        float x = gridRect.x + ((cellX - minX) * cellSize);
        float y = gridRect.y + ((maxY - cellY) * cellSize);
        return new Rect(x, y, cellSize, cellSize);
    }

    private static void DrawCellOutline(Rect rect, Color color)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
    }

    private static void DrawForwardIndicator(Rect gridRect)
    {
        Rect labelRect = new(gridRect.x, gridRect.y - 18f, gridRect.width, 16f);
        GUIStyle arrowStyle = new(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.9f, 1f) }
        };

        GUI.Label(labelRect, "Forward ↑", arrowStyle);
    }
}
