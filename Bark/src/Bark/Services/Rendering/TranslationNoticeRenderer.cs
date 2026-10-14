using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class TranslationNoticeRenderer
{
    public static string Missing(Localization localization, string originalHref) =>
        Build("translation-notice translation-notice--missing", localization.TranslationMissing, localization.TranslationMissingLink, originalHref);

    public static string Machine(Localization localization, string originalHref) =>
        Build("translation-notice translation-notice--machine", localization.TranslationMachine, localization.TranslationMissingLink, originalHref);

    public static string Stale(Localization localization, string originalHref) =>
        Build("translation-notice translation-notice--stale", localization.TranslationStale, localization.TranslationStaleLink, originalHref);

    internal static string Build(string cssClass, string message, string linkText, string href) =>
        $"<div class=\"{cssClass}\" role=\"note\"><span>{LayoutProvider.HtmlEncode(message)}</span> " +
        $"<a href=\"{LayoutProvider.HtmlEncode(href)}\">{LayoutProvider.HtmlEncode(linkText)}</a></div>";
}
