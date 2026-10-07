using System;
using StructureViewer.Application.Commands;
using StructureViewer.Domain.Visibility;

namespace StructureViewer.Application.Visibility
{
    // Every visibility action (category, level, isolate, hide, show all) is a before/after swap of the immutable state,
    // so one command type covers them all and undo restores the exact previous state.
    public sealed class VisibilityCommand : ICommand
    {
        private readonly VisibilityService _service;
        private readonly VisibilityState _before;
        private readonly VisibilityState _after;

        public VisibilityCommand(VisibilityService service, VisibilityState before, VisibilityState after)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _before = before ?? throw new ArgumentNullException(nameof(before));
            _after = after ?? throw new ArgumentNullException(nameof(after));
        }

        public void Execute() => _service.Set(_after);

        public void Undo() => _service.Set(_before);
    }
}
