using System;
using System.Collections.Generic;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Structure;
using UnityEngine;

namespace StructureViewer.Presentation.Display
{
    // Colour of a member type as "Color by Type" shows it, for the takeoff swatch column. Null for types not in the model.
    public sealed class TypeColorProvider
    {
        private readonly DisplayPaletteAsset _palette;
        private readonly StructureSession _session;
        private readonly Dictionary<string, ElementCategory> _categoryOf = new Dictionary<string, ElementCategory>();
        private StructureModel _indexed;

        public TypeColorProvider(DisplayPaletteAsset palette, StructureSession session)
        {
            _palette = palette ? palette : throw new ArgumentNullException(nameof(palette));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public Color? ColorOf(string type)
        {
            if (type == null || !_session.HasModel)
                return null;

            Index(_session.Current);
            return _categoryOf.TryGetValue(type, out var category) ? _palette.TypeColor(type, category) : (Color?)null;
        }

        // Unknown types fall back to their category colour, so the category of each type is needed.
        private void Index(StructureModel model)
        {
            if (ReferenceEquals(model, _indexed))
                return;

            _indexed = model;
            _categoryOf.Clear();
            foreach (var element in model.Elements)
            {
                if (element.Info.Type != null && !_categoryOf.ContainsKey(element.Info.Type))
                    _categoryOf.Add(element.Info.Type, element.Info.Category);
            }
        }
    }
}
