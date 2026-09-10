namespace Project2FA.Converters
{
    // Bound to HideTOTPCode: hidden codes offer "show", visible codes offer "hide".
    public partial class TOTPVisibilityTooltipConverter : BoolToValueConverter
    {
        protected override object TrueValue => Strings.Resources.AccountCodePageTooltipShowTOTP;
        protected override object FalseValue => Strings.Resources.AccountCodePageTooltipHideTOTP;
    }
}
