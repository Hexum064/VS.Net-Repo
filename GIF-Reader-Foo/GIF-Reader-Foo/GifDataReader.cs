using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GIF_Reader_Foo
{
    public class GifDataReader
    {
        private const byte EXT_INTRO = 0x21;
        private const byte GFX_CONTROL = 0xF9;
        private const byte APP_EXTENSION = 0xFF;
        private const byte IMG_DESCRIPTOR = 0x2C;
        private const byte TEXT_EXTENSION = 0x01;
        private const byte COMMENT_EXTENSION = 0xFE;
        private const byte TRAILER_BYTE = 0x3B;

        private GifHeader _header;
        private ScreenDescriptor _screenDescriptor;
        private ColorTable _globalColorTable;

        public async Task<GifData> LoadGIFAsync(string path)
        {
            return Task.Run(() =>
            {
                GifData gifData = new GifData();
                bool readingData = true;
                byte[] block;
                List<ImageData> images = new List<ImageData>();
                ImageData imageData = null;



                Console.WriteLine($"Loading from {path}");

                using (FileStream reader = File.OpenRead(path))
                {
                    _header = LoadHeaderAsync(reader);
                    _screenDescriptor = LoadScreenDescriptorAsync(reader);
                    _globalColorTable = LoadColorTable(reader, _screenDescriptor.GlobalColorTableSize);

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

                                        imageData.GraphicsExtention = GetGraphicsExtensionBlock(reader);

                                        break;
                                    case APP_EXTENSION:
                                        GetApplicationExtension(reader);
                                        break;
                                    case TEXT_EXTENSION:
                                        GetPlainTextExtension(reader);
                                        break;
                                    case COMMENT_EXTENSION:
                                        GetCommentExtension(reader);

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

                                imageData.DescriptorBlock = GetImageDescriptorBlock(reader);

                                if (imageData.DescriptorBlock.LocalColorTableFlag)
                                {
                                    imageData.LocalColorTable = LoadColorTable(reader, imageData.DescriptorBlock.ColorTableSize);
                                }

                                imageData.RawImageData = GetRawImageData(reader);
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

                return gifData;

            });




        }


        private GifHeader LoadHeaderAsync(FileStream reader)
        {
         
            GifHeader header = new GifHeader();

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
          
        }


        private ScreenDescriptor LoadScreenDescriptorAsync(FileStream reader)
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
    
        }

        private ColorTable LoadColorTable(FileStream reader, ushort colorTableSize)
        {

            byte[] buff = new byte[colorTableSize * 3];
            reader.Read(buff, 0, buff.Length);
            return buff.LoadColorTable(0, colorTableSize);
            
        }

        private ImageDescriptorBlock GetImageDescriptorBlock(FileStream reader)
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
       
        }


        private GraphicsControlExtension GetGraphicsExtensionBlock(FileStream reader)
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
        
        }

        private void GetApplicationExtension(FileStream reader)
        {
          
            //For now, just skipping past the data;
            int len = reader.ReadByte(); //Get the len of the header of the block. Should be 11 bytes
            reader.Position += len; //Move to the new pos
            len = reader.ReadByte(); //Get the len of the rest of the block
            reader.Position += len + 1; //Move to the new pos + 1 to accound for the termination byte
          
        }

        private void GetPlainTextExtension(FileStream reader)
        {
            //For now, just skipping past the data;
            int len = reader.ReadByte(); //Get the len of the header of the block. Should be 12 bytes
            reader.Position += len; //Move to the new pos
            while (reader.ReadByte() != 0) //Read until the 0 terminator
            { }

        }

        private  void GetCommentExtension(FileStream reader)
        {
          
            //For now, just skipping past the data
            while (reader.ReadByte() != 0) //Read until the 0 terminator
            { }

          
        }

        private ImageLzwData GetRawImageData(FileStream reader)
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
         
        }
    }
}
