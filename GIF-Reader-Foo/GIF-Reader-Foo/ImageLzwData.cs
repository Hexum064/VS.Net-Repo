using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{

    public class ImageLzwData
    {
        public byte lzwMinCodeSize;
        public List<byte[]> LzwBytes = new List<byte[]>();
    }
}
