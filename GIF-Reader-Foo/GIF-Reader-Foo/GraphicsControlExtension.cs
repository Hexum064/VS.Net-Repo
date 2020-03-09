using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class GraphicsControlExtension
    {
        public byte BlockSize { get; set; }
        public byte DisposalMethod { get; set; }
        public bool UserInputFlag { get; set; }
        public bool TransparentColorFlag { get; set; }
        public ushort DelayTime { get; set; }
        public byte TransparentColorIndex { get; set; }

        public override string ToString()
        {
            return $"Block Size: {BlockSize}, Disposal Method: {DisposalMethod}, User Input Flag: {UserInputFlag}, Transparent Color Flag: {TransparentColorFlag}, Delay Time: {DelayTime}, Transparent Color Index: {TransparentColorIndex}";
        }
    }
}
