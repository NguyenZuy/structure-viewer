using System;
using System.Collections.Generic;

namespace StructureViewer.Application.Commands
{
    public sealed class CommandHistory
    {
        public const int DefaultCapacity = 100;

        private readonly int _capacity;
        private readonly LinkedList<ICommand> _undo = new LinkedList<ICommand>();
        private readonly Stack<ICommand> _redo = new Stack<ICommand>();

        public CommandHistory(int capacity = DefaultCapacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        public event Action Changed;

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Execute(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Execute();
            _undo.AddLast(command);
            if (_undo.Count > _capacity)
                _undo.RemoveFirst();
            _redo.Clear();
            Changed?.Invoke();
        }

        public void Undo()
        {
            if (!CanUndo)
                return;

            var command = _undo.Last.Value;
            _undo.RemoveLast();
            command.Undo();
            _redo.Push(command);
            Changed?.Invoke();
        }

        public void Redo()
        {
            if (!CanRedo)
                return;

            var command = _redo.Pop();
            command.Execute();
            _undo.AddLast(command);
            Changed?.Invoke();
        }

        public void Clear()
        {
            if (!CanUndo && !CanRedo)
                return;

            _undo.Clear();
            _redo.Clear();
            Changed?.Invoke();
        }
    }
}
