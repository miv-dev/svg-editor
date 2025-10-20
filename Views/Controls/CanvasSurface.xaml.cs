using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.Graphics.Canvas.UI.Xaml;
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
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace svg_editor.Views.Controls
{
    public sealed partial class CanvasSurface : UserControl
    {

        private CanvasSvgDocument? _cachedUnsupportedDoc;
        private string? _cachedUnsupportedXml;
        private MainViewModel VM => (MainViewModel)DataContext!;
        private SvgStore Store => VM.Store;
        private AppState AppState => VM.AppState;

        public CanvasSurface()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                // Ensure DataContext is set to MainViewModel, not AppState
                DataContext = App.GetService<MainViewModel>(); // Ensure this gets the MainViewModel
            }

            // Now you can safely access VM
            var vm = (MainViewModel)DataContext;

            // Size the paper and canvas to the logical canvas size
            Paper.Width = Store.CanvasWidth;
            Paper.Height = Store.CanvasHeight;
            Win2D.Width = Store.CanvasWidth;
            Win2D.Height = Store.CanvasHeight;

            Store.Changed += Store_Changed;
            AppState.PropertyChanged += AppState_Changed;

            Win2D.Invalidate(); // initial render
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Store.Changed -= Store_Changed;
            AppState.PropertyChanged -= AppState_Changed;
        }

        private void AppState_Changed(object? sender, EventArgs e)
        {
            // If you want to reflect mode/tool visually, trigger a redraw.
            Win2D.Invalidate();
        }

        private void Store_Changed(object? sender, EventArgs e)
        {
            // Reflect store changes and size updates
            Paper.Width = Store.CanvasWidth;
            Paper.Height = Store.CanvasHeight;
            Win2D.Width = Store.CanvasWidth;
            Win2D.Height = Store.CanvasHeight;

            // если поменялся xml «неподдержанного», сбросим кэш
            if (Store.UnsupportedSvgXml != _cachedUnsupportedXml)
            {
                _cachedUnsupportedXml = Store.UnsupportedSvgXml;
                _cachedUnsupportedDoc?.Dispose();
                _cachedUnsupportedDoc = null;
            }

            Win2D.Invalidate();
        }

        // === Input: add shape on click when in Edit mode with a tool selected ===
        private void Win2D_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pt = e.GetCurrentPoint(Win2D);
            if (!pt.Properties.IsLeftButtonPressed) return;

            // Single-click: if in EDIT mode with tool selected -> add shape
            if (AppState.Mode == EditorMode.Edit && AppState.ActiveTool != Tool.None)
            {
                VM.OnCanvasTapped((float)pt.Position.X, (float)pt.Position.Y);
                return;
            }

            // Single-click without tool: select top-most shape
            var pos = new Vector2((float)pt.Position.X, (float)pt.Position.Y);
            var top = Store.HitTestTop(pos);
            Store.SetSelected(top);
        }



        // === Win2D Render ===
        private void Win2D_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;


            if (!string.IsNullOrWhiteSpace(Store.UnsupportedSvgXml))
            {
                if (_cachedUnsupportedDoc == null)
                {
                    try
                    {
                        _cachedUnsupportedDoc = CanvasSvgDocument.LoadFromXml(sender.Device, Store.UnsupportedSvgXml);
                    }
                    catch
                    {
                        // Если svg слишком сложный — тишина; можно вывести плашку-текст
                    }
                }

                if (_cachedUnsupportedDoc != null)
                {
                    // Рисуем в размерах полотна
                    ds.DrawSvg(_cachedUnsupportedDoc, new Size(Store.CanvasWidth, Store.CanvasHeight));
                }
            }

            foreach (var s in Store.Shapes)
            {
                var fill = s.Fill;
                var stroke = s.Stroke;
                var strokeWidth = s.StrokeWidth;

                using var style = new CanvasStrokeStyle()
                {
                    LineJoin = CanvasLineJoin.Miter
                };

                // Draw
                if (s is RectangleModel r)
                {
                    var rect = new Rect(r.Position.X, r.Position.Y, r.Width, r.Height);
                    if (r.RadiusX > 0 || r.RadiusY > 0)
                    {
                        using var geo = CanvasGeometry.CreateRoundedRectangle(
                            sender.Device, (float)rect.X, (float)rect.Y,
                            (float)rect.Width, (float)rect.Height,
                            r.RadiusX, r.RadiusY);
                        ds.FillGeometry(geo, fill);
                        if (strokeWidth > 0) ds.DrawGeometry(geo, stroke, strokeWidth, style);
                    }
                    else
                    {
                        ds.FillRectangle(rect, fill);
                        if (strokeWidth > 0) ds.DrawRectangle(rect, stroke, strokeWidth, style);
                    }
                }
                else if (s is EllipseModel e)
                {
                    var center = new Vector2(e.Position.X + e.Width / 2f, e.Position.Y + e.Height / 2f);
                    using var geo = CanvasGeometry.CreateEllipse(sender.Device, center, e.Width / 2f, e.Height / 2f);
                    ds.FillGeometry(geo, fill);
                    if (strokeWidth > 0) ds.DrawGeometry(geo, stroke, strokeWidth, style);
                }
                else if (s is TriangleModel t)
                {
                    var p1 = new Vector2(t.Position.X + t.Width / 2f, t.Position.Y);
                    var p2 = new Vector2(t.Position.X, t.Position.Y + t.Height);
                    var p3 = new Vector2(t.Position.X + t.Width, t.Position.Y + t.Height);
                    using var pb = new CanvasPathBuilder(sender.Device);
                    pb.BeginFigure(p1); pb.AddLine(p2); pb.AddLine(p3); pb.EndFigure(CanvasFigureLoop.Closed);
                    using var geo = CanvasGeometry.CreatePath(pb);
                    ds.FillGeometry(geo, fill);
                    if (strokeWidth > 0) ds.DrawGeometry(geo, stroke, strokeWidth, style);
                }

                // Selection highlight overlay
                if (s == Store.Selected)
                {
                    // dashed outline around the bounds
                    using var selStyle = new CanvasStrokeStyle
                    {
                        DashStyle = CanvasDashStyle.Dash,
                        LineJoin = CanvasLineJoin.Miter
                    };
                    var bounds = new Rect(s.Position.X - 4, s.Position.Y - 4, s.Width + 8, s.Height + 8);
                    ds.DrawRectangle(bounds, Color.FromArgb(255, 0, 120, 215), 1.5f, selStyle);
                }
            }
        }

        private void Win2D_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var p = e.GetPosition(Win2D);
            Store.SelectNextAt(new Vector2((float)p.X, (float)p.Y));
        }
    }
}
