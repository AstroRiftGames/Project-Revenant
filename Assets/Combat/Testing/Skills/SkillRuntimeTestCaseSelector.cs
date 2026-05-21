using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillRuntimeTestCaseSelector : MonoBehaviour
{
    [SerializeField] private List<SkillRuntimeTestHarness> _testCases = new();
    [SerializeField] private int _activeIndex;
    [SerializeField] private KeyCode _nextCaseKey = KeyCode.Y;
    [SerializeField] private KeyCode _castKey = KeyCode.T;

    private void OnEnable()
    {
        EnsureCases();
        ClampActiveIndex();
        LogActiveCase();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (Input.GetKeyDown(_nextCaseKey))
        {
            SelectNextCase();
            return;
        }

        if (Input.GetKeyDown(_castKey))
            CastActiveCase();
    }

    private void SelectNextCase()
    {
        int count = CountValidCases();
        if (count == 0)
            return;

        _activeIndex++;
        if (_activeIndex >= count)
            _activeIndex = 0;

        LogActiveCase();
    }

    private void CastActiveCase()
    {
        SkillRuntimeTestHarness activeHarness = GetActiveHarness();
        if (activeHarness == null)
        {
            Debug.LogWarning("[SkillRuntimeTestCaseSelector] No active test case is configured.", this);
            return;
        }

        activeHarness.TriggerCastForDebug();
    }

    private SkillRuntimeTestHarness GetActiveHarness()
    {
        ClampActiveIndex();

        int validIndex = 0;
        for (int i = 0; i < _testCases.Count; i++)
        {
            SkillRuntimeTestHarness candidate = _testCases[i];
            if (candidate == null)
                continue;

            if (validIndex == _activeIndex)
                return candidate;

            validIndex++;
        }

        return null;
    }

    private int CountValidCases()
    {
        EnsureCases();

        int count = 0;
        for (int i = 0; i < _testCases.Count; i++)
        {
            if (_testCases[i] != null)
                count++;
        }

        return count;
    }

    private void ClampActiveIndex()
    {
        int count = CountValidCases();
        if (count <= 0)
        {
            _activeIndex = 0;
            return;
        }

        if (_activeIndex < 0 || _activeIndex >= count)
            _activeIndex = 0;
    }

    private void LogActiveCase()
    {
        SkillRuntimeTestHarness activeHarness = GetActiveHarness();
        if (activeHarness == null)
        {
            Debug.LogWarning("[SkillRuntimeTestCaseSelector] No test cases available.", this);
            return;
        }

        Debug.Log($"[SkillRuntimeTestCaseSelector] Active case: {activeHarness.name}", this);
    }

    private void EnsureCases()
    {
        if (_testCases.Count > 0)
            return;

        _testCases.Clear();
        GetComponentsInChildren(includeInactive: true, _testCases);

        if (_testCases.Count > 0)
            return;

        SkillRuntimeTestHarness[] sceneCases =
            FindObjectsByType<SkillRuntimeTestHarness>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < sceneCases.Length; i++)
        {
            SkillRuntimeTestHarness sceneCase = sceneCases[i];
            if (sceneCase == null)
                continue;

            _testCases.Add(sceneCase);
        }

        _testCases.Sort(CompareHarnessesByName);
    }

    private static int CompareHarnessesByName(SkillRuntimeTestHarness left, SkillRuntimeTestHarness right)
    {
        if (ReferenceEquals(left, right))
            return 0;

        if (left == null)
            return 1;

        if (right == null)
            return -1;

        return string.CompareOrdinal(left.name, right.name);
    }
}
