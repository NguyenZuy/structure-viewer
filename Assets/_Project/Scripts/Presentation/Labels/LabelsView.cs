using System.Collections.Generic;
using StructureViewer.Domain.Labels;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Labels
{
    // Screen-projected assembly labels on a full-screen UI overlay. Labels are pooled and repositioned every frame
    // without allocations (struct styles; text set only when anchors change).
    public sealed class LabelsView : MonoBehaviour, ILabelsView
    {
        private static readonly Color Background = new Color(0.118f, 0.122f, 0.133f, 0.85f);
        private static readonly Color EmphasisBackground = new Color(0.239f, 0.545f, 1f, 0.95f);

        private readonly List<Label> _pool = new List<Label>();
        private readonly List<bool> _shown = new List<bool>();
        private VisualElement _overlay;
        private IReadOnlyList<AssemblyAnchor> _anchors = System.Array.Empty<AssemblyAnchor>();
        private bool[] _groupVisible = System.Array.Empty<bool>();
        private float _nearFade;
        private float _farFade;
        private int _emphasized = -1;
        private bool _enabled = true;

        public Camera Camera { get; set; }

        // Mount full-screen under the panels (e.g. first child of the shell's root); it never takes pointer input.
        public VisualElement Overlay
        {
            get
            {
                EnsureOverlay();
                return _overlay;
            }
        }

        public void ShowAnchors(IReadOnlyList<AssemblyAnchor> anchors, float nearFade, float farFade)
        {
            EnsureOverlay();
            _anchors = anchors ?? System.Array.Empty<AssemblyAnchor>();
            _nearFade = nearFade;
            _farFade = farFade;
            _groupVisible = new bool[_anchors.Count];
            for (int i = 0; i < _groupVisible.Length; i++)
                _groupVisible[i] = true;

            while (_pool.Count < _anchors.Count)
                _pool.Add(CreateLabel());
            for (int i = 0; i < _pool.Count; i++)
            {
                if (i < _anchors.Count)
                    _pool[i].text = _anchors[i].Group;
                Show(i, false);
            }
        }

        public void SetGroupVisible(int anchorIndex, bool visible)
        {
            if (anchorIndex >= 0 && anchorIndex < _groupVisible.Length)
                _groupVisible[anchorIndex] = visible;
        }

        public void SetEmphasized(int anchorIndex)
        {
            if (_emphasized >= 0 && _emphasized < _pool.Count)
                _pool[_emphasized].style.backgroundColor = Background;
            _emphasized = anchorIndex;
            if (_emphasized >= 0 && _emphasized < _pool.Count)
                _pool[_emphasized].style.backgroundColor = EmphasisBackground;
        }

        public void SetEnabled(bool enabled)
        {
            EnsureOverlay();
            _enabled = enabled;
            _overlay.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void LateUpdate()
        {
            if (!_enabled || Camera == null || _overlay?.panel == null)
                return;

            for (int i = 0; i < _anchors.Count; i++)
            {
                var anchor = _anchors[i].Position;
                var viewport = Camera.WorldToViewportPoint(anchor);
                // The emphasised (selected) assembly never fades.
                float opacity = LabelVisibilityRule.Opacity(_groupVisible[i], viewport, _nearFade, _farFade);
                if (opacity > 0f && i == _emphasized)
                    opacity = 1f;

                Show(i, opacity > 0f);
                if (opacity <= 0f)
                    continue;

                var screen = Camera.WorldToScreenPoint(anchor);
                // Panel space has a top-left origin.
                var panelPoint = RuntimePanelUtils.ScreenToPanel(_overlay.panel, new Vector2(screen.x, Screen.height - screen.y));
                var style = _pool[i].style;
                style.left = panelPoint.x;
                style.top = panelPoint.y;
                style.opacity = opacity;
            }
        }

        private void Show(int index, bool visible)
        {
            while (_shown.Count <= index)
                _shown.Add(true);
            if (_shown[index] == visible)
                return;
            _shown[index] = visible;
            _pool[index].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private Label CreateLabel()
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            var style = label.style;
            style.position = Position.Absolute;
            // Centred above the anchor point.
            style.translate = new Translate(Length.Percent(-50), new Length(-120, LengthUnit.Percent));
            style.paddingLeft = 6;
            style.paddingRight = 6;
            style.paddingTop = 2;
            style.paddingBottom = 2;
            style.borderTopLeftRadius = 4;
            style.borderTopRightRadius = 4;
            style.borderBottomLeftRadius = 4;
            style.borderBottomRightRadius = 4;
            style.backgroundColor = Background;
            style.color = Color.white;
            style.fontSize = 11;
            style.unityTextAlign = TextAnchor.MiddleCenter;
            _overlay.Add(label);
            return label;
        }

        private void EnsureOverlay()
        {
            if (_overlay != null)
                return;

            _overlay = new VisualElement { name = "labels-overlay", pickingMode = PickingMode.Ignore };
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.top = 0;
            _overlay.style.right = 0;
            _overlay.style.bottom = 0;
            _overlay.style.overflow = Overflow.Hidden;
        }

        private void OnDestroy() => _overlay?.RemoveFromHierarchy();
    }
}
