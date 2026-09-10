#if TWOFAST_WINDOWS
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
namespace Project2FA.Services.Desktop;
internal static class WindowsCameras
{
    [ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface DeviceEnumerator
    {
        [PreserveSig] int CreateClassEnumerator([In] ref Guid category, out IEnumMoniker monikers, int flags);
    }
    [ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface PropertyBag
    {
        [PreserveSig] int Read([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.Struct)] out object value, IntPtr errorLog);
        [PreserveSig] int Write([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.Struct)] ref object value);
    }
    internal static List<WindowsCaptureSource> Sources()
    {
        var result = new List<WindowsCaptureSource>();
        object enumerator = null; IEnumMoniker monikers = null;
        try
        {
            enumerator = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("62BE5D10-60EB-11D0-BD3B-00A0C911CE86"))!);
            var category = new Guid("860BB310-5D01-11D0-BD3B-00A0C911CE86");
            if (((DeviceEnumerator)enumerator).CreateClassEnumerator(ref category, out monikers, 0) != 0 || monikers == null) return result;
            var item = new IMoniker[1]; int index = 0;
            while (monikers.Next(1, item, IntPtr.Zero) == 0)
            {
                object storage = null;
                try
                {
                    var iid = typeof(PropertyBag).GUID;
                    item[0].BindToStorage(null, null, ref iid, out storage);
                    string name = ((PropertyBag)storage).Read("FriendlyName", out var value, IntPtr.Zero) == 0 ? value?.ToString() : null;
                    result.Add(new("Camera: " + (name ?? (index + 1).ToString()), IntPtr.Zero, 0, 0, 0, 0, index));
                }
                finally { if (storage != null) Marshal.ReleaseComObject(storage); Marshal.ReleaseComObject(item[0]); index++; }
            }
        }
        catch { /* Screen capture remains available without a camera or camera enumeration support. */ }
        finally { if (monikers != null) Marshal.ReleaseComObject(monikers); if (enumerator != null) Marshal.ReleaseComObject(enumerator); }
        return result;
    }
}
#endif
