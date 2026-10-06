using System.Collections.Generic;
using StructureViewer.Presentation.Contracts;
using UnityEngine;

namespace StructureViewer.Tests.Fixtures
{
    public sealed class FakeCameraControl : ICameraControl
    {
        public FakeCameraControl(Camera camera = null) => Camera = camera;

        public Camera Camera { get; set; }

        public List<Bounds> FitAllCalls { get; } = new List<Bounds>();
        public List<Bounds> FocusCalls { get; } = new List<Bounds>();

        public void FitAll(Bounds bounds) => FitAllCalls.Add(bounds);
        public void Focus(Bounds bounds) => FocusCalls.Add(bounds);
    }
}
