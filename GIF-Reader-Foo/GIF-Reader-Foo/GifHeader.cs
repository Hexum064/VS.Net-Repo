using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class GifHeader
    {
        public byte[] Name { get; private set; } = new byte[3];
        public byte[] Version { get; private set; } = new byte[3];

        public override string ToString()
        {
            return $"Name: {string.Join("", Name.Select((b) => (char)b))}, Version: {string.Join("", Version.Select((b) => (char)b))}";
        }
    }
}
