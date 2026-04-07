using UnityEngine;

public class CombatProjectileVisual : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _arrivalThreshold = 0.05f;
    [SerializeField] private bool _orientToTravelDirection = true;

    private Transform _target;
    private Vector3 _fallbackTargetPosition;

    private void Update()
    {
        Vector3 targetPosition = GetTargetPosition();
        targetPosition.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);

        Vector3 direction = targetPosition - transform.position;
        if (_orientToTravelDirection && direction.sqrMagnitude > 0.0001f)
            transform.right = direction.normalized;

        if (Vector3.Distance(transform.position, targetPosition) <= _arrivalThreshold)
            Destroy(gameObject);
    }

    public void Launch(Vector3 origin, Transform target, Vector3 fallbackTargetPosition)
    {
        transform.position = origin;
        _target = target;
        _fallbackTargetPosition = fallbackTargetPosition;
    }

    private Vector3 GetTargetPosition()
    {
        if (_target != null && _target.gameObject.activeInHierarchy)
            return _target.position;

        return _fallbackTargetPosition;
    }
}
