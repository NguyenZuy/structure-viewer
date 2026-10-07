using System;
using System.Collections.Generic;
using StructureViewer.Presentation.Contracts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StructureViewer.Presentation.Display
{
    // Shared colour variants of the template materials: one clone per (template, colour), created on first use and
    // destroyed on Dispose. Clones keep the template's surface type (variants for switching it may be stripped).
    public sealed class MaterialLibrary : IDisposable
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private readonly RenderingConfig _config;
        private readonly Dictionary<(Material Template, Color Color), Material> _cache = new Dictionary<(Material, Color), Material>();

        public MaterialLibrary(RenderingConfig config) => _config = config ? config : throw new ArgumentNullException(nameof(config));

        public RenderingConfig Config => _config;
        public int Count => _cache.Count;

        public Material Get(Material template, Color color)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            var key = (template, color);
            if (!_cache.TryGetValue(key, out var material))
            {
                material = new Material(template) { name = $"{template.name} #{ColorUtility.ToHtmlStringRGBA(color)}" };
                material.SetColor(BaseColor, color);
                _cache.Add(key, material);
            }
            return material;
        }

        // Template colour multiplied by the tint, keeping the template's alpha (textured looks stay translucent where they were).
        public Material Tint(Material template, Color tint)
        {
            tint.a = template.GetColor(BaseColor).a;
            return Get(template, tint);
        }

        public Material Flat(Color color)
        {
            color.a = 1f;
            return Get(_config.FlatOpaque, color);
        }

        public Material Translucent(Color color, float alpha)
        {
            color.a = alpha;
            return Get(_config.FlatTransparent, color);
        }

        public void Dispose()
        {
            foreach (var material in _cache.Values)
            {
                if (UnityEngine.Application.isPlaying)
                    Object.Destroy(material);
                else
                    Object.DestroyImmediate(material);
            }
            _cache.Clear();
        }
    }
}
