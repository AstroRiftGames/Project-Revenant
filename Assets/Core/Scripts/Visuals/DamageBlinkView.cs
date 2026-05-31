using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitVisualMaterialController))]
public class DamageBlinkView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _flashDuration = 0.08f;

    private LifeController _lifeController;
    private UnitVisualMaterialController _visualMaterialController;
    private Coroutine _blinkCoroutine;
    private bool _hasLoggedMissingVisualMaterialController;

    private void Awake()
    {
        _lifeController = GetComponent<LifeController>();
        _visualMaterialController = ResolveVisualMaterialController();
    }

    private UnitVisualMaterialController ResolveVisualMaterialController()
    {
        if (TryGetComponent(out UnitVisualMaterialController visualMaterialController))
            return visualMaterialController;

        if (_hasLoggedMissingVisualMaterialController)
            return null;

        Debug.LogWarning(
            $"[{nameof(DamageBlinkView)}] Missing visual authoring component '{nameof(UnitVisualMaterialController)}' on '{name}'. " +
            $"Object path: '{BuildHierarchyPath(transform)}'. Scene: '{ResolveScenePath()}'. " +
            "Damage blink feedback may degrade. Add it to the prefab root manually.",
            this);
        _hasLoggedMissingVisualMaterialController = true;
        return null;
    }

    private string ResolveScenePath()
    {
        var scene = gameObject.scene;
        if (!scene.IsValid())
            return "<invalid scene>";

        if (!string.IsNullOrWhiteSpace(scene.path))
            return scene.path;

        return !string.IsNullOrWhiteSpace(scene.name) ? scene.name : "<runtime scene>";
    }

    private static string BuildHierarchyPath(Transform target)
    {
        if (target == null)
            return "<missing transform>";

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }

    private void OnEnable()
    {
        if (_lifeController != null)
        {
            _lifeController.OnDamageTaken += HandleDamageTaken;
        }
    }

    private void OnDisable()
    {
        if (_lifeController != null)
        {
            _lifeController.OnDamageTaken -= HandleDamageTaken;
        }

        StopBlink();
        _visualMaterialController?.SetBlinkOverride(false);
    }

    private void HandleDamageTaken(int amount)
    {
        StopBlink();
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlink()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
    }

    private IEnumerator BlinkRoutine()
    {
        _visualMaterialController?.SetBlinkOverride(true);

        yield return new WaitForSeconds(_flashDuration);

        _visualMaterialController?.SetBlinkOverride(false);
        _blinkCoroutine = null;
    }
}
