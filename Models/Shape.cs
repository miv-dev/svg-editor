using CommunityToolkit.Mvvm.ComponentModel;
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
    public abstract partial class ShapeModel : ObservableObject
    {
        [ObservableProperty] private Vector2 position = new(100, 100);
        [ObservableProperty] private float width = 150;
        [ObservableProperty] private float height = 100;

        [ObservableProperty] private Color fill = Colors.White;
        [ObservableProperty] private Color stroke = Colors.Black;
        [ObservableProperty] private float strokeWidth = 2;

        public abstract string Kind { get; }
    }


    public sealed partial class RectangleModel : ShapeModel
    {
        [ObservableProperty] private float radiusX = 4;
        [ObservableProperty] private float radiusY = 4;
        public override string Kind => "rect";
    }

    public sealed partial class EllipseModel : ShapeModel
    {
        public override string Kind => "ellipse";
    }

    public sealed partial class TriangleModel : ShapeModel
    {
        public override string Kind => "triangle";
    }
}
