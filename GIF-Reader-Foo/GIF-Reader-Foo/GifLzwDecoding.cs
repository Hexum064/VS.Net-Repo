using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIF_Reader_Foo
{
    public class GifLzwDecoding
    {

        private CodeTable _codeTable = new CodeTable();
 
        private byte _minCodeSize = 0;

        private ushort _clearCode;
        private ushort _eodCode;

        public GifLzwDecoding(byte minCodeSize)
        {
            MinCodeSize = minCodeSize;          
        }

        public byte MinCodeSize
        {
            get { return _minCodeSize; }

            private set
            {
                _minCodeSize = value;
 

                _clearCode = (ushort)(1 << _minCodeSize);
                _eodCode = (ushort)((1 << _minCodeSize) + 1);
                _codeTable.Reset(_minCodeSize);
            }

        }

        //This method does not expect the first byte to contain the minimum code size. 
        public List<byte> DecodeLzwGifData(LzwCodeBytes codes)
        {
      
            List<byte> output = new List<byte>();
            List<byte> prevData = null;
            ushort prevCode = 0;
            List<byte> data = null;
            byte prevByte = 0;
            ushort code = 0;

            ResetCodeTableAndCodeSize(codes); //Initializing the code table and codes size

            while (!codes.IsAtEnd)
            {
                //set CODE-1
                prevCode = code;
                //get CODE
                code = codes.GetNextCode();


                if (code == _clearCode)
                {
                    ResetCodeTableAndCodeSize(codes);

                    code = codes.GetNextCode();
                    data = _codeTable.GetEntry(code);
                    output.AddRange(data); 

                    continue;
                }

                if (code == _eodCode)
                {
                    break;
                }

                //get {CODE}
                data = _codeTable.GetEntry(code);

                //get {CODE-1}
                prevData = _codeTable.GetEntry(prevCode);

                //Code is not in table so just use the prevData as data
                if (data.Count == 0) 
                {
                    data = prevData;
                }

                prevByte = data.First();
                //create {CODE-1} + K
                prevData.Add(prevByte);

                //output {CODE}
                output.AddRange(data);

       
                _codeTable.AddEntry(prevData);
                //Need to update the BitsPerCode here because the number may have changed after adding the last code table entry
                codes.BitsPerCode = _codeTable.BitsPerCode;

            }

            return output;
        }

        private void ResetCodeTableAndCodeSize(LzwCodeBytes codes)
        {
            _codeTable.Reset(MinCodeSize);
            codes.BitsPerCode = _codeTable.BitsPerCode;
        }

    }
    
}
