using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ChestState : MonoBehaviour
{
    public bool IsOpened { get; private set; }

    public event Action<bool> OnOpenedStateChanged;

    public bool TryOpen()
    {
        if (IsOpened)
            return false;

        IsOpened = true;
        OnOpenedStateChanged?.Invoke(IsOpened);
        return true;
    }

    public void ResetState()
    {
        if (!IsOpened)
            return;

        IsOpened = false;
        OnOpenedStateChanged?.Invoke(IsOpened);
    }
}
