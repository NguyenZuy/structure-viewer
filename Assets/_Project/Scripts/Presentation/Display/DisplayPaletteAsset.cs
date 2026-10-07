using System;
using System.Collections.Generic;
using StructureViewer.Domain.Display;
using StructureViewer.Domain.Structure;
using UnityEngine;

namespace StructureViewer.Presentation.Display
{
    // Every display-mode colour, tweakable without code. Field initialisers are the shipped defaults, so
    // CreateInstance gives a complete palette. Orange and saturated blue are reserved for selection.
    [CreateAssetMenu(fileName = "DisplayPalette", menuName = "Structure Viewer/Display Palette")]
    public sealed class DisplayPaletteAsset : ScriptableObject
    {
        [Header("Selection & hover")]
        [SerializeField] private Color _memberHighlight = Hex("#FF8A1F");
        [SerializeField] private Color _assemblyHighlight = Hex("#3D8BFF");
        [SerializeField] private Color _hover = Hex("#FFE7A3");

        [Header("Color by")]
        [Tooltip("Indexed by ElementCategory: Wall, Floor, Roof, Sheathing, Slab.")]
        [SerializeField] private Color[] _categoryColors =
        {
            Hex("#5B8DA8"), Hex("#5FA35A"), Hex("#B5475A"), Hex("#8E6CC9"), Hex("#9A9A9A")
        };

        // Families: walls steel blues/teals, floors greens, roof reds/pinks, sheathing violets. Unknown types use their category colour.
        [SerializeField] private TypeEntry[] _typeColors =
        {
            new TypeEntry("Stud", Hex("#4F86A6")),
            new TypeEntry("TrimmerStud", Hex("#2F6E8A")),
            new TypeEntry("CrippleStud", Hex("#86B6CC")),
            new TypeEntry("BottomPlate", Hex("#3E5F7A")),
            new TypeEntry("TopPlate", Hex("#6C8FB0")),
            new TypeEntry("Nogging", Hex("#5FADA8")),
            new TypeEntry("Lintel", Hex("#24566B")),
            new TypeEntry("Sill", Hex("#A8D5D1")),
            new TypeEntry("Joist", Hex("#5FA35A")),
            new TypeEntry("Bearer", Hex("#2E7D4F")),
            new TypeEntry("Blocking", Hex("#9BCB7A")),
            new TypeEntry("TrussTopChord", Hex("#B5475A")),
            new TypeEntry("TrussBottomChord", Hex("#7E2A3A")),
            new TypeEntry("TrussWeb", Hex("#D98A97")),
            new TypeEntry("RoofSheathing", Hex("#8E6CC9")),
            new TypeEntry("WallSheathing", Hex("#B39DDB")),
            new TypeEntry("Slab", Hex("#9A9A9A"))
        };

        [Tooltip("Cycled by level index.")]
        [SerializeField] private Color[] _levelColors =
        {
            Hex("#5FA35A"), Hex("#8E6CC9"), Hex("#C9A227"), Hex("#4FA3A5"), Hex("#B5475A"), Hex("#7A8C99")
        };

        [SerializeField, Range(0f, 1f)] private float _colorByPanelAlpha = 0.35f;

        [Header("X-ray")]
        [SerializeField] private Color _xRay = Hex("#B8C4D6");
        [SerializeField, Range(0f, 1f)] private float _xRayMemberAlpha = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _xRayOtherAlpha = 0.08f;

        [Header("Clay")]
        [SerializeField] private Color _clay = Hex("#E8E6E1");
        [SerializeField] private Color _claySlab = Hex("#CFCBC4");
        [SerializeField, Range(0f, 1f)] private float _clayPanelAlpha = 0.25f;

        private Dictionary<string, Color> _typeLookup;

        public Color MemberHighlight => _memberHighlight;
        public Color AssemblyHighlight => _assemblyHighlight;
        public Color Hover => _hover;
        public float ColorByPanelAlpha => _colorByPanelAlpha;
        public Color XRay => _xRay;
        public float XRayMemberAlpha => _xRayMemberAlpha;
        public float XRayOtherAlpha => _xRayOtherAlpha;
        public Color Clay => _clay;
        public Color ClaySlab => _claySlab;
        public float ClayPanelAlpha => _clayPanelAlpha;

        // Category is needed for types missing from the table (they fall back to their category colour).
        public Color ColorOf(ColorKey key, ElementCategory category) =>
            key.Field switch
            {
                ColorByField.Category => CategoryColor((ElementCategory)key.Ordinal),
                ColorByField.Level => _levelColors.Length > 0 ? _levelColors[key.Ordinal % _levelColors.Length] : Color.grey,
                _ => TypeColor(key.Name, category)
            };

        public Color TypeColor(string type, ElementCategory category)
        {
            if (_typeLookup == null)
            {
                _typeLookup = new Dictionary<string, Color>();
                foreach (var entry in _typeColors)
                    _typeLookup[entry.Type] = entry.Color;
            }
            return type != null && _typeLookup.TryGetValue(type, out var color) ? color : CategoryColor(category);
        }

        private Color CategoryColor(ElementCategory category)
        {
            int i = (int)category;
            return i >= 0 && i < _categoryColors.Length ? _categoryColors[i] : Color.grey;
        }

        // Edits in the inspector must reach the cached lookup.
        private void OnValidate() => _typeLookup = null;

        // Plain C# on purpose: field initialisers run in the constructor, where Unity APIs are not allowed.
        private static Color Hex(string html)
        {
            uint rgb = Convert.ToUInt32(html.Substring(1), 16);
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        [Serializable]
        private struct TypeEntry
        {
            [SerializeField] private string _type;
            [SerializeField] private Color _color;

            public TypeEntry(string type, Color color)
            {
                _type = type;
                _color = color;
            }

            public string Type => _type;
            public Color Color => _color;
        }
    }
}
