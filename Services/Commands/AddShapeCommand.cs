using svg_editor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svg_editor.Services.Commands
{
    internal class AddShapeCommand : IUndoableCommand
    {
        private readonly SvgStore _store;
        private readonly ShapeModel _shape;

        public AddShapeCommand(SvgStore store, ShapeModel shape)
        {
            _store = store;
            _shape = shape;
        }

        public void Execute() => _store.Add(_shape);
        public void Undo() => _store.Remove(_shape);
    }
}
