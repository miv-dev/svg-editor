using CommunityToolkit.Mvvm.ComponentModel;
using svg_editor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace svg_editor.Services
{
    public sealed class SvgStore
    {
        private readonly List<ShapeModel> _shapes = new();

        public event EventHandler? Changed;

        public IReadOnlyList<ShapeModel> Shapes => _shapes;

        public float CanvasWidth { get; private set; } = 1600;
        public float CanvasHeight { get; private set; } = 1200;

        public void Clear()
        {
            _shapes.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void SetCanvas(float w, float h)
        {
            CanvasWidth = w;
            CanvasHeight = h;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Add(ShapeModel shape)
        {
            _shapes.Add(shape);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(ShapeModel shape)
        {
            _shapes.Remove(shape);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}





