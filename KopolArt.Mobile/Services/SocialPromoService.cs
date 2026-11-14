namespace KopolArt.Mobile.Services
{
    public class SocialPromoService
    {
        /// <summary>
        /// Returns true if the promo popup should be shown (first launch or ≥3 days since last shown).
        /// </summary>
        public bool ShouldShowPromo()
        {
            var lastShown = Preferences.Get(Constants.Keys.SocialPromoLastShown, string.Empty);

            if (string.IsNullOrEmpty(lastShown))
                return true;

            if (DateTime.TryParse(lastShown, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var lastDate))
            {
                return (DateTime.UtcNow - lastDate).TotalDays >= Constants.SocialPromo.IntervalDays;
            }

            return true;
        }

        /// <summary>
        /// Records the current UTC time as the last time the promo was shown.
        /// </summary>
        public void MarkPromoShown()
        {
            Preferences.Set(Constants.Keys.SocialPromoLastShown,
                DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}