using NUnit.Framework;
using StructureViewer.Infrastructure.Quality;

namespace StructureViewer.Tests.EditMode.Quality
{
    public sealed class QualitySelectorTests
    {
        private static readonly string[] ProjectLevels = { "Mobile", "PC" };

        [Test]
        public void SelectLevel_Mobile_ReturnsMobileLevel()
        {
            Assert.AreEqual(0, QualitySelector.SelectLevel(true, ProjectLevels));
        }

        [Test]
        public void SelectLevel_Desktop_ReturnsPcLevel()
        {
            Assert.AreEqual(1, QualitySelector.SelectLevel(false, ProjectLevels));
        }

        [Test]
        public void SelectLevel_LevelOrderChanged_MatchesByName()
        {
            Assert.AreEqual(0, QualitySelector.SelectLevel(false, new[] { "PC", "Mobile" }));
        }

        [Test]
        public void SelectLevel_LevelMissing_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, QualitySelector.SelectLevel(true, new[] { "Low", "High" }));
        }
    }
}
