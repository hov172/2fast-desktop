using System.Reflection;
using System.Reflection.Emit;

internal static class GeneratedCardChecks
{
    internal static void Run(Assembly app)
    {
        var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
            .ToDictionary(op => unchecked((ushort)op.Value));
        int completeCards = 0;
        foreach (var type in app.GetTypes().Where(t => t.FullName!.Contains("AccountCodePage") && t.Name.Contains("DatTem")))
        {
            int texts = 0, buttons = 0;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var il = method.GetMethodBody()?.GetILAsByteArray();
                if (il == null) continue;
                for (int i = 0; i < il.Length;)
                {
                    ushort code = il[i++];
                    if (code == 0xfe) code = (ushort)(0xfe00 | il[i++]);
                    var op = opcodes[code];
                    if (op == OpCodes.Newobj)
                    {
                        var constructor = method.Module.ResolveMethod(BitConverter.ToInt32(il, i));
                        if (constructor?.DeclaringType?.FullName == "Microsoft.UI.Xaml.Controls.TextBlock") texts++;
                        if (constructor?.DeclaringType?.FullName is "Microsoft.UI.Xaml.Controls.Button" or "Microsoft.UI.Xaml.Controls.MenuFlyoutItem") buttons++;
                    }
                    i += op.OperandType switch
                    {
                        OperandType.InlineNone => 0,
                        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                        OperandType.InlineVar => 2,
                        OperandType.InlineI8 or OperandType.InlineR => 8,
                        OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, i),
                        _ => 4
                    };
                }
            }
            if (texts >= 5 && buttons >= 5) completeCards++;
        }
        if (completeCards != 2) throw new Exception("Compiled XAML omitted account details: expected two complete card templates, got " + completeCards);
        Console.WriteLine("Generated XAML: both compiled account cards construct their text and action controls.");
    }
}
