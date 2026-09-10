namespace Project2FA.Converters
{
    public partial class ShowCodeToIconConverter : BoolToValueConverter
    {
        protected override object TrueValue => "\uE5F0";
        protected override object FalseValue => "\uE5F4";
    }
}
