using System;
using StructureViewer.Application.Events;

namespace StructureViewer.Application.Loading
{
    public sealed class LoadStructureUseCase
    {
        private readonly IStructureParser _parser;
        private readonly StructureSession _session;
        private readonly EventBus _bus;

        public LoadStructureUseCase(IStructureParser parser, StructureSession session, EventBus bus)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        // On failure the current session is left untouched and nothing is published.
        public ParseResult Execute(string json)
        {
            var result = _parser.Parse(json);
            if (!result.Success)
                return result;

            _session.Set(result.Model);
            _bus.Publish(new StructureLoaded(result.Model));
            return result;
        }
    }
}
