using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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
            pathText.Text = @"..\..\..\giphy.gif";
            //pathText.Text = @"C:\Users\Owner\Documents\Visual Studio 2019\Projects\GIF-Reader-Foo\giphy.gif";
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            LoadGIFAsync(pathText.Text);
        }

        private async void LoadGIFAsync(string path)
        {
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

                        switch(blockType)
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

                
            

        }



        private Task<GIFHeader> LoadHeaderAsync(FileStream reader)
        {
            return Task.Run(() =>
            {
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
            });
        }


        private Task<ScreenDescriptor> LoadScreenDescriptorAsync(FileStream reader)
        {
            return Task.Run(() =>
            {
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
            });
        }

        private Task<ColorTable> LoadColorTable(FileStream reader, ushort colorTableSize)
        {
            return Task.Run(() =>
            {
                byte[] buff = new byte[colorTableSize * 3];
                reader.Read(buff, 0, buff.Length);
                return buff.LoadColorTable(0, colorTableSize);
            });
        }

        private Task<ImageDescriptorBlock> GetImageDescriptorBlock(FileStream reader)
        {
            return Task.Run(() =>
            {
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
            });
        }


        private  Task<GraphicsControlExtension> GetGraphicsExtensionBlock(FileStream reader)
        {
            return Task.Run(() =>
            {
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
            });
        }

        private Task GetApplicationExtension(FileStream reader)
        {
            return Task.Run(() =>
            {
                //For now, just skipping past the data;
                int len = reader.ReadByte(); //Get the len of the header of the block. Should be 11 bytes
                reader.Position += len; //Move to the new pos
                len = reader.ReadByte(); //Get the len of the rest of the block
                reader.Position += len + 1; //Move to the new pos + 1 to accound for the termination byte
            });
        }

        private Task GetPlainTextExtension(FileStream reader)
        {
            return Task.Run(() =>
            {
                //For now, just skipping past the data;
                int len = reader.ReadByte(); //Get the len of the header of the block. Should be 12 bytes
                reader.Position += len; //Move to the new pos
                while (reader.ReadByte() != 0) //Read until the 0 terminator
                { }
                
            });
        }

        private Task GetCommentExtension(FileStream reader)
        {
            return Task.Run(() =>
            {
                //For now, just skipping past the data
                while (reader.ReadByte() != 0) //Read until the 0 terminator
                { }

            });
        }

        private Task<ImageLzwData> GetRawImageData(FileStream reader)
        {
            return Task.Run(() =>
            {
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
            });
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
