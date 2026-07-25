using UnityEngine;

public class PlayerActionLock : MonoBehaviour
{
    public bool IsEating { get; private set; }
    public bool IsMutating { get; private set; }

    public bool CanMove => !IsEating && !IsMutating;
    public bool CanOpenMutation => !IsEating && !IsMutating;

    public void SetEating(bool value)
    {
        IsEating = value;
    }

    public void SetMutating(bool value)
    {
        IsMutating = value;
    }
}