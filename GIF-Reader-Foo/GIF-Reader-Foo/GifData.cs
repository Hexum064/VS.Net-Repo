using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class GifData
    {
        public GifHeader Header { get; set; }
        public ScreenDescriptor ScreenDescriptor { get; set; }
        public ColorTable GlobalColorTable { get; set; }
        public List<ImageData> Images { get; private set; } = new List<ImageData>();
    }
}
