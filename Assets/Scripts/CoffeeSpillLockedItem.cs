using UnityEngine;

[DisallowMultipleComponent]
public class CoffeeSpillLockedItem : MonoBehaviour
{
    [SerializeField]
    private bool startLocked = true;

    public bool IsLocked { get; private set; }

    private void Awake()
    {
        if (startLocked)
        {
            IsLocked = true;
        }
    }

    public void Lock()
    {
        IsLocked = true;
    }
}

