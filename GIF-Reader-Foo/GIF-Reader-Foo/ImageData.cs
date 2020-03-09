using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class ImageData
    {
        public ImageLzwData RawImageData { get; set; }
        public ImageDescriptorBlock DescriptorBlock { get; set; }
        public ColorTable LocalColorTable { get; set; }
        public GraphicsControlExtension GraphicsExtention { get; set; }
    }
}
