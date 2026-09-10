namespace Project2FA.Converters
{
    public partial class FavouriteToIconConverter : BoolToValueConverter
    {
        protected override object TrueValue => "\uE735";
        protected override object FalseValue => "\uE734";
    }
}
