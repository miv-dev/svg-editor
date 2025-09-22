using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Svg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace svg_editor.Services
{
    public static class SvgIoService
    {
        public static async Task<CanvasSvgDocument> LoadAsync(StorageFile file, CanvasDevice device)
        {
            using IRandomAccessStream s = await file.OpenReadAsync();
            return await CanvasSvgDocument.LoadAsync(device, s);
        }

        public static async Task SaveAsync(CanvasSvgDocument document, StorageFile file)
        {
            using var s = await file.OpenAsync(FileAccessMode.ReadWrite);
            await document.SaveAsync(s);
        }
    }
}
