using System;

namespace OfficeFlipOut.UI
{
    public enum ClipboardTab
    {
        Directory = 0,
        Progress = 1,
        Map = 2
    }

    public static class ClipboardUIState
    {
        public static bool IsOpen { get; private set; }
        public static ClipboardTab ActiveTab { get; private set; } = ClipboardTab.Directory;

        public static bool ShouldBlockGameplayInput => IsOpen;

        public static event Action<bool> ClipboardOpenChanged;
        public static event Action<ClipboardTab> ClipboardTabChanged;

        public static void SetOpen(bool isOpen)
        {
            if (IsOpen == isOpen)
                return;

            IsOpen = isOpen;
            ClipboardOpenChanged?.Invoke(IsOpen);
        }

        public static void SetTab(ClipboardTab tab)
        {
            if (ActiveTab == tab)
                return;

            ActiveTab = tab;
            ClipboardTabChanged?.Invoke(ActiveTab);
        }
    }
}
