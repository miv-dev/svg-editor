using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using svg_editor.Models;
using svg_editor.Services;
using svg_editor.Services.Commands;
using svg_editor.Utils;
using svg_editor.Views.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace svg_editor.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public SvgStore Store { get; }
        public AppState AppState { get; }
        public CommandManager Commands { get; }

        public MainViewModel(SvgStore store, AppState appState, CommandManager cmdMgr)
        {
            Store = store;
            AppState = appState;
            Commands = cmdMgr;

            Commands.Changed += (_, __) =>
            {
                UndoCommand.NotifyCanExecuteChanged();
                RedoCommand.NotifyCanExecuteChanged();
            };
        }

        [RelayCommand]
        private void NewDocument()
        {
            Store.Clear();
            AppState.ResetTools();
        }

        [RelayCommand] private void SetModeView() => AppState.Mode = EditorMode.View;
        [RelayCommand] private void SetModeEdit() => AppState.Mode = EditorMode.Edit;

        [RelayCommand] private void UseRect() => AppState.ActiveTool = Tool.Rectangle;
        [RelayCommand] private void UseEllipse() => AppState.ActiveTool = Tool.Ellipse;
        [RelayCommand] private void UseTriangle() => AppState.ActiveTool = Tool.Triangle;

        [RelayCommand(CanExecute = nameof(CanUndo))] private void Undo() => Commands.Undo();
        [RelayCommand(CanExecute = nameof(CanRedo))] private void Redo() => Commands.Redo();
        private bool CanUndo() => Commands.CanUndo;
        private bool CanRedo() => Commands.CanRedo;

        public void OnCanvasTapped(float x, float y)
        {
            if (AppState.Mode != EditorMode.Edit) return;

            ShapeModel shape = AppState.ActiveTool switch
            {
                Tool.Rectangle => new RectangleModel { Position = new(x - 75, y - 50) },
                Tool.Ellipse => new EllipseModel { Position = new(x - 60, y - 60), Width = 120, Height = 120 },
                Tool.Triangle => new TriangleModel { Position = new(x - 70, y - 60), Width = 140, Height = 120 },
                _ => null!
            };
            if (shape == null) return;

            Commands.Do(new AddShapeCommand(Store, shape));
        }

        [RelayCommand]
        private void LoadDemo()
        {
            Store.Clear();
            Store.SetCanvas(1600, 1200);
            Commands.Do(new AddShapeCommand(Store, new RectangleModel { Position = new(260, 220), Width = 300, Height = 160 }));
            Commands.Do(new AddShapeCommand(Store, new EllipseModel { Position = new(900, 420), Width = 180, Height = 180 }));
            Commands.Do(new AddShapeCommand(Store, new TriangleModel { Position = new(520, 700), Width = 280, Height = 160 }));
            AppState.Mode = EditorMode.View;
            AppState.ResetTools();
        }


        public String GetSvg()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{Store.CanvasWidth}" height="{Store.CanvasHeight}">""");

            foreach (var s in Store.Shapes)
            {
                string fill = $"rgb({s.Fill.R},{s.Fill.G},{s.Fill.B})";
                string stroke = $"rgb({s.Stroke.R},{s.Stroke.G},{s.Stroke.B})";
                if (s is RectangleModel r)
                {
                    sb.AppendLine($"""  <rect x="{r.Position.X}" y="{r.Position.Y}" width="{r.Width}" height="{r.Height}" rx="{r.RadiusX}" ry="{r.RadiusY}" fill="{fill}" stroke="{stroke}" stroke-width="{r.StrokeWidth}"/>""");
                }
                else if (s is EllipseModel e)
                {
                    var cx = e.Position.X + e.Width / 2f;
                    var cy = e.Position.Y + e.Height / 2f;
                    sb.AppendLine($"""  <ellipse cx="{cx}" cy="{cy}" rx="{e.Width / 2f}" ry="{e.Height / 2f}" fill="{fill}" stroke="{stroke}" stroke-width="{e.StrokeWidth}"/>""");
                }
                else if (s is TriangleModel t)
                {
                    var x1 = t.Position.X + t.Width / 2f;
                    var y1 = t.Position.Y;
                    var x2 = t.Position.X;
                    var y2 = t.Position.Y + t.Height;
                    var x3 = t.Position.X + t.Width;
                    var y3 = t.Position.Y + t.Height;
                    sb.AppendLine($"""  <polygon points="{x1},{y1} {x2},{y2} {x3},{y3}" fill="{fill}" stroke="{stroke}" stroke-width="{t.StrokeWidth}"/>""");
                }
            }

            sb.AppendLine("</svg>");

            return sb.ToString();
        }
       
    }
}
