using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace AutoSwitchCam.Helper
{
    class JpegEncoderHelper
    {
        public static MemoryStream BitmapToJpeg(Bitmap image)
        {
            MemoryStream imageBuffer = new MemoryStream();

            EncoderParameters encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
            image.Save(imageBuffer, GetEncoder(ImageFormat.Jpeg), encoderParameters);

            return imageBuffer;
        }
        public static MemoryStream WriteableBitmapToJpeg(WriteableBitmap writeableImage)
        {
            MemoryStream imageBuffer = new MemoryStream();

            JpegBitmapEncoder encoder = new JpegBitmapEncoder { QualityLevel = 100, FlipHorizontal = true };
            encoder.Frames.Add(BitmapFrame.Create(writeableImage));
            encoder.Save(imageBuffer);

            return imageBuffer;
        }

        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
