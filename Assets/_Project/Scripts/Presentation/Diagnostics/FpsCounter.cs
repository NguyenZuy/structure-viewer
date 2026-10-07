using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Diagnostics
{
    // Development builds only (added by AppBootstrap under DEVELOPMENT_BUILD): average FPS and frame time, twice a second.
    public sealed class FpsCounter : MonoBehaviour
    {
        private const float Interval = 0.5f;

        private Label _label;
        private int _frames;
        private float _elapsed;

        public VisualElement Overlay
        {
            get
            {
                if (_label == null)
                {
                    _label = new Label { name = "fps-counter", pickingMode = PickingMode.Ignore };
                    _label.style.position = Position.Absolute;
                    _label.style.left = 8;
                    _label.style.bottom = 8;
                    _label.style.paddingLeft = 6;
                    _label.style.paddingRight = 6;
                    _label.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
                    _label.style.color = Color.white;
                    _label.style.fontSize = 11;
                }
                return _label;
            }
        }

        private void Update()
        {
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < Interval || _label == null)
                return;

            // One small string twice a second is fine; the per-frame path stays allocation-free.
            _label.text = $"{_frames / _elapsed:0} FPS · {_elapsed * 1000f / _frames:0.0} ms";
            _frames = 0;
            _elapsed = 0f;
        }

        private void OnDestroy() => _label?.RemoveFromHierarchy();
    }
}
