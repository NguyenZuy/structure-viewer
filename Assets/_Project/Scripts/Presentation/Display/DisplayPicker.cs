using System;
using StructureViewer.Application.Display;
using StructureViewer.Application.Events;
using StructureViewer.Domain.Display;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Display
{
    public interface IDisplayPickerView
    {
        event Action<DisplayMode> ModeClicked;
        event Action<ColorByField> FieldClicked;

        // The field row only shows in "Color by".
        void Render(DisplayMode mode, ColorByField field);
    }

    // The toolbar "Display ▾" content: one button per mode, plus the Category / Type / Level sub-selector.
    // Built in code and styled by the shell's button classes (it always lives inside the shell's sheet).
    public sealed class DisplayPickerView : IDisplayPickerView
    {
        public const string ActiveClass = "sv-toolbar__item--active";

        private static readonly (DisplayMode Mode, string Label)[] Modes =
        {
            (DisplayMode.Realistic, "1  Realistic"), (DisplayMode.ColorBy, "2  Color by"), (DisplayMode.XRay, "3  X-ray"), (DisplayMode.Clay, "4  Clay")
        };

        private static readonly (ColorByField Field, string Label)[] Fields =
        {
            (ColorByField.Category, "Category"), (ColorByField.Type, "Type"), (ColorByField.Level, "Level")
        };

        private readonly Button[] _modeButtons = new Button[Modes.Length];
        private readonly Button[] _fieldButtons = new Button[Fields.Length];
        private readonly VisualElement _fieldRow;

        public DisplayPickerView()
        {
            Root = new VisualElement { name = "display-picker" };
            Root.style.paddingLeft = 12;
            Root.style.paddingRight = 12;
            Root.style.paddingBottom = 12;

            var modeRow = Row();
            for (int i = 0; i < Modes.Length; i++)
            {
                var mode = Modes[i].Mode;
                _modeButtons[i] = AddButton(modeRow, Modes[i].Label, () => ModeClicked?.Invoke(mode));
            }

            var caption = new Label("Color by");
            caption.style.marginTop = 8;
            _fieldRow = new VisualElement();
            _fieldRow.Add(caption);
            var fieldButtons = Row();
            _fieldRow.Add(fieldButtons);
            for (int i = 0; i < Fields.Length; i++)
            {
                var field = Fields[i].Field;
                _fieldButtons[i] = AddButton(fieldButtons, Fields[i].Label, () => FieldClicked?.Invoke(field));
            }

            Root.Add(modeRow);
            Root.Add(_fieldRow);
        }

        public VisualElement Root { get; }

        public event Action<DisplayMode> ModeClicked;
        public event Action<ColorByField> FieldClicked;

        public void Render(DisplayMode mode, ColorByField field)
        {
            for (int i = 0; i < Modes.Length; i++)
                _modeButtons[i].EnableInClassList(ActiveClass, Modes[i].Mode == mode);
            for (int i = 0; i < Fields.Length; i++)
                _fieldButtons[i].EnableInClassList(ActiveClass, Fields[i].Field == field);
            _fieldRow.style.display = mode == DisplayMode.ColorBy ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            return row;
        }

        private static Button AddButton(VisualElement row, string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("sv-button");
            button.style.marginRight = 8;
            button.style.marginTop = 8;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            row.Add(button);
            return button;
        }
    }

    public sealed class DisplayPickerPresenter : IDisposable
    {
        private readonly IDisplayPickerView _view;
        private readonly DisplaySettings _settings;
        private readonly IDisposable _changed;

        public DisplayPickerPresenter(IDisplayPickerView view, DisplaySettings settings, EventBus bus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            _view.ModeClicked += _settings.SetMode;
            _view.FieldClicked += _settings.SetField;
            _changed = bus.Subscribe<DisplayModeChanged>(evt => _view.Render(evt.Mode, evt.Field));
            _view.Render(_settings.Mode, _settings.Field);
        }

        public void Dispose()
        {
            _view.ModeClicked -= _settings.SetMode;
            _view.FieldClicked -= _settings.SetField;
            _changed.Dispose();
        }
    }
}
