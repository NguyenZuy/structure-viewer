using System;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Contracts
{
    // Layout host only: knows slots, sheets, toolbar and toasts, nothing about features.
    public interface IShell
    {
        VisualElement LeftSlot { get; }
        VisualElement RightSlot { get; }
        VisualElement BottomSlot { get; }

        // True below the width breakpoint: panels open as sheets instead of slots.
        bool IsCompact { get; }
        event Action<bool> CompactChanged;

        // One sheet at a time; showing another replaces the current one.
        void ShowSheet(VisualElement content, string title);
        void HideSheet();

        // Raised whenever the sheet goes away: HideSheet, the close button, or a swipe down. Not raised on replace.
        event Action SheetHidden;

        void AddToolbarItem(ToolbarItem item);
        void SetToolbarItemState(string id, bool active, bool enabled);

        void ShowToast(string message, bool isError = false);
    }
}
