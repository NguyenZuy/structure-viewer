using System;
using StructureViewer.Presentation.Contracts;
using UnityEngine;

namespace StructureViewer.Tests.Fixtures
{
    public sealed class FakePointerEvents : IPointerEvents
    {
        public PointerDevice Current { get; set; } = PointerDevice.Mouse;

        public event Action<TapEvent> Tapped;
        public event Action<Vector2> Hovered;
        public event Action<Vector2> Orbited;
        public event Action<Vector2> Panned;
        public event Action<float, Vector2> Zoomed;

        public bool HasSubscribers =>
            Tapped != null || Hovered != null || Orbited != null || Panned != null || Zoomed != null;

        // Uses Current as the device.
        public void RaiseTap(Vector2 screenPosition, bool isDouble = false, bool additive = false) =>
            Tapped?.Invoke(new TapEvent(screenPosition, isDouble, additive, Current));

        public void RaiseTap(TapEvent tap) => Tapped?.Invoke(tap);
        public void RaiseHover(Vector2 screenPosition) => Hovered?.Invoke(screenPosition);
        public void RaiseOrbit(Vector2 delta) => Orbited?.Invoke(delta);
        public void RaisePan(Vector2 delta) => Panned?.Invoke(delta);
        public void RaiseZoom(float amount, Vector2 focus) => Zoomed?.Invoke(amount, focus);
    }
}
