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
using System.Xml.Linq;
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


        public string GetSvg()
        {
            var sb = new StringBuilder();

            // ширину/высоту можно оставить целыми (у вас уже Convert.ToInt32),
            // либо прогнать через MathUtils.Fmt, если DECIMALS > 0.
            sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{Convert.ToInt32(Store.CanvasWidth)}" height="{Convert.ToInt32(Store.CanvasHeight)}">""");

            // 1) Вклеим «остатки» (если нужно) ПЕРЕД нашими фигурами — как фоновый слой
            if (!string.IsNullOrWhiteSpace(Store.UnsupportedSvgXml))
            {
                sb.AppendLine(@"  <!-- Unsupported original SVG content (not editable) -->");
                sb.AppendLine(@"  <g id=""unsupported"" data-svg-editor=""unsupported"">");

                foreach (var el in UnsupportedXmlHelper.ExtractElements(Store.UnsupportedSvgXml))
                {
                    // Вставляем как есть, без переоформления (оставляем xmlns, стили и пр.)
                    sb.AppendLine("    " + el.ToString(SaveOptions.DisableFormatting));
                }

                sb.AppendLine(@"  </g>");
            }


            foreach (var s in Store.Shapes)
            {
                string fill = $"rgb({s.Fill.R},{s.Fill.G},{s.Fill.B})";
                string stroke = $"rgb({s.Stroke.R},{s.Stroke.G},{s.Stroke.B})";
                string sw = MathUtils.Fmt(s.StrokeWidth);

                if (s is RectangleModel r)
                {
                    sb.AppendLine(
                        $"""  <rect x="{MathUtils.Fmt(r.Position.X)}" y="{MathUtils.Fmt(r.Position.Y)}" width="{MathUtils.Fmt(r.Width)}" height="{MathUtils.Fmt(r.Height)}" rx="{MathUtils.Fmt(r.RadiusX)}" ry="{MathUtils.Fmt(r.RadiusY)}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>""");
                }
                else if (s is EllipseModel e)
                {
                    var cx = e.Position.X + e.Width / 2f;
                    var cy = e.Position.Y + e.Height / 2f;
                    var rx = e.Width / 2f;
                    var ry = e.Height / 2f;

                    sb.AppendLine(
                        $"""  <ellipse cx="{MathUtils.Fmt(cx)}" cy="{MathUtils.Fmt(cy)}" rx="{MathUtils.Fmt(rx)}" ry="{MathUtils.Fmt(ry)}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>""");
                }
                else if (s is TriangleModel t)
                {
                    var x1 = t.Position.X + t.Width / 2f; var y1 = t.Position.Y;
                    var x2 = t.Position.X; var y2 = t.Position.Y + t.Height;
                    var x3 = t.Position.X + t.Width; var y3 = t.Position.Y + t.Height;

                    sb.AppendLine(
                        $"""  <polygon points="{MathUtils.Fmt(x1)},{MathUtils.Fmt(y1)} {MathUtils.Fmt(x2)},{MathUtils.Fmt(y2)} {MathUtils.Fmt(x3)},{MathUtils.Fmt(y3)}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>""");
                }
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        public void ImportSvg(string svgContent)
        {
            SvgImportResult res;
            try
            {
                res = SvgImporter.Parse(svgContent);
            }
            catch (Exception ex)
            {
                // Сообщите пользователю любым способом — здесь просто очистим и выйдем
                return;
            }

            Store.Clear();
            if (res.Width.HasValue && res.Height.HasValue)
                Store.SetCanvas(res.Width.Value, res.Height.Value);

            foreach (var r in res.Rects) Store.Add(r);
            foreach (var e in res.Ellipses) Store.Add(e);
            foreach (var t in res.Triangles) Store.Add(t);

            Store.UnsupportedSvgXml = res.UnsupportedSvgXml;
        }
    }
}
