using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls.Primitives;
using svg_editor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace svg_editor.Services
{
    public sealed partial class SvgStore: ObservableObject
    {
        private readonly List<ShapeModel> _shapes = new();

        public event EventHandler? Changed;

        public IReadOnlyList<ShapeModel> Shapes => _shapes;


        [ObservableProperty] private float canvasWidth = 1600;
        [ObservableProperty] private float canvasHeight = 1200;

        // ВАЖНО: Selected с уведомлением INotifyPropertyChanged
        [ObservableProperty] private ShapeModel? selected;

        // Удобные производные проперти для XAML (чтобы не делать касты в разметке)
        public bool IsRectSelected => Selected is RectangleModel;
        public RectangleModel? SelectedRect => Selected as RectangleModel;


        public void Clear()
        {
            foreach (var s in _shapes) s.PropertyChanged -= OnShapeChanged;
            _shapes.Clear();
            SetSelected(null);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void SetCanvas(float w, float h)
        {
            CanvasWidth = w;
            CanvasHeight = h;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Add(ShapeModel shape)
        {
            _shapes.Add(shape);
            shape.PropertyChanged += OnShapeChanged;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(ShapeModel shape)
        {
            if (_shapes.Remove(shape))
            {
                shape.PropertyChanged -= OnShapeChanged;
                if (Selected == shape) SetSelected(null);
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        // Централизованная смена Selected с нужными нотификациями
        public void SetSelected(ShapeModel? shape)
        {
            if (Selected == shape) return;

            var oldWasRect = IsRectSelected;
            Selected = shape;                         // это поднимет PropertyChanged(nameof(Selected))
            Changed?.Invoke(this, EventArgs.Empty);  // для Canvas

            // Дополнительно оповестим зависящие проперти, чтобы XAML мог показать секцию радиусов
            OnPropertyChanged(nameof(IsRectSelected));
            OnPropertyChanged(nameof(SelectedRect));
        }

        private void OnShapeChanged(object? sender, PropertyChangedEventArgs e)
            => Changed?.Invoke(this, EventArgs.Empty);

        // --- Хит-тест и выбор (как раньше) ---
        public ShapeModel? HitTestTop(Vector2 p)
        {
            for (int i = _shapes.Count - 1; i >= 0; i--)
                if (ContainsPoint(_shapes[i], p)) return _shapes[i];
            return null;
        }

        public void SelectNextAt(Vector2 p)
        {
            var under = new List<ShapeModel>();
            for (int i = 0; i < _shapes.Count; i++)
                if (ContainsPoint(_shapes[i], p)) under.Add(_shapes[i]);

            if (under.Count == 0) { SetSelected(null); return; }

            var ordered = under.OrderBy(s => _shapes.IndexOf(s)).ToList();

            if (Selected is null || !under.Contains(Selected))
            {
                SetSelected(ordered[^1]); // верхний
                return;
            }

            int idx = ordered.IndexOf(Selected);
            int next = idx - 1; if (next < 0) next = ordered.Count - 1;
            SetSelected(ordered[next]);
        }

        private static bool ContainsPoint(ShapeModel s, Vector2 p)
        {
            if (p.X < s.Position.X || p.Y < s.Position.Y ||
                p.X > s.Position.X + s.Width || p.Y > s.Position.Y + s.Height)
                return false;

            return s switch
            {
                EllipseModel => IsInsideEllipse(s, p),
                TriangleModel t => PointInTriangle(
                    p,
                    new Vector2(t.Position.X + t.Width / 2f, t.Position.Y),
                    new Vector2(t.Position.X, t.Position.Y + t.Height),
                    new Vector2(t.Position.X + t.Width, t.Position.Y + t.Height)),
                _ => true, // прямоугольник — AABB
            };
        }

        private static bool IsInsideEllipse(ShapeModel s, Vector2 p)
        {
            var cx = s.Position.X + s.Width / 2f;
            var cy = s.Position.Y + s.Height / 2f;
            var rx = s.Width / 2f; var ry = s.Height / 2f;
            if (rx <= 0 || ry <= 0) return false;
            var nx = (p.X - cx) / rx; var ny = (p.Y - cy) / ry;
            return (nx * nx + ny * ny) <= 1f;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var v0 = c - a; var v1 = b - a; var v2 = p - a;
            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);
            float inv = 1f / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * inv;
            float v = (dot00 * dot12 - dot01 * dot02) * inv;
            return (u >= 0) && (v >= 0) && (u + v <= 1);
        }
    }
}





