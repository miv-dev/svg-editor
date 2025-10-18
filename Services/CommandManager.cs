using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace svg_editor.Services
{
    public interface IUndoableCommand
    {
        void Execute();
        void Undo();
    }

    public sealed class CommandManager
    {
        private readonly Stack<IUndoableCommand> _undo = new();
        private readonly Stack<IUndoableCommand> _redo = new();

        public event EventHandler? Changed;

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Do(IUndoableCommand cmd)
        {
            cmd.Execute();
            _undo.Push(cmd);
            _redo.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var cmd = _undo.Pop();
            cmd.Undo();
            _redo.Push(cmd);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var cmd = _redo.Pop();
            cmd.Execute();
            _undo.Push(cmd);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}