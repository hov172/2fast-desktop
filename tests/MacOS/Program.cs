using Project2FA.Services.MacOS;

if (args.Contains("--inspect-private-qr"))
{
    string payload = Console.In.ReadToEnd();
    bool valid = MacOSOtpParser.TryParse(payload, out var imported, out var error);
    Console.WriteLine("Apple Vision decoded QR: " + (payload.Length > 0));
    if (Uri.TryCreate(payload, UriKind.Absolute, out var parsedUri))
    {
        Console.WriteLine("QR scheme: " + (parsedUri.Scheme == "mobileid" ? "MobileID" : parsedUri.Scheme == "otpauth" ? "otpauth" : "other"));
        Console.WriteLine("Includes device registration: " + (parsedUri.Query.Contains("regurl=") || parsedUri.Query.Contains("sid=")));
    }
    Console.WriteLine("Import valid: " + valid);
    if (!valid) Console.WriteLine("Validation result: " + error);
    else Console.WriteLine("Account mode: " + (imported.Any(p => p.Key == "mobileid") ? "Deepnet MobileID OTP + OCRA" : "TOTP/OCRA"));
    imported.Clear();
    return;
}
int count = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    count++;
Console.WriteLine("PASS: " + name);
}
const string prefix = "otpauth://totp/2fast:Test?secret=JBSWY3DPEHPK3PXP&issuer=2fast";
Check(MacOSOtpParser.TryParse(prefix, out var pairs), "standard TOTP URI");
var values = pairs.ToDictionary(x => x.Key, x => x.Value);
Check(values["label"] == "2fast" && values["issuer"] == "Test" && values["digits"] == "6" && values["period"] == "30", "account model mapping and defaults");
Check(MacOSOtpParser.TryParse(prefix + "&algorithm=SHA512&digits=8&period=60", out _), "supported custom TOTP settings");
Check(MacOSOtpParser.TryParse("otpauth://totp/A%26B:a%2Bb%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=A%26B", out pairs)
    && pairs.Single(x => x.Key == "label").Value == "A&B" && pairs.Single(x => x.Key == "issuer").Value == "a+b@example.com", "escaped labels do not become query parameters");
foreach (var text in new[]
{
    "https://example.com/", "otpauth://hotp/Test?secret=JBSWY3DPEHPK3PXP", "junk " + prefix,
    prefix + "&secret=AAAAAAAAAAAAAAAA", prefix + "&digits=0", prefix + "&digits=9999999999999999999",
    prefix + "&period=0", prefix + "&period=-1", prefix + "&period=9999999999", prefix + "&algorithm=MD5",
    prefix.Replace("JBSWY3DPEHPK3PXP", "INVALID01"), prefix.Replace("issuer=2fast", "issuer=other"),
    prefix + "#fragment", prefix.Replace("/2fast:Test", "/%0A"), new string('A', 16385),
    "otpauth://user@totp/Test?secret=JBSWY3DPEHPK3PXP"
}) Check(!MacOSOtpParser.TryParse(text, out _), "malformed/unsupported payload rejected " + count);
    // Synthetic MobileID envelope; expected key/OTP computed independently using Python hashlib/hmac.
string mobile = "mobileid://www.deepnetsecurity.com/mobileid/install?sn=123456789&seed=c4APXbSD1NJVmdFIEsbQSfZ/&suite=OCRA-1:HOTP-SHA1-6:QN08-T1M&v=1&tn=Synthetic%20Test&ac=24681357";
Check(MacOSOtpParser.TryParse(mobile, out pairs), "encrypted MobileID QR import");
var mobileValues = pairs.ToDictionary(x => x.Key, x => x.Value);
Check(mobileValues["secret"] == "AGZQPLF2J5KPKWVPYM53A25363FIAPU2" && mobileValues["mobileid"] == "plain" && mobileValues["period"] == "60", "MobileID derives the key and profile correctly");
Check(MacOSMobileId.ComputeOtp(OtpNet.Base32Encoding.ToBytes(mobileValues["secret"]), 60, 6, false, DateTimeOffset.FromUnixTimeSeconds(1200000000)) == "473642", "MobileID fixed-offset OTP fixture");
Check(!MacOSOtpParser.TryParse(mobile.Replace("&ac=24681357", ""), out _, out var importError) && importError == "Authorization code required", "missing authorization code requests input");
Check(MacOSOtpParser.TryParse(mobile.Replace("&ac=24681357", ""), out _, out _, "24681357"), "separate authorization code imports");
Check(!MacOSOtpParser.TryParse(mobile, out _, out _, "incorrect"), "wrong authorization code rejected");
Check(MacOSOtpParser.TryParse(mobile + "&regurl=https%3A%2F%2Fexample.com", out pairs) && pairs.Any(p => p.Key == "importnotice") && !pairs.Any(p => p.Key == "regurl"), "offline import discards push registration and labels the limitation");
Check(!MacOSOtpParser.TryParse(mobile + "&sn=999", out _), "duplicate MobileID serial rejected");
Check(!MacOSOtpParser.TryParse(mobile.Replace("sn=123456789", "sn=123456788"), out _), "serial checksum rejected");
string ocra = "otpauth://ocra/Test?secret=JBSWY3DPEHPK3PXP&suite=OCRA-1:HOTP-SHA1-6:QN08-T1M";
Check(MacOSOtpParser.TryParse(ocra, out pairs) && pairs.Any(x => x.Key == "ocrasuite"), "OCRA QR profile preserved");
Check(!MacOSOtpParser.TryParse(ocra + "&period=30", out _), "inconsistent OCRA timing rejected");
Check(MacOSOtpParser.TryParse("otpauth://totp/Standalone?secret=JBSWY3DPEHPK3PXP", out pairs) && pairs.Single(x => x.Key == "label").Value == "Standalone", "QR without issuer has a usable account label");
Console.WriteLine($"{count} parser checks passed.");
