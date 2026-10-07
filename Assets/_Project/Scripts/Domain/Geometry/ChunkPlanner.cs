using System.Collections.Generic;

namespace StructureViewer.Domain.Geometry
{
    // One draw chunk = the visible elements of a group that share a material.
    public sealed class Chunk
    {
        public Chunk(int materialId) => MaterialId = materialId;

        public int MaterialId { get; }
        public List<int> Elements { get; } = new List<int>();
    }

    public static class ChunkPlanner
    {
        // Chunks come out in order of first appearance, so rebuilding an unchanged group yields the same chunks.
        public static List<Chunk> Plan(IReadOnlyList<int> groupElements, IReadOnlyList<bool> visible, IReadOnlyList<int> materialIds)
        {
            var chunks = new List<Chunk>();
            foreach (int element in groupElements)
            {
                if (!visible[element])
                    continue;

                int material = materialIds[element];
                Chunk chunk = null;
                foreach (var existing in chunks)
                {
                    if (existing.MaterialId == material)
                    {
                        chunk = existing;
                        break;
                    }
                }
                if (chunk == null)
                {
                    chunk = new Chunk(material);
                    chunks.Add(chunk);
                }
                chunk.Elements.Add(element);
            }
            return chunks;
        }
    }
}
