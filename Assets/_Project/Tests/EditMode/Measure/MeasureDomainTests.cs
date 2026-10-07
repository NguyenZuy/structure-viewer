using NUnit.Framework;
using StructureViewer.Domain.Measure;
using StructureViewer.Tests.Fixtures;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Measure
{
    public sealed class MeasureDomainTests
    {
        private static readonly Vector3 Hit = new Vector3(1f, 2f, 3f);

        [Test]
        public void Snap_InsideRadius_SnapsToCandidate()
        {
            var candidates = new[] { new SnapCandidate(Vector3.one, new Vector2(105f, 100f), SnapKind.Endpoint) };

            var point = MeasureSnapper.Snap(Hit, new Vector2(100f, 100f), candidates, 12f);

            Assert.AreEqual(SnapKind.Endpoint, point.Kind);
            Assert.AreEqual(Vector3.one, point.World);
        }

        [Test]
        public void Snap_TwoInside_NearestWins()
        {
            var candidates = new[]
            {
                new SnapCandidate(Vector3.one, new Vector2(110f, 100f), SnapKind.Endpoint),
                new SnapCandidate(Vector3.up, new Vector2(103f, 100f), SnapKind.Midpoint)
            };

            var point = MeasureSnapper.Snap(Hit, new Vector2(100f, 100f), candidates, 12f);

            Assert.AreEqual(SnapKind.Midpoint, point.Kind);
            Assert.AreEqual(Vector3.up, point.World);
        }

        [Test]
        public void Snap_OutsideRadius_IsFreeAtTheHit()
        {
            var candidates = new[] { new SnapCandidate(Vector3.one, new Vector2(130f, 100f), SnapKind.Endpoint) };

            var point = MeasureSnapper.Snap(Hit, new Vector2(100f, 100f), candidates, 12f);

            Assert.AreEqual(SnapKind.Free, point.Kind);
            Assert.AreEqual(Hit, point.World);
        }

        [Test]
        public void Session_PicksAThenB_ThenAThirdPickStartsOver()
        {
            var session = new MeasureSession();
            var a = new MeasurePoint(Vector3.zero, SnapKind.Free);
            var b = new MeasurePoint(Vector3.right, SnapKind.Endpoint);
            var c = new MeasurePoint(Vector3.up, SnapKind.Midpoint);

            Assert.IsFalse(session.Pick(a), "idle ignores picks");
            session.Start();
            Assert.AreEqual(MeasureState.AwaitingA, session.State);

            session.Pick(a);
            Assert.AreEqual(MeasureState.AwaitingB, session.State);
            session.Pick(b);
            Assert.AreEqual(MeasureState.Done, session.State);
            Assert.AreEqual(1000f, session.Result.Distance, 1e-3f);

            session.Pick(c);
            Assert.AreEqual(MeasureState.AwaitingB, session.State);
            Assert.AreEqual(c.World, session.A.World);
            Assert.IsFalse(session.HasResult);
        }

        [Test]
        public void Session_CancelFromEveryState_GoesIdle()
        {
            var point = new MeasurePoint(Vector3.zero, SnapKind.Free);
            for (int picks = 0; picks <= 2; picks++)
            {
                var session = new MeasureSession();
                session.Start();
                for (int i = 0; i < picks; i++)
                    session.Pick(point);

                session.Cancel();

                Assert.AreEqual(MeasureState.Idle, session.State, $"after {picks} picks");
                Assert.IsFalse(session.HasA);
            }
        }

        [Test]
        public void Result_VerticalStud_IsAllDz()
        {
            var model = TestStructures.MiniHouse();
            var stud = model.Elements[model.IndexOf(TestStructures.VerticalStudId)].Member;

            var result = MeasureResult.Between(stud.Start, stud.End);

            Assert.AreEqual(stud.Length * 1000f, result.Distance, 1e-2f);
            Assert.AreEqual(stud.Length * 1000f, result.Delta.z, 1e-2f);
            Assert.AreEqual(0f, result.Delta.x, 1e-3f);
            Assert.AreEqual(0f, result.Delta.y, 1e-3f);
        }

        [Test]
        public void Result_PlanDepthIsDy_AndDeltasAreAbsolute()
        {
            var result = MeasureResult.Between(new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 0f));

            Assert.AreEqual(2000f, result.Delta.y, 1e-3f);
            Assert.AreEqual(0f, result.Delta.z, 1e-3f);
        }
    }
}
