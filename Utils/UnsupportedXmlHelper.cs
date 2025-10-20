using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace svg_editor.Utils
{
    public static class UnsupportedXmlHelper
    {
        private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

        /// <summary>
        /// Возвращает элементы для вклейки внутрь нашего <g>.
        /// Поддерживает как полный документ <svg>...</svg>, так и фрагменты.
        /// </summary>
        public static IEnumerable<XElement> ExtractElements(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return Enumerable.Empty<XElement>();

            XDocument? doc = null;
            try
            {
                doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            }
            catch
            {
                // Попробуем обернуть фрагмент в корневой <svg> и распарсить
                var wrapped = $@"<svg xmlns=""{SvgNs}"">{xml}</svg>";
                try { doc = XDocument.Parse(wrapped, LoadOptions.PreserveWhitespace); }
                catch { return Enumerable.Empty<XElement>(); }
            }

            if (doc?.Root == null) return Enumerable.Empty<XElement>();

            // Если корень — svg, возвращаем его дочерние элементы (чтобы не дублировать <svg> внутри <svg>)
            if (doc.Root.Name == SvgNs + "svg")
                return doc.Root.Elements();

            // Иначе вернём сам корень как элемент
            return new[] { doc.Root };
        }
    }
}
