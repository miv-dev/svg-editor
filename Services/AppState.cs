using CommunityToolkit.Mvvm.ComponentModel;
using svg_editor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svg_editor.Services
{
    public partial class AppState : ObservableObject
    {
        [ObservableProperty]
        private EditorMode mode = EditorMode.View;

        [ObservableProperty]
        private Tool activeTool = Tool.None;

        public void ResetTools() => ActiveTool = Tool.None;
    }
}
