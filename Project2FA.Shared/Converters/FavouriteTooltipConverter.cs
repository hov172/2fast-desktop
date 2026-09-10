namespace Project2FA.Converters
{
    public partial class FavouriteTooltipConverter : BoolToValueConverter
    {
        protected override object TrueValue => Strings.Resources.AccountCodePageTooltipDeleteFavourite;
        protected override object FalseValue => Strings.Resources.AccountCodePageTooltipSetFavourite;
    }
}
