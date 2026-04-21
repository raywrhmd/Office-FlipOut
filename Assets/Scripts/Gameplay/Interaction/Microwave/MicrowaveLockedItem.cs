using UnityEngine;

[DisallowMultipleComponent]
public class MicrowaveLockedItem : MonoBehaviour
{
    public bool IsLocked { get; private set; }

    public void Lock()
    {
        IsLocked = true;
    }
}
