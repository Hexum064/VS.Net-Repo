using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class CodeTable 
    {
        private ushort _baseTableSize = 0;
        private List<List<byte>> _table = new List<List<byte>>();

        public CodeTable()
        {
      
        }

        public byte BitsPerCode
        {
            get;
            private set;
        }
        


        public void Reset(byte minCodeSize)
        {
            _table.Clear();
            BitsPerCode = (byte)(minCodeSize + 1);
            _baseTableSize = (ushort)(1 << minCodeSize);
        }

        public void AddEntry(List<byte> entry)
        {
            _table.Add(entry.ToList());
            CheckCodeTableSizeAndUpdateCodeLen();
        }

        //Using this method to get values from the code table so we don't need to store any regular indexes and special codes in the dictionary.
        public List<byte> GetEntry(ushort index)
        {

            if (index < _baseTableSize) //If index is in the range of the base color indexes, just return the index
            {
                return new List<byte> { (byte)index };
            }

            index -= (ushort)(_baseTableSize + 2); //adjust the index to account for the base color indexes and two special codes

            if (index >= 0 && index < _table.Count) //Check that index >= 0 incase the index was for one of the two special characters. Just a safety check
            {
                return _table[index].ToList();
            }

            return new List<byte>();
        }


        private void CheckCodeTableSizeAndUpdateCodeLen()
        {
            if ((_table.Count + _baseTableSize + 2) == 1 << BitsPerCode)
            {
                BitsPerCode++;
            }
        }
    }
}
