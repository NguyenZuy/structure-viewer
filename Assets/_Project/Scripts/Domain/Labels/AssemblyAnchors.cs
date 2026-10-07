using System;
using System.Collections.Generic;
using StructureViewer.Domain.Structure;
using StructureViewer.Domain.Visibility;
using UnityEngine;

namespace StructureViewer.Domain.Labels
{
    public sealed class AssemblyAnchor
    {
        public AssemblyAnchor(string group, Vector3 position, IReadOnlyList<int> elements)
        {
            Group = group;
            Position = position;
            Elements = elements;
        }

        public string Group { get; }

        // Top centre of the group's bounds, Unity space.
        public Vector3 Position { get; }
        public IReadOnlyList<int> Elements { get; }

        public bool AnyVisible(IVisibility visibility)
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                if (visibility.IsVisible(Elements[i]))
                    return true;
            }
            return false;
        }
    }

    public static class AssemblyAnchors
    {
        // One per group, in the model's group order. Computed once per load.
        public static IReadOnlyList<AssemblyAnchor> Build(StructureModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var anchors = new List<AssemblyAnchor>(model.Groups.Count);
            foreach (string group in model.Groups)
            {
                var elements = model.ElementsInGroup(group);
                var bounds = model.Elements[elements[0]].Bounds;
                for (int i = 1; i < elements.Count; i++)
                    bounds.Encapsulate(model.Elements[elements[i]].Bounds);
                anchors.Add(new AssemblyAnchor(group, new Vector3(bounds.center.x, bounds.max.y, bounds.center.z), elements));
            }
            return anchors;
        }
    }

    public static class LabelVisibilityRule
    {
        public const float MinOpacity = 0.35f;

        // Viewport point from Camera.WorldToViewportPoint (z = distance in front of the camera).
        // 0 = hidden: group invisible, behind the camera or off screen. Otherwise 1 up close, fading to MinOpacity far away.
        public static float Opacity(bool groupVisible, Vector3 viewport, float nearFade, float farFade)
        {
            if (!groupVisible || viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                return 0f;
            if (farFade <= nearFade)
                return 1f;
            return Mathf.Lerp(1f, MinOpacity, Mathf.InverseLerp(nearFade, farFade, viewport.z));
        }
    }
}
