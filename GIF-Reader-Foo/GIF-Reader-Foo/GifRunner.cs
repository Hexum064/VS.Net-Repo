using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace GIF_Reader_Foo
{
    public class GifRunner :IDisposable
    {
        public const byte DISPOSAL_NOT_SPECIFIED = 0;
        public const byte DISPOSAL_DO_NOT_DISPOSE = 1;
        public const byte DISPOSAL_RESTORE_BACKGROUND = 2;
        public const byte DISPOSAL_RESTORE_PREVIOUS = 3;

        public event EventHandler<ImageReadyEventArgs> ImageDataReady;

        private List<RgbColor> _currentRgbImageData = new List<RgbColor>();
        private List<RgbColor> _previousRgbImageData = new List<RgbColor>();
        
        public GifRunner(GifData gifData)
        {
            GifData = gifData;
        }

        public async void Start()
        {
            IsRunning = true;
            await InitImageAsync();
            await AnimateGif();
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public bool IsRunning { get; private set; }

        private Task AnimateGif()
        {
            //TODO: address limited or no animation. For now assume infinite repeat
            return Task.Run(() =>
            {
               
                while (IsRunning)
                {

                    GifData.Images
                        .ForEach((image) => DrawImageToCurrent(image));
                    
                }
            });

        }

        private Task InitImageAsync()
        {
            return Task.Run(() =>
            {
                //Assumes that there is a global color table
                _currentRgbImageData = LoadBackgroundIndex();
                _previousRgbImageData = LoadBackgroundIndex();
            });
        }

        private List<RgbColor> LoadBackgroundIndex(byte transparencyColorIndex = 0, bool hasTransparency = false)
        {
            RgbColor color = GifData?.GlobalColorTable?.Colors[GifData.ScreenDescriptor.BackgroundColorIndex] ?? new RgbColor();
            color.Transparent = hasTransparency && transparencyColorIndex == GifData.ScreenDescriptor.BackgroundColorIndex;

            return Enumerable.Repeat(color, GifData.ScreenDescriptor.Width * GifData.ScreenDescriptor.Height)
                .ToList();
        }

        private void DrawImageToCurrent(ImageData imageData)
        {
            int delayTime = imageData?.GraphicsExtention?.DelayTime ?? 1;

            //Always have a delay of at least 10ms
            if (delayTime < 1)
            {
                delayTime = 1;
            }

            //Convert to hundredths of seconds
            delayTime *= 10;

            //Get the RGB image data
            List<RgbColor> rgbImageData = ColorizeDecodedImageBytes(DecodeImage(imageData), imageData);

            //Create a copy of the current image and store it in the previous image buffer
            _previousRgbImageData = _currentRgbImageData
                .ToList();

            OverlayNewImageOnCurrent(rgbImageData, imageData);

            //This allows the implementer to draw the image to the screen
            RaiseImageDataReady(_currentRgbImageData, imageData);

            Thread.Sleep(delayTime);
        }

        private void OverlayNewImageOnCurrent(List<RgbColor> rgbImageData, ImageData imageData)
        {

            DoImageDisposalOnCurrent(imageData);
            int index = 0;

            //TODO: Support interlacing

            //The limiting number for the for blocks needs to be the starting coordinate + the total distance
            for (int y = imageData.DescriptorBlock.Y; y < imageData.DescriptorBlock.Y + imageData.DescriptorBlock.Height; y++)
            {
                for (int x = imageData.DescriptorBlock.X; x < imageData.DescriptorBlock.X + imageData.DescriptorBlock.Width; x++)
                {
                    if  (index < rgbImageData.Count)
                    //Make sure to use the full width of the GIF here instead of just the current image
                    _currentRgbImageData[(y * GifData.ScreenDescriptor.Width) + x] = rgbImageData[index++];

                }
            }

        }

        private void DoImageDisposalOnCurrent(ImageData imageData)
        {
            switch (imageData?.GraphicsExtention?.DisposalMethod ?? 0)
            {
                case DISPOSAL_NOT_SPECIFIED:
                case DISPOSAL_DO_NOT_DISPOSE:
                    break;
                case DISPOSAL_RESTORE_BACKGROUND:
                    _currentRgbImageData = LoadBackgroundIndex(imageData?.GraphicsExtention?.TransparentColorIndex ?? 0, imageData?.GraphicsExtention?.TransparentColorFlag ?? false);
                    break;
                case DISPOSAL_RESTORE_PREVIOUS:
                    _currentRgbImageData = _previousRgbImageData
                        .ToList();
                    break;
            }
        }

        private List<byte> DecodeImage(ImageData image)
        {
            GifLzwDecoding decoder = new GifLzwDecoding(image.RawImageData.MinCodeSize);
            return decoder.DecodeLzwGifData(image.RawImageData.ImageSubBlocks);        
        }

        private List<RgbColor> ColorizeDecodedImageBytes(List<byte> decodedBytes, ImageData image)
        {
            ColorTable colorTable = image.DescriptorBlock.LocalColorTableFlag ? image.LocalColorTable : GifData.GlobalColorTable;


            return decodedBytes
                .Select((b) => image?.GraphicsExtention?.TransparentColorFlag == true && image?.GraphicsExtention?.TransparentColorIndex == b ? new RgbColor() { Transparent = true } : colorTable.Colors[b])
                .ToList();               
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public GifData GifData
        {
            get;
            private set;
        }

        private void RaiseImageDataReady(IEnumerable<RgbColor> rgbImageData, ImageData imageData)
        {
            ImageDataReady?.Invoke(this, new ImageReadyEventArgs(rgbImageData, imageData));
        }

        
    }
}
