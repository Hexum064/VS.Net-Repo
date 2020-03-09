using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class ImageReadyEventArgs : EventArgs
    {
        public ImageReadyEventArgs(IEnumerable<RgbColor> rgbImageData, ImageData imageData)
        {
            RgbImageData = rgbImageData.ToArray();
            ImageData = imageData;
        }

        public RgbColor[] RgbImageData { get; private set; }
        public ImageData ImageData { get; private set; }
    }
}
