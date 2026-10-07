using System;
using StructureViewer.Presentation.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace StructureViewer.Presentation.Input
{
    // The single place where raw mouse/touch input becomes gestures (IPointerEvents).
    // Presses that start over UI Toolkit are ignored until released, so panels don't orbit the camera.
    public sealed class PointerInput : MonoBehaviour, IPointerEvents
    {
        // Browsers synthesize mouse events from touches; ignore the mouse for a while after any touch.
        private const float MouseAfterTouchSuppression = 0.5f;

        [SerializeField] private UIDocument _ui;

        private GestureClassifier _classifier;
        private bool _mouseBlocked;
        private bool _touchBlocked;
        private float _lastTouchTime = float.NegativeInfinity;
        private float _lastDpi = -1f;

        public PointerDevice Current => Classifier.Current;

        public event Action<TapEvent> Tapped
        {
            add => Classifier.Tapped += value;
            remove => Classifier.Tapped -= value;
        }

        public event Action<Vector2> Hovered
        {
            add => Classifier.Hovered += value;
            remove => Classifier.Hovered -= value;
        }

        public event Action<Vector2> Orbited
        {
            add => Classifier.Orbited += value;
            remove => Classifier.Orbited -= value;
        }

        public event Action<Vector2> Panned
        {
            add => Classifier.Panned += value;
            remove => Classifier.Panned -= value;
        }

        public event Action<float, Vector2> Zoomed
        {
            add => Classifier.Zoomed += value;
            remove => Classifier.Zoomed -= value;
        }

        // Created lazily so subscribers can attach before Awake runs.
        // Public so end-to-end tests can feed real gestures through the same classifier the devices use.
        public GestureClassifier Classifier => _classifier ??= new GestureClassifier(GestureSettings.ForDpi(Screen.dpi));

        public UIDocument Ui
        {
            get => _ui;
            set => _ui = value;
        }

        private void OnEnable() => EnhancedTouchSupport.Enable();

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            Classifier.MouseCancel();
        }

        private void Update()
        {
            if (!Mathf.Approximately(Screen.dpi, _lastDpi))
            {
                _lastDpi = Screen.dpi;
                Classifier.UpdateSettings(GestureSettings.ForDpi(Screen.dpi));
            }

            float time = Time.unscaledTime;
            bool touched = PollTouches(time);
            if (!touched && time - _lastTouchTime > MouseAfterTouchSuppression)
                PollMouse(time);
        }

        private bool PollTouches(float time)
        {
            var touches = Touch.activeTouches;
            int count = touches.Count;
            if (count == 0)
            {
                if (!_touchBlocked)
                    Classifier.Touches(0, default, default, time);
                _touchBlocked = false;
                return false;
            }

            _lastTouchTime = time;
            var first = touches[0].screenPosition;
            var second = count > 1 ? touches[1].screenPosition : first;
            if (touches[0].phase == UnityEngine.InputSystem.TouchPhase.Began && count == 1 && IsOverUi(first))
                _touchBlocked = true;
            if (!_touchBlocked)
                Classifier.Touches(count, first, second, time);
            return true;
        }

        private void PollMouse(float time)
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var position = mouse.position.ReadValue();
            bool additive = IsAdditiveHeld();

            Press(mouse.leftButton, MouseButton.Left, position);
            Press(mouse.rightButton, MouseButton.Right, position);
            Press(mouse.middleButton, MouseButton.Middle, position);

            if (!_mouseBlocked)
                Classifier.MouseMove(position);

            Release(mouse.leftButton, MouseButton.Left, position, additive, time);
            Release(mouse.rightButton, MouseButton.Right, position, additive, time);
            Release(mouse.middleButton, MouseButton.Middle, position, additive, time);

            float scroll = mouse.scroll.ReadValue().y;
            if (!Mathf.Approximately(scroll, 0f) && !IsOverUi(position))
                Classifier.Scroll(scroll, position);
        }

        private void Press(UnityEngine.InputSystem.Controls.ButtonControl control, MouseButton button, Vector2 position)
        {
            if (!control.wasPressedThisFrame)
                return;
            if (IsOverUi(position))
            {
                _mouseBlocked = true;
                return;
            }
            Classifier.MouseDown(button, position);
        }

        private void Release(UnityEngine.InputSystem.Controls.ButtonControl control, MouseButton button, Vector2 position, bool additive, float time)
        {
            if (!control.wasReleasedThisFrame)
                return;
            if (_mouseBlocked)
            {
                _mouseBlocked = AnyMouseButtonHeld();
                return;
            }
            Classifier.MouseUp(button, position, additive, time);
        }

        private static bool AnyMouseButtonHeld()
        {
            var mouse = Mouse.current;
            return mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed);
        }

        private static bool IsAdditiveHeld()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.ctrlKey.isPressed || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed);
        }

        // UI containers that should let the 3D view through must use PickingMode.Ignore (see B07 hand-off).
        private bool IsOverUi(Vector2 screenPosition)
        {
            var panel = _ui != null && _ui.rootVisualElement != null ? _ui.rootVisualElement.panel : null;
            if (panel == null)
                return false;
            var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            return panel.Pick(panelPosition) != null;
        }
    }
}
