#import "../../src/Project2FA.Uno/Platforms/Desktop/Native/TwoFastMac.m"
#import <CoreImage/CoreImage.h>

static void Require(BOOL condition, const char *name) {
    if (!condition) { fprintf(stderr, "FAIL: %s\n", name); exit(1); }
    printf("PASS: %s\n", name);
}
static void CameraResult(int status, const char *payload, void *state) {
    printf("Camera completed with status %d; payload present: %s\n", status, payload ? "yes" : "no");
    fflush(stdout);
    [NSApp stop:nil];
    // Wake NSApplication.run after stop.
    [NSApp postEvent:[NSEvent otherEventWithType:NSEventTypeApplicationDefined location:NSZeroPoint
        modifierFlags:0 timestamp:0 windowNumber:0 context:nil subtype:0 data1:0 data2:0] atStart:NO];
}
// Exercise the real screen-frame path, including the camera-to-screen throttle edge.
@interface FrameTestScanner : TFScanner
@property(nonatomic, copy) NSString *decodedPayload;
@end
@implementation FrameTestScanner
- (void)finish:(int)status payload:(NSString *)payload {
    self.decodedPayload = payload;
    self.finished = YES;
}
@end
static void TestStaticScreenFrame(NSString *payload) {
    CIFilter *qrFilter = [CIFilter filterWithName:@"CIQRCodeGenerator"];
    [qrFilter setValue:[payload dataUsingEncoding:NSUTF8StringEncoding] forKey:@"inputMessage"];
    CIImage *qr = [qrFilter.outputImage imageByApplyingTransform:CGAffineTransformMakeScale(6, 6)];
    size_t width = (size_t)qr.extent.size.width, height = (size_t)qr.extent.size.height;
    CVPixelBufferRef pixels = NULL;
    NSDictionary *options = @{(id)kCVPixelBufferCGImageCompatibilityKey:@YES, (id)kCVPixelBufferCGBitmapContextCompatibilityKey:@YES};
    Require(CVPixelBufferCreate(kCFAllocatorDefault, width, height, kCVPixelFormatType_32BGRA, (__bridge CFDictionaryRef)options, &pixels) == kCVReturnSuccess, "Screen fixture pixel buffer");
    CIContext *context = [CIContext context];
    [context render:qr toCVPixelBuffer:pixels];
    CMVideoFormatDescriptionRef format = NULL;
    CMVideoFormatDescriptionCreateForImageBuffer(kCFAllocatorDefault, pixels, &format);
    CMSampleBufferRef sample = NULL;
    CMSampleTimingInfo timing = {kCMTimeInvalid, kCMTimeZero, kCMTimeInvalid};
    CMSampleBufferCreateReadyWithImageBuffer(kCFAllocatorDefault, pixels, format, &timing, &sample);
    FrameTestScanner *scanner = [FrameTestScanner new];
    scanner.screenMode = YES;
    scanner.lastFrame = CACurrentMediaTime();
    scanner.imageContext = context;
    scanner.screenPreview = [CALayer layer];
    scanner.request = [VNDetectBarcodesRequest new];
    scanner.request.symbologies = @[VNBarcodeSymbologyQR];
    [scanner processSample:sample];
    NSDate *deadline = [NSDate dateWithTimeIntervalSinceNow:3];
    while (!scanner.finished && deadline.timeIntervalSinceNow > 0)
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:0.02]];
    Require([scanner.decodedPayload isEqualToString:payload], "First static screen QR survives recent camera frame");
    Require(scanner.screenPreview.contents != nil, "Selected screen frame reaches preview layer");
    CFRelease(sample); CFRelease(format); CVPixelBufferRelease(pixels);
}
static NSWindow *fixtureWindow;
int main(int argc, const char *argv[]) {
    @autoreleasepool {
        BOOL interactive = argc > 1 && strcmp(argv[1], "--biometric") == 0;
        BOOL screen = argc > 1 && strcmp(argv[1], "--screen") == 0;
        BOOL camera = screen || (argc > 1 && strcmp(argv[1], "--camera") == 0);
        if (camera) {
            [NSApplication sharedApplication];
            [NSApp setActivationPolicy:NSApplicationActivationPolicyRegular];
            [NSApp activateIgnoringOtherApps:YES];
            if (screen) {
                fixtureWindow = [[NSWindow alloc] initWithContentRect:NSMakeRect(100, 100, 520, 560)
                    styleMask:NSWindowStyleMaskTitled | NSWindowStyleMaskClosable backing:NSBackingStoreBuffered defer:NO];
                fixtureWindow.title = @"2fast Synthetic QR Test";
                fixtureWindow.releasedWhenClosed = NO;
                CIFilter *filter = [CIFilter filterWithName:@"CIQRCodeGenerator"];
                [filter setValue:[@"otpauth://totp/2fast:IntegrationTest?secret=JBSWY3DPEHPK3PXP&issuer=2fast" dataUsingEncoding:NSUTF8StringEncoding] forKey:@"inputMessage"];
                CIImage *qr = [filter.outputImage imageByApplyingTransform:CGAffineTransformMakeScale(8, 8)];
                CGImageRef image = [[CIContext context] createCGImage:qr fromRect:qr.extent];
                NSImageView *imageView = [[NSImageView alloc] initWithFrame:NSMakeRect(40, 80, 440, 440)];
                imageView.image = [[NSImage alloc] initWithCGImage:image size:NSZeroSize];
                CGImageRelease(image);
                [fixtureWindow.contentView addSubview:imageView];
                NSTextField *label = [NSTextField labelWithString:@"Synthetic test account — no real credentials"];
                label.frame = NSMakeRect(40, 20, 440, 40);
                [fixtureWindow.contentView addSubview:label];
                [fixtureWindow makeKeyAndOrderFront:nil];
            }
            tf_camera_start(1, screen, CameraResult, NULL);
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, 15 * NSEC_PER_SEC), dispatch_get_main_queue(), ^{
                printf("Camera permission=%ld devices=%lu running=%d framesReceived=%d\n",
                    (long)[AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo],
                    (unsigned long)activeScanner.devices.count, activeScanner.session.isRunning,
                    activeScanner.lastFrame > 0);
                fflush(stdout);
            });
            [NSApp run];
            return 0;
        }
        int biometryStatus = tf_biometry_status();
        printf("Touch ID capability status: %d\n", biometryStatus);
        NSString *account = [@"integration-test:" stringByAppendingString:NSUUID.UUID.UUIDString];
        const char *key = account.UTF8String;
        const unsigned char *secret = (const unsigned char *)"synthetic-test-secret";
        int result = tf_keychain_write(NULL, key, secret, 21, 0);
        printf("Keychain write status: %d\n", result);
        Require(result == 0, "Data Protection Keychain write with app entitlements");
        unsigned char *bytes = NULL; int length = 0;
        Require(tf_keychain_read(NULL, key, &bytes, &length) == 0 && length == 21 && memcmp(bytes, secret, length) == 0, "Keychain exact round trip");
        tf_secret_free(bytes, length);
        Require(tf_keychain_delete(key) == 0 && tf_keychain_delete(key) == 0, "Keychain deletion is idempotent");
        Require(tf_keychain_read(NULL, key, &bytes, &length) == errSecItemNotFound && bytes == NULL && length == 0, "Missing credential never yields a secret");
        Require(tf_keychain_write(NULL, key, secret, 0, 0) == errSecParam, "Empty secret rejected");
        void *context = tf_auth_create();
        tf_auth_cancel(context);
        Require(tf_auth_scan(context) != 0, "Invalidated authentication context cannot succeed");
        tf_auth_release(context);
        if (biometryStatus == LAErrorBiometryNotEnrolled) {
            context = tf_auth_create();
            Require(tf_keychain_write(context, key, secret, 21, 1) == LAErrorBiometryNotEnrolled, "Unenrolled Touch ID refuses protected credential enrollment");
            tf_auth_release(context);
            Require(tf_keychain_read(NULL, key, &bytes, &length) == errSecItemNotFound && bytes == NULL, "Failed biometric enrollment leaves no stored password");
        }
        if (interactive) {
            context = tf_auth_create();
            result = tf_keychain_write(context, key, secret, 21, 1);
            tf_auth_release(context);
            Require(result == 0, "Touch ID enrollment stores a protected test credential");
            Require(tf_keychain_read(NULL, key, &bytes, &length) != 0 && bytes == NULL, "Protected credential refuses non-interactive access");
            context = tf_auth_create();
            result = tf_keychain_read(context, key, &bytes, &length);
            tf_auth_release(context);
            BOOL passed = result == 0 && length == 21 && memcmp(bytes, secret, length) == 0;
            tf_secret_free(bytes, length);
            tf_keychain_delete(key);
            Require(passed, "Touch ID unlock returns the protected test secret");
        }
        // A public, synthetic QR exercises Apple's decoder; no real OTP secrets are used.
        NSString *payload = @"otpauth://totp/2fast:IntegrationTest?secret=JBSWY3DPEHPK3PXP&issuer=2fast";
        CIFilter *filter = [CIFilter filterWithName:@"CIQRCodeGenerator"];
        [filter setValue:[payload dataUsingEncoding:NSUTF8StringEncoding] forKey:@"inputMessage"];
        CIImage *qr = [filter.outputImage imageByApplyingTransform:CGAffineTransformMakeScale(8, 8)];
        CGImageRef image = [[CIContext context] createCGImage:qr fromRect:qr.extent];
        VNImageRequestHandler *handler = [[VNImageRequestHandler alloc] initWithCGImage:image options:@{}];
        VNDetectBarcodesRequest *request = [VNDetectBarcodesRequest new];
        request.symbologies = @[VNBarcodeSymbologyQR];
        Require([handler performRequests:@[request] error:nil] && [request.results.firstObject.payloadStringValue isEqualToString:payload], "Vision decodes a synthetic TOTP QR");
        CGImageRelease(image);
        TestStaticScreenFrame(@"otpauth://ocra/Test?secret=JBSWY3DPEHPK3PXP&suite=OCRA-1:HOTP-SHA1-6:QN08-T1M");
        TestStaticScreenFrame(@"mobileid://www.deepnetsecurity.com/mobileid/install?sn=123&seed=synthetic&suite=OCRA-1:HOTP-SHA1-6:QN08-T1M");
        puts("Native checks complete.");
        return 0;
    }
}
