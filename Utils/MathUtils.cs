using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svg_editor.Utils
{
    public static class MathUtils
    {

        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

    }
}
