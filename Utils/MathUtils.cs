using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svg_editor.Utils
{
    public static class MathUtils
    {
        private const int DECIMALS = 0;

        public static string Fmt(float v)
        {
            // Округляем
            var rounded = MathF.Round(v, DECIMALS, MidpointRounding.AwayFromZero);

            // Убираем -0
            if (rounded == 0f) rounded = 0f;

            // Инвариантная культура, чтобы всегда была точка
            // Формат без лишних нулей справа (для DECIMALS > 0 – используем "0.###")
            if (DECIMALS == 0)
                return rounded.ToString("0", CultureInfo.InvariantCulture);

            // Пример для 3 знаков: "0.###" — можно динамически собрать строку
            string fmt = "0." + new string('#', DECIMALS);
            return rounded.ToString(fmt, CultureInfo.InvariantCulture);
        }

        // Небольшая нормализация для чисел типа 1.0000001 → 1

        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

    }

     

    }
