#if TWOFAST_DESKTOP
namespace Project2FA.Services.Desktop;
internal sealed class DesktopQrFrameDecoder
{
    private readonly ZXing.BarcodeReaderGeneric reader = new()
    {
        AutoRotate = true,
        Options = new ZXing.Common.DecodingOptions
        { TryHarder = true, TryInverted = true, PossibleFormats = new[] { ZXing.BarcodeFormat.QR_CODE } }
    };
    internal string Decode(byte[] pixels, int width, int height)
    {
        if (width <= 0 || height <= 0 || (long)width * height > 33_554_432 || pixels?.LongLength != (long)width * height * 4)
            throw new ArgumentException("Invalid QR frame dimensions.");
        string value = reader.Decode(pixels, width, height, ZXing.RGBLuminanceSource.BitmapFormat.BGRA32)?.Text;
        return !string.IsNullOrWhiteSpace(value) && value.Length <= 16384 ? value : null;
    }
}
#endif
