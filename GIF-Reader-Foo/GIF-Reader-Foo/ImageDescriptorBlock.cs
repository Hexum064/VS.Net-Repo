using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class ImageDescriptorBlock
    {
        public ushort X;
        public ushort Y;
        public ushort Width;
        public ushort Height;
        public bool LocalColorTableFlag;
        public bool InterlacedFlag;
        public bool SortedFlag;
        public ushort ColorTableSize;

        public override string ToString()
        {
            return $"X: {X}, Y: {Y}, Width: {Width}, Height: {Height}, Local Color Table Flag: {LocalColorTableFlag}, Interlaced Flag: {InterlacedFlag}, Sorted Flag: {SortedFlag}, Color Table Size: {ColorTableSize}";
        }
    }
}
