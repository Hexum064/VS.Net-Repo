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
            int interByteIndex = BitIndex % 8; //Get the place in the byte where the first bit exists
            uint bitMask = (uint)((1 << BitsPerCode) - 1); //Get a mask for the length of bits we need for a single code
            uint joinedBytes = 0;

                                                                 //Join the byte at index with the next byte so we can apply a bit mask that is possibly longer than 8 bits or will cross a byte boundary

            if (interByteIndex + BitsPerCode < 8) //requested bits will fit within the first byte at index
            {
                joinedBytes = _codes[index];
            }
            else if (interByteIndex + BitsPerCode < 16) //requested bits will fit within the first and second byte starting at index
            {
                joinedBytes = (uint)(((index + 1 < _codes.Length ? _codes[index + 1] : 0) * 256) + _codes[index]);
            }
            else //requested bits will span the byte at index and the next two
            {
                joinedBytes = (uint)(((index + 2 < _codes.Length ? _codes[index + 2] : 0) * 65536) + ((index + 1 < _codes.Length ? _codes[index + 1] : 0) * 256) + _codes[index]);
            }



            joinedBytes = joinedBytes >> interByteIndex; //Move the bits in the joinedBytes over so the bit mask can easily be applied;

            BitIndex += BitsPerCode;

            return (ushort)(joinedBytes & bitMask); //Return the code that is masked from the joinedBytes and the bitMask
        }

        public LzwCodeBytes Clone()
        {
            return new LzwCodeBytes(_codes) { BitIndex = BitIndex, BitsPerCode = BitsPerCode };
        }

    }
}
