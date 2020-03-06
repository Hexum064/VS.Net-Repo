using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GIF_Reader_Foo
{
    public class GIFHeader
    {
        public byte[] Name = new byte[3];
        public byte[] Version = new byte[3];

        public override string ToString()
        {
            return $"Name: {string.Join("", Name.Select((b) => (char)b))}, Version: {string.Join("", Version.Select((b) => (char)b))}";
        }
    }

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

    public class RGBColor
    {
        public byte Red;
        public byte Green;
        public byte Blue;

        public override string ToString()
        {
            return "{" + Red + "," + Green + "," + Blue + "}";
        }
    }

    public class ColorTable
    {
        public RGBColor[] Colors;
    }

    public class GraphicsControlExtension
    {
        public byte BlockSize;
        public byte DisposalMethod;
        public bool UserInputFlag;
        public bool TransparentColorFlag;
        public ushort DelayTime;
        public byte TransparentColorIndex;

        public override string ToString()
        {
            return $"Block Size: {BlockSize}, Disposal Method: {DisposalMethod}, User Input Flag: {UserInputFlag}, Transparent Color Flag: {TransparentColorFlag}, Delay Time: {DelayTime}, Transparent Color Index: {TransparentColorIndex}";
        }
    }

    public class ImageDescriptorBlock
    {
        public ushort X;
        public ushort Y;
        public ushort Width;
        public ushort Height;
        public bool LocalColorTableFlag;
        public bool InterlacedFlag;
        public bool SortedFlag;
        public ushort ColorTableSize;

        public override string ToString()
        {
            return $"X: {X}, Y: {Y}, Width: {Width}, Height: {Height}, Local Color Table Flag: {LocalColorTableFlag}, Interlaced Flag: {InterlacedFlag}, Sorted Flag: {SortedFlag}, Color Table Size: {ColorTableSize}";
        }
    }

    public class ImageLzwData
    {
        public byte lzwMinCodeSize;
        public List<byte[]> LzwBytes = new List<byte[]>();
    }

    public class ImageData
    {
        public ImageLzwData RawImageData;
        public ImageDescriptorBlock DescriptorBlock;
        public ColorTable LocalColorTable;
        public GraphicsControlExtension GraphicsExtention;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const byte EXT_INTRO = 0x21;
        private const byte GFX_CONTROL = 0xF9;
        private const byte APP_EXTENSION = 0xFF;
        private const byte IMG_DESCRIPTOR = 0x2C;
        private const byte TEXT_EXTENSION = 0x01;
        private const byte COMMENT_EXTENSION = 0xFE;
        private const byte TRAILER_BYTE = 0x3B;

        private GIFHeader _header;
        private ScreenDescriptor _screenDescriptor;
        private ColorTable _globalColorTable;


        public MainWindow()
        {
            InitializeComponent();

            pathText.Text = @"M:\My Documents\VS.Net-Repo\GIF-Reader-Foo\giphy.gif";
            //pathText.Text = @"..\..\..\giphy.gif";
            pathText.Text = @"..\..\..\light.gif";
            //pathText.Text = @"C:\Users\Owner\Documents\Visual Studio 2019\Projects\GIF-Reader-Foo\giphy.gif";
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {

            RunLoadGIF();
        }

        private async void RunLoadGIF()
        {
            displayImage.Source =  await LoadGIFAsync(pathText.Text);
        }

        private async Task<BitmapSource> LoadGIFAsync(string path)
        {
            //return Task.Run(async () =>
            //{

            bool readingData = true;
            byte[] block;
            List<ImageData> images = new List<ImageData>();
            ImageData imageData = null;
     


            try
            {
                Console.WriteLine($"Loading from {path}");

                using (FileStream reader = File.OpenRead(path))
                {
                    _header = await LoadHeaderAsync(reader);
                    _screenDescriptor = await LoadScreenDescriptorAsync(reader);
                    _globalColorTable = await LoadColorTable(reader, _screenDescriptor.GlobalColorTableSize);

                    while (readingData)
                    {


                        Console.WriteLine("Pos: " + reader.Position.ToString("X8"));
                        byte blockType = (byte)reader.ReadByte();
                        //block = await GetNextBlock(reader);

                        switch (blockType)
                        {
                            case EXT_INTRO:
                                blockType = (byte)reader.ReadByte();
                                switch (blockType)
                                {
                                    case GFX_CONTROL:

                                        if (imageData == null)
                                        {
                                            imageData = new ImageData();
                                        }

                                        imageData.GraphicsExtention = await GetGraphicsExtensionBlock(reader);

                                        break;
                                    case APP_EXTENSION:
                                        await GetApplicationExtension(reader);
                                        break;
                                    case TEXT_EXTENSION:
                                        await GetPlainTextExtension(reader);
                                        break;
                                    case COMMENT_EXTENSION:
                                        await GetCommentExtension(reader);

                                        break;
                                    default:

                                        continue;
                                }


                                break;
                            case IMG_DESCRIPTOR:

                                if (imageData == null)
                                {
                                    imageData = new ImageData();
                                }

                                imageData.DescriptorBlock = await GetImageDescriptorBlock(reader);

                                if (imageData.DescriptorBlock.LocalColorTableFlag)
                                {
                                    imageData.LocalColorTable = await LoadColorTable(reader, imageData.DescriptorBlock.ColorTableSize);
                                }

                                imageData.RawImageData = await GetRawImageData(reader);
                                images.Add(imageData);
                                imageData = null;
                                break;
                            case TRAILER_BYTE:
                                Console.WriteLine($"Done loading images. Image count: {images.Count}");
                                readingData = false;
                                break;
                        }

                    }

                }

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }

            PixelFormat pf = PixelFormats.Bgr24;
            int rawStride = (_screenDescriptor.Width * pf.BitsPerPixel + 7) / 8;
            byte[] rawImage = new byte[rawStride * _screenDescriptor.Height];
            DecodeData(images, rawImage);

            BitmapSource bmp = BitmapSource.Create(_screenDescriptor.Width, _screenDescriptor.Height, 96, 96, pf, null, rawImage, rawStride);
            return bmp;



        }




        private void DecodeData(List<ImageData> images, byte[] rawImage)
        {
            foreach (ImageData imageData in images)
            {

                Console.WriteLine();
                byte[] bytes;
                LzwEncoding encoding = new LzwEncoding(imageData.RawImageData.lzwMinCodeSize);
                bytes = encoding.DecodeLzwGifData(imageData.RawImageData.LzwBytes.First()).ToArray();
                DrawImage(bytes, rawImage, 0, imageData.DescriptorBlock);
                return;
            }
        }

        private void DrawImage(byte[] bytes, byte[] destBytes, int startingIndex, ImageDescriptorBlock descriptorBlock)
        {
            int imageXYOffset = descriptorBlock.Y * _screenDescriptor.Width + descriptorBlock.X;

            for (int i = 0; i < bytes.Length && i < _screenDescriptor.Width * _screenDescriptor.Height; i++)
            {
                Console.Write($"{bytes[i].ToString("000")}");
                if ((i + 1) % _screenDescriptor.Width == 0)
                {
                    Console.WriteLine();
                }
                destBytes[i * 3] = _globalColorTable.Colors[bytes[i]].Blue;
                destBytes[i * 3 + 1] = _globalColorTable.Colors[bytes[i]].Green;
                destBytes[i * 3 + 2] = _globalColorTable.Colors[bytes[i]].Red;
            }
            Console.WriteLine();
            //for (int y = 0; y < _screenDescriptor.Height; y++)
            //{
            //    for (int x = 0; x < _screenDescriptor.Width; x++)
            //    {
            //        if (y * _screenDescriptor.Width + x >= bytes.Length)
            //        {
            //            return;
            //        }

            //        Console.WriteLine(_globalColorTable.Colors[bytes[y * _screenDescriptor.Width + x]]);

            //    }
            //}
        }

        private async Task<GIFHeader> LoadHeaderAsync(FileStream reader)
        {
            //return Task.Run(() =>
            //{
            GIFHeader header = new GIFHeader();

            byte[] buff = new byte[6];
            reader.Read(buff, 0, 6);


            for (int i = 0; i < 3; i++)
            {
                header.Name[i] = buff[i];
            }

            for (int i = 0; i < 3; i++)
            {
                header.Version[i] = buff[i + 3];
            }

            Console.WriteLine(header.ToString());

            return header;
            //});
        }


        private async Task<ScreenDescriptor> LoadScreenDescriptorAsync(FileStream reader)
        {
            //return Task.Run(() =>
            //{
            ScreenDescriptor screenDescriptor = new ScreenDescriptor();

            byte[] buff = new byte[7];
            reader.Read(buff, 0, 7);


            screenDescriptor.Width = buff.GetUWord(0);
            screenDescriptor.Height = buff.GetUWord(2);
            screenDescriptor.GlobalColorTableFlag = (buff[4] & 0x80) != 0;
            screenDescriptor.ColorResolution = (byte)((buff[4] & 0x70) >> 4);
            screenDescriptor.SortFlag = (buff[4] & 0x08) != 0;
            screenDescriptor.GlobalColorTableSize = (ushort)Math.Pow(2, ((buff[4] & 0x07) >> 0) + 1);
            screenDescriptor.BackgroundColorIndex = buff[5];
            screenDescriptor.PixelAspectRatio = buff[6];

            Console.WriteLine(screenDescriptor.ToString());

            return screenDescriptor;
            //});
        }

        private async Task<ColorTable> LoadColorTable(FileStream reader, ushort colorTableSize)
        {
            //return Task.Run(() =>
            //{
            byte[] buff = new byte[colorTableSize * 3];
            reader.Read(buff, 0, buff.Length);
            return buff.LoadColorTable(0, colorTableSize);
            //});
        }

        private async Task<ImageDescriptorBlock> GetImageDescriptorBlock(FileStream reader)
        {
            //return Task.Run(() =>
            //{
            ImageDescriptorBlock block = new ImageDescriptorBlock();
            byte[] buff = new byte[9];
            reader.Read(buff, 0, 9);

            block.X = buff.GetUWord(0);
            block.Y = buff.GetUWord(2);
            block.Width = buff.GetUWord(4);
            block.Height = buff.GetUWord(6);
            block.LocalColorTableFlag = (buff[8] & 0x80) != 0;
            block.LocalColorTableFlag = (buff[8] & 0x40) != 0;
            block.LocalColorTableFlag = (buff[8] & 0x20) != 0;
            block.ColorTableSize = (ushort)Math.Pow(2, (buff[8] & 0x07) + 1);

            Console.WriteLine(block.ToString());

            return block;
            //});
        }


        private async Task<GraphicsControlExtension> GetGraphicsExtensionBlock(FileStream reader)
        {
            //return Task.Run(() =>
            //{
            GraphicsControlExtension extension = new GraphicsControlExtension();
            int len = reader.ReadByte();
            byte[] buff = new byte[len];

            reader.Read(buff, 0, len);

            extension.DisposalMethod = (byte)((buff[0] & 0x1C) >> 2);
            extension.UserInputFlag = (buff[0] & 0x02) != 0;
            extension.TransparentColorFlag = (buff[0] & 0x01) != 0;
            extension.DelayTime = buff.GetUWord(1);
            extension.TransparentColorIndex = buff[3];
            reader.Position++; //Skip the termination byte

            Console.WriteLine(extension.ToString());
            return extension;
            //});
        }

        private async Task GetApplicationExtension(FileStream reader)
        {
            //return Task.Run(() =>
            //{
            //For now, just skipping past the data;
            int len = reader.ReadByte(); //Get the len of the header of the block. Should be 11 bytes
            reader.Position += len; //Move to the new pos
            len = reader.ReadByte(); //Get the len of the rest of the block
            reader.Position += len + 1; //Move to the new pos + 1 to accound for the termination byte
            //});
        }

        private async Task GetPlainTextExtension(FileStream reader)
        {
            //return Task.Run(() =>
            //{
            //For now, just skipping past the data;
            int len = reader.ReadByte(); //Get the len of the header of the block. Should be 12 bytes
            reader.Position += len; //Move to the new pos
            while (reader.ReadByte() != 0) //Read until the 0 terminator
            { }

            //});
        }

        private async Task GetCommentExtension(FileStream reader)
        {
            //return Task.Run(() =>
            //{
            //For now, just skipping past the data
            while (reader.ReadByte() != 0) //Read until the 0 terminator
            { }

            //});
        }

        private async Task<ImageLzwData> GetRawImageData(FileStream reader)
        {
            //return Task.Run(() =>
            //{
            ImageLzwData lzwData = new ImageLzwData();
            lzwData.lzwMinCodeSize = (byte)reader.ReadByte();

            while (true)
            {
                int len = reader.ReadByte();
                byte[] buff;

                if (len == 0)
                {
                    break;

                }

                buff = new byte[len];
                reader.Read(buff, 0, len);
                lzwData.LzwBytes.Add(buff);
                Console.WriteLine($"Added buff #{lzwData.LzwBytes.Count} of size {len}");
            }

            Console.WriteLine("Done");

            return lzwData;
            //});
        }

        private void LzwCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(lzwCodeTextBlock.Text))
            {
                return;
            }

            byte[] bytes = GetBytesFromHexString(lzwCodeTextBlock.Text);
            byte minCodeSize = bytes[0];

            LzwEncoding lzw = new LzwEncoding(minCodeSize);
            List<byte> imageBytes = lzw.DecodeLzwGifData(bytes.Skip(2).ToArray());

            //HARDCODED SIZE

            int i = 0;

            imageBytes.ForEach((b) =>
            {
                if (i % 11 == 0)
                {
                    Console.WriteLine();
                }

                Console.Write($"{imageBytes[i].ToString("000")} ");

                i++;
            });

            Console.WriteLine();
        }


        public byte[] GetBytesFromHexString(string hexString)
        {
            string[] hexVals = hexString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return hexVals.Select((v) => Convert.ToByte("0x" + v.Trim(), 16)).ToArray();
        }
    }

    public class LzwEncoding
    {
        //private Dictionary<ushort, List<byte>> _codeTable = null;
        private List<List<byte>> _codeTable = null; //A dictionary could be used here but because the index can be calculated, and will always be contiguous, we do not need to use a key
        private byte _bitsPerCode = 0;
        private ushort _baseTableSize = 0;
        private byte _minCodeSize = 0;

        private ushort _clearCode;
        private ushort _eodCode;

        public LzwEncoding(byte minCodeSize)
        {
            MinCodeSize = minCodeSize;
            _codeTable = new List<List<byte>>();// new Dictionary<ushort, List<byte>>();
        }

        public byte MinCodeSize
        {
            get { return _minCodeSize; }

            set
            {
                _minCodeSize = value;
                _baseTableSize = (ushort)(1 << _minCodeSize);

                _clearCode = _baseTableSize;
                _eodCode = (ushort)(_baseTableSize + 1);
                ClearCodeTable();
            }

        }

        private void ClearCodeTable()
        {
            _codeTable = new List<List<byte>>();
            _bitsPerCode = (byte)(_minCodeSize + 1);
        }


        //Using this method to get values from the code table so we don't need to store any regular indexes and special codes in the dictionary.
        private List<byte> GetCodeTableEntry(ushort index)
        {

            if (index < _baseTableSize) //If index is in the range of the base color indexes, just return the index
            {
                return new List<byte> { (byte)index };
            }

            index -= (ushort)(_baseTableSize + 2); //adjust the index to account for the base color indexes and two special codes

            if (index >= 0 && index < _codeTable.Count) //Check that index >= 0 incase the index was for one of the two special characters. Just a safety check
            {
                return _codeTable[index].ToList();
            }

            return new List<byte>();
        }

        private void AddCodeTableEntry(List<byte> bytes)
        {
            _codeTable.Add(bytes);
        }

        private void CheckCodeTableSizeAndUpdateCodeLen()
        {
            if ((_codeTable.Count + _baseTableSize + 2) == 1 << _bitsPerCode)
            {
                _bitsPerCode++;
            }
        }




        //This method does not expect the first byte to contain the minimum code size. 
        public List<byte> DecodeLzwGifData(byte[] bytes)
        {
            int bitIndex = 0;
            List<byte> output = new List<byte>();
            List<byte> prevData = null;
            ushort prevCode = 0;
            List<byte> data = null;
            byte prevByte = 0;

            ClearCodeTable();

            ushort code = GetCode(bytes, _bitsPerCode, bitIndex);
            bitIndex += _bitsPerCode; //skip the Clear Table bit


            code = GetCode(bytes, _bitsPerCode, bitIndex);
            data = GetCodeTableEntry(code);
            output.AddRange(data);

            bitIndex += _bitsPerCode;


            while (bitIndex < bytes.Length * 8)
            {
                //set CODE-1
                prevCode = code;
                //get CODE
                code = GetCode(bytes, _bitsPerCode, bitIndex);


                if (code == _clearCode)
                {
                    ClearCodeTable();
                    bitIndex += _bitsPerCode;
                    code = prevCode;
                    continue;
                }

                if (code == _eodCode)
                {
                    break;
                }

                //get {CODE}
                data = GetCodeTableEntry(code);

                //get {CODE-1}
                prevData = GetCodeTableEntry(prevCode);

                if (data.Count == 0) //Code is in table
                {
                    data = prevData;
                }

                prevByte = data.First();
                //create {CODE-1} + K
                prevData.Add(prevByte);

                //output {CODE}
                output.AddRange(data);

                AddCodeTableEntry(prevData);

                bitIndex += _bitsPerCode;
                
                CheckCodeTableSizeAndUpdateCodeLen();

            }

            return output;
        }

        private ushort GetCode(byte[] bytes, byte len, int startBitIndex)
        {
            int index = (startBitIndex / 8); //Get the index into the bytes array from the starting bit index
            int bitIndex = startBitIndex % 8; //Get the place in the byte where the first bit exists
            ushort bitMask = (ushort)((1 << len) - 1); //Get a mask for the length of bits we need for a single code
            //Join the byte at index with the next byte so we can apply a bit mask that is possibly longer than 8 bits or will cross a byte boundary
            ushort joinedBytes = (ushort)(((index + 1 < bytes.Length ? bytes[index + 1] : 0) * 256) + bytes[index]); //This may need to be reversed where the MSB is the next byte, not the current byte

            joinedBytes = (ushort)(joinedBytes >> bitIndex); //Move the bits in the joinedBytes over so the bit mask can easily be applied;

            return (ushort)(joinedBytes & bitMask); //Return the code that is masked from the joinedBytes and the bitMask


        }

    }



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
            table.Colors = new RGBColor[colors];

            for (int i = 0; i < colors; i++)
            {
                table.Colors[i] = new RGBColor() { Red = buff[i * 3], Green = buff[i * 3 + 1], Blue = buff[i * 3 + 2] };
            }

            return table;
        }

    }
}
