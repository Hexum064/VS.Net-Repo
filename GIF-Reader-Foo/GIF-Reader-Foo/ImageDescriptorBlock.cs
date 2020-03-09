using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class ImageDescriptorBlock
    {
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public ushort Width { get; set; }
        public ushort Height { get; set; }
        public bool LocalColorTableFlag { get; set; }
        public bool InterlacedFlag { get; set; }
        public bool SortedFlag { get; set; }
        public ushort ColorTableSize { get; set; }

        public override string ToString()
        {
            return $"X: {X}, Y: {Y}, Width: {Width}, Height: {Height}, Local Color Table Flag: {LocalColorTableFlag}, Interlaced Flag: {InterlacedFlag}, Sorted Flag: {SortedFlag}, Color Table Size: {ColorTableSize}";
        }
    }
}
