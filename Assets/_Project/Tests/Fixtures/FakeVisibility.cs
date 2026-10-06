using System.Collections.Generic;
using StructureViewer.Domain.Visibility;

namespace StructureViewer.Tests.Fixtures
{
    public sealed class FakeVisibility : IVisibility
    {
        public FakeVisibility(params int[] hidden) => Hidden = new HashSet<int>(hidden);

        public HashSet<int> Hidden { get; }

        public bool IsVisible(int index) => !Hidden.Contains(index);

        public FakeVisibility Hide(params int[] indices)
        {
            Hidden.UnionWith(indices);
            return this;
        }
    }
}
