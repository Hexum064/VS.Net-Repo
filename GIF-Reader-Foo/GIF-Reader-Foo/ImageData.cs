using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class ImageData
    {
        public ImageLzwData RawImageData;
        public ImageDescriptorBlock DescriptorBlock;
        public ColorTable LocalColorTable;
        public GraphicsControlExtension GraphicsExtention;
    }
}
