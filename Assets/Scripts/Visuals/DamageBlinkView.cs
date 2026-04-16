using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DamageBlinkView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _flashDuration = 0.08f;
    [SerializeField] private string _flashPropertyName = "_FlashAmount";

    private int _flashPropertyId;
    private LifeController _lifeController;
    private SpriteRenderer[] _spriteRenderers;
    private MaterialPropertyBlock _propertyBlock;
    private Coroutine _blinkCoroutine;

    private void Awake()
    {
        _flashPropertyId = Shader.PropertyToID(_flashPropertyName);
        _lifeController = GetComponent<LifeController>();
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        _propertyBlock = new MaterialPropertyBlock();
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
        SetFlashAmount(0f);
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
        SetFlashAmount(1f);

        yield return new WaitForSeconds(_flashDuration);

        SetFlashAmount(0f);
        _blinkCoroutine = null;
    }

    private void SetFlashAmount(float amount)
    {
        if (_spriteRenderers == null) return;

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = _spriteRenderers[i];
            if (renderer == null) continue;

            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(_flashPropertyId, amount);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
