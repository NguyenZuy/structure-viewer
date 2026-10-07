using System;
using StructureViewer.Application.Events;
using StructureViewer.Application.Onboarding;
using StructureViewer.Presentation.Contracts;

namespace StructureViewer.Presentation.Notices
{
    // First-visit controls hint, worded for the pointer actually in use. Dismissing it is remembered.
    public sealed class OnboardingPresenter : IDisposable
    {
        public const string MouseHint = "Drag to rotate · Right-drag to pan · Scroll to zoom · Click to select · Double-click for the whole assembly";
        public const string TouchHint = "Drag to rotate · Two fingers to pan · Pinch to zoom · Tap to select · Double-tap for the whole assembly";

        private readonly INoticeView _view;
        private readonly IOnboardingStore _store;
        private readonly IPointerEvents _pointer;
        private bool _shown;
        private PointerDevice _device;

        // initialDevice: best guess before any input (touch screen or not).
        public OnboardingPresenter(INoticeView view, IOnboardingStore store, IPointerEvents pointer, PointerDevice initialDevice)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));

            _view.CloseClicked += Dismiss;
            if (_store.HasSeenHint)
                return;

            _shown = true;
            _device = initialDevice;
            _pointer.Tapped += OnTapped;
            _view.Show(TextFor(_device), null);
        }

        public static string TextFor(PointerDevice device) => device == PointerDevice.Touch ? TouchHint : MouseHint;

        public void Dispose()
        {
            _view.CloseClicked -= Dismiss;
            _pointer.Tapped -= OnTapped;
        }

        private void Dismiss()
        {
            if (!_shown)
                return;
            _shown = false;
            _pointer.Tapped -= OnTapped;
            _store.MarkHintSeen();
            _view.Hide();
        }

        // A laptop with a touch screen may guess wrong at start; the first tap tells.
        private void OnTapped(TapEvent tap)
        {
            if (tap.Device == _device)
                return;
            _device = tap.Device;
            _view.Show(TextFor(_device), null);
        }
    }

    // "Everything is hidden" state with a one-tap way back, so a filtered-out model never looks like a broken viewer.
    public sealed class EverythingHiddenPresenter : IDisposable
    {
        public const string Message = "Everything is hidden.";
        public const string ActionLabel = "Show all";

        private readonly INoticeView _view;
        private readonly IStructureRenderer _renderer;
        private readonly Action _showAll;
        private readonly IDisposable _changed;

        public EverythingHiddenPresenter(INoticeView view, IStructureRenderer renderer, Action showAll, EventBus bus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _showAll = showAll ?? throw new ArgumentNullException(nameof(showAll));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            _view.ActionClicked += _showAll;
            _view.CloseClicked += _view.Hide;
            _changed = bus.Subscribe<VisibilityChanged>(OnVisibilityChanged);
            _view.Hide();
        }

        public void Dispose()
        {
            _view.ActionClicked -= _showAll;
            _view.CloseClicked -= _view.Hide;
            _changed.Dispose();
        }

        private void OnVisibilityChanged(VisibilityChanged evt)
        {
            int count = _renderer.Count;
            bool anyVisible = false;
            for (int i = 0; i < count && !anyVisible; i++)
                anyVisible = evt.Visibility.IsVisible(i);

            if (count > 0 && !anyVisible)
                _view.Show(Message, ActionLabel);
            else
                _view.Hide();
        }
    }
}
