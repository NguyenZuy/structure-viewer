using System;
using StructureViewer.Presentation.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Shell
{
    // Runs after UIDocument (-100), which builds the tree in its OnEnable, and before default-order wiring code.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ShellView : MonoBehaviour, IShell
    {
        public const string CompactClass = "compact";
        private const long ToastMs = 4000;
        private const int MaxToasts = 3;
        private const long SlotPollMs = 250;

        private VisualElement _root;
        private VisualElement _shellRoot;
        private VisualElement _overlay;
        private VisualElement _toasts;
        private VisualElement _leftFrame;
        private VisualElement _rightFrame;
        private ToolbarView _toolbar;
        private SheetView _sheet;

        public VisualElement LeftSlot { get; private set; }
        public VisualElement RightSlot { get; private set; }
        public VisualElement BottomSlot { get; private set; }
        public bool IsCompact { get; private set; }

        public event Action<bool> CompactChanged;
        public event Action SheetHidden;

        // UIDocument rebuilds its tree on every enable, so everything is re-queried here.
        private void OnEnable()
        {
            var document = GetComponent<UIDocument>();
            if (document.visualTreeAsset == null)
                throw new InvalidOperationException("ShellView needs MainLayout.uxml on its UIDocument.");

            _root = document.rootVisualElement;
            _root.pickingMode = PickingMode.Ignore;
            _shellRoot = _root.Q("shell-root");
            _overlay = _root.Q("overlay");
            _toasts = _root.Q("toasts");
            _leftFrame = _root.Q("left-frame");
            _rightFrame = _root.Q("right-frame");
            LeftSlot = _root.Q("left-slot");
            RightSlot = _root.Q("right-slot");
            BottomSlot = _root.Q("bottom-slot");

            _toolbar = new ToolbarView(_root);
            _sheet = new SheetView(_root);
            _sheet.Hidden += () => SheetHidden?.Invoke();

            SetupCollapse(_leftFrame, _root.Q<Button>("left-toggle"), "‹", "›");
            SetupCollapse(_rightFrame, _root.Q<Button>("right-toggle"), "›", "‹");

            IsCompact = false;
            _root.RemoveFromClassList(CompactClass);
            _root.RegisterCallback<GeometryChangedEvent>(_ => ApplyScreen());
            // There is no child-added event; empty slots hide their frame (and collapse button) on a cheap poll.
            _root.schedule.Execute(UpdateSlotFrames).Every(SlotPollMs);
            UpdateSlotFrames();
        }

        // Full-screen layers drawn over the 3D view but under every panel (measure line, labels). They must ignore picking.
        public void AddViewportOverlay(VisualElement overlay) => _root.Insert(0, overlay);

        public void ShowSheet(VisualElement content, string title) => _sheet.Show(content, title);

        public void HideSheet() => _sheet.Hide();

        public void AddToolbarItem(ToolbarItem item) => _toolbar.Add(item);

        public void SetToolbarItemState(string id, bool active, bool enabled) => _toolbar.SetState(id, active, enabled);

        public void ShowToast(string message, bool isError = false)
        {
            while (_toasts.childCount >= MaxToasts)
                _toasts.RemoveAt(0);

            var toast = new Label(message) { pickingMode = PickingMode.Ignore };
            toast.AddToClassList("sv-toast");
            toast.EnableInClassList("sv-toast--error", isError);
            _toasts.Add(toast);
            toast.schedule.Execute(toast.RemoveFromHierarchy).StartingIn(ToastMs);
        }

        private void ApplyScreen()
        {
            float width = _root.layout.width;
            if (float.IsNaN(width) || width <= 0f)
                return;

            var insets = ResponsiveRules.SafeArea(Screen.safeArea, new Vector2(Screen.width, Screen.height), Screen.width / width);
            _shellRoot.style.paddingLeft = insets.Left;
            _shellRoot.style.paddingRight = insets.Right;
            _shellRoot.style.paddingTop = insets.Top;
            _shellRoot.style.paddingBottom = insets.Bottom;
            // Overlays are absolute, which ignores the parent's padding, so they get the insets as offsets instead.
            _overlay.style.left = insets.Left;
            _overlay.style.right = insets.Right;
            _overlay.style.top = insets.Top;
            _overlay.style.bottom = insets.Bottom;

            bool compact = ResponsiveRules.IsCompact(width);
            if (compact == IsCompact)
                return;
            IsCompact = compact;
            _root.EnableInClassList(CompactClass, compact);
            _toolbar.SetCompact(compact);
            CompactChanged?.Invoke(compact);
        }

        private void UpdateSlotFrames()
        {
            _leftFrame.EnableInClassList("sv-hidden", LeftSlot.childCount == 0);
            _rightFrame.EnableInClassList("sv-hidden", RightSlot.childCount == 0);
        }

        private static void SetupCollapse(VisualElement frame, Button toggle, string expandedGlyph, string collapsedGlyph)
        {
            toggle.clicked += () =>
            {
                frame.ToggleInClassList("sv-slot-frame--collapsed");
                toggle.text = frame.ClassListContains("sv-slot-frame--collapsed") ? collapsedGlyph : expandedGlyph;
            };
        }
    }
}
