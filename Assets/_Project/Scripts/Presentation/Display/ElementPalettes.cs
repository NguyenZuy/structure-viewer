using StructureViewer.Domain.Display;
using StructureViewer.Domain.Structure;
using UnityEngine;

namespace StructureViewer.Presentation.Display
{
    // One strategy per display mode (DESIGN.md table). Every result is a shared material from the library or config.
    public interface IElementPalette
    {
        Material Resolve(Element element, HighlightState state);
    }

    public sealed class RealisticPalette : IElementPalette
    {
        private readonly MaterialLibrary _library;
        private readonly DisplayPaletteAsset _palette;

        public RealisticPalette(MaterialLibrary library, DisplayPaletteAsset palette)
        {
            _library = library;
            _palette = palette;
        }

        // Highlights tint the textured look instead of replacing it, as in the reference. Panels (sheathing, doors, glazing)
        // get a per-type tone (their template stays transparent; only the colour and alpha change).
        public Material Resolve(Element element, HighlightState state)
        {
            var config = _library.Config;
            if (element.Kind == ElementKind.Panel)
            {
                var tone = _palette.RealisticPanelFor(element.Info.Type);
                var color = state == HighlightState.None ? tone : Highlight(_palette, state);
                color.a = tone.a;
                return _library.Get(config.Sheathing, color);
            }

            var template = element.Kind == ElementKind.Slab ? config.Concrete : config.Wood;
            return state == HighlightState.None ? template : _library.Tint(template, Highlight(_palette, state));
        }

        internal static Color Highlight(DisplayPaletteAsset palette, HighlightState state) =>
            state switch
            {
                HighlightState.Member => palette.MemberHighlight,
                HighlightState.Assembly => palette.AssemblyHighlight,
                _ => palette.Hover
            };
    }

    public sealed class ColorByPalette : IElementPalette
    {
        private readonly MaterialLibrary _library;
        private readonly DisplayPaletteAsset _palette;

        public ColorByPalette(MaterialLibrary library, DisplayPaletteAsset palette)
        {
            _library = library;
            _palette = palette;
        }

        public ColorByField Field { get; set; } = ColorByField.Type;

        // Panels stay translucent in every state so a highlighted roof never hides the frame under it.
        public Material Resolve(Element element, HighlightState state)
        {
            var color = state == HighlightState.None
                ? _palette.ColorOf(ColorKeyResolver.KeyOf(element, Field), element.Info.Category)
                : RealisticPalette.Highlight(_palette, state);
            return element.Kind == ElementKind.Panel ? _library.Translucent(color, _palette.ColorByPanelAlpha) : _library.Flat(color);
        }
    }

    public sealed class XRayPalette : IElementPalette
    {
        private readonly MaterialLibrary _library;
        private readonly DisplayPaletteAsset _palette;

        public XRayPalette(MaterialLibrary library, DisplayPaletteAsset palette)
        {
            _library = library;
            _palette = palette;
        }

        // Highlighted elements are opaque so the selection pops out of the ghosted model.
        public Material Resolve(Element element, HighlightState state)
        {
            switch (state)
            {
                case HighlightState.Member:
                case HighlightState.Assembly:
                    return _library.Flat(RealisticPalette.Highlight(_palette, state));
                case HighlightState.Hover:
                    return _library.Translucent(_palette.Hover, 0.35f);
                default:
                    float alpha = element.Kind == ElementKind.Member ? _palette.XRayMemberAlpha : _palette.XRayOtherAlpha;
                    return _library.Translucent(_palette.XRay, alpha);
            }
        }
    }

    public sealed class ClayPalette : IElementPalette
    {
        private readonly MaterialLibrary _library;
        private readonly DisplayPaletteAsset _palette;

        public ClayPalette(MaterialLibrary library, DisplayPaletteAsset palette)
        {
            _library = library;
            _palette = palette;
        }

        public Material Resolve(Element element, HighlightState state)
        {
            var color = state != HighlightState.None ? RealisticPalette.Highlight(_palette, state)
                : element.Kind == ElementKind.Slab ? _palette.ClaySlab
                : element.Kind == ElementKind.Panel ? Color.white
                : _palette.Clay;
            return element.Kind == ElementKind.Panel ? _library.Translucent(color, _palette.ClayPanelAlpha) : _library.Flat(color);
        }
    }
}
