using ImageMagick;

namespace Innkeep2.Print.Printer;

public sealed record RasterImage(byte[] Data, int WidthBytes, int HeightDots);

public sealed class UnreadableImageException(string message, Exception inner) : Exception(message, inner);

public static class ImageRasterizer
{
    /// <summary>
    /// Guards against images that are small as a file but huge once decoded.
    /// </summary>
    private const uint MaxSourceDimension = 12000;

    static ImageRasterizer()
    {
       ResourceLimits.Width = MaxSourceDimension;
       ResourceLimits.Height = MaxSourceDimension;
    }

    /// <summary>
    /// Converts an image to a 1 bit (black/white, dithered) bitmap no wider than <paramref name="maxWidthDots"/>.
    /// Images narrower than that keep their size. With <paramref name="rotateLandscape"/>, landscape images are turned
    /// by 90 degrees first.
    /// </summary>
    public static RasterImage ToRaster(Stream imageStream, int maxWidthDots, bool rotateLandscape = false)
    {
       MagickImage image;

       try
       {
          // Animated formats yield their first frame.
          image = new MagickImage(imageStream);
       }
       catch (MagickException ex)
       {
          throw new UnreadableImageException("Die Datei konnte nicht als Bild gelesen werden.", ex);
       }

       using (image)
       {
          // 1 bit sources (typical for black and white logos) would otherwise export as packed bits.
          image.Depth = 8;

          image.AutoOrient();

          // The paper is narrow and endless, so a landscape image prints much larger when turned to run along it.
          if (rotateLandscape && image.Width > image.Height)
             image.Rotate(90);

          // Transparent areas would otherwise become black.
          image.BackgroundColor = MagickColors.White;
          image.Alpha(AlphaOption.Remove);

          // The '>' flag only ever shrinks.
          image.Resize(new MagickGeometry($"{maxWidthDots}x>"));

          image.ColorSpace = ColorSpace.Gray;
          image.Quantize(new QuantizeSettings
          {
             Colors = 2,
             ColorSpace = ColorSpace.Gray,
             DitherMethod = DitherMethod.FloydSteinberg
          });

          var width = (int)image.Width;
          var height = (int)image.Height;
          var gray = image.ToByteArray(MagickFormat.Gray);

          var widthBytes = (width + 7) / 8;
          var data = new byte[widthBytes * height];

          for (var y = 0; y < height; y++)
          for (var x = 0; x < width; x++)
          {
             // Quantizing leaves only black and white, so any dark value means a printed dot.
             if (gray[y * width + x] < 128)
                data[y * widthBytes + x / 8] |= (byte)(0x80 >> (x % 8));
          }

          return new RasterImage(data, widthBytes, height);
       }
    }
}
