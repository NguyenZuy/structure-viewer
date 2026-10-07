using System;
using StructureViewer.Domain.Structure;

namespace StructureViewer.Domain.Display
{
    // What an element is coloured by in "Color by" mode. Category and level keys carry their ordinal (palette index);
    // type keys carry the type name.
    public readonly struct ColorKey : IEquatable<ColorKey>
    {
        public ColorKey(ColorByField field, int ordinal, string name)
        {
            Field = field;
            Ordinal = ordinal;
            Name = name;
        }

        public ColorByField Field { get; }

        // (int)ElementCategory or level index; -1 for types.
        public int Ordinal { get; }

        // Category name or type name; null for levels (their label comes from the model).
        public string Name { get; }

        public bool Equals(ColorKey other) => Field == other.Field && Ordinal == other.Ordinal && Name == other.Name;
        public override bool Equals(object obj) => obj is ColorKey other && Equals(other);
        public override int GetHashCode() => ((int)Field * 397 ^ Ordinal) * 397 ^ (Name?.GetHashCode() ?? 0);
        public override string ToString() => $"{Field}:{Name ?? Ordinal.ToString()}";
    }

    public static class ColorKeyResolver
    {
        private static readonly string[] CategoryNames = Enum.GetNames(typeof(ElementCategory));

        public static ColorKey KeyOf(Element element, ColorByField field)
        {
            var info = element.Info;
            return field switch
            {
                ColorByField.Category => new ColorKey(field, (int)info.Category, CategoryNames[(int)info.Category]),
                ColorByField.Level => new ColorKey(field, info.LevelIndex, null),
                _ => new ColorKey(field, -1, info.Type ?? string.Empty)
            };
        }
    }
}
