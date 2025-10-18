using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace svg_editor.Models
{
    public abstract class ShapeModel
    {
        public Vector2 Position { get; set; } = new(100, 100);
        public float Width { get; set; } = 150;
        public float Height { get; set; } = 100;

        public Color Fill { get; set; } = Colors.White;
        public Color Stroke { get; set; } = Colors.Black;
        public float StrokeWidth { get; set; } = 2;

        public abstract string Kind { get; }
    }

    public sealed class RectangleModel : ShapeModel
    {
        public float RadiusX { get; set; } = 4;
        public float RadiusY { get; set; } = 4;
        public override string Kind => "rect";
    }

    public sealed class EllipseModel : ShapeModel
    {
        public override string Kind => "ellipse";
    }

    public sealed class TriangleModel : ShapeModel
    {
        // Equilateral-ish triangle inside Width/Height at Position
        public override string Kind => "triangle";
    }
}
