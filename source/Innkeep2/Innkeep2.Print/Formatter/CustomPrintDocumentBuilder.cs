using Innkeep2.Print.Printer;

namespace Innkeep2.Print.Formatter;

public static class CustomPrintDocumentBuilder
{
    /// <summary>
    /// Printed text always takes at least this many lines, otherwise the slip is too short to grab.
    /// </summary>
    private const int MinimumTextLines = 10;

    public static byte[] BuildText(string text)
    {
       var builder = new PrintDocumentBuilder();

       var lines = text.ReplaceLineEndings("\n").Split('\n');

       foreach (var line in lines)
          builder.AddLine(line);

       for (var i = lines.Length; i < MinimumTextLines; i++)
          builder.AddEmptyLine();

       builder.Cut();

       return builder.GetBytes();
    }

    public static byte[] BuildImage(Stream imageStream, int maxWidthDots, bool rotateLandscape = false)
    {
       var raster = ImageRasterizer.ToRaster(imageStream, maxWidthDots, rotateLandscape);

       var builder = new PrintDocumentBuilder();

       builder.AddRasterImage(raster);
       builder.Cut();

       return builder.GetBytes();
    }

    /// <summary>
    /// A frame exactly <paramref name="widthDots"/> wide. If its left or right edge is missing on paper, the width is too large.
    /// </summary>
    public static byte[] BuildWidthTest(int widthDots)
    {
       const int height = 120;
       const int border = 4;

       var widthBytes = (widthDots + 7) / 8;
       var data = new byte[widthBytes * height];

       for (var y = 0; y < height; y++)
       for (var x = 0; x < widthDots; x++)
       {
          var onFrame = y < border || y >= height - border || x < border || x >= widthDots - border;

          if (onFrame)
             data[y * widthBytes + x / 8] |= (byte)(0x80 >> (x % 8));
       }

       var builder = new PrintDocumentBuilder();

       builder.AddRasterImage(new RasterImage(data, widthBytes, height));
       builder.AddLine($"{widthDots} Punkte");
       builder.Cut();

       return builder.GetBytes();
    }
}
