using Microsoft.Graphics.Canvas.Geometry;
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

            Win2D.Invalidate();
        }

        // === Input: add shape on click when in Edit mode with a tool selected ===
        private void Win2D_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pt = e.GetCurrentPoint(Win2D);
            if (!pt.Properties.IsLeftButtonPressed) return;

            VM.OnCanvasTapped((float)pt.Position.X, (float)pt.Position.Y);
            // VM issues command -> store changes -> Invalidate via event
        }

        // === Win2D Render ===
        private void Win2D_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;

            // Optional: light checkerboard or grid could be drawn here

            // Render every shape from the store
            foreach (var s in Store.Shapes)
            {
                // Brushes: Win2D uses Colors directly; stroke widths are float
                var fill = s.Fill;   // Windows.UI.Color
                var stroke = s.Stroke;

                if (s is RectangleModel r)
                {
                    var rect = new Rect(r.Position.X, r.Position.Y, r.Width, r.Height);

                    if (r.RadiusX > 0 || r.RadiusY > 0)
                    {
                        using var geo = CanvasGeometry.CreateRoundedRectangle(
                            sender.Device,
                            (float)rect.X, (float)rect.Y,
                            (float)rect.Width, (float)rect.Height,
                            r.RadiusX, r.RadiusY);

                        ds.FillGeometry(geo, fill);
                        if (r.StrokeWidth > 0)
                            ds.DrawGeometry(geo, stroke, r.StrokeWidth);
                    }
                    else
                    {
                        ds.FillRectangle(rect, fill);
                        if (r.StrokeWidth > 0)
                            ds.DrawRectangle(rect, stroke, r.StrokeWidth);
                    }
                }
                else if (s is EllipseModel e)
                {
                    var center = new Vector2(e.Position.X + e.Width / 2f, e.Position.Y + e.Height / 2f);
                    var radiusX = e.Width / 2f;
                    var radiusY = e.Height / 2f;

                    using var geo = CanvasGeometry.CreateEllipse(sender.Device, center, radiusX, radiusY);

                    ds.FillGeometry(geo, fill);
                    if (e.StrokeWidth > 0)
                        ds.DrawGeometry(geo, stroke, e.StrokeWidth);
                }
                else if (s is TriangleModel t)
                {
                    // Isosceles triangle inside its bounds: (top, bottom-left, bottom-right)
                    var p1 = new Vector2(t.Position.X + t.Width / 2f, t.Position.Y);
                    var p2 = new Vector2(t.Position.X, t.Position.Y + t.Height);
                    var p3 = new Vector2(t.Position.X + t.Width, t.Position.Y + t.Height);

                    using var path = new CanvasPathBuilder(sender.Device);
                    path.BeginFigure(p1);
                    path.AddLine(p2);
                    path.AddLine(p3);
                    path.EndFigure(CanvasFigureLoop.Closed);

                    using var geo = CanvasGeometry.CreatePath(path);

                    ds.FillGeometry(geo, fill);
                    if (t.StrokeWidth > 0)
                        ds.DrawGeometry(geo, stroke, t.StrokeWidth);
                }
            }

            // Optional visual cue: draw a border around the paper
            ds.DrawRectangle(new Rect(0, 0, Store.CanvasWidth, Store.CanvasHeight), Color.FromArgb(255, 220, 220, 220), 1f);
        }
    }
}
