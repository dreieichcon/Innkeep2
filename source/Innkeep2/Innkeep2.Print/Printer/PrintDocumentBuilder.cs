using System.Text;
using ESCPOS;

namespace Innkeep2.Print.Printer;

public sealed class PrintDocumentBuilder
{
    private readonly List<byte> _bytes = [];
    
    static PrintDocumentBuilder()
    {
       Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public PrintDocumentBuilder()
    {
       Append(Commands.SelectCodeTable(CodeTable.Windows1252));
    }

    public PrintDocumentBuilder AddTitle(
       string title,
       Justification justification = Justification.Center,
       CharSizeWidth width = CharSizeWidth.Double,
       CharSizeHeight height = CharSizeHeight.Double
    )
    {
       Append(Commands.SelectJustification(justification));
       Append(Commands.SelectCharSize(width, height));
       Append(title);
       Append(Commands.SelectCharSize(CharSizeWidth.Normal, CharSizeHeight.Normal));
       Append(Commands.SelectJustification(Justification.Left));
       Append(Commands.LF);
       return this;
    }

    public PrintDocumentBuilder AddLine(string lineText, Justification justification = Justification.Left)
    {
       Append(Commands.SelectJustification(justification));
       Append(lineText);
       Append(Commands.LF);
       Append(Commands.SelectJustification(Justification.Left));
       return this;
    }

    public PrintDocumentBuilder AddEmptyLine() => AddEmptyLines(1);

    public PrintDocumentBuilder AddEmptyLines(int count)
    {
       for (var i = 0; i < count; i++)
          Append(Commands.LF);
       return this;
    }

    public PrintDocumentBuilder AddQrCode(string content, QRCodeModel model = QRCodeModel.Model2, QRCodeSize size = QRCodeSize.Normal)
    {
       Append(Commands.SelectJustification(Justification.Center));
       Append(Commands.QRCode(content, model, qrCodeSize: size));
       Append(Commands.LF);
       return this;
    }

    public PrintDocumentBuilder Cut()
    {
       for (var i = 0; i < 5; i++)
          Append(Commands.LF);

       Append(Commands.FullPaperCut);
       return this;
    }

    public PrintDocumentBuilder PartialCut()
    {
       for (var i = 0; i < 5; i++)
          Append(Commands.LF);

       Append(Commands.PaperCut);
       return this;
    }
    
    public PrintDocumentBuilder AddEmphasizedLine(string text, Justification justification = Justification.Center)
    {
       Append(Commands.SelectJustification(justification));
       Append(Commands.SelectCharSize(CharSizeWidth.Normal, CharSizeHeight.Double));
       Append(text);
       Append(Commands.SelectCharSize(CharSizeWidth.Normal, CharSizeHeight.Normal));
       Append(Commands.LF);
       Append(Commands.SelectJustification(Justification.Left));
       return this;
    }

    public PrintDocumentBuilder OpenDrawer()
    {
       Append(Commands.OpenDrawer);
       return this;
    }

    public byte[] GetBytes() => _bytes.ToArray();

    private void Append(byte[] command) => _bytes.AddRange(command);

    private void Append(string text) => _bytes.AddRange(Encoding.GetEncoding(1252).GetBytes(text));
}