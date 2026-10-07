using System;
using System.Collections.Generic;
using StructureViewer.Application.Display;
using StructureViewer.Application.Events;
using StructureViewer.Domain.Display;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Contracts;
using UnityEngine;

namespace StructureViewer.Presentation.Display
{
    // Resolves every element's material from mode + highlight and pushes only real changes to the renderer
    // (each change dirties a chunk). Never touches visibility.
    public sealed class MaterialApplier : IDisposable
    {
        private readonly IStructureRenderer _renderer;
        private readonly MaterialLibrary _library;
        private readonly IElementPalette _realistic;
        private readonly ColorByPalette _colorBy;
        private readonly IElementPalette _xRay;
        private readonly IElementPalette _clay;
        private readonly IDisposable[] _subscriptions;

        private IElementPalette _current;
        private StructureModel _model;
        private SelectionSnapshot _selection = SelectionSnapshot.Empty;
        private int _hover = HoverChanged.None;

        // What the renderer shows; null until it matches the model (first load, or the renderer was not built yet).
        private Material[] _applied;

        public MaterialApplier(IStructureRenderer renderer, RenderingConfig config, DisplayPaletteAsset palette,
            DisplaySettings settings, EventBus bus)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            _library = new MaterialLibrary(config);
            _realistic = new RealisticPalette(_library, palette);
            _colorBy = new ColorByPalette(_library, palette);
            _xRay = new XRayPalette(_library, palette);
            _clay = new ClayPalette(_library, palette);
            UseMode(settings.Mode, settings.Field);

            _subscriptions = new[]
            {
                bus.Subscribe<StructureLoaded>(OnLoaded),
                bus.Subscribe<SelectionChanged>(OnSelectionChanged),
                bus.Subscribe<HoverChanged>(OnHoverChanged),
                bus.Subscribe<DisplayModeChanged>(OnModeChanged)
            };
        }

        public int MaterialCount => _library.Count;

        // Pass to StructureRenderer.Build so a fresh build already matches the current mode (nothing is selected after a load).
        public Material BaseMaterial(Element element) => _current.Resolve(element, HighlightState.None);

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
            _library.Dispose();
        }

        // Handlers of one event run in subscription order, so the renderer may not be rebuilt yet: any order ends
        // correct because a fresh build uses BaseMaterial and nothing is highlighted after a load.
        private void OnLoaded(StructureLoaded evt)
        {
            _model = evt.Model;
            _selection = SelectionSnapshot.Empty;
            _hover = HoverChanged.None;
            _applied = null;
            ApplyAll();
        }

        private void OnSelectionChanged(SelectionChanged evt)
        {
            var previous = _selection;
            _selection = evt.Selection ?? SelectionSnapshot.Empty;
            Apply(previous.Indices);
            Apply(_selection.Indices);
        }

        private void OnHoverChanged(HoverChanged evt)
        {
            int previous = _hover;
            _hover = evt.Index;
            Apply(previous);
            Apply(_hover);
        }

        private void OnModeChanged(DisplayModeChanged evt)
        {
            UseMode(evt.Mode, evt.Field);
            ApplyAll();
        }

        private void UseMode(DisplayMode mode, ColorByField field)
        {
            _colorBy.Field = field;
            _current = mode switch
            {
                DisplayMode.ColorBy => _colorBy,
                DisplayMode.XRay => _xRay,
                DisplayMode.Clay => _clay,
                _ => _realistic
            };
        }

        // The renderer must hold this model before indices mean anything; the first time it does, every element is resolved.
        private bool Synced()
        {
            if (_model == null || _renderer.Count != _model.Elements.Count)
            {
                _applied = null;
                return false;
            }
            if (_applied == null)
            {
                _applied = new Material[_model.Elements.Count];
                for (int i = 0; i < _applied.Length; i++)
                    ApplyOne(i);
            }
            return true;
        }

        private void ApplyAll()
        {
            if (!Synced())
                return;
            for (int i = 0; i < _applied.Length; i++)
                ApplyOne(i);
        }

        private void Apply(IReadOnlyList<int> indices)
        {
            if (!Synced())
                return;
            for (int i = 0; i < indices.Count; i++)
                Apply(indices[i]);
        }

        private void Apply(int index)
        {
            if (Synced() && index >= 0 && index < _applied.Length)
                ApplyOne(index);
        }

        private void ApplyOne(int index)
        {
            var material = _current.Resolve(_model.Elements[index], HighlightResolver.Resolve(index, _selection, _hover));
            if (material == _applied[index])
                return;

            _applied[index] = material;
            _renderer.SetMaterial(index, material);
        }
    }
}
