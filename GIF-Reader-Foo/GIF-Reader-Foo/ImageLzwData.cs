using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{

    public class ImageLzwData
    {
        public byte MinCodeSize { get; set; }
        public LzwCodeBytes ImageSubBlocks { get; set; }
    }
}
