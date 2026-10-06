using System.Collections.Generic;
using NUnit.Framework;
using StructureViewer.Application.Commands;

namespace StructureViewer.Tests.EditMode.Application
{
    public sealed class CommandHistoryTests
    {
        // Appends its name on Execute and removes it on Undo, so the log is the observable state.
        private sealed class LogCommand : ICommand
        {
            private readonly List<string> _log;
            private readonly string _name;

            public LogCommand(List<string> log, string name)
            {
                _log = log;
                _name = name;
            }

            public void Execute() => _log.Add(_name);

            public void Undo()
            {
                Assert.AreEqual(_name, _log[_log.Count - 1], "Undo out of order");
                _log.RemoveAt(_log.Count - 1);
            }
        }

        private List<string> _log;
        private CommandHistory _history;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _history = new CommandHistory();
        }

        [Test]
        public void Undo_AfterTwoCommands_RevertsInReverseOrder()
        {
            _history.Execute(new LogCommand(_log, "a"));
            _history.Execute(new LogCommand(_log, "b"));

            _history.Undo();
            CollectionAssert.AreEqual(new[] { "a" }, _log);

            _history.Undo();
            CollectionAssert.IsEmpty(_log);
            Assert.IsFalse(_history.CanUndo);
            Assert.IsTrue(_history.CanRedo);
        }

        [Test]
        public void Redo_AfterUndos_ReappliesInOriginalOrder()
        {
            _history.Execute(new LogCommand(_log, "a"));
            _history.Execute(new LogCommand(_log, "b"));
            _history.Undo();
            _history.Undo();

            _history.Redo();
            _history.Redo();

            CollectionAssert.AreEqual(new[] { "a", "b" }, _log);
            Assert.IsFalse(_history.CanRedo);
        }

        [Test]
        public void Execute_AfterUndo_ClearsRedo()
        {
            _history.Execute(new LogCommand(_log, "a"));
            _history.Undo();

            _history.Execute(new LogCommand(_log, "b"));
            _history.Redo();

            Assert.IsFalse(_history.CanRedo);
            CollectionAssert.AreEqual(new[] { "b" }, _log);
        }

        [Test]
        public void UndoRedoClear_EmptyHistory_NoOpWithoutChangedEvent()
        {
            int changes = 0;
            _history.Changed += () => changes++;

            _history.Undo();
            _history.Redo();
            _history.Clear();

            Assert.AreEqual(0, changes);
        }

        [Test]
        public void Changed_ExecuteUndoRedoClear_FiresEachTime()
        {
            int changes = 0;
            _history.Changed += () => changes++;

            _history.Execute(new LogCommand(_log, "a"));
            _history.Undo();
            _history.Redo();
            _history.Clear();

            Assert.AreEqual(4, changes);
        }

        [Test]
        public void Execute_BeyondCapacity_DropsOldest()
        {
            var history = new CommandHistory(capacity: 2);
            history.Execute(new LogCommand(_log, "a"));
            history.Execute(new LogCommand(_log, "b"));
            history.Execute(new LogCommand(_log, "c"));

            history.Undo();
            history.Undo();

            Assert.IsFalse(history.CanUndo);
            CollectionAssert.AreEqual(new[] { "a" }, _log);
        }

        [Test]
        public void Clear_WithUndoAndRedo_EmptiesBoth()
        {
            _history.Execute(new LogCommand(_log, "a"));
            _history.Execute(new LogCommand(_log, "b"));
            _history.Undo();

            _history.Clear();

            Assert.IsFalse(_history.CanUndo);
            Assert.IsFalse(_history.CanRedo);
        }
    }
}
