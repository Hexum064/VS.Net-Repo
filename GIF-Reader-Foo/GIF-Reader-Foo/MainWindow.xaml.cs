using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        GifRunner _gifRunner = null;
        GifData _gifData = null;

        public MainWindow()
        {
            InitializeComponent();

           
            pathText.Text = @"..\..\..\sheep.gif";
            //pathText.Text = @"..\..\..\light.gif";
            //pathText.Text = @"C:\Users\Owner\Documents\Visual Studio 2019\Projects\GIF-Reader-Foo\giphy.gif";
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_gifRunner?.IsRunning == true)
            {
                _gifRunner.Stop();
            }

            RunLoadGIF();
        }

        private async void RunLoadGIF()
        {
            GifDataReader gifReader = new GifDataReader(pathText.Text);
            _gifData = await gifReader.LoadGifAsync();
            _gifRunner = new GifRunner(_gifData);
            _gifRunner.ImageDataReady += GifRunner_ImageDataReady;

            _gifRunner.Start();

            //PixelFormat pf = PixelFormats.Bgr24;
            //int rawStride = (gifData.ScreenDescriptor.Width * pf.BitsPerPixel + 7) / 8;
            //byte[] rawImage = new byte[rawStride * gifData.ScreenDescriptor.Height];
            //DecodeData(gifData.Images, rawImage, gifData);

            //displayImage.Source = BitmapSource.Create(gifData.ScreenDescriptor.Width, gifData.ScreenDescriptor.Height, 96, 96, pf, null, rawImage, rawStride);


        }

        private void GifRunner_ImageDataReady(object sender, ImageReadyEventArgs e)
        {
            //Console.WriteLine(e.RgbImageData.Length);
            DrawToScreen(e.RgbImageData);
        }

        private void DrawToScreen(IEnumerable<RgbColor> rgbData)
        {
            PixelFormat pf = PixelFormats.Bgra32;
            int rawStride = (_gifData.ScreenDescriptor.Width * pf.BitsPerPixel + 7) / 8;
            byte[] rawImage = new byte[rawStride * _gifData.ScreenDescriptor.Height];

            int i = 0;

            rgbData
                .ToList()
                .ForEach((rgb) =>
                {
                    rawImage[i++] = rgb.Blue;
                    rawImage[i++] = rgb.Green;
                    rawImage[i++] = rgb.Red;
                    rawImage[i++] = (byte)(rgb.Transparent ? 0 : 255);
                });
            try
            {
                Dispatcher.Invoke(() => displayImage.Source = BitmapSource.Create(_gifData.ScreenDescriptor.Width, _gifData.ScreenDescriptor.Height, 96, 96, pf, null, rawImage, rawStride));
            }
            catch
            {

            }
        }

        private void LzwCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(lzwCodeTextBlock.Text))
            {
                return;
            }

            byte[] bytes = GetBytesFromHexString(lzwCodeTextBlock.Text);
            LzwCodeBytes codeBytes = new LzwCodeBytes(bytes.Skip(1)) { BitsPerCode = bytes[0] };
            

            GifLzwDecoding lzw = new GifLzwDecoding(bytes[0]);
            List<byte> imageBytes = lzw.DecodeLzwGifData(codeBytes);

            //HARDCODED SIZE

            int i = 0;

            imageBytes.ForEach((b) =>
            {
                if (i % 11 == 0)
                {
                    Debug.WriteLine("");
                }

                Debug.Write($"{imageBytes[i].ToString("000")} ");

                i++;
            });

            Debug.WriteLine("");
        }


        public byte[] GetBytesFromHexString(string hexString)
        {
            string[] hexVals = hexString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return hexVals.Select((v) => Convert.ToByte("0x" + v.Trim(), 16)).ToArray();
        }
    }





 
}
