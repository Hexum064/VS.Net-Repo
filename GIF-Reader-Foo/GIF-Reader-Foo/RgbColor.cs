using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class RgbColor
    {
        public byte Red;
        public byte Green;
        public byte Blue;

        public override string ToString()
        {
            return "{" + Red + "," + Green + "," + Blue + "}";
        }
    }
}
