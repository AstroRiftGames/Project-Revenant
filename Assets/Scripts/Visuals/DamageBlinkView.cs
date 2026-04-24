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

    private void Awake()
    {
        _lifeController = GetComponent<LifeController>();
        _visualMaterialController = GetComponent<UnitVisualMaterialController>() ?? gameObject.AddComponent<UnitVisualMaterialController>();
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
