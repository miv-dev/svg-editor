using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svg_editor.Models
{
    public enum EditorMode
    {
        View = 0,
        Edit = 1
    }

    public enum Tool
    {
        None = 0,
        Rectangle = 1,
        Ellipse = 2,
        Triangle = 3
    }
}
