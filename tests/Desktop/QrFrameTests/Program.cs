using Project2FA.Services.MacOS;
var scanner = new DesktopQrFrameDecoder();
var writer = new ZXing.BarcodeWriterPixelData { Format = ZXing.BarcodeFormat.QR_CODE, Options = new ZXing.Common.EncodingOptions { Width = 512, Height = 512, Margin = 4 } };
if (scanner.Decode(Enumerable.Repeat((byte)255, 512 * 512 * 4).ToArray(), 512, 512) != null) throw new Exception("Blank frame accepted");
foreach (string payload in new[] { "otpauth://totp/Issuer:Account?secret=JBSWY3DPEHPK3PXP&issuer=Issuer", "otpauth://ocra/Synthetic?secret=JBSWY3DPEHPK3PXP&ocrasuite=OCRA-1:HOTP-SHA1-6:QN08-T1M", "mobileid://synthetic-profile-for-decoder-only", "otpauth://totp/Example%3AUser?secret=JBSWY3DPEHPK3PXP&issuer=Example%26Co" })
{
    var frame = writer.Write(payload);
    if (scanner.Decode(frame.Pixels, frame.Width, frame.Height) != payload) throw new Exception("QR payload changed");
    for (int i = 0; i < frame.Pixels.Length; i += 4) for (int c = 0; c < 3; c++) frame.Pixels[i + c] ^= 255;
    if (scanner.Decode(frame.Pixels, frame.Width, frame.Height) != payload) throw new Exception("Inverted QR missed");
}
try { scanner.Decode(new byte[4], int.MaxValue, int.MaxValue); throw new Exception("Oversized frame accepted"); } catch (ArgumentException) { }
Console.WriteLine("QR frames: blank-to-QR progression, repeated TOTP/OCRA/MobileID decoding, inverted QR, exact URI preservation and dimension limits passed.");
