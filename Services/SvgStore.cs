using CommunityToolkit.Mvvm.ComponentModel;
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
    public sealed class SvgStore
    {
        private readonly List<ShapeModel> _shapes = new();

        public event EventHandler? Changed;

        public IReadOnlyList<ShapeModel> Shapes => _shapes;

        public float CanvasWidth { get; private set; } = 1600;
        public float CanvasHeight { get; private set; } = 1200;

        public ShapeModel? Selected { get; private set; }

        public void Clear()
        {

            foreach (var s in _shapes) s.PropertyChanged -= OnShapeChanged;
            _shapes.Clear();
            Selected = null;

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
                if (Selected == shape) Selected = null;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetSelected(ShapeModel? shape)
        {
            if (Selected == shape) return;
            Selected = shape;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OnShapeChanged(object? sender, PropertyChangedEventArgs e)
          => Changed?.Invoke(this, EventArgs.Empty);

        public ShapeModel? HitTestTop(Vector2 p)
        {
            for (int i = _shapes.Count - 1; i >= 0; i--)
            {
                if (ContainsPoint(_shapes[i], p)) return _shapes[i];
            }
            return null;
        }

        // Cycle through all shapes under point, picking the "next" after current Selected
        public void SelectNextAt(Vector2 p)
        {
            var under = new List<ShapeModel>();
            for (int i = 0; i < _shapes.Count; i++)
                if (ContainsPoint(_shapes[i], p)) under.Add(_shapes[i]);

            if (under.Count == 0)
            {
                SetSelected(null);
                return;
            }

            // Order by z (draw order = list order). Top-most last.
            // We’ll cycle in top-most-first order for UX.
            var ordered = under.OrderBy(s => _shapes.IndexOf(s)).ToList();

            if (Selected is null || !under.Contains(Selected))
            {
                SetSelected(ordered[^1]); // pick top-most first
                return;
            }

            // Find next above current selected (wrapping)
            int idxInUnder = ordered.IndexOf(Selected);
            int next = (idxInUnder - 1);           // moving towards even more top-most
            if (next < 0) next = ordered.Count - 1;
            SetSelected(ordered[next]);
        }

        private static bool ContainsPoint(ShapeModel s, Vector2 p)
        {
            // Quick AABB reject
            if (p.X < s.Position.X || p.Y < s.Position.Y ||
                p.X > s.Position.X + s.Width || p.Y > s.Position.Y + s.Height)
                return false;

            // Precise tests
            switch (s)
            {
                case EllipseModel e:
                    {
                        var cx = s.Position.X + s.Width / 2f;
                        var cy = s.Position.Y + s.Height / 2f;
                        var rx = s.Width / 2f;
                        var ry = s.Height / 2f;
                        if (rx <= 0 || ry <= 0) return false;
                        var nx = (p.X - cx) / rx;
                        var ny = (p.Y - cy) / ry;
                        return (nx * nx + ny * ny) <= 1f;
                    }

                case RectangleModel r:
                    // AABB already enough; if you want rounded-corner exclusion, add extra check
                    return true;

                case TriangleModel t:
                    {
                        var p1 = new Vector2(t.Position.X + t.Width / 2f, t.Position.Y);
                        var p2 = new Vector2(t.Position.X, t.Position.Y + t.Height);
                        var p3 = new Vector2(t.Position.X + t.Width, t.Position.Y + t.Height);
                        return PointInTriangle(p, p1, p2, p3);
                    }

                default:
                    return true;
            }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            // Barycentric technique
            var v0 = c - a;
            var v1 = b - a;
            var v2 = p - a;

            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            float invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return (u >= 0) && (v >= 0) && (u + v <= 1);
        }
    }
}





