using NUnit.Framework;
using StructureViewer.Application.Display;
using StructureViewer.Application.Events;
using StructureViewer.Domain.Display;
using StructureViewer.Presentation.Display;
using UnityEngine.UIElements;

namespace StructureViewer.Tests.EditMode.Composition
{
    public sealed class DisplayPickerTests
    {
        [Test]
        public void Picker_ReflectsSettings_AndShowsFieldsOnlyInColorBy()
        {
            var bus = new EventBus();
            var settings = new DisplaySettings(bus);
            var view = new DisplayPickerView();
            var presenter = new DisplayPickerPresenter(view, settings, bus);
            var buttons = view.Root.Query<Button>().ToList();

            Assert.IsTrue(buttons[0].ClassListContains(DisplayPickerView.ActiveClass), "Realistic active by default");
            var fieldRow = buttons[4].parent.parent;
            Assert.AreEqual(DisplayStyle.None, fieldRow.style.display.value);

            settings.SetMode(DisplayMode.ColorBy);

            Assert.IsTrue(buttons[1].ClassListContains(DisplayPickerView.ActiveClass));
            Assert.IsFalse(buttons[0].ClassListContains(DisplayPickerView.ActiveClass));
            Assert.AreEqual(DisplayStyle.Flex, fieldRow.style.display.value);
            presenter.Dispose();
        }
    }
}
