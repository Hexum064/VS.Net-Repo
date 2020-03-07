using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class ScreenDescriptor
    {
        public ushort Width;
        public ushort Height;
        public bool GlobalColorTableFlag;
        public byte ColorResolution;
        public bool SortFlag;
        public ushort GlobalColorTableSize;
        public byte BackgroundColorIndex;
        public byte PixelAspectRatio;

        public override string ToString()
        {
            return $"Width: {Width}, height: {Height}, Global Color Table: {GlobalColorTableFlag}, Color Res: {ColorResolution}, Sort: {SortFlag}, Global Color table size: {GlobalColorTableSize}, Background color index: {BackgroundColorIndex}, Pixel aspect ratio: {PixelAspectRatio}";
        }
    }
}
