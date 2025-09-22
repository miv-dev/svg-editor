using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using svg_editor.Models;
using svg_editor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace svg_editor.ViewModels
{
    public partial class MainViewModel :ObservableObject
    {

        public SvgStore Store { get; }

        public MainViewModel(SvgStore store) {

            Store = store;
            if (!store.HasArtboard)
            {
                NewArtboard();

            }
        }


        [RelayCommand] private void NewArtboard() => Store.NewArtboard(1200, 800);

        [RelayCommand]
        private void AddRect()
            => Store.AddRect(100, 100, 220, 140, new ShapeStyle(0xFFFFE08A, 0xFFAA7700, 2f));

        [RelayCommand]
        private void AddEllipse()
            => Store.AddEllipse(520, 240, 120, 80, new ShapeStyle(0xFFCCEEFF, 0xFF1E88E5, 2f, new float[] { 6, 3 }));

        [RelayCommand]
        private void AddLine()
            => Store.AddLine(200, 420, 620, 360, new ShapeStyle(null, 0xFF0D47A1, 3f));

        [RelayCommand]
        private void AddPolyline()
            => Store.AddPolyline(new[] { new Vector2(700, 120), new(760, 180), new(720, 230), new(660, 180) },
                                 new ShapeStyle(null, 0xFF388E3C, 2f));

        [RelayCommand]
        private void AddPolygon()
            => Store.AddPolygon(new[] { new Vector2(820, 120), new(900, 220), new(740, 220) },
                                new ShapeStyle(0xFFA5D6A7, 0xFF2E7D32, 2f));

        [RelayCommand]
        private void AddPath()
            => Store.AddPath("M 60 60 L 180 60 120 160 Z",
                             new ShapeStyle(0xFFB3CFFF, 0xFF1A237E, 2f));
    }
}
