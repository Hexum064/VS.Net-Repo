using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class GraphicsControlExtension
    {
        public byte BlockSize;
        public byte DisposalMethod;
        public bool UserInputFlag;
        public bool TransparentColorFlag;
        public ushort DelayTime;
        public byte TransparentColorIndex;

        public override string ToString()
        {
            return $"Block Size: {BlockSize}, Disposal Method: {DisposalMethod}, User Input Flag: {UserInputFlag}, Transparent Color Flag: {TransparentColorFlag}, Delay Time: {DelayTime}, Transparent Color Index: {TransparentColorIndex}";
        }
    }
}
