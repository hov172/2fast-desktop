#if !WINDOWS_UWP
namespace Project2FA.Services;
internal static class QrImageRenderer
{
    internal static byte[] RenderPng(string value)
    {
        var writer = new ZXing.BarcodeWriterPixelData
        {
            Format = ZXing.BarcodeFormat.QR_CODE,
            Options = new ZXing.Common.EncodingOptions { Width = 320, Height = 320, Margin = 4 }
        };
        var pixels = writer.Write(value);
        using var bitmap = new SkiaSharp.SKBitmap(pixels.Width, pixels.Height, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Opaque);
        System.Runtime.InteropServices.Marshal.Copy(pixels.Pixels, 0, bitmap.GetPixels(), pixels.Pixels.Length);
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }
}
#endif
