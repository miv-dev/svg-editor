using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svg_editor.Utils
{
    public sealed class FloatDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is float f ? (double)f : 0d;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => value is double d ? (float)d : 0f;
    }
}
