using System.Collections.Generic;
using Core.Systems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel shown when the player interacts with the Dungeon Entrance and has at least one
/// unlocked shortcut. Dynamically builds one button per unlocked floor plus a "Floor 1"
/// normal-start button, then loads the dungeon when the player picks a floor.
/// </summary>
[DisallowMultipleComponent]
public class ShortcutFloorSelectUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent transform where floor buttons will be spawned.")]
    [SerializeField] private Transform _buttonContainer;

    [Tooltip("Prefab used for each floor button. Must have a Button and a TMP_Text child.")]
    [SerializeField] private GameObject _buttonPrefab;

    [Tooltip("Assign a 'Close' button in the panel if you want an explicit cancel option.")]
    [SerializeField] private Button _closeButton;

    [Header("Labels")]
    [SerializeField] private string _floor1Label = "Floor 1  —  Normal Run";
    [SerializeField] private string _shortcutLabelFormat = "Floor {0}  —  Shortcut";

    // ─── Runtime state ────────────────────────────────────────────────────

    private readonly List<GameObject> _spawnedButtons = new();

    // ─── Unity lifecycle ──────────────────────────────────────────────────

    private void Awake()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        ShortcutProgressService.OnShortcutsChanged += RefreshButtons;
        RefreshButtons();
    }

    private void OnDisable()
    {
        ShortcutProgressService.OnShortcutsChanged -= RefreshButtons;
    }

    // ─── Public API ───────────────────────────────────────────────────────

    /// <summary>Shows the panel and rebuilds the floor list.</summary>
    public void Open()
    {
        gameObject.SetActive(true);
        RefreshButtons();
    }

    /// <summary>Hides the panel without loading anything.</summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }

    // ─── Private helpers ──────────────────────────────────────────────────

    private void RefreshButtons()
    {
        ClearButtons();

        // Always include Floor 1 as the normal-run option.
        SpawnButton(1, _floor1Label);

        // Add one button per unlocked shortcut (these are already sorted multiples of 5).
        if (ShortcutProgressService.Instance != null)
        {
            IReadOnlyList<int> shortcuts = ShortcutProgressService.Instance.UnlockedShortcuts;
            foreach (int floor in shortcuts)
            {
                string label = string.Format(_shortcutLabelFormat, floor);
                SpawnButton(floor, label);
            }
        }
    }

    private void SpawnButton(int floor, string label)
    {
        if (_buttonPrefab == null || _buttonContainer == null)
        {
            Debug.LogWarning($"[{nameof(ShortcutFloorSelectUI)}] Missing buttonPrefab or buttonContainer reference.", this);
            return;
        }

        GameObject instance = Instantiate(_buttonPrefab, _buttonContainer);
        _spawnedButtons.Add(instance);

        // Set the label text — supports both TMP_Text and legacy Text.
        TMP_Text tmp = instance.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = label;
        }
        else
        {
            Text legacyText = instance.GetComponentInChildren<Text>();
            if (legacyText != null)
                legacyText.text = label;
        }

        // Wire up the click.
        Button btn = instance.GetComponent<Button>() ?? instance.GetComponentInChildren<Button>();
        if (btn != null)
        {
            int capturedFloor = floor; // closure capture
            btn.onClick.AddListener(() => OnFloorSelected(capturedFloor));
        }
    }

    private void ClearButtons()
    {
        foreach (GameObject btn in _spawnedButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        _spawnedButtons.Clear();
    }

    private void OnFloorSelected(int floor)
    {
        Close();

        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadDungeon(floor);
        }
        else
        {
            Debug.LogWarning($"[{nameof(ShortcutFloorSelectUI)}] GameSceneManager not found.", this);
        }
    }
}
