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



 

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {



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
                GifLzwDecoding encoding = new GifLzwDecoding(imageData.RawImageData.lzwMinCodeSize);
                bytes = encoding.DecodeLzwGifData(new LzwCodeBytes(imageData.RawImageData.LzwBytes.First())).ToArray();
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

     

        private void LzwCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(lzwCodeTextBlock.Text))
            {
                return;
            }

            byte[] bytes = GetBytesFromHexString(lzwCodeTextBlock.Text);
            byte minCodeSize = bytes[0];

            GifLzwDecoding lzw = new GifLzwDecoding(minCodeSize);
            List<byte> imageBytes = lzw.DecodeLzwGifData(new LzwCodeBytes(bytes.Skip(2)));

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





 
}
