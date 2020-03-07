using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public static class Extensions
    {
        public static ushort GetUWord(this byte[] buff, int startIndex, bool lsbFirst = true)
        {
            if (lsbFirst)
            {
                return (ushort)(buff[startIndex] + (buff[startIndex + 1] * 0xFF));
            }

            return (ushort)(buff[startIndex + 1] + (buff[startIndex] * 0xFF));
        }

        public static ColorTable LoadColorTable(this byte[] buff, int startIndex, int colors)
        {
            ColorTable table = new ColorTable();
            table.Colors = new RgbColor[colors];

            for (int i = 0; i < colors; i++)
            {
                table.Colors[i] = new RgbColor() { Red = buff[i * 3], Green = buff[i * 3 + 1], Blue = buff[i * 3 + 2] };
            }

            return table;
        }



    }
}
