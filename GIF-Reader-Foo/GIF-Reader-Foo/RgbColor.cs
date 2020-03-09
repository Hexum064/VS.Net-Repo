using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class RgbColor
    {
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
        public bool Transparent { get; set; }

        public override string ToString()
        {
            return "{" + Red + "," + Green + "," + Blue + "}";
        }
    }
}
