#if TWOFAST_WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenCvSharp;
using Project2FA.Services;
using Project2FA.UnoApp;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Windows.Storage.Streams;

namespace Project2FA.Services.Desktop;
internal static class WindowsQrScanner
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly SemaphoreSlim CameraGate = new(1, 1);
    internal static async Task<string?> Scan(CancellationToken token, bool screen)
    {
        if (!await Gate.WaitAsync(0, token)) throw new InvalidOperationException("A scanner is already open.");
        try { return await new Session().Run(token, screen); }
        finally { Gate.Release(); }
    }
    private sealed class Session
    {
        private readonly Image preview = new() { Height = 330, Stretch = Stretch.Uniform };
        private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, MaxWidth = 560 };
        private readonly ComboBox sources = new() { Header = DesktopText.Get("QrCaptureSource", "Capture source"), MinWidth = 360 };
        private readonly Button refresh = new() { Content = DesktopText.Get("QrRefreshSources", "Refresh sources") };
        private ContentDialog dialog;
        private CancellationTokenSource capture;
        private Task loop = Task.CompletedTask;
        private int generation;
        private bool closed, changing;
        private string result;
        internal async Task<string?> Run(CancellationToken token, bool screen)
        {
            var panel = new StackPanel { Spacing = 12, MinWidth = 560 };
            panel.Children.Add(new TextBlock { Text = DesktopText.Get("QrScannerInstructions", "Choose a camera, window or screen. Scanning continues until a QR code is found or you close this dialog."), TextWrapping = TextWrapping.Wrap, MaxWidth = 560 });
            panel.Children.Add(sources); panel.Children.Add(refresh); panel.Children.Add(preview); panel.Children.Add(status);
            dialog = new ContentDialog { Title = DesktopText.Get("QrScannerTitle", "Scan QR code"), Content = panel, CloseButtonText = DesktopText.Get("QrScannerCancel", "Cancel"), XamlRoot = DesktopSession.Shell.XamlRoot };
            void Refresh()
            {
                var choices = WindowsScreenCapture.Sources();
                choices.AddRange(WindowsCameras.Sources());
                sources.ItemsSource = choices;
                int selected = screen ? 0 : choices.FindIndex(s => s.Camera == 0);
                sources.SelectedIndex = selected >= 0 ? selected : (choices.Count > 0 ? 0 : -1);
            }
            Refresh();
            sources.SelectionChanged += async (_, _) => await Restart();
            refresh.Click += (_, _) => Refresh();
            dialog.Opened += async (_, _) => await Restart();
            dialog.Closing += (_, _) => { closed = true; generation++; capture?.Cancel(); preview.Source = null; };
            using var registration = token.Register(() => dialog.DispatcherQueue.TryEnqueue(() => dialog.Hide()));
            try { await dialog.ShowAsync(); }
            finally { closed = true; generation++; capture?.Cancel(); await StopLoop(); capture?.Dispose(); preview.Source = null; }
            token.ThrowIfCancellationRequested();
            return result;
        }
        private async Task Restart()
        {
            if (closed || changing) return;
            changing = true; sources.IsEnabled = refresh.IsEnabled = false;
            try
            {
                generation++; capture?.Cancel(); await StopLoop(); capture?.Dispose(); preview.Source = null;
                if (closed || sources.SelectedItem is not WindowsCaptureSource source) return;
                capture = new CancellationTokenSource(); int current = generation;
                status.Text = source.Camera >= 0 ? DesktopText.Get("QrOpeningCamera", "Opening camera… If access fails, use a window or screen, or allow desktop camera access in Windows Settings.") : DesktopText.Get("QrScanningContinuously", "Scanning continuously. Keep the QR visible. If a window preview is black, select its screen instead.");
                var captureToken = capture.Token;
                loop = Task.Run(() => CaptureLoop(source, current, captureToken));
            }
            finally { changing = false; sources.IsEnabled = refresh.IsEnabled = true; }
        }
        private async Task StopLoop()
        {
            // Drivers cannot always cancel a synchronous read. The worker retains ownership of
            // its native handles; the user can close the dialog and choose screen capture meanwhile.
            try { await loop.WaitAsync(TimeSpan.FromSeconds(3)); }
            catch (TimeoutException) { }
        }
        private async Task CaptureLoop(WindowsCaptureSource source, int current, CancellationToken token)
        {
            VideoCapture camera = null;
            bool ownsCamera = false;
            try
            {
                if (source.Camera >= 0)
                {
                    ownsCamera = await CameraGate.WaitAsync(0, token);
                    if (!ownsCamera) throw new IOException("The previous camera is still stopping. You can scan a screen while it closes.");
                    camera = new VideoCapture(source.Camera, VideoCaptureAPIs.DSHOW);
                    if (!camera.IsOpened()) throw new IOException("Camera unavailable. Check Windows Settings → Privacy & security → Camera, select another camera, or scan a screen.");
                    camera.Set(VideoCaptureProperties.FrameWidth, 1280); camera.Set(VideoCaptureProperties.FrameHeight, 720);
                }
                var reader = new DesktopQrFrameDecoder();
                while (!token.IsCancellationRequested)
                {
                    byte[] pixels; int width, height;
                    if (camera == null) (pixels, width, height) = WindowsScreenCapture.Capture(source);
                    else
                    {
                        using var frame = new Mat(); using var bgra = new Mat();
                        if (!camera.Read(frame) || frame.Empty()) throw new IOException("Camera stopped delivering frames. Reconnect it or select another source.");
                        Cv2.CvtColor(frame, bgra, ColorConversionCodes.BGR2BGRA);
                        width = bgra.Width; height = bgra.Height; pixels = new byte[checked(width * height * 4)]; Marshal.Copy(bgra.Data, pixels, 0, pixels.Length);
                    }
                    try
                    {
                        var decoded = reader.Decode(pixels, width, height);
                        using var bitmap = new SkiaSharp.SKBitmap(width, height, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Opaque);
                        Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
                        using var small = width > 720 || height > 420
                            ? bitmap.Resize(new SkiaSharp.SKImageInfo(Math.Max(1, (int)(width * Math.Min(720d / width, 420d / height))), Math.Max(1, (int)(height * Math.Min(720d / width, 420d / height)))), new SkiaSharp.SKSamplingOptions(SkiaSharp.SKFilterMode.Linear, SkiaSharp.SKMipmapMode.Linear)) : null;
                        using var image = SkiaSharp.SKImage.FromBitmap(small ?? bitmap); using var png = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 80);
                        byte[] bytes = png.ToArray();
                        var applied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        if (!dialog.DispatcherQueue.TryEnqueue(async () =>
                        {
                            try
                            {
                                if (closed || current != generation || token.IsCancellationRequested) return;
                                // DataWriter.DetachStream is not implemented in Uno desktop; write the buffer directly.
                                using var stream = new InMemoryRandomAccessStream(); await stream.WriteAsync(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(bytes));
                                stream.Seek(0); var sourceImage = new BitmapImage(); await sourceImage.SetSourceAsync(stream);
                                if (closed || current != generation || token.IsCancellationRequested) return;
                                preview.Source = sourceImage;
                                if (decoded != null)
                                { result = decoded; dialog.Hide(); }
                            }
                            catch (Exception error) { DesktopScanDiagnostics.Record("Windows scan preview", error); if (!closed) status.Text = DesktopText.Get("QrPreviewFailed", "Preview could not be displayed. Select the source again."); }
                            finally { CryptographicOperations.ZeroMemory(bytes); applied.TrySetResult(); }
                        })) break;
                        await applied.Task.WaitAsync(token);
                        if (result != null) break;
                    }
                    finally { CryptographicOperations.ZeroMemory(pixels); }
                    await Task.Delay(150, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                DesktopScanDiagnostics.Record("Windows scanner", error);
                dialog.DispatcherQueue.TryEnqueue(() => { if (!closed && current == generation) status.Text = error is IOException ? error.Message : DesktopText.Get("QrCaptureFailed", "Capture failed. Try another source or check camera permissions."); });
            }
            finally { camera?.Dispose(); if (ownsCamera) CameraGate.Release(); }
        }
    }
}
#endif
