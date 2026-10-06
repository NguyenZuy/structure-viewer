using System;
using UnityEngine;

namespace StructureViewer.Presentation.Contracts
{
    public enum PointerDevice
    {
        Mouse,
        Touch
    }

    // Gesture-level input, already classified. Screen positions and deltas are in pixels.
    public interface IPointerEvents
    {
        PointerDevice Current { get; }

        event Action<TapEvent> Tapped;

        // Mouse only; touch never hovers.
        event Action<Vector2> Hovered;

        event Action<Vector2> Orbited;
        event Action<Vector2> Panned;

        // Amount > 1 zooms in (pinch spread / wheel up), toward the focus screen point.
        event Action<float, Vector2> Zoomed;
    }

    public readonly struct TapEvent
    {
        public TapEvent(Vector2 screenPosition, bool isDouble, bool additive, PointerDevice device)
        {
            ScreenPosition = screenPosition;
            IsDouble = isDouble;
            Additive = additive;
            Device = device;
        }

        public Vector2 ScreenPosition { get; }
        public bool IsDouble { get; }

        // Ctrl held (mouse); the touch multi-select toggle is applied by the consumer.
        public bool Additive { get; }
        public PointerDevice Device { get; }
    }
}
