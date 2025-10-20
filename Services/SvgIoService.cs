using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.UI;
using svg_editor.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;

namespace svg_editor.Services
{
    public sealed class SvgImportResult
    {
        public float? Width { get; init; }
        public float? Height { get; init; }
        public RectangleModel[] Rects { get; init; } = Array.Empty<RectangleModel>();
        public EllipseModel[] Ellipses { get; init; } = Array.Empty<EllipseModel>();
        public TriangleModel[] Triangles { get; init; } = Array.Empty<TriangleModel>();
        public string? UnsupportedSvgXml { get; init; }
        public int UnsupportedCount { get; init; }
    }

    public static class SvgImporter
    {
        static readonly XNamespace svg = "http://www.w3.org/2000/svg";
        static readonly NumberFormatInfo N = CultureInfo.InvariantCulture.NumberFormat;

        public static SvgImportResult Parse(string xml)
        {
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            var root = doc.Root ?? throw new InvalidOperationException("SVG root not found.");

            // Считаем размеры (width/height или из viewBox)
            (float? w, float? h) = ReadSize(root);

            // Соберём распознанные фигуры
            var rects = root.Descendants(svg + "rect")
                .Select(TryRect).Where(r => r != null)!.ToArray();

            // <circle> -> ellipse
            var circlesAsEllipses = root.Descendants(svg + "circle")
                .Select(TryCircle).Where(e => e != null)!.ToArray();

            var ellipses = root.Descendants(svg + "ellipse")
                .Select(TryEllipse).Where(e => e != null)!.Concat(circlesAsEllipses).ToArray();

            // Треугольники (polygon с 3 точками)
            var triangles = root.Descendants(svg + "polygon")
                .Select(TryTriangleFromPolygon).Where(t => t != null)!.ToArray();

            // Удалим распознанные элементы и оставшееся считаем "неподдержанным"
            var clone = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            RemoveRecognized(clone);
            var unsupportedNodes = clone.Root!.Descendants().Where(e => e.Name != svg + "svg").ToList();
            string? unsupportedXml = unsupportedNodes.Any() ? clone.ToString(SaveOptions.DisableFormatting) : null;
            int unsupportedCount = unsupportedNodes.Count;

            return new SvgImportResult
            {
                Width = w,
                Height = h,
                Rects = rects!,
                Ellipses = ellipses!,
                Triangles = triangles!,
                UnsupportedSvgXml = unsupportedXml,
                UnsupportedCount = unsupportedCount
            };
        }

        private static (float? w, float? h) ReadSize(XElement root)
        {
            float? w = TryFloat(root.Attribute("width")?.Value);
            float? h = TryFloat(root.Attribute("height")?.Value);
            if (w == null || h == null)
            {
                var vb = root.Attribute("viewBox")?.Value?.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (vb != null && vb.Length == 4)
                {
                    // viewBox: minX minY width height
                    w = TryFloat(vb[2]); h = TryFloat(vb[3]);
                }
            }
            return (w, h);
        }

        private static RectangleModel? TryRect(XElement el)
        {
            float? x = TryFloat(el.Attribute("x")?.Value) ?? 0;
            float? y = TryFloat(el.Attribute("y")?.Value) ?? 0;
            float? w = TryFloat(el.Attribute("width")?.Value);
            float? h = TryFloat(el.Attribute("height")?.Value);
            if (w == null || h == null) return null;

            var rx = TryFloat(el.Attribute("rx")?.Value) ?? 0;
            var ry = TryFloat(el.Attribute("ry")?.Value) ?? 0;

            var (fill, stroke, sw) = ReadPaint(el);

            return new RectangleModel
            {
                Position = new Vector2(x.Value, y.Value),
                Width = w.Value,
                Height = h.Value,
                RadiusX = rx,
                RadiusY = ry,
                Fill = fill,
                Stroke = stroke,
                StrokeWidth = sw
            };
        }

        private static EllipseModel? TryCircle(XElement el)
        {
            float? cx = TryFloat(el.Attribute("cx")?.Value);
            float? cy = TryFloat(el.Attribute("cy")?.Value);
            float? r = TryFloat(el.Attribute("r")?.Value);
            if (cx == null || cy == null || r == null) return null;

            var (fill, stroke, sw) = ReadPaint(el);

            return new EllipseModel
            {
                Position = new Vector2(cx.Value - r.Value, cy.Value - r.Value),
                Width = r.Value * 2f,
                Height = r.Value * 2f,
                Fill = fill,
                Stroke = stroke,
                StrokeWidth = sw
            };
        }

        private static EllipseModel? TryEllipse(XElement el)
        {
            float? cx = TryFloat(el.Attribute("cx")?.Value);
            float? cy = TryFloat(el.Attribute("cy")?.Value);
            float? rx = TryFloat(el.Attribute("rx")?.Value);
            float? ry = TryFloat(el.Attribute("ry")?.Value);
            if (cx == null || cy == null || rx == null || ry == null) return null;

            var (fill, stroke, sw) = ReadPaint(el);

            return new EllipseModel
            {
                Position = new Vector2(cx.Value - rx.Value, cy.Value - ry.Value),
                Width = rx.Value * 2f,
                Height = ry.Value * 2f,
                Fill = fill,
                Stroke = stroke,
                StrokeWidth = sw
            };
        }

        private static TriangleModel? TryTriangleFromPolygon(XElement el)
        {
            var ptsStr = el.Attribute("points")?.Value;
            if (string.IsNullOrWhiteSpace(ptsStr)) return null;

            var pts = ptsStr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(TryFloat).Where(v => v != null).Select(v => v!.Value).ToArray();
            if (pts.Length != 6) return null; // не 3 точки

            var p1 = new Vector2(pts[0], pts[1]);
            var p2 = new Vector2(pts[2], pts[3]);
            var p3 = new Vector2(pts[4], pts[5]);

            // нормализуем в рамку Position/Width/Height
            float minX = MathF.Min(p1.X, MathF.Min(p2.X, p3.X));
            float maxX = MathF.Max(p1.X, MathF.Max(p2.X, p3.X));
            float minY = MathF.Min(p1.Y, MathF.Min(p2.Y, p3.Y));
            float maxY = MathF.Max(p1.Y, MathF.Max(p2.Y, p3.Y));

            var (fill, stroke, sw) = ReadPaint(el);

            return new TriangleModel
            {
                Position = new Vector2(minX, minY),
                Width = MathF.Max(1, maxX - minX),
                Height = MathF.Max(1, maxY - minY),
                Fill = fill,
                Stroke = stroke,
                StrokeWidth = sw
            };
        }

        private static (Color fill, Color stroke, float strokeWidth) ReadPaint(XElement el)
        {
            // Простой разбор fill/stroke/stroke-width из атрибутов или style
            string? style = el.Attribute("style")?.Value;
            string? fillStr = el.Attribute("fill")?.Value;
            string? strokeStr = el.Attribute("stroke")?.Value;
            string? swStr = el.Attribute("stroke-width")?.Value;

            if (!string.IsNullOrWhiteSpace(style))
            {
                var parts = style.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var kv = p.Split(':');
                    if (kv.Length != 2) continue;
                    var k = kv[0].Trim(); var v = kv[1].Trim();
                    if (k == "fill") fillStr = v;
                    else if (k == "stroke") strokeStr = v;
                    else if (k == "stroke-width") swStr = v;
                }
            }

            var fill = ParseColor(fillStr) ?? Colors.Transparent;
            var stroke = ParseColor(strokeStr) ?? Colors.Black;
            var sw = TryFloat(swStr) ?? 1f;

            return (fill, stroke, sw);
        }

        private static Color? ParseColor(string? s)
        {
            if (string.IsNullOrWhiteSpace(s) || s == "none") return null;
            // Только #RRGGBB и #RGB для простоты
            if (s.StartsWith("#"))
            {
                if (s.Length == 7)
                {
                    byte r = Convert.ToByte(s.Substring(1, 2), 16);
                    byte g = Convert.ToByte(s.Substring(3, 2), 16);
                    byte b = Convert.ToByte(s.Substring(5, 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
                if (s.Length == 4)
                {
                    byte r = Convert.ToByte(new string(s[1], 2), 16);
                    byte g = Convert.ToByte(new string(s[2], 2), 16);
                    byte b = Convert.ToByte(new string(s[3], 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
            }
            // можно расширить до именованных цветов
            return null;
        }

        private static float? TryFloat(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            // уберем px, pt и т.д.
            s = new string(s.Where(ch => char.IsDigit(ch) || ch == '.' || ch == '-' || ch == '+' || ch == 'e' || ch == 'E').ToArray());
            if (float.TryParse(s, NumberStyles.Float, N, out var f)) return f;
            return null;
        }

        private static void RemoveRecognized(XDocument clone)
        {
            var r = clone.Root!;
            // Удаляем только явно распознанные: rect, circle, ellipse, polygon(3pt)
            r.Descendants().Where(e => e.Name == svg + "rect").ToList().ForEach(e => e.Remove());
            r.Descendants().Where(e => e.Name == svg + "circle").ToList().ForEach(e => e.Remove());
            r.Descendants().Where(e => e.Name == svg + "ellipse").ToList().ForEach(e => e.Remove());
            foreach (var poly in r.Descendants(svg + "polygon").ToList())
            {
                var pts = poly.Attribute("points")?.Value;
                if (pts is null) continue;
                var count = pts.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Length / 2;
                if (count == 3) poly.Remove();
            }
            // Всё остальное останется как "unsupported"
        }
    }
}
