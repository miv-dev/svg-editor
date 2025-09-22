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
    public sealed partial class SvgStore : ObservableObject
    {

        [ObservableProperty] private double width;
        [ObservableProperty] private double height;
        [ObservableProperty] private bool hasArtboard;



        public ObservableCollection<Shape> Shapes { get; } = new();
        public Guid? SelectedId { get; private set; }


        public event EventHandler? Changed;

        public void NewArtboard(double w, double h)
        {
            Width = w;
            Height = h;
            HasArtboard = true;

            Shapes.Clear(); SelectedId = null;
            OnChanged();
        }



        public Guid? HitTest(double x, double y)
        {

            for (int i = Shapes.Count - 1; i >= 0; i--)
                if (Hit(Shapes[i], x, y)) return Shapes[i].Id;
            return null;
        }



        private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);


        public void SetSelected(Guid? id)
        {
            if (SelectedId != id)
            {
                SelectedId = id;
                OnChanged();
            }
        }


        private static bool Hit(Shape s, double x, double y)
        => s switch
        {
            RectShape r => x >= r.X && x <= r.X + r.W && y >= r.Y && y <= r.Y + r.H,
            EllipseShape e => Math.Pow((x - e.Cx) / e.Rx, 2) + Math.Pow((y - e.Cy) / e.Ry, 2) <= 1.0,
            LineShape l => DistPointToSegment(x, y, l.X1, l.Y1, l.X2, l.Y2) <= Math.Max(6, l.Style.StrokeWidth + 4),
            PolyShape p => p.Closed
                                ? PointInPolygon(p.Points, new Vector2((float)x, (float)y))
                                : NearAnySegment(p.Points, new Vector2((float)x, (float)y), Math.Max(6, p.Style.StrokeWidth + 4)),
            PathShape _ => false,
            RawShape raw when raw.X.HasValue && raw.Y.HasValue && raw.W.HasValue && raw.H.HasValue =>
            x >= raw.X && x <= raw.X + raw.W && y >= raw.Y && y <= raw.Y + raw.H,
            _ => false
        };

        private static double Hypot(double x, double y)
        {
            x = Math.Abs(x);
            y = Math.Abs(y);
            if (x < y) { var t = x; x = y; y = t; }   // swap so x >= y
            if (x == 0) return 0;
            double r = y / x;
            return x * Math.Sqrt(1 + r * r);
        }

        private static double DistPointToSegment(double px, double py, double x1, double y1, double x2, double y2)
        {
            double vx = x2 - x1, vy = y2 - y1;
            double wx = px - x1, wy = py - y1;
            double c1 = vx * wx + vy * wy;
            if (c1 <= 0) return Hypot(px - x1, py - y1);
            double c2 = vx * vx + vy * vy;
            if (c2 <= c1) return Hypot(px - x2, py - y2);
            double b = c1 / c2;
            double bx = x1 + b * vx, by = y1 + b * vy;
            return Hypot(px - bx, py - by);
        }

        private static bool NearAnySegment(Vector2[] pts, Vector2 p, double tol)
        {
            if (pts.Length < 2) return false;
            for (int i = 0; i < pts.Length - 1; i++)
                if (DistPointToSegment(p.X, p.Y, pts[i].X, pts[i].Y, pts[i + 1].X, pts[i + 1].Y) <= tol)
                    return true;
            return false;
        }

        private static bool PointInPolygon(Vector2[] pts, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                var pi = pts[i]; var pj = pts[j];
                bool intersect = ((pi.Y > p.Y) != (pj.Y > p.Y)) &&
                                 (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y + 1e-6f) + pi.X);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        public ObservableCollection<string> RawDefs { get; } = new();

        public Guid AddRaw(string xml, double? x = null, double? y = null, double? w = null, double? h = null)
            => Add(new RawShape(Guid.NewGuid(), xml, x, y, w, h, DefaultStyle(), "Raw"));

        public Guid AddRect(double x, double y, double w, double h, ShapeStyle? style = null)
        => Add(new RectShape(Guid.NewGuid(), x, y, w, h, style ?? DefaultStyle(), "Rect"));

        public Guid AddEllipse(double cx, double cy, double rx, double ry, ShapeStyle? style = null)
            => Add(new EllipseShape(Guid.NewGuid(), cx, cy, rx, ry, style ?? DefaultStyle(), "Ellipse"));

        public Guid AddLine(double x1, double y1, double x2, double y2, ShapeStyle? style = null)
            => Add(new LineShape(Guid.NewGuid(), x1, y1, x2, y2, style ?? DefaultStyle() with { FillArgb = null }, "Line"));

        public Guid AddPolyline(Vector2[] pts, ShapeStyle? style = null)
            => Add(new PolyShape(Guid.NewGuid(), pts, false, style ?? DefaultStyle() with { FillArgb = null }, "Polyline"));

        public Guid AddPolygon(Vector2[] pts, ShapeStyle? style = null)
            => Add(new PolyShape(Guid.NewGuid(), pts, true, style ?? DefaultStyle(), "Polygon"));

        public Guid AddPath(string d, ShapeStyle? style = null)
            => Add(new PathShape(Guid.NewGuid(), d, style ?? DefaultStyle(), "Path"));

        private Guid Add(Shape s) { Shapes.Add(s); OnChanged(); return s.Id; }


        ShapeStyle DefaultStyle() => new();



    }
}





