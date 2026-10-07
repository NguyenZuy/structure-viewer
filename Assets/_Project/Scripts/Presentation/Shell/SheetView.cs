using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Shell
{
    // Bottom sheet: one content at a time, half/full height, dragged by its handle or header.
    internal sealed class SheetView
    {
        private const float SnapThreshold = 24f;
        private const float MinDragHeight = 60f;
        private const float MaxHeightFraction = 0.9f;

        private readonly VisualElement _sheet;
        private readonly Label _title;
        private readonly ScrollView _content;
        private SheetHeight _height = SheetHeight.Closed;
        private int _pointerId = -1;
        private VisualElement _dragTarget;
        private float _dragStartY;
        private float _dragStartHeight;
        private float _draggedUp;

        public SheetView(VisualElement root)
        {
            _sheet = root.Q("sheet");
            _title = root.Q<Label>("sheet-title");
            _content = root.Q<ScrollView>("sheet-content");
            root.Q<Button>("sheet-close").clicked += Hide;

            RegisterDrag(root.Q("sheet-handle"));
            RegisterDrag(_title.parent);
        }

        public event Action Hidden;

        public VisualElement Current { get; private set; }
        public SheetHeight Height => _height;

        public void Show(VisualElement content, string title)
        {
            if (Current != null && Current != content)
                Current.RemoveFromHierarchy();
            Current = content;
            _content.Add(content);
            _title.text = title;
            SetHeight(SheetHeight.Half);
        }

        public void Hide()
        {
            if (Current == null)
                return;
            Current.RemoveFromHierarchy();
            Current = null;
            SetHeight(SheetHeight.Closed);
            Hidden?.Invoke();
        }

        private void SetHeight(SheetHeight height)
        {
            _height = height;
            _sheet.EnableInClassList("sv-hidden", height == SheetHeight.Closed);
            _sheet.EnableInClassList("sv-sheet--full", height == SheetHeight.Full);
        }

        private void RegisterDrag(VisualElement target)
        {
            target.RegisterCallback<PointerDownEvent>(evt => OnDown(target, evt));
            target.RegisterCallback<PointerMoveEvent>(OnMove);
            target.RegisterCallback<PointerUpEvent>(OnUp);
            target.RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());
        }

        private void OnDown(VisualElement target, PointerDownEvent evt)
        {
            // The close button lives in the header; let it handle its own press.
            if (_pointerId >= 0 || evt.target is Button)
                return;
            _pointerId = evt.pointerId;
            _dragTarget = target;
            _dragStartY = evt.position.y;
            _dragStartHeight = _sheet.resolvedStyle.height;
            _draggedUp = 0f;
            _sheet.AddToClassList("sv-sheet--dragging");
            target.CapturePointer(evt.pointerId);
        }

        private void OnMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointerId)
                return;
            _draggedUp = _dragStartY - evt.position.y;
            float max = _sheet.parent.resolvedStyle.height * MaxHeightFraction;
            _sheet.style.height = Mathf.Clamp(_dragStartHeight + _draggedUp, MinDragHeight, max);
        }

        private void OnUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId)
                return;
            var next = SheetSnap.AfterDrag(_height, _draggedUp, SnapThreshold);
            _dragTarget.ReleasePointer(evt.pointerId);
            EndDrag();
            if (next == SheetHeight.Closed)
                Hide();
            else
                SetHeight(next);
        }

        private void EndDrag()
        {
            if (_pointerId < 0)
                return;
            _pointerId = -1;
            _dragTarget = null;
            _sheet.style.height = StyleKeyword.Null;
            _sheet.RemoveFromClassList("sv-sheet--dragging");
        }
    }
}
