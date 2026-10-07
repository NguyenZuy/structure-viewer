using StructureViewer.Domain.Measure;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Measure
{
    // Draws the measure line, markers and label as a screen-space UI overlay: always on top of the model and a
    // constant pixel width at any zoom, with no extra shader. Reprojected every frame while a point exists.
    public sealed class MeasureView : MonoBehaviour, IMeasureView
    {
        private static readonly Color Accent = new Color(1f, 0.541f, 0.122f);
        private static readonly Color Outline = new Color(0f, 0f, 0f, 0.6f);
        private const float LineWidth = 3f;
        private const float MarkerRadius = 6f;

        private OverlayElement _overlay;
        private Label _label;
        private MeasureVisual _visual;

        public Camera Camera { get; set; }

        // Mount full-screen under the panels (e.g. first child of the shell's root); it never takes pointer input.
        public VisualElement Overlay
        {
            get
            {
                EnsureElements();
                return _overlay;
            }
        }

        public float Dpi => Screen.dpi;

        public bool TryWorldToScreen(Vector3 world, out Vector2 screen)
        {
            screen = default;
            if (Camera == null)
                return false;

            var point = Camera.WorldToScreenPoint(world);
            screen = point;
            return point.z > 0f;
        }

        public void Render(MeasureVisual visual)
        {
            EnsureElements();
            _visual = visual;
            if (_label.text != visual.Label)
                _label.text = visual.Label;
            _label.style.display = visual.Label != null ? DisplayStyle.Flex : DisplayStyle.None;
            _overlay.Visual = visual;
            _overlay.MarkDirtyRepaint();
        }

        private void LateUpdate()
        {
            if (_overlay == null || _overlay.panel == null || !_visual.HasA)
                return;

            _overlay.HasA = TryToPanel(_visual.A.World, out _overlay.PanelA);
            _overlay.HasB = _visual.HasB && TryToPanel(_visual.B.World, out _overlay.PanelB);
            _overlay.MarkDirtyRepaint();

            if (_visual.Label != null && _overlay.HasA && _overlay.HasB)
            {
                var mid = (_overlay.PanelA + _overlay.PanelB) * 0.5f;
                _label.style.left = mid.x;
                _label.style.top = mid.y;
                _label.style.visibility = Visibility.Visible;
            }
            else
            {
                _label.style.visibility = Visibility.Hidden;
            }
        }

        private bool TryToPanel(Vector3 world, out Vector2 panelPoint)
        {
            panelPoint = default;
            if (!TryWorldToScreen(world, out var screen))
                return false;

            // Panel space has a top-left origin.
            panelPoint = RuntimePanelUtils.ScreenToPanel(_overlay.panel, new Vector2(screen.x, Screen.height - screen.y));
            return true;
        }

        private void EnsureElements()
        {
            if (_overlay != null)
                return;

            _overlay = new OverlayElement { name = "measure-overlay", pickingMode = PickingMode.Ignore };
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.top = 0;
            _overlay.style.right = 0;
            _overlay.style.bottom = 0;

            _label = new Label { name = "measure-label", pickingMode = PickingMode.Ignore };
            _label.style.position = Position.Absolute;
            _label.style.translate = new Translate(Length.Percent(-50), new Length(-140, LengthUnit.Percent));
            _label.style.paddingLeft = 8;
            _label.style.paddingRight = 8;
            _label.style.paddingTop = 4;
            _label.style.paddingBottom = 4;
            _label.style.borderTopLeftRadius = 4;
            _label.style.borderTopRightRadius = 4;
            _label.style.borderBottomLeftRadius = 4;
            _label.style.borderBottomRightRadius = 4;
            _label.style.backgroundColor = new Color(0.118f, 0.122f, 0.133f, 0.92f);
            _label.style.color = Color.white;
            _label.style.fontSize = 13;
            _label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _label.style.display = DisplayStyle.None;
            _overlay.Add(_label);
        }

        private void OnDestroy() => _overlay?.RemoveFromHierarchy();

        private sealed class OverlayElement : VisualElement
        {
            public MeasureVisual Visual;
            public bool HasA;
            public bool HasB;
            public Vector2 PanelA;
            public Vector2 PanelB;

            public OverlayElement() => generateVisualContent += Draw;

            private void Draw(MeshGenerationContext context)
            {
                if (!Visual.HasA || !HasA)
                    return;

                var painter = context.painter2D;
                if (Visual.HasB && HasB)
                {
                    Stroke(painter, PanelA, PanelB, Outline, LineWidth + 2f);
                    Stroke(painter, PanelA, PanelB, Accent, LineWidth);
                }

                DrawMarker(painter, PanelA, Visual.A.Kind);
                if (Visual.HasB && HasB)
                    DrawMarker(painter, PanelB, Visual.B.Kind);
            }

            private static void Stroke(Painter2D painter, Vector2 from, Vector2 to, Color color, float width)
            {
                painter.strokeColor = color;
                painter.lineWidth = width;
                painter.BeginPath();
                painter.MoveTo(from);
                painter.LineTo(to);
                painter.Stroke();
            }

            // Endpoint: filled dot. Midpoint: filled diamond. Free: ring (no snap happened).
            private static void DrawMarker(Painter2D painter, Vector2 at, SnapKind kind)
            {
                painter.fillColor = Accent;
                painter.strokeColor = Outline;
                painter.lineWidth = 2f;
                painter.BeginPath();
                if (kind == SnapKind.Midpoint)
                {
                    painter.MoveTo(at + new Vector2(0f, -MarkerRadius - 1f));
                    painter.LineTo(at + new Vector2(MarkerRadius + 1f, 0f));
                    painter.LineTo(at + new Vector2(0f, MarkerRadius + 1f));
                    painter.LineTo(at + new Vector2(-MarkerRadius - 1f, 0f));
                    painter.ClosePath();
                }
                else
                {
                    painter.Arc(at, MarkerRadius, 0f, 360f);
                    painter.ClosePath();
                }

                if (kind == SnapKind.Free)
                {
                    painter.strokeColor = Accent;
                    painter.Stroke();
                }
                else
                {
                    painter.Fill();
                    painter.Stroke();
                }
            }
        }
    }
}
