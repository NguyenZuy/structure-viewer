using System;
using StructureViewer.Presentation.Contracts;
using UnityEngine;

namespace StructureViewer.Presentation.Input
{
    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }

    // Turns raw pointer samples into gestures. Engine-free so every rule is EditMode-testable:
    // a press that never moves beyond the drag threshold is a tap; mouse LMB drag / one finger orbits,
    // RMB/MMB drag / two fingers pan, wheel / pinch zoom toward the pointer. Hot path allocates nothing.
    public sealed class GestureClassifier : IPointerEvents
    {
        private GestureSettings _settings;

        private bool _mousePressed;
        private MouseButton _mouseButton;
        private Vector2 _mousePressPosition;
        private Vector2 _mouseLast;
        private bool _mouseDragging;
        private Vector2 _lastHover = new Vector2(float.NaN, float.NaN);

        private int _touchCount;
        private int _maxTouches;
        private bool _touchDragging;
        private bool _touchSuppressed;
        private Vector2 _touchPressPosition;
        private Vector2 _touchLast;
        private Vector2 _pinchCentroid;
        private float _pinchDistance;

        private bool _hasLastTap;
        private float _lastTapTime;
        private Vector2 _lastTapPosition;

        public GestureClassifier(GestureSettings settings) => _settings = settings;

        public PointerDevice Current { get; private set; } = PointerDevice.Mouse;

        public event Action<TapEvent> Tapped;
        public event Action<Vector2> Hovered;
        public event Action<Vector2> Orbited;
        public event Action<Vector2> Panned;
        public event Action<float, Vector2> Zoomed;

        public void UpdateSettings(GestureSettings settings) => _settings = settings;

        public void MouseDown(MouseButton button, Vector2 position)
        {
            if (_mousePressed)
                return;
            Current = PointerDevice.Mouse;
            _mousePressed = true;
            _mouseButton = button;
            _mousePressPosition = position;
            _mouseLast = position;
            _mouseDragging = false;
        }

        public void MouseMove(Vector2 position)
        {
            if (!_mousePressed)
            {
                if (position != _lastHover)
                {
                    _lastHover = position;
                    Current = PointerDevice.Mouse;
                    Hovered?.Invoke(position);
                }
                return;
            }

            if (!_mouseDragging && (position - _mousePressPosition).magnitude > _settings.DragThreshold)
                _mouseDragging = true;
            if (_mouseDragging && position != _mouseLast)
            {
                var delta = position - _mouseLast;
                if (_mouseButton == MouseButton.Left)
                    Orbited?.Invoke(delta);
                else
                    Panned?.Invoke(delta);
            }
            _mouseLast = position;
        }

        public void MouseUp(MouseButton button, Vector2 position, bool additive, float time)
        {
            if (!_mousePressed || button != _mouseButton)
                return;
            MouseMove(position);
            _mousePressed = false;
            if (!_mouseDragging && button == MouseButton.Left)
                EmitTap(position, additive, PointerDevice.Mouse, time);
        }

        // Cancels a press without a tap, e.g. when the pointer leaves the canvas or focus is lost.
        public void MouseCancel()
        {
            _mousePressed = false;
            _mouseDragging = false;
        }

        public void Scroll(float delta, Vector2 position)
        {
            if (Mathf.Approximately(delta, 0f))
                return;
            Current = PointerDevice.Mouse;
            float notches = Mathf.Clamp(delta / _settings.ScrollPerNotch, -3f, 3f);
            Zoomed?.Invoke(Mathf.Pow(_settings.ZoomPerNotch, notches), position);
        }

        // Called every frame with the active touch count and the first two touch positions.
        public void Touches(int count, Vector2 first, Vector2 second, float time)
        {
            if (count > 0)
                Current = PointerDevice.Touch;

            if (_touchCount == 0 && count > 0)
                BeginTouch(first);

            if (count >= 2)
                UpdatePinch(first, second, startNew: _touchCount < 2);
            else if (count == 1 && !_touchSuppressed && _maxTouches == 1)
                UpdateOneFinger(first);

            if (count == 0 && _touchCount > 0)
                EndTouch(time);
            else if (count < _touchCount && count > 0)
                _touchSuppressed = true; // A finger lifted mid-gesture: ignore the rest until all fingers are up.

            _touchCount = count;
        }

        private void BeginTouch(Vector2 position)
        {
            _maxTouches = 1;
            _touchDragging = false;
            _touchSuppressed = false;
            _touchPressPosition = position;
            _touchLast = position;
        }

        private void UpdateOneFinger(Vector2 position)
        {
            if (!_touchDragging && (position - _touchPressPosition).magnitude > _settings.DragThreshold)
                _touchDragging = true;
            if (_touchDragging && position != _touchLast)
                Orbited?.Invoke(position - _touchLast);
            _touchLast = position;
        }

        private void UpdatePinch(Vector2 first, Vector2 second, bool startNew)
        {
            _maxTouches = 2;
            var centroid = (first + second) * 0.5f;
            float distance = Vector2.Distance(first, second);
            if (startNew || _touchSuppressed)
            {
                _touchSuppressed = false;
                _pinchCentroid = centroid;
                _pinchDistance = distance;
                return;
            }

            if (centroid != _pinchCentroid)
                Panned?.Invoke(centroid - _pinchCentroid);
            if (_pinchDistance > 1f && !Mathf.Approximately(distance, _pinchDistance))
                Zoomed?.Invoke(distance / _pinchDistance, centroid);
            _pinchCentroid = centroid;
            _pinchDistance = distance;
        }

        private void EndTouch(float time)
        {
            if (_maxTouches == 1 && !_touchDragging && !_touchSuppressed)
                EmitTap(_touchLast, false, PointerDevice.Touch, time);
            _maxTouches = 0;
            _touchSuppressed = false;
        }

        // The second tap of a pair is flagged double and consumes the pair, so a triple tap is double + single.
        private void EmitTap(Vector2 position, bool additive, PointerDevice device, float time)
        {
            bool isDouble = _hasLastTap
                            && time - _lastTapTime <= _settings.DoubleTapWindow
                            && (position - _lastTapPosition).magnitude <= _settings.DoubleTapRadius;
            _hasLastTap = !isDouble;
            _lastTapTime = time;
            _lastTapPosition = position;
            Tapped?.Invoke(new TapEvent(position, isDouble, additive, device));
        }
    }
}
