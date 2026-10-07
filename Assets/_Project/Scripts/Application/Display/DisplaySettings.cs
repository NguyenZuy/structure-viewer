using System;
using StructureViewer.Application.Events;
using StructureViewer.Domain.Display;

namespace StructureViewer.Application.Display
{
    // A view setting: not undoable and never touches visibility, selection or camera.
    public sealed class DisplaySettings
    {
        private readonly EventBus _bus;

        public DisplaySettings(EventBus bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));

        public DisplayMode Mode { get; private set; } = DisplayMode.Realistic;

        // Type is the most informative default (DESIGN.md).
        public ColorByField Field { get; private set; } = ColorByField.Type;

        public void SetMode(DisplayMode mode)
        {
            if (mode == Mode)
                return;
            Mode = mode;
            Publish();
        }

        public void SetField(ColorByField field)
        {
            if (field == Field)
                return;
            Field = field;
            Publish();
        }

        private void Publish() => _bus.Publish(new DisplayModeChanged(Mode, Field));
    }
}
