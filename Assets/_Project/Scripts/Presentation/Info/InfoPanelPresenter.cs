using System;
using System.Collections.Generic;
using System.Globalization;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Application.Selection;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;

namespace StructureViewer.Presentation.Info
{
    public sealed class InfoPanelPresenter : IDisposable
    {
        public const string EmptyMessage = "Tap a member to see details";
        private const string None = "—";

        private static readonly CultureInfo Format = CultureInfo.InvariantCulture;

        private readonly IInfoPanelView _view;
        private readonly StructureSession _session;
        private readonly SelectionService _selection;
        private readonly IDisposable _selectionChanged;

        public InfoPanelPresenter(IInfoPanelView view, StructureSession session, SelectionService selection, EventBus bus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            _view.SelectAssemblyClicked += OnSelectAssembly;
            _view.IsolateAssemblyClicked += OnIsolateAssembly;
            _view.ClearClicked += OnClear;
            _selectionChanged = bus.Subscribe<SelectionChanged>(evt => Render(evt.Selection));
            Render(_selection.Current);
        }

        // Group id of the assembly to isolate; visibility belongs to another feature (wired in C2).
        public event Action<string> IsolateAssemblyRequested;

        public void Dispose()
        {
            _view.SelectAssemblyClicked -= OnSelectAssembly;
            _view.IsolateAssemblyClicked -= OnIsolateAssembly;
            _view.ClearClicked -= OnClear;
            _selectionChanged.Dispose();
        }

        private void OnSelectAssembly()
        {
            var current = _selection.Current;
            if (current.Kind == SelectionKind.Member)
                _selection.SelectAssembly(current.Indices[0]);
        }

        private void OnIsolateAssembly()
        {
            string group = _session.HasModel ? SelectedGroup(_session.Current, _selection.Current) : null;
            if (!string.IsNullOrEmpty(group))
                IsolateAssemblyRequested?.Invoke(group);
        }

        private void OnClear() => _selection.Clear();

        private void Render(SelectionSnapshot selection)
        {
            if (selection.IsEmpty || !_session.HasModel)
            {
                _view.Render(InfoPanelContent.Empty(EmptyMessage));
                return;
            }

            var model = _session.Current;
            _view.Render(selection.Kind == SelectionKind.Member
                ? DescribeElement(model, model.Elements[selection.Indices[0]])
                : DescribeSet(model, selection));
        }

        private static InfoPanelContent DescribeElement(StructureModel model, Element element)
        {
            var info = element.Info;
            bool hasGroup = !string.IsNullOrEmpty(info.Group);
            var rows = new List<InfoRow>
            {
                new InfoRow("Type", info.Type),
                new InfoRow("Category", info.Category.ToString()),
                new InfoRow("Level", model.Levels[info.LevelIndex].Name),
                new InfoRow("Assembly", hasGroup ? info.Group : None)
            };

            if (element.Member != null)
            {
                rows.Add(new InfoRow("Section", element.Member.Section.Label));
                rows.Add(new InfoRow("Material", string.IsNullOrEmpty(element.Member.Material) ? None : element.Member.Material));
                rows.Add(new InfoRow("Length", Millimetres(element.Member.Length)));
            }
            else if (element.Panel != null)
            {
                rows.Add(new InfoRow("Thickness", Millimetres(element.Panel.Thickness)));
                rows.Add(new InfoRow("Area", SquareMetres(element.Panel.Area)));
            }
            else if (element.Slab != null)
            {
                rows.Add(new InfoRow("Thickness", Millimetres(element.Slab.Thickness)));
                rows.Add(new InfoRow("Area", SquareMetres(element.Slab.Area)));
            }

            return new InfoPanelContent(SelectionKind.Member, info.Id, info.Type, rows, null, hasGroup, hasGroup);
        }

        private static InfoPanelContent DescribeSet(StructureModel model, SelectionSnapshot selection)
        {
            var summary = SelectionSummary.Of(model, selection.Indices);
            var rows = new List<InfoRow> { new InfoRow("Elements", summary.Count.ToString(Format)) };
            if (summary.TotalLength > 0f)
                rows.Add(new InfoRow("Total length", Metres(summary.TotalLength)));
            if (summary.TotalArea > 0f)
                rows.Add(new InfoRow("Total area", SquareMetres(summary.TotalArea)));

            var breakdown = new List<InfoRow>(summary.ByType.Count);
            for (int i = 0; i < summary.ByType.Count; i++)
            {
                var total = summary.ByType[i];
                string count = total.Count.ToString(Format);
                string value = total.Length > 0f ? $"{count} · {Metres(total.Length)}"
                    : total.Area > 0f ? $"{count} · {SquareMetres(total.Area)}"
                    : count;
                breakdown.Add(new InfoRow(total.Type, value));
            }

            bool isAssembly = selection.Kind == SelectionKind.Assembly;
            string title = isAssembly ? $"Assembly {SelectedGroup(model, selection)}" : $"{summary.Count} selected";
            string subtitle = isAssembly ? $"{summary.Count} elements" : "Multiple elements";
            return new InfoPanelContent(selection.Kind, title, subtitle, rows, breakdown, false, isAssembly);
        }

        // Member: its own group. Assembly: the shared group. Multi: none, it may span several.
        private static string SelectedGroup(StructureModel model, SelectionSnapshot selection) =>
            selection.Kind == SelectionKind.Member || selection.Kind == SelectionKind.Assembly
                ? model.Elements[selection.Indices[0]].Info.Group
                : null;

        private static string Millimetres(float metres) => $"{(metres * 1000f).ToString("N0", Format)} mm";
        private static string Metres(float metres) => $"{metres.ToString("N2", Format)} m";
        private static string SquareMetres(float area) => $"{area.ToString("N2", Format)} m²";
    }
}
