using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Notices
{
    public interface INoticeView
    {
        event Action ActionClicked;
        event Action CloseClicked;

        // actionLabel null = no action button.
        void Show(string message, string actionLabel);
        void Hide();
    }

    // A small card centred under the toolbar, over the 3D view. Only the card takes pointer input.
    // Built in code and styled with the shell's tokens/classes (it lives in the shell document).
    public sealed class NoticeView : INoticeView
    {
        private const float TopOffset = 64f;

        private readonly Label _message;
        private readonly Button _action;

        public NoticeView(string name)
        {
            Root = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.right = 0;
            Root.style.top = TopOffset;
            Root.style.alignItems = Align.Center;
            Root.style.display = DisplayStyle.None;

            var card = new VisualElement();
            card.AddToClassList("sv-toast");
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.marginLeft = 12;
            card.style.marginRight = 12;
            Root.Add(card);

            _message = new Label();
            _message.style.flexShrink = 1;
            _message.style.whiteSpace = WhiteSpace.Normal;
            card.Add(_message);

            _action = new Button(() => ActionClicked?.Invoke());
            _action.AddToClassList("sv-button");
            _action.style.marginLeft = 8;
            _action.style.color = new Color(1f, 0.541f, 0.122f);
            card.Add(_action);

            var close = new Button(() => CloseClicked?.Invoke()) { text = "×", tooltip = "Dismiss" };
            close.AddToClassList("sv-button");
            close.style.marginLeft = 4;
            card.Add(close);
        }

        public VisualElement Root { get; }
        public bool IsShown => Root.style.display.value == DisplayStyle.Flex;

        public event Action ActionClicked;
        public event Action CloseClicked;

        public void Show(string message, string actionLabel)
        {
            _message.text = message;
            _action.text = actionLabel;
            _action.style.display = actionLabel != null ? DisplayStyle.Flex : DisplayStyle.None;
            Root.style.display = DisplayStyle.Flex;
        }

        public void Hide() => Root.style.display = DisplayStyle.None;
    }
}
