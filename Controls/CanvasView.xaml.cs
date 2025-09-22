using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using svg_editor.Models;
using svg_editor.Services;
using svg_editor.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace svg_editor.Controls
{
    public sealed partial class CanvasView : UserControl
    {
        private readonly SvgStore _store;
        private Guid? _hoverId;   

        private readonly MainViewModel _vm;



        public CanvasView()
        {
            InitializeComponent();

            _store =  App.GetService<SvgStore>();

            _vm = new MainViewModel(_store);

            DataContext = _vm;
            _store.Changed += (_, __) => Surface.Invalidate();
        }

        private static CanvasSvgNamedElement BuildGeneric(CanvasSvgDocument svg, System.Xml.Linq.XElement node)
        {
            var el = svg.Root.CreateAndAppendNamedChildElement(node.Name.LocalName);

            foreach (var a in node.Attributes())
                el.SetStringAttribute(a.Name.LocalName, a.Value);

            foreach (var child in node.Elements())
                BuildGeneric(svg, child);

            return el;
        }

        private CanvasSvgDocument BuildSvgForCurrentDevice(ICanvasResourceCreator rc)
        {
            var svg = new CanvasSvgDocument(rc);

            var root = svg.Root;
            root.SetStringAttribute("viewBox", $"0 0 {_store.Width} {_store.Height}");
            root.SetLengthAttribute("width", (float)_store.Width, CanvasSvgLengthUnits.Number);
            root.SetLengthAttribute("height", (float)_store.Height, CanvasSvgLengthUnits.Number);


            if (_store.RawDefs.Count > 0)
            {
                var defs = root.CreateAndAppendNamedChildElement("defs");
                foreach (var xml in _store.RawDefs)
                    defs.AppendChild(BuildGeneric(svg, System.Xml.Linq.XElement.Parse(xml)));
                root.AppendChild(defs);
            }


            foreach (var s in _store.Shapes)
            {
                CanvasSvgNamedElement el = s switch
                {
                    RectShape r => MakeRect(svg, r),
                    EllipseShape e => MakeEllipse(svg, e),
                    LineShape l => MakeLine(svg, l),
                    PolyShape p => MakePoly(svg, p),
                    PathShape p => MakePath(svg, p),
                    RawShape raw => MakeRaw(svg, raw),
                    _ => root.CreateAndAppendNamedChildElement("g")
                };
                root.AppendChild(el);
            }

            return svg;
        }




        private CanvasSvgNamedElement MakeRect(CanvasSvgDocument svg, RectShape r)
        {
            var el = svg.Root.CreateAndAppendNamedChildElement("rect");
            el.SetFloatAttribute("x", (float)r.X);
            el.SetFloatAttribute("y", (float)r.Y);
            el.SetFloatAttribute("width", (float)r.W);
            el.SetFloatAttribute("height", (float)r.H);
            ApplyStyle(svg, el, r.Style);
            return el;
        }

        private CanvasSvgNamedElement MakeEllipse(CanvasSvgDocument svg, EllipseShape e)
        {
            var el = svg.Root.CreateAndAppendNamedChildElement("ellipse");
            el.SetFloatAttribute("cx", (float)e.Cx);
            el.SetFloatAttribute("cy", (float)e.Cy);
            el.SetFloatAttribute("rx", (float)e.Rx);
            el.SetFloatAttribute("ry", (float)e.Ry);
            ApplyStyle(svg, el, e.Style);
            return el;
        }

        private CanvasSvgNamedElement MakeLine(CanvasSvgDocument svg, LineShape l)
        {
            var el = svg.Root.CreateAndAppendNamedChildElement("line");
            el.SetFloatAttribute("x1", (float)l.X1);
            el.SetFloatAttribute("y1", (float)l.Y1);
            el.SetFloatAttribute("x2", (float)l.X2);
            el.SetFloatAttribute("y2", (float)l.Y2);
            ApplyStyle(svg, el, l.Style with { FillArgb = null }); // no fill for lines
            return el;
        }

        private CanvasSvgNamedElement MakePoly(CanvasSvgDocument svg, PolyShape p)
        {
            var name = p.Closed ? "polygon" : "polyline";
            var el = svg.Root.CreateAndAppendNamedChildElement(name);
            var pts = svg.CreatePointsAttribute();
            pts.SetPoints(0, p.Points.ToArray());
            el.SetAttribute("points", pts);
            ApplyStyle(svg, el, p.Style);
            return el;
        }

        private CanvasSvgNamedElement MakePath(CanvasSvgDocument svg, PathShape p)
        {
            var el = svg.Root.CreateAndAppendNamedChildElement("path");
            // simplest: set raw SVG data
            el.SetStringAttribute("d", p.D);
            ApplyStyle(svg, el, p.Style);
            return el;
        }
        private static CanvasSvgNamedElement MakeRaw(CanvasSvgDocument svg, RawShape raw)
    => BuildGeneric(svg, System.Xml.Linq.XElement.Parse(raw.Xml));


        private static void ApplyStyle(CanvasSvgDocument svg, CanvasSvgNamedElement el, ShapeStyle s)
        {
            if (s.FillArgb is uint fill)
                el.SetAttribute("fill", svg.CreatePaintAttribute(CanvasSvgPaintType.Color, ToColor(fill), null));
            else
                el.SetStringAttribute("fill", "none");

            if (s.StrokeArgb is uint stroke)
            {
                el.SetAttribute("stroke", svg.CreatePaintAttribute(CanvasSvgPaintType.Color, ToColor(stroke), null));
                el.SetFloatAttribute("stroke-width", s.StrokeWidth);
                if (s.Dashes is { Length: > 0 })
                {
                    var da = svg.CreateStrokeDashArrayAttribute();
                    da.SetDashes(0, s.Dashes.Select(f => (float)f).ToArray());
                    el.SetAttribute("stroke-dasharray", da);
                }
            }
            else el.SetStringAttribute("stroke", "none");
        }

        private static Windows.UI.Color ToColor(uint argb)
            => Windows.UI.Color.FromArgb(
                (byte)((argb >> 24) & 0xFF),
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF));

        private void DrawOverlays(CanvasDrawingSession ds)
        {
            foreach (var s in _store.Shapes)
            {
                bool isHover = s.Id == _hoverId;
                bool isSel = s.Id == _store.SelectedId;
                if (!isHover && !isSel) continue;

                Rect? frame = s switch
                {
                    RectShape r => new Rect(r.X, r.Y, r.W, r.H),
                    EllipseShape e => new Rect(e.Cx - e.Rx, e.Cy - e.Ry, 2 * e.Rx, 2 * e.Ry),
                    LineShape l => new Rect(Math.Min(l.X1, l.X2), Math.Min(l.Y1, l.Y2),
                                            Math.Abs(l.X2 - l.X1), Math.Abs(l.Y2 - l.Y1)),
                    PolyShape p => BoundsOf(p.Points),
                    RawShape raw when raw.X.HasValue && raw.Y.HasValue && raw.W.HasValue && raw.H.HasValue
                                  => new Rect(raw.X!.Value, raw.Y!.Value, raw.W!.Value, raw.H!.Value),
                    _ => null
                };

                if (frame is null) continue; // для RAW без рамки — никакого hover/selection визуала

                var color = isSel
                    ? Windows.UI.Color.FromArgb(255, 0, 120, 255)
                    : Windows.UI.Color.FromArgb(200, 0, 200, 255);

                ds.DrawRectangle(frame.Value, color, isSel ? 2.5f : 1.5f);
            }
        }

        private static Rect BoundsOf(System.Numerics.Vector2[] pts)
        {
            double minX = pts.Min(p => p.X), maxX = pts.Max(p => p.X);
            double minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private static (Rect? frame, Rect[]? handles) OverlayGeometry(Shape s)
        {
            const float hs = 6f;
            switch (s)
            {
                case RectShape r:
                    var rr = new Rect(r.X, r.Y, r.W, r.H);
                    return (rr, new[]
                    {
                new Rect(rr.X-hs/2, rr.Y-hs/2, hs, hs),
                new Rect(rr.Right-hs/2, rr.Y-hs/2, hs, hs),
                new Rect(rr.X-hs/2, rr.Bottom-hs/2, hs, hs),
                new Rect(rr.Right-hs/2, rr.Bottom-hs/2, hs, hs),
            });

                case EllipseShape e:
                    var er = new Rect(e.Cx - e.Rx, e.Cy - e.Ry, 2 * e.Rx, 2 * e.Ry);
                    return (er, new[]
                    {
                new Rect(er.X-hs/2, er.Y-hs/2, hs, hs),
                new Rect(er.Right-hs/2, er.Y-hs/2, hs, hs),
                new Rect(er.X-hs/2, er.Bottom-hs/2, hs, hs),
                new Rect(er.Right-hs/2, er.Bottom-hs/2, hs, hs),
            });

                case LineShape l:
                    var lr = new Rect(Math.Min(l.X1, l.X2), Math.Min(l.Y1, l.Y2),
                                      Math.Abs(l.X2 - l.X1), Math.Abs(l.Y2 - l.Y1));
                    return (lr, new[]
                    {
                new Rect(l.X1 - hs/2, l.Y1 - hs/2, hs, hs),
                new Rect(l.X2 - hs/2, l.Y2 - hs/2, hs, hs),
            });

                case PolyShape p:
                    double minX = p.Points.Min(v => v.X), maxX = p.Points.Max(v => v.X);
                    double minY = p.Points.Min(v => v.Y), maxY = p.Points.Max(v => v.Y);
                    var pr = new Rect(minX, minY, maxX - minX, maxY - minY);
                    return (pr, new[]
                    {
                new Rect(pr.X-hs/2, pr.Y-hs/2, hs, hs),
                new Rect(pr.Right-hs/2, pr.Y-hs/2, hs, hs),
                new Rect(pr.X-hs/2, pr.Bottom-hs/2, hs, hs),
                new Rect(pr.Right-hs/2, pr.Bottom-hs/2, hs, hs),
            });

                default:
                    return (null, null); // path: no overlay yet
            }
        }


        private static void DrawGrid(CanvasDrawingSession ds, double w, double h, float step)
        {
            var c = Windows.UI.Color.FromArgb(30, 255, 255, 255);
            for (float x = 0; x <= (float)w; x += step)
                ds.DrawLine(new Vector2(x, 0), new Vector2(x, (float)h), c, 1);
            for (float y = 0; y <= (float)h; y += step)
                ds.DrawLine(new Vector2(0, y), new Vector2((float)w, y), c, 1);
        }


        private void Surface_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_store.HasArtboard) return;

            var p = e.GetCurrentPoint(Surface).Position;
            var hit = _store.HitTest(p.X, p.Y);
            if (hit != _hoverId) { _hoverId = hit; Surface.Invalidate(); }
        }

        private void Surface_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_hoverId != null) { _hoverId = null; Surface.Invalidate(); }
        }

        private void Surface_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!_store.HasArtboard) return;
            var pt = e.GetCurrentPoint(Surface);
            if (!pt.Properties.IsLeftButtonPressed) return;

            var hit = _store.HitTest(pt.Position.X, pt.Position.Y);
            _store.SetSelected(hit);           // updates and repaints via event
        }

        private void Surface_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;
            ds.Clear(Colors.White);

            if (!_store.HasArtboard) return;

            var svg = BuildSvgForCurrentDevice(sender);
            DrawGrid(ds, _store.Width, _store.Height, 50f);
            ds.DrawRectangle(new Rect(0, 0, _store.Width, _store.Height), Colors.DimGray);
            ds.DrawSvg(svg, new Size(_store.Width, _store.Height));

            // --- overlays (hover/selection) ---
            DrawOverlays(ds);
        }
    }
}
