#import <AppKit/AppKit.h>
#import <AVFoundation/AVFoundation.h>
#import <LocalAuthentication/LocalAuthentication.h>
#import <Security/Security.h>
#import <Vision/Vision.h>
#import <ScreenCaptureKit/ScreenCaptureKit.h>
#import <CoreImage/CoreImage.h>

// C ABI: no Objective-C objects or retained buffers cross into managed code.
// Keychain calls run on a managed worker thread; all scanner UI runs on main.
#define EXPORT __attribute__((visibility("default")))
static NSString *const Service = @"jpweber.it.Project2FA.Uno.macOS.v1";
EXPORT void *tf_auth_create(void) { return (__bridge_retained void *)[LAContext new]; }
EXPORT void tf_auth_cancel(void *p) { [(__bridge LAContext *)p invalidate]; }
EXPORT void tf_auth_release(void *p) { if (p) CFRelease(p); }
EXPORT int tf_biometry_status(void) {
    @autoreleasepool {
        LAContext *context = [LAContext new];
        NSError *error = nil;
        BOOL enabled = [context canEvaluatePolicy:LAPolicyDeviceOwnerAuthenticationWithBiometrics error:&error];
        return enabled && context.biometryType == LABiometryTypeTouchID ? 0 : (int)(error.code ?: LAErrorBiometryNotAvailable);
    }
}
static int Authenticate(LAContext *context) {
    context.localizedFallbackTitle = @"";
    context.touchIDAuthenticationAllowableReuseDuration = 0;
    NSError *error = nil;
    if (![context canEvaluatePolicy:LAPolicyDeviceOwnerAuthenticationWithBiometrics error:&error]) return (int)error.code;
    dispatch_semaphore_t done = dispatch_semaphore_create(0);
    __block int result = LAErrorAuthenticationFailed;
    [context evaluatePolicy:LAPolicyDeviceOwnerAuthenticationWithBiometrics
           localizedReason:@"unlock your 2fast data file"
                     reply:^(BOOL success, NSError *failure) {
        result = success ? 0 : (int)(failure.code ?: LAErrorAuthenticationFailed);
        dispatch_semaphore_signal(done);
    }];
    dispatch_semaphore_wait(done, DISPATCH_TIME_FOREVER);
    return result;
}
EXPORT int tf_auth_scan(void *p) { @autoreleasepool { return Authenticate((__bridge LAContext *)p); } }
static NSMutableDictionary *Query(const char *key) {
    if (!key) return nil;
    NSString *account = [[NSString alloc] initWithUTF8String:key];
    if (!account.length || account.length > 512) return nil;
    return [@{(__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
              (__bridge id)kSecAttrService: Service,
              (__bridge id)kSecAttrAccount: account,
              (__bridge id)kSecUseDataProtectionKeychain: @YES,
              (__bridge id)kSecAttrSynchronizable: @NO} mutableCopy];
}
EXPORT int tf_keychain_write(void *p, const char *key, const unsigned char *bytes, int length, int biometric) {
    @autoreleasepool {
        NSMutableDictionary *query = Query(key);
        if (!query || !bytes || length < 1) return errSecParam;
        LAContext *context = (__bridge LAContext *)p;
        if (biometric) {
            int result = Authenticate(context);
            if (result) return result;
        }
        // NSData owns a mutable copy that is wiped after SecItemAdd/Update completes.
        NSMutableData *data = [NSMutableData dataWithBytes:bytes length:length];
        OSStatus result;
        if (biometric) {
            CFErrorRef error = NULL;
            SecAccessControlRef access = SecAccessControlCreateWithFlags(NULL,
                kSecAttrAccessibleWhenUnlockedThisDeviceOnly, kSecAccessControlBiometryCurrentSet, &error);
            if (!access) { if (error) CFRelease(error); [data resetBytesInRange:NSMakeRange(0, data.length)]; return errSecParam; }
            // Re-enrollment follows a fresh biometric check. Never weaken the ACL.
            result = SecItemDelete((__bridge CFDictionaryRef)query);
            if (result == errSecSuccess || result == errSecItemNotFound) {
                query[(__bridge id)kSecAttrAccessControl] = (__bridge id)access;
                query[(__bridge id)kSecUseAuthenticationContext] = context;
                query[(__bridge id)kSecValueData] = data;
                result = SecItemAdd((__bridge CFDictionaryRef)query, NULL);
            }
            CFRelease(access);
        } else {
            result = SecItemUpdate((__bridge CFDictionaryRef)query,
                (__bridge CFDictionaryRef)@{(__bridge id)kSecValueData:data});
            if (result == errSecItemNotFound) {
                query[(__bridge id)kSecAttrAccessible] = (__bridge id)kSecAttrAccessibleWhenUnlockedThisDeviceOnly;
                query[(__bridge id)kSecValueData] = data;
                result = SecItemAdd((__bridge CFDictionaryRef)query, NULL);
            }
        }
        [data resetBytesInRange:NSMakeRange(0, data.length)];
        return result;
    }
}
EXPORT int tf_keychain_read(void *p, const char *key, unsigned char **bytes, int *length) {
    @autoreleasepool {
        *bytes = NULL; *length = 0;
        NSMutableDictionary *query = Query(key);
        if (!query) return errSecParam;
        query[(__bridge id)kSecReturnData] = @YES;
        query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;
        if (p) {
            LAContext *context = (__bridge LAContext *)p;
            context.localizedReason = @"unlock your 2fast data file";
            context.localizedFallbackTitle = @"";
            context.touchIDAuthenticationAllowableReuseDuration = 0;
            query[(__bridge id)kSecUseAuthenticationContext] = context;
        } else {
            // Non-interactive reads must never trigger a biometric prompt.
            LAContext *context = [LAContext new];
            context.interactionNotAllowed = YES;
            query[(__bridge id)kSecUseAuthenticationContext] = context;
        }
        CFTypeRef value = NULL;
        OSStatus result = SecItemCopyMatching((__bridge CFDictionaryRef)query, &value);
        if (result == errSecSuccess) {
            NSData *data = CFBridgingRelease(value);
            if (data.length > INT_MAX) return errSecParam;
            *bytes = malloc(data.length);
            if (!*bytes) return errSecAllocate;
            memcpy(*bytes, data.bytes, data.length);
            *length = (int)data.length;
        }
        return result;
    }
}
EXPORT int tf_keychain_delete(const char *key) {
    @autoreleasepool {
        NSMutableDictionary *query = Query(key);
        if (!query) return errSecParam;
        OSStatus result = SecItemDelete((__bridge CFDictionaryRef)query);
        return result == errSecItemNotFound ? 0 : result;
    }
}
EXPORT void tf_secret_free(void *bytes, int length) {
    if (bytes) { if (length > 0) memset_s(bytes, length, 0, length); free(bytes); }
}

typedef void (*ScanCallback)(int status, const char *payload, void *state);
@interface TFScanner : NSWindowController <NSWindowDelegate, AVCaptureVideoDataOutputSampleBufferDelegate, SCContentSharingPickerObserver, SCStreamOutput, SCStreamDelegate>
@property(nonatomic, strong) AVCaptureSession *session;
@property(nonatomic, strong) dispatch_queue_t sessionQueue;
@property(nonatomic, strong) dispatch_queue_t frameQueue;
@property(nonatomic, strong) NSArray<AVCaptureDevice *> *devices;
@property(nonatomic, strong) NSPopUpButton *selector;
@property(nonatomic, strong) NSTextField *hint;
@property(nonatomic, strong) AVCaptureVideoPreviewLayer *preview;
@property(nonatomic, strong) VNDetectBarcodesRequest *request;
@property(atomic) BOOL finished;
@property ScanCallback callback;
@property void *state;
@property uint64_t identifier;
@property CFTimeInterval lastFrame;
@property(nonatomic, strong) id runtimeObserver;
@property(nonatomic, strong) id disconnectObserver;
@property(nonatomic, strong) id resignObserver;
@property(atomic) BOOL screenMode;
@property(nonatomic, strong) SCStream *screenStream;
@property(nonatomic, strong) CALayer *screenPreview;
@property(nonatomic, strong) CIContext *imageContext;
@property(nonatomic, strong) NSButton *screenButton;
@property BOOL observingPicker;
@property BOOL screenStarting;
@property BOOL stopping;
@property(atomic) BOOL receivedScreenFrame;
@property(nonatomic, strong) NSTimer *frameWatchdog;
@property int finishStatus;
@property(nonatomic, copy) NSString *finishPayload;
- (void)begin;
- (void)finish:(int)status payload:(NSString *)payload;
@end
static TFScanner *activeScanner;
@implementation TFScanner
- (void)begin {
    NSWindow *window = [[NSWindow alloc] initWithContentRect:NSMakeRect(0, 0, 720, 560)
        styleMask:NSWindowStyleMaskTitled | NSWindowStyleMaskClosable
        backing:NSBackingStoreBuffered defer:NO];
    window.title = @"Scan an authenticator QR code";
    window.releasedWhenClosed = NO;
    window.delegate = self;
    self.window = window;
    self.session = [AVCaptureSession new];
    self.sessionQueue = dispatch_queue_create("2fast.camera.session", DISPATCH_QUEUE_SERIAL);
    self.frameQueue = dispatch_queue_create("2fast.camera.frames", DISPATCH_QUEUE_SERIAL);
    self.request = [VNDetectBarcodesRequest new];
    self.request.symbologies = @[VNBarcodeSymbologyQR];
    self.hint = [NSTextField wrappingLabelWithString:@"Allow camera access to scan. No images are saved or sent anywhere."];
    self.hint.frame = NSMakeRect(20, 8, 680, 44);
    [window.contentView addSubview:self.hint];
    NSButton *cancel = [NSButton buttonWithTitle:@"Cancel" target:self action:@selector(cancel:)];
    cancel.frame = NSMakeRect(610, 516, 90, 30);
    cancel.keyEquivalent = @"\e";
    [window.contentView addSubview:cancel];
    self.selector = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(20, 516, 365, 30) pullsDown:NO];
    self.selector.target = self;
    self.selector.action = @selector(changeCamera:);
    [window.contentView addSubview:self.selector];
    self.screenButton = [NSButton buttonWithTitle:@"Scan from screen…" target:self action:@selector(chooseScreen:)];
    self.screenButton.frame = NSMakeRect(395, 516, 205, 30);
    [window.contentView addSubview:self.screenButton];
    NSView *view = [[NSView alloc] initWithFrame:NSMakeRect(20, 58, 680, 448)];
    view.wantsLayer = YES;
    self.preview = [AVCaptureVideoPreviewLayer layerWithSession:self.session];
    self.preview.videoGravity = AVLayerVideoGravityResizeAspect;
    self.preview.frame = view.bounds;
    [view.layer addSublayer:self.preview];
    self.screenPreview = [CALayer layer];
    self.screenPreview.frame = view.bounds;
    self.screenPreview.contentsGravity = kCAGravityResizeAspect;
    self.screenPreview.hidden = YES;
    [view.layer addSublayer:self.screenPreview];
    [window.contentView addSubview:view];
    [window center];
    [window makeKeyAndOrderFront:nil];
    __weak TFScanner *weakSelf = self;
    self.runtimeObserver = [[NSNotificationCenter defaultCenter] addObserverForName:AVCaptureSessionRuntimeErrorNotification
        object:self.session queue:NSOperationQueue.mainQueue usingBlock:^(NSNotification *n) { if (!weakSelf.screenMode) [weakSelf finish:5 payload:nil]; }];
    self.disconnectObserver = [[NSNotificationCenter defaultCenter] addObserverForName:AVCaptureDeviceWasDisconnectedNotification
        object:nil queue:NSOperationQueue.mainQueue usingBlock:^(NSNotification *n) {
        TFScanner *scanner = weakSelf;
        if (!scanner || scanner.screenMode) return;
        NSInteger index = scanner.selector.indexOfSelectedItem;
        if (index >= 0 && index < (NSInteger)scanner.devices.count && n.object == scanner.devices[index]) [scanner finish:4 payload:nil];
    }];
    if (self.screenMode) { [self chooseScreen:nil]; return; }
    AVAuthorizationStatus status = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
    if (status == AVAuthorizationStatusNotDetermined) {
        [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted) {
            dispatch_async(dispatch_get_main_queue(), ^{
                if (weakSelf.finished || weakSelf.screenMode) return;
                if (granted) [weakSelf configure];
                else { weakSelf.hint.stringValue = @"Camera access is disabled. Enable it in System Settings, or choose Scan from screen."; weakSelf.selector.enabled = NO; }
            });
        }];
    } else if (status == AVAuthorizationStatusAuthorized) [self configure];
    else { self.hint.stringValue = @"Camera access is disabled. Enable it in System Settings, or choose Scan from screen."; self.selector.enabled = NO; }
}
- (void)configure {
    if (self.finished) return;
    // Install only after the system permission prompt has finished.
    __weak TFScanner *weakSelf = self;
    self.resignObserver = [[NSNotificationCenter defaultCenter] addObserverForName:NSApplicationDidResignActiveNotification
        object:nil queue:NSOperationQueue.mainQueue usingBlock:^(NSNotification *n) { [weakSelf finish:1 payload:nil]; }];
    self.devices = [AVCaptureDeviceDiscoverySession discoverySessionWithDeviceTypes:
        @[AVCaptureDeviceTypeBuiltInWideAngleCamera, AVCaptureDeviceTypeExternal, AVCaptureDeviceTypeContinuityCamera]
        mediaType:AVMediaTypeVideo position:AVCaptureDevicePositionUnspecified].devices;
    for (AVCaptureDevice *device in self.devices) [self.selector addItemWithTitle:device.localizedName];
    if (!self.devices.count) { self.hint.stringValue = @"No camera found. Connect a camera, or choose Scan from screen."; self.selector.enabled = NO; return; }
    [self.selector selectItemAtIndex:0];
    [self changeCamera:nil];
}
- (void)changeCamera:(id)sender {
    NSInteger index = self.selector.indexOfSelectedItem;
    if (self.finished || index < 0 || index >= (NSInteger)self.devices.count) return;
    AVCaptureDevice *device = self.devices[index];
    self.hint.stringValue = @"Point the camera at a TOTP, OCRA, or Deepnet MobileID setup QR code. You can review the account before saving.";
    dispatch_async(self.sessionQueue, ^{
        if (self.finished) return;
        [self.session stopRunning];
        [self.session beginConfiguration];
        for (AVCaptureInput *input in self.session.inputs) [self.session removeInput:input];
        for (AVCaptureOutput *output in self.session.outputs) [self.session removeOutput:output];
        NSError *error = nil;
        AVCaptureDeviceInput *input = [AVCaptureDeviceInput deviceInputWithDevice:device error:&error];
        AVCaptureVideoDataOutput *output = [AVCaptureVideoDataOutput new];
        output.alwaysDiscardsLateVideoFrames = YES;
        [output setSampleBufferDelegate:self queue:self.frameQueue];
        if (!input || ![self.session canAddInput:input]) {
            [self.session commitConfiguration];
            dispatch_async(dispatch_get_main_queue(), ^{ [self finish:4 payload:nil]; }); return;
        }
        [self.session addInput:input];
        if (![self.session canAddOutput:output]) {
            [self.session commitConfiguration];
            dispatch_async(dispatch_get_main_queue(), ^{ [self finish:5 payload:nil]; }); return;
        }
        [self.session addOutput:output];
        if ([self.session canSetSessionPreset:AVCaptureSessionPreset1280x720]) self.session.sessionPreset = AVCaptureSessionPreset1280x720;
        [self.session commitConfiguration];
        if (!self.finished) [self.session startRunning];
    });
}
- (void)chooseScreen:(id)sender {
    if (self.finished) return;
    self.screenMode = YES;
    if (self.resignObserver) { [[NSNotificationCenter defaultCenter] removeObserver:self.resignObserver]; self.resignObserver = nil; }
    dispatch_async(self.sessionQueue, ^{ [self.session stopRunning]; });
    self.selector.hidden = YES;
    self.preview.hidden = YES;
    self.screenPreview.hidden = NO;
    self.window.title = @"Scan QR from a window or screen";
    self.screenButton.title = @"Choose window or screen…";
    self.hint.stringValue = @"Select the browser window with your setup QR code. Sharing stops after a scan or when you cancel. Nothing is recorded.";
    self.imageContext = [CIContext context];
    SCContentSharingPicker *picker = SCContentSharingPicker.sharedPicker;
    if (!self.observingPicker) { [picker addObserver:self]; self.observingPicker = YES; }
    SCContentSharingPickerConfiguration *configuration = [SCContentSharingPickerConfiguration new];
    configuration.allowedPickerModes = SCContentSharingPickerModeSingleWindow | SCContentSharingPickerModeSingleDisplay;
    configuration.excludedWindowIDs = @[@(self.window.windowNumber)];
    configuration.allowsChangingSelectedContent = YES;
    picker.defaultConfiguration = configuration;
    picker.maximumStreamCount = @1;
    picker.active = YES;
    if (self.screenStream) [picker presentPickerForStream:self.screenStream];
    else [picker present];
}
- (void)contentSharingPicker:(SCContentSharingPicker *)picker didCancelForStream:(SCStream *)stream {
    dispatch_async(dispatch_get_main_queue(), ^{ if (!self.screenStream) [self finish:1 payload:nil]; });
}
- (void)contentSharingPickerStartDidFailWithError:(NSError *)error {
    dispatch_async(dispatch_get_main_queue(), ^{ [self finish:7 payload:nil]; });
}
- (void)contentSharingPicker:(SCContentSharingPicker *)picker didUpdateWithFilter:(SCContentFilter *)filter forStream:(SCStream *)stream {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (self.finished || (stream && stream != self.screenStream)) return;
        [self.window makeKeyAndOrderFront:nil];
        self.receivedScreenFrame = NO;
        self.screenPreview.contents = nil;
        SCStreamConfiguration *configuration = [SCStreamConfiguration new];
        double pixelScale = MAX(1, filter.pointPixelScale);
        double width = filter.contentRect.size.width * pixelScale;
        double height = filter.contentRect.size.height * pixelScale;
        if (width < 2 || height < 2) { [self finish:7 payload:nil]; return; }
        double scale = MIN(1, 4096.0 / MAX(width, height));
        configuration.width = MAX(1, (NSUInteger)(width * scale));
        configuration.height = MAX(1, (NSUInteger)(height * scale));
        configuration.minimumFrameInterval = CMTimeMake(1, 8);
        configuration.queueDepth = 3;
        configuration.showsCursor = NO;
        configuration.capturesAudio = NO;
        configuration.pixelFormat = kCVPixelFormatType_32BGRA;
        if (self.screenStream) {
            [self.screenStream updateConfiguration:configuration completionHandler:^(NSError *error) {
                if (error) dispatch_async(dispatch_get_main_queue(), ^{ [self finish:7 payload:nil]; });
            }];
            [self.screenStream updateContentFilter:filter completionHandler:^(NSError *error) {
                if (error) dispatch_async(dispatch_get_main_queue(), ^{ [self finish:7 payload:nil]; });
            }];
            return;
        }
        self.screenStream = [[SCStream alloc] initWithFilter:filter configuration:configuration delegate:self];
        NSError *error = nil;
        if (![self.screenStream addStreamOutput:self type:SCStreamOutputTypeScreen sampleHandlerQueue:self.frameQueue error:&error]) {
            [self finish:7 payload:nil]; return;
        }
        self.screenStarting = YES;
        __weak TFScanner *weakScanner = self;
        self.frameWatchdog = [NSTimer scheduledTimerWithTimeInterval:8 repeats:YES block:^(NSTimer *timer) {
            TFScanner *scanner = weakScanner;
            if (!scanner || scanner.finished) { [timer invalidate]; return; }
            if (!scanner.receivedScreenFrame) scanner.hint.stringValue = @"No screen frames received. Choose the window again, keep it open (not minimized), or select its display.";
        }];
        [self.screenStream startCaptureWithCompletionHandler:^(NSError *error) {
            dispatch_async(dispatch_get_main_queue(), ^{
                self.screenStarting = NO;
                if (self.finished) [self stopScreenAndFinish];
                else if (error) [self finish:7 payload:nil];
                else if (!self.finished) self.hint.stringValue = @"Scanning the selected content. Keep the QR code visible. Close this window or use macOS Stop Sharing to cancel.";
            });
        }];
    });
}
- (void)stream:(SCStream *)stream didStopWithError:(NSError *)error {
    dispatch_async(dispatch_get_main_queue(), ^{ [self finish:1 payload:nil]; });
}
- (void)stream:(SCStream *)stream didOutputSampleBuffer:(CMSampleBufferRef)buffer ofType:(SCStreamOutputType)type {
    if (type != SCStreamOutputTypeScreen || self.finished || !CMSampleBufferIsValid(buffer)) return;
    NSArray *attachments = (__bridge NSArray *)CMSampleBufferGetSampleAttachmentsArray(buffer, false);
    NSNumber *status = attachments.firstObject[SCStreamFrameInfoStatus];
    if (!status || status.integerValue != SCFrameStatusComplete || !CMSampleBufferGetImageBuffer(buffer)) return;
    [self processSample:buffer];
}
- (void)captureOutput:(AVCaptureOutput *)output didOutputSampleBuffer:(CMSampleBufferRef)buffer fromConnection:(AVCaptureConnection *)connection {
    if (!self.screenMode) [self processSample:buffer];
}
- (void)processSample:(CMSampleBufferRef)buffer {
    @autoreleasepool {
        BOOL fromScreen = self.screenMode;
        CFTimeInterval now = CACurrentMediaTime();
        if (self.finished || (!fromScreen && now - self.lastFrame < 0.15)) return;
        self.lastFrame = now;
        if (self.screenMode) {
            CVPixelBufferRef pixels = CMSampleBufferGetImageBuffer(buffer);
            if (!pixels) return;
            self.receivedScreenFrame = YES;
            CIImage *image = [CIImage imageWithCVPixelBuffer:pixels];
            // The preview exists only in memory and is dropped as soon as capture ends.
            id preview = CFBridgingRelease([self.imageContext createCGImage:image fromRect:image.extent]);
            dispatch_async(dispatch_get_main_queue(), ^{ if (!self.finished) self.screenPreview.contents = preview; });
        }
        VNImageRequestHandler *handler = [[VNImageRequestHandler alloc] initWithCMSampleBuffer:buffer options:@{}];
        NSError *error = nil;
        if (![handler performRequests:@[self.request] error:&error]) return;
        for (VNBarcodeObservation *code in self.request.results) {
            NSString *payload = code.payloadStringValue;
            if (!payload || payload.length > 16384) continue;
            // Decode here; validate the complete URI in the managed parser. Do not
            // discard OCRA/MobileID payloads before the importer can inspect them.
            if (payload.length > 0) {
                dispatch_async(dispatch_get_main_queue(), ^{ if (self.screenMode == fromScreen) [self finish:0 payload:payload]; });
                return;
            }
            dispatch_async(dispatch_get_main_queue(), ^{
                if (!self.finished) self.hint.stringValue = @"The QR code has no readable text. Try enlarging it.";
            });
        }
    }
}
- (void)cancel:(id)sender { [self finish:1 payload:nil]; }
- (BOOL)windowShouldClose:(NSWindow *)sender { [self finish:1 payload:nil]; return NO; }
- (void)finish:(int)status payload:(NSString *)payload {
    if (self.finished) return;
    self.finished = YES;
    [self.frameWatchdog invalidate]; self.frameWatchdog = nil;
    self.finishStatus = status;
    self.finishPayload = payload;
    if (self.observingPicker) {
        [SCContentSharingPicker.sharedPicker removeObserver:self];
        SCContentSharingPicker.sharedPicker.active = NO;
        self.observingPicker = NO;
    }
    for (id observer in @[self.runtimeObserver ?: NSNull.null, self.disconnectObserver ?: NSNull.null, self.resignObserver ?: NSNull.null])
        if (observer != NSNull.null) [[NSNotificationCenter defaultCenter] removeObserver:observer];
    self.window.delegate = nil;
    [self.window orderOut:nil];
    if (!self.screenStarting) [self stopScreenAndFinish];
}
- (void)stopScreenAndFinish {
    if (self.stopping) return;
    self.stopping = YES;
    if (self.screenStream) {
        [self.screenStream stopCaptureWithCompletionHandler:^(NSError *error) {
            dispatch_async(dispatch_get_main_queue(), ^{ [self completeFinish:self.finishStatus payload:self.finishPayload]; });
        }];
    } else [self completeFinish:self.finishStatus payload:self.finishPayload];
}
- (void)completeFinish:(int)status payload:(NSString *)payload {
    dispatch_async(self.sessionQueue, ^{
        for (AVCaptureVideoDataOutput *output in self.session.outputs) [output setSampleBufferDelegate:nil queue:NULL];
        [self.session stopRunning];
        dispatch_async(self.frameQueue, ^{
            dispatch_async(dispatch_get_main_queue(), ^{
                self.preview.session = nil;
                self.screenPreview.contents = nil;
                self.screenStream = nil;
                [self.window close];
                if (activeScanner == self) activeScanner = nil;
                if (status == 0) [NSApp activateIgnoringOtherApps:YES];
                self.callback(status, payload.UTF8String, self.state);
            });
        });
    });
}
@end
EXPORT void tf_camera_start(uint64_t identifier, int screen, ScanCallback callback, void *state) {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (activeScanner) { callback(6, NULL, state); return; }
        TFScanner *scanner = [TFScanner new];
        scanner.callback = callback; scanner.state = state; scanner.identifier = identifier;
        scanner.screenMode = screen != 0;
        activeScanner = scanner;
        [scanner begin];
    });
}
EXPORT void tf_camera_cancel(uint64_t identifier) {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (activeScanner && activeScanner.identifier == identifier) [activeScanner finish:1 payload:nil];
    });
}
