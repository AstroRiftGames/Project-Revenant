using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitVisualBumpView : MonoBehaviour
{
    [SerializeField] private Transform _visualRoot;

    private Coroutine _activeBump;
    private Vector3 _restingLocalPosition;
    private bool _hasRestingLocalPosition;

    public bool TryPlayBump(Vector3 worldDirection, float distanceWorld, float duration)
    {
        if (!isActiveAndEnabled || _visualRoot == null)
            return false;

        if (worldDirection.sqrMagnitude <= Mathf.Epsilon)
            return false;

        float resolvedDistanceWorld = Mathf.Max(0f, distanceWorld);
        if (resolvedDistanceWorld <= Mathf.Epsilon)
            return false;

        float resolvedDuration = Mathf.Max(0.01f, duration);

        Transform parent = _visualRoot.parent;
        Vector3 localDirection = parent != null
            ? parent.InverseTransformDirection(worldDirection)
            : worldDirection;
        localDirection.z = 0f;

        if (localDirection.sqrMagnitude <= Mathf.Epsilon)
            return false;

        localDirection.Normalize();

        CancelActiveBump(true);
        CacheRestingLocalPosition();

        _activeBump = StartCoroutine(PlayBumpRoutine(localDirection, resolvedDistanceWorld, resolvedDuration));
        return true;
    }

    private void OnDisable()
    {
        CancelActiveBump(true);
    }

    private void OnDestroy()
    {
        CancelActiveBump(true);
    }

    private void CacheRestingLocalPosition()
    {
        if (_visualRoot == null)
        {
            _hasRestingLocalPosition = false;
            return;
        }

        _restingLocalPosition = _visualRoot.localPosition;
        _hasRestingLocalPosition = true;
    }

    private void CancelActiveBump(bool restoreRestingPosition)
    {
        if (_activeBump != null)
        {
            StopCoroutine(_activeBump);
            _activeBump = null;
        }

        if (restoreRestingPosition && _visualRoot != null && _hasRestingLocalPosition)
            _visualRoot.localPosition = _restingLocalPosition;
    }

    private IEnumerator PlayBumpRoutine(Vector3 localDirection, float distanceWorld, float duration)
    {
        Vector3 peakOffset = localDirection * distanceWorld;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_visualRoot == null)
            {
                _activeBump = null;
                yield break;
            }

            float normalizedTime = elapsed / duration;
            float bumpAmount = Mathf.Sin(normalizedTime * Mathf.PI);
            _visualRoot.localPosition = _restingLocalPosition + peakOffset * bumpAmount;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_visualRoot != null && _hasRestingLocalPosition)
            _visualRoot.localPosition = _restingLocalPosition;

        _activeBump = null;
    }
}
