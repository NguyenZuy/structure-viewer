using System;
using System.Collections.Generic;
using StructureViewer.Domain.Selection;

namespace StructureViewer.Presentation.Info
{
    // What the info panel shows, already formatted, so the view only copies strings into labels.
    public sealed class InfoPanelContent
    {
        private static readonly IReadOnlyList<InfoRow> NoRows = Array.Empty<InfoRow>();

        public InfoPanelContent(SelectionKind kind, string title, string subtitle, IReadOnlyList<InfoRow> rows,
            IReadOnlyList<InfoRow> breakdown, bool canSelectAssembly, bool canIsolateAssembly)
        {
            Kind = kind;
            Title = title;
            Subtitle = subtitle;
            Rows = rows ?? NoRows;
            Breakdown = breakdown ?? NoRows;
            CanSelectAssembly = canSelectAssembly;
            CanIsolateAssembly = canIsolateAssembly;
        }

        public SelectionKind Kind { get; }
        public bool IsEmpty => Kind == SelectionKind.None;

        // For an empty selection, the hint shown instead of details.
        public string Title { get; }
        public string Subtitle { get; }
        public IReadOnlyList<InfoRow> Rows { get; }
        public IReadOnlyList<InfoRow> Breakdown { get; }
        public bool CanSelectAssembly { get; }
        public bool CanIsolateAssembly { get; }

        public static InfoPanelContent Empty(string message) =>
            new InfoPanelContent(SelectionKind.None, message, null, NoRows, NoRows, false, false);
    }

    public readonly struct InfoRow
    {
        public InfoRow(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public string Value { get; }
    }
}
