using System;
using UnityEngine;

namespace StructureViewer.Domain.Structure
{
    // Exactly one of Member / Panel / Slab is set, matching Kind.
    public sealed class Element
    {
        private Element(int index, ElementKind kind, ElementInfo info, Member member, Panel panel, Slab slab, Bounds bounds)
        {
            Index = index;
            Kind = kind;
            Info = info ?? throw new ArgumentNullException(nameof(info));
            Member = member;
            Panel = panel;
            Slab = slab;
            Bounds = bounds;
        }

        public int Index { get; }
        public ElementKind Kind { get; }
        public ElementInfo Info { get; }
        public Member Member { get; }
        public Panel Panel { get; }
        public Slab Slab { get; }

        // World-space AABB (exact for slabs, slightly conservative for members and panels).
        public Bounds Bounds { get; }

        public static Element ForMember(int index, ElementInfo info, Member member) =>
            new Element(index, ElementKind.Member, info, member ?? throw new ArgumentNullException(nameof(member)), null, null, member.ComputeBounds());

        public static Element ForPanel(int index, ElementInfo info, Panel panel) =>
            new Element(index, ElementKind.Panel, info, null, panel ?? throw new ArgumentNullException(nameof(panel)), null, panel.ComputeBounds());

        public static Element ForSlab(int index, ElementInfo info, Slab slab) =>
            new Element(index, ElementKind.Slab, info, null, null, slab ?? throw new ArgumentNullException(nameof(slab)), slab.ComputeBounds());
    }
}
