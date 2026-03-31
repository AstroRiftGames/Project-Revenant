using UnityEngine;

public class MovementRangeTest : MonoBehaviour
{
    [SerializeField] private UnitMovement mover;
    [SerializeField] private Unit target;
    [SerializeField] private int rangeInCells = 1;

    private void Start()
    {
        mover.SetTarget(target, rangeInCells);
    }
}

