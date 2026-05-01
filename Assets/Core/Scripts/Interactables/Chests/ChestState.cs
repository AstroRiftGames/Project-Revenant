using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ChestState : MonoBehaviour
{
    public bool IsOpened { get; private set; }
    public bool CanOpen => !IsOpened;

    public event Action<bool> OnOpenedStateChanged;

    public bool TryOpen()
    {
        if (!CanOpen)
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
