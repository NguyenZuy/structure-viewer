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

        // Label box sits LabelLift of its height above the anchor (see the translate in CreateLabel).
        private const float LabelLift = 1.2f;
        private const float CollisionPadding = 2f;

        private readonly List<Label> _pool = new List<Label>();
        private readonly List<bool> _shown = new List<bool>();
        private VisualElement _overlay;
        private IReadOnlyList<AssemblyAnchor> _anchors = System.Array.Empty<AssemblyAnchor>();
        private bool[] _groupVisible = System.Array.Empty<bool>();
        private float _nearFade;
        private float _farFade;
        private int _emphasized = -1;
        private bool _enabled = true;

        // Per-frame scratch, sized once per load: panel position, opacity, rect, priority key and order of each label.
        private Vector2[] _position = System.Array.Empty<Vector2>();
        private float[] _opacity = System.Array.Empty<float>();
        private Rect[] _rects = System.Array.Empty<Rect>();
        private float[] _priority = System.Array.Empty<float>();
        private int[] _order = System.Array.Empty<int>();
        private bool[] _keep = System.Array.Empty<bool>();

        // Last measured size per label: a hidden label measures zero, which would let it pop back in and flicker.
        private Vector2[] _size = System.Array.Empty<Vector2>();

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
            int count = _anchors.Count;
            _groupVisible = new bool[count];
            _position = new Vector2[count];
            _opacity = new float[count];
            _rects = new Rect[count];
            _priority = new float[count];
            _order = new int[count];
            _keep = new bool[count];
            _size = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                _groupVisible[i] = true;
                _order[i] = i;
            }

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

            int count = _anchors.Count;
            for (int i = 0; i < count; i++)
            {
                var anchor = _anchors[i].Position;
                var viewport = Camera.WorldToViewportPoint(anchor);
                float opacity = LabelVisibilityRule.Opacity(_groupVisible[i], viewport, _nearFade, _farFade);
                // The emphasised (selected) assembly never fades and always wins a collision.
                if (opacity > 0f && i == _emphasized)
                    opacity = 1f;
                _opacity[i] = opacity;
                _keep[i] = opacity > 0f;
                _priority[i] = i == _emphasized ? float.MinValue : viewport.z;
                if (!_keep[i])
                    continue;

                var screen = Camera.WorldToScreenPoint(anchor);
                // Panel space has a top-left origin.
                _position[i] = RuntimePanelUtils.ScreenToPanel(_overlay.panel, new Vector2(screen.x, Screen.height - screen.y));
                var resolved = _pool[i].resolvedStyle;
                if (resolved.width > 0f && resolved.height > 0f)
                    _size[i] = new Vector2(resolved.width, resolved.height);
                // Matches the translate in CreateLabel: centred horizontally, bottom edge above the anchor.
                var size = _size[i];
                _rects[i] = new Rect(_position[i].x - size.x * 0.5f, _position[i].y - size.y * LabelLift, size.x, size.y);
            }

            // Nearer labels first, so the ones in front stay readable.
            LabelDeclutter.SortByKey(_order, _priority, count);
            LabelDeclutter.Apply(_rects, _order, _keep, CollisionPadding);

            for (int i = 0; i < count; i++)
            {
                Show(i, _keep[i]);
                if (!_keep[i])
                    continue;
                var style = _pool[i].style;
                style.left = _position[i].x;
                style.top = _position[i].y;
                style.opacity = _opacity[i];
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
            style.translate = new Translate(Length.Percent(-50), new Length(-LabelLift * 100f, LengthUnit.Percent));
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
