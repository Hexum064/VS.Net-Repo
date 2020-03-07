using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class LzwCodeBytes
    {
        private byte[] _codes = null;

        public LzwCodeBytes(IEnumerable<byte> codes)
        {
            _codes = codes.ToArray();
            
        }

        public int BitIndex
        {
            get;
            private set;
        } = 0;

        public byte BitsPerCode
        {
            get;
            set;
        }

        public bool IsAtEnd
        {
            get
            {
                return BitIndex >= (_codes.Length * 8 - 1);
            }
        }

        public ushort GetNextCode()
        {
            int index = (BitIndex / 8); //Get the index into the bytes array from the starting bit index
            int bitIndex = BitIndex % 8; //Get the place in the byte where the first bit exists
            ushort bitMask = (ushort)((1 << BitsPerCode) - 1); //Get a mask for the length of bits we need for a single code
                                                                 //Join the byte at index with the next byte so we can apply a bit mask that is possibly longer than 8 bits or will cross a byte boundary
            ushort joinedBytes = (ushort)(((index + 1 < _codes.Length ? _codes[index + 1] : 0) * 256) + _codes[index]); //This may need to be reversed where the MSB is the next byte, not the current byte

            joinedBytes = (ushort)(joinedBytes >> bitIndex); //Move the bits in the joinedBytes over so the bit mask can easily be applied;

            BitIndex += BitsPerCode;

            return (ushort)(joinedBytes & bitMask); //Return the code that is masked from the joinedBytes and the bitMask
        }

    }
}
