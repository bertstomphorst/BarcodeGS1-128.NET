using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;

namespace Tools.Barcodes;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class Barcode_GS1_128
{
    private readonly List<bool> tags = [];

    public Barcode_GS1_128(string barcodeString)
    {
        var sum = 0;
        var multiply = 2;
        
        PushDot(true, 10); // Quiet zone start
        
        PushChar(105); // Start Code C
        sum += 105;
        PushChar(102); // FNC1
        sum += 102;

        var segments = barcodeString.Split('(', '\'');
        var isVariableLengthSegment = false;
        foreach (var segment in segments.Where(s => s.Length > 2))
        {
            var appIdentifier = segment[..2];
            var segmentValue = segment.Replace(")", "")[2..];

            // Previous segment variable-length? than first close this segment
            if (isVariableLengthSegment)
            {
                PushChar(102); // FNC1
                sum += 102 * multiply;
                multiply++;
            }
                
            isVariableLengthSegment = appIdentifier == "10";

            // app identifier, always 2-characters
            if (!int.TryParse(appIdentifier, out var parsedAppIdentifier))
                throw new InfoException("GS1-128-C supports only 0-9 for app identifiers");
            sum += parsedAppIdentifier * multiply;
            PushChar(parsedAppIdentifier);
            multiply++;

            // add prefix for fixed-length / min-length
            if (!isVariableLengthSegment && segmentValue.Length % 2 != 0)
                segmentValue = "0" + segmentValue;
            
            // segment value, in blocks of 2 characters, and eventually last block as 1 character
            for (var i = 0; i < segmentValue.Length; i += 2)
            {
                var part = segmentValue.Substring(i, Math.Min(2, segmentValue.Length - i));
                if (!int.TryParse(part, out var parsedPart))
                    throw new InfoException("GS1-128-C supports only 0-9, ( and )");

                // part-length 1, than switch to CodeSet B
                if (part.Length == 1)
                {
                    PushChar(100); // Code B
                    sum += 100 * multiply;
                    multiply++;

                    // actual part
                    parsedPart += 16; // +16 to get the right character from codeset B
                    sum += parsedPart * multiply;
                    PushChar(parsedPart);
                    multiply++;
                    
                    // back to Code C
                    PushChar(99); 
                    sum += 99 * multiply;
                    multiply++;
                }
                else
                {
                    sum += parsedPart * multiply;
                    PushChar(parsedPart);
                    multiply++;
                }
            }
        }

        //Checksum
        var checkCharacter = sum % 103;
        PushChar(checkCharacter);
        
        // Stop
        PushChar(106); 
                
        // Quiet zone end
        PushDot(true, 10);
    }

    public Bitmap ToBitMap(int width, int height)
    {
        var barWidth = width / tags.Count;
        var halfBarWidth = (int)(barWidth * 0.5);
        var shiftAdjustment = (width - barWidth * tags.Count) / 2;

        if (barWidth < 1)
            throw new InfoException("Pixel per dot < 1, barcode doesn't fit in given width");

        var bmp = new Bitmap(width, height);

        // clears the image and colors the entire background
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);

        // draw the bars
        using var pen = new Pen(Color.Black, barWidth);

        for (var pos = 0; pos < tags.Count; pos++)
            if (!tags[pos])
                g.DrawLine(pen, new Point(pos * barWidth + shiftAdjustment + halfBarWidth, 0), new Point(pos * barWidth + shiftAdjustment + halfBarWidth, height));

        return bmp;
    }

    public string ToBase64PNG(int dotHeight, int pixelPerDot)
    {
        var bmp = ToBitMap(dotHeight, pixelPerDot);
        var stream = new MemoryStream();
        bmp.Save(stream, ImageFormat.Png);
        return Convert.ToBase64String(stream.ToArray());
    }

    public string ToDataUriPNG(int dotHeight, int pixelPerDot)
    {
        return "data:image/png;base64," + ToBase64PNG(dotHeight, pixelPerDot);
    }

    private void PushChar(int charCode)
    {
        switch (charCode)
        {
            case 00:
                PushPattern(2, 1, 2, 2, 2, 2);
                break;
            case 01:
                PushPattern(2, 2, 2, 1, 2, 2);
                break;
            case 02:
                PushPattern(2, 2, 2, 2, 2, 1);
                break;
            case 03:
                PushPattern(1, 2, 1, 2, 2, 3);
                break;
            case 04:
                PushPattern(1, 2, 1, 3, 2, 2);
                break;
            case 05:
                PushPattern(1, 3, 1, 2, 2, 2);
                break;
            case 06:
                PushPattern(1, 2, 2, 2, 1, 3);
                break;
            case 07:
                PushPattern(1, 2, 2, 3, 1, 2);
                break;
            case 08:
                PushPattern(1, 3, 2, 2, 1, 2);
                break;
            case 09:
                PushPattern(2, 2, 1, 2, 1, 3);
                break;

            case 10:
                PushPattern(2, 2, 1, 3, 1, 2);
                break;
            case 11:
                PushPattern(2, 3, 1, 2, 1, 2);
                break;
            case 12:
                PushPattern(1, 1, 2, 2, 3, 2);
                break;
            case 13:
                PushPattern(1, 2, 2, 1, 3, 2);
                break;
            case 14:
                PushPattern(1, 2, 2, 2, 3, 1);
                break;
            case 15:
                PushPattern(1, 1, 3, 2, 2, 2);
                break;
            case 16:
                PushPattern(1, 2, 3, 1, 2, 2);
                break;
            case 17:
                PushPattern(1, 2, 3, 2, 2, 1);
                break;
            case 18:
                PushPattern(2, 2, 3, 2, 1, 1);
                break;
            case 19:
                PushPattern(2, 2, 1, 1, 3, 2);
                break;

            case 20:
                PushPattern(2, 2, 1, 2, 3, 1);
                break;
            case 21:
                PushPattern(2, 1, 3, 2, 1, 2);
                break;
            case 22:
                PushPattern(2, 2, 3, 1, 1, 2);
                break;
            case 23:
                PushPattern(3, 1, 2, 1, 3, 1);
                break;
            case 24:
                PushPattern(3, 1, 1, 2, 2, 2);
                break;
            case 25:
                PushPattern(3, 2, 1, 1, 2, 2);
                break;
            case 26:
                PushPattern(3, 2, 1, 2, 2, 1);
                break;
            case 27:
                PushPattern(3, 1, 2, 2, 1, 2);
                break;
            case 28:
                PushPattern(3, 2, 2, 1, 1, 2);
                break;
            case 29:
                PushPattern(3, 2, 2, 2, 1, 1);
                break;

            case 30:
                PushPattern(2, 1, 2, 1, 2, 3);
                break;
            case 31:
                PushPattern(2, 1, 2, 3, 2, 1);
                break;
            case 32:
                PushPattern(2, 3, 2, 1, 2, 1);
                break;
            case 33:
                PushPattern(1, 1, 1, 3, 2, 3);
                break;
            case 34:
                PushPattern(1, 3, 1, 1, 2, 3);
                break;
            case 35:
                PushPattern(1, 3, 1, 3, 2, 1);
                break;
            case 36:
                PushPattern(1, 1, 2, 3, 1, 3);
                break;
            case 37:
                PushPattern(1, 3, 2, 1, 1, 3);
                break;
            case 38:
                PushPattern(1, 3, 2, 3, 1, 1);
                break;
            case 39:
                PushPattern(2, 1, 1, 3, 1, 3);
                break;

            case 40:
                PushPattern(2, 3, 1, 1, 1, 3);
                break;
            case 41:
                PushPattern(2, 3, 1, 3, 1, 1);
                break;
            case 42:
                PushPattern(1, 1, 2, 1, 3, 3);
                break;
            case 43:
                PushPattern(1, 1, 2, 3, 3, 1);
                break;
            case 44:
                PushPattern(1, 3, 2, 1, 3, 1);
                break;
            case 45:
                PushPattern(1, 1, 3, 1, 2, 3);
                break;
            case 46:
                PushPattern(1, 1, 3, 3, 2, 1);
                break;
            case 47:
                PushPattern(1, 3, 3, 1, 2, 1);
                break;
            case 48:
                PushPattern(3, 1, 3, 1, 2, 1);
                break;
            case 49:
                PushPattern(2, 1, 1, 3, 3, 1);
                break;

            case 50:
                PushPattern(2, 3, 1, 1, 3, 1);
                break;
            case 51:
                PushPattern(2, 1, 3, 1, 1, 3);
                break;
            case 52:
                PushPattern(2, 1, 3, 3, 1, 1);
                break;
            case 53:
                PushPattern(2, 1, 3, 1, 3, 1);
                break;
            case 54:
                PushPattern(3, 1, 1, 1, 2, 3);
                break;
            case 55:
                PushPattern(3, 1, 1, 3, 2, 1);
                break;
            case 56:
                PushPattern(3, 3, 1, 1, 2, 1);
                break;
            case 57:
                PushPattern(3, 1, 2, 1, 1, 3);
                break;
            case 58:
                PushPattern(3, 1, 2, 3, 1, 1);
                break;
            case 59:
                PushPattern(3, 3, 2, 1, 1, 1);
                break;

            case 60:
                PushPattern(3, 1, 4, 1, 1, 1);
                break;
            case 61:
                PushPattern(2, 2, 1, 4, 1, 1);
                break;
            case 62:
                PushPattern(4, 3, 1, 1, 1, 1);
                break;
            case 63:
                PushPattern(1, 1, 1, 2, 2, 4);
                break;
            case 64:
                PushPattern(1, 1, 1, 4, 2, 2);
                break;
            case 65:
                PushPattern(1, 2, 1, 1, 2, 4);
                break;
            case 66:
                PushPattern(1, 2, 1, 4, 2, 1);
                break;
            case 67:
                PushPattern(1, 4, 1, 1, 2, 2);
                break;
            case 68:
                PushPattern(1, 4, 1, 2, 2, 1);
                break;
            case 69:
                PushPattern(1, 1, 2, 2, 1, 4);
                break;

            case 70:
                PushPattern(1, 1, 2, 4, 1, 2);
                break;
            case 71:
                PushPattern(1, 2, 2, 1, 1, 4);
                break;
            case 72:
                PushPattern(1, 2, 2, 4, 1, 1);
                break;
            case 73:
                PushPattern(1, 4, 2, 1, 1, 2);
                break;
            case 74:
                PushPattern(1, 4, 2, 2, 1, 1);
                break;
            case 75:
                PushPattern(2, 4, 1, 2, 1, 1);
                break;
            case 76:
                PushPattern(2, 2, 1, 1, 1, 4);
                break;
            case 77:
                PushPattern(4, 1, 3, 1, 1, 1);
                break;
            case 78:
                PushPattern(2, 4, 1, 1, 1, 2);
                break;
            case 79:
                PushPattern(1, 3, 4, 1, 1, 1);
                break;

            case 80:
                PushPattern(1, 1, 1, 2, 4, 2);
                break;
            case 81:
                PushPattern(1, 2, 1, 1, 4, 2);
                break;
            case 82:
                PushPattern(1, 2, 1, 2, 4, 1);
                break;
            case 83:
                PushPattern(1, 1, 4, 2, 1, 2);
                break;
            case 84:
                PushPattern(1, 2, 4, 1, 1, 2);
                break;
            case 85:
                PushPattern(1, 2, 4, 2, 1, 1);
                break;
            case 86:
                PushPattern(4, 1, 1, 2, 1, 2);
                break;
            case 87:
                PushPattern(4, 2, 1, 1, 1, 2);
                break;
            case 88:
                PushPattern(4, 2, 1, 2, 1, 1);
                break;
            case 89:
                PushPattern(2, 1, 2, 1, 4, 1);
                break;

            case 90:
                PushPattern(2, 1, 4, 1, 2, 1);
                break;
            case 91:
                PushPattern(4, 1, 2, 1, 2, 1);
                break;
            case 92:
                PushPattern(1, 1, 1, 1, 4, 3);
                break;
            case 93:
                PushPattern(1, 1, 1, 3, 4, 1);
                break;
            case 94:
                PushPattern(1, 3, 1, 1, 4, 1);
                break;
            case 95:
                PushPattern(1, 1, 4, 1, 1, 3);
                break;
            case 96:
                PushPattern(1, 1, 4, 3, 1, 1);
                break;
            case 97:
                PushPattern(4, 1, 1, 1, 1, 3);
                break;
            case 98:
                PushPattern(4, 1, 1, 3, 1, 1);
                break;
            case 99:
                PushPattern(1, 1, 3, 1, 4, 1);
                break;

            case 100:
                PushPattern(1, 1, 4, 1, 3, 1);
                break;
            case 101:
                PushPattern(3, 1, 1, 1, 4, 1);
                break;
            case 102:
                PushPattern(4, 1, 1, 1, 3, 1);
                break;
            case 103:
                PushPattern(2, 1, 1, 4, 1, 2);
                break;
            case 104:
                PushPattern(2, 1, 1, 2, 1, 4);
                break;
            case 105:
                PushPattern(2, 1, 1, 2, 3, 2);
                break;
            case 106:
                PushPattern(2, 3, 3, 1, 1, 1, 2);
                break;
        }
    }

    private void PushPattern(int b1, int s1, int b2, int s2, int b3, int s3, int b5 = 0)
    {
        PushDot(false, b1);
        PushDot(true, s1);
        PushDot(false, b2);
        PushDot(true, s2);
        PushDot(false, b3);
        PushDot(true, s3);
        PushDot(false, b5);
    }

    private void PushDot(bool isWhite, int dot)
    {
        for (var i = 0; i < dot; i++)
            tags.Add(isWhite);
    }
}