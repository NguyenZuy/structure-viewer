using System;
using System.Collections.Generic;
using StructureViewer.Presentation.Contracts;
using UnityEngine.UIElements;

namespace StructureViewer.Tests.Fixtures
{
    public sealed class FakeShell : IShell
    {
        public VisualElement LeftSlot { get; } = new VisualElement();
        public VisualElement RightSlot { get; } = new VisualElement();
        public VisualElement BottomSlot { get; } = new VisualElement();

        public bool IsCompact { get; private set; }
        public event Action<bool> CompactChanged;

        public Dictionary<string, ToolbarItem> ToolbarItems { get; } = new Dictionary<string, ToolbarItem>();
        public Dictionary<string, (bool Active, bool Enabled)> ToolbarStates { get; } = new Dictionary<string, (bool, bool)>();
        public VisualElement SheetContent { get; private set; }
        public string SheetTitle { get; private set; }
        public List<(string Message, bool IsError)> Toasts { get; } = new List<(string, bool)>();

        public void SetCompact(bool compact)
        {
            if (IsCompact == compact)
                return;
            IsCompact = compact;
            CompactChanged?.Invoke(compact);
        }

        public void ShowSheet(VisualElement content, string title)
        {
            SheetContent = content;
            SheetTitle = title;
        }

        public void HideSheet()
        {
            SheetContent = null;
            SheetTitle = null;
        }

        public void AddToolbarItem(ToolbarItem item)
        {
            ToolbarItems.Add(item.Id, item);
            ToolbarStates[item.Id] = (false, true);
        }

        public void SetToolbarItemState(string id, bool active, bool enabled) => ToolbarStates[id] = (active, enabled);

        public void ShowToast(string message, bool isError = false) => Toasts.Add((message, isError));

        public void Click(string toolbarItemId) => ToolbarItems[toolbarItemId].OnClick();
    }
}
