using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using svg_editor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace svg_editor.Controls
{

    public sealed class ShapeKindToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, string l) => value switch
        {
            ShapeType.Rect => "\uE71A",
            ShapeType.Ellipse => "\uEA3A",
            ShapeType.Line => "\uE738",
            ShapeType.Polyline => "\uEDFB",
            ShapeType.Polygon => "\uE879",
            ShapeType.Path => "\uF003",
            ShapeType.Raw => "\uE943",
            _ => "\uF156"
        };
        public object ConvertBack(object v, Type t, object p, string l) => throw new NotImplementedException();
    }

}
