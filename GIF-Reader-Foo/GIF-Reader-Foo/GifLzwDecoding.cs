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
        List<byte> _prevData = null;
        ushort _prevCode = 0;
        List<byte> _data = null;
        byte _prevByte = 0;
        ushort _code = 0;

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
        public List<byte> DecodeLzwGifData(LzwCodeBytes codeBytes)
        {

            //First make a copy so we are not modifying an object from outside of this class unintentionally
            LzwCodeBytes codes = codeBytes.Clone();
            List<byte> output = new List<byte>();

            //Ensuring that the bits per code has been set correctly
            codes.BitsPerCode = _codeTable.BitsPerCode;
  

            while (!codes.IsAtEnd)
            {

                //set CODE-1
                _prevCode = _code;
                //get CODE
                _code = codes.GetNextCode();


                if (_code == _clearCode)
                {

                    _codeTable.Reset(MinCodeSize);
                    codes.BitsPerCode = _codeTable.BitsPerCode;

                    _code = codes.GetNextCode();
                    _data = _codeTable.GetEntry(_code);
                    output.AddRange(_data);

                    continue;
                }

                if (_code == _eodCode)
                {
                    break;
                }

                //get {CODE}
                _data = _codeTable.GetEntry(_code);

                //get {CODE-1}
                _prevData = _codeTable.GetEntry(_prevCode);

                //Code is not in table so just use the prevData as data
                if (_data.Count == 0) 
                {
                    _data = _prevData;
                }


                if (_data.Count > 0)
                _prevByte = _data.First();
                //create {CODE-1} + K
                _prevData.Add(_prevByte);

                //Note: If _code didn't exist in the table, therefore _data.Count == 0, we set _data = _prevData meaning they reference the same thing, so changing _prevData is the same as changing _data
                //output {CODE}
                output.AddRange(_data);

       
                _codeTable.AddEntry(_prevData);
                //Need to update the BitsPerCode here because the number may have changed after adding the last code table entry
                codes.BitsPerCode = _codeTable.BitsPerCode;

                if (_codeTable.BitsPerCode > 12)
                {
                    _codeTable.Reset(MinCodeSize);
                    codes.BitsPerCode = _codeTable.BitsPerCode;
                }

            }

            return output;
        }



    }
    
}
