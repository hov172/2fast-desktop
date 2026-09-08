using System.Text;
using Project2FA.Services.MacOS;
int passed = 0;
void Check(string actual, string expected) { if (actual != expected) throw new Exception($"OCRA vector mismatch: {actual} != {expected}"); passed++; }
var key20 = Encoding.ASCII.GetBytes("12345678901234567890");
var key64 = Encoding.ASCII.GetBytes("1234567890123456789012345678901234567890123456789012345678901234");
var expected1 = new[] { "237653", "243178", "653583", "740991", "608993", "388898", "816933", "224598", "750600", "294470" };
for (int i = 0; i < 10; i++) Check(MacOSOcra.Compute(key20, new string((char)('0' + i), 8), DateTimeOffset.UnixEpoch, "OCRA-1:HOTP-SHA1-6:QN08"), expected1[i]);
var timestamp = DateTimeOffset.FromUnixTimeSeconds(0x132d0b6L * 60);
Check(MacOSOcra.Compute(key20, "12345678", timestamp), "866866"); // Independent Python HMAC fixture for the requested profile.
var expected2 = new[] { "95209754", "55907591", "22048402", "24218844", "36209546" };
for (int i = 0; i < 5; i++) Check(MacOSOcra.Compute(key64, new string((char)('0' + i), 8), timestamp, "OCRA-1:HOTP-SHA512-8:QN08-T1M"), expected2[i]);
foreach (var invalid in new[] { "", "123456789", "abcdefgh", "１２３", "-1", "1 2" })
{
    try { MacOSOcra.Compute(key20, invalid, timestamp); throw new Exception("Invalid challenge accepted"); }
    catch (ArgumentException) { passed++; }
}
Check(MacOSOcra.Compute(key20, "00000001", timestamp), MacOSOcra.Compute(key20, "1", timestamp));
Check(MacOSOcra.Compute(key20, "12345678", timestamp), MacOSOcra.Compute(key20, "12345678", timestamp.AddSeconds(59)));
if (MacOSOcra.Compute(key20, "12345678", timestamp) == MacOSOcra.Compute(key20, "12345678", timestamp.AddSeconds(60))) throw new Exception("Minute rollover ignored");
passed++;
Console.WriteLine($"OCRA: {passed} checks passed, including RFC 6287 vectors.");
