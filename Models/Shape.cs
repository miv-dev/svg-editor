using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace svg_editor.Models
{
    public abstract record Shape(Guid Id, ShapeType Kind, ShapeStyle Style, string Name);

    public record RectShape(Guid Id, double X, double Y, double W, double H, ShapeStyle Style, string Name)
      : Shape(Id, ShapeType.Rect, Style, Name);

    public record EllipseShape(Guid Id, double Cx, double Cy, double Rx, double Ry, ShapeStyle Style, string Name)
      : Shape(Id, ShapeType.Ellipse, Style, Name);

    public record LineShape(Guid Id, double X1, double Y1, double X2, double Y2, ShapeStyle Style, string Name)
      : Shape(Id, ShapeType.Line, Style, Name);

    public record PolyShape(Guid Id, Vector2[] Points, bool Closed, ShapeStyle Style, string Name)
      : Shape(Id, Closed ? ShapeType.Polygon : ShapeType.Polyline, Style, Name);

    public record PathShape(Guid Id, string D, ShapeStyle Style, string Name)
      : Shape(Id, ShapeType.Path, Style, Name);

    public record RawShape(
        Guid Id,
        string Xml,
        double? X, double? Y,
        double? W, double? H,
        ShapeStyle Style, string Name
    ) : Shape(Id, ShapeType.Raw, Style, Name);

    public enum ShapeType { Rect, Ellipse, Line, Polyline, Polygon, Path, Raw }

    public record ShapeStyle(
        uint? FillArgb = 0xFFFFFFFF,    // null => no fill
        uint? StrokeArgb = 0xFF222222,  // null => no stroke
        float StrokeWidth = 1.5f,
        float[]? Dashes = null
    );
}
