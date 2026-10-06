using NUnit.Framework;
using StructureViewer.Domain.Structure;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Structure
{
    public sealed class ModelAxesTests
    {
        private const float Eps = 1e-4f;

        [Test]
        public void ToUnity_ModelPoint_SwapsYZAndConvertsToMetres()
        {
            var unity = ModelAxes.ToUnity(4200f, 3600f, 7000f);

            Assert.AreEqual(4.2f, unity.x, Eps);
            Assert.AreEqual(7.0f, unity.y, Eps);
            Assert.AreEqual(3.6f, unity.z, Eps);
        }

        [Test]
        public void ToModel_AfterToUnity_RoundTripsExactly()
        {
            var model = new Vector3(10000f, 8000f, 5400f);

            var roundTrip = ModelAxes.ToModel(ModelAxes.ToUnity(model));

            Assert.AreEqual(model.x, roundTrip.x, Eps);
            Assert.AreEqual(model.y, roundTrip.y, Eps);
            Assert.AreEqual(model.z, roundTrip.z, Eps);
        }

        [Test]
        public void ToUnity_ModelUp_IsUnityUp()
        {
            Assert.AreEqual(Vector3.up, ModelAxes.ToUnity(new Vector3(0f, 0f, 1000f)));
        }

        [Test]
        public void PlanToUnity_PlanPoint_MapsToUnityXZInMetres()
        {
            Assert.AreEqual(new Vector2(10f, 8f), ModelAxes.PlanToUnity(10000f, 8000f));
        }

        [Test]
        public void Lengths_RoundTrip()
        {
            Assert.AreEqual(0.035f, ModelAxes.ToUnityLength(35f), 1e-6f);
            Assert.AreEqual(35f, ModelAxes.ToModelLength(0.035f), Eps);
        }
    }
}
