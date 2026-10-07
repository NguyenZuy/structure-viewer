using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Shell;

namespace StructureViewer.Tests.EditMode.Shell
{
    public sealed class IconsTests
    {
        private const string ProjectRoot = "Assets/_Project";

        [Test]
        public void Get_EveryIcon_IsAFullSizeVectorImage()
        {
            foreach (ToolbarIcon icon in Enum.GetValues(typeof(ToolbarIcon)))
            {
                var image = Icons.Get(icon);

                Assert.IsNotNull(image, icon.ToString());
                Assert.AreEqual(Icons.Size, image.width, 0.5f, icon.ToString());
                Assert.AreEqual(Icons.Size, image.height, 0.5f, icon.ToString());
            }
        }

        [Test]
        public void Get_SameIconTwice_ReusesTheImage()
        {
            Assert.AreSame(Icons.Get(ToolbarIcon.Measure), Icons.Get(ToolbarIcon.Measure));
        }

        // Regression: Unicode symbols used as toolbar glyphs (⌂ ↔ ■ …) showed in the Editor through OS font fallback and
        // vanished in WebGL. Runtime text may only use characters every Latin font has; symbols go through Icons.
        [Test]
        public void RuntimeText_UsesOnlyLatinCharacters()
        {
            var offenders = new List<string>();
            var sources = Directory.GetFiles(Path.Combine(ProjectRoot, "Scripts"), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Replace('\\', '/').Contains("/Editor/"))
                .Concat(Directory.GetFiles(Path.Combine(ProjectRoot, "UI"), "*.uxml", SearchOption.AllDirectories));
            foreach (var file in sources)
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (Match literal in Regex.Matches(StripComment(lines[i], file), "\"[^\"]*\""))
                    {
                        foreach (char c in literal.Value)
                        {
                            if (!IsSafe(c))
                                offenders.Add($"{Path.GetFileName(file)}:{i + 1} U+{(int)c:X4} {c}");
                        }
                    }
                }
            }
            CollectionAssert.IsEmpty(offenders);
        }

        // ASCII, Latin-1 (× · ² ³ …) and the en/em dashes.
        private static bool IsSafe(char c) => c < 0x7F || (c >= 0xA0 && c <= 0xFF) || c == '–' || c == '—';

        private static string StripComment(string line, string file)
        {
            if (!file.EndsWith(".cs"))
                return line;
            int comment = line.IndexOf("//", StringComparison.Ordinal);
            return comment >= 0 && line.LastIndexOf('"', comment) < 0 ? line.Substring(0, comment) : line;
        }
    }
}
