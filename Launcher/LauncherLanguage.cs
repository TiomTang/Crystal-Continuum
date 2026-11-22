using System.Globalization;
using System.Resources;

namespace Launcher;

/// <summary>
/// Selects the launcher UI culture used by the compiled resource table. The
/// launcher ships as its own artifact, so its translations are embedded in
/// <c>Launcher.exe</c> (English) and its satellite assemblies (for example
/// <c>zh-Hans/Launcher.resources.dll</c>) next to <c>Launcher.exe</c>; it never
/// reads the client's language store or the shared game localization.
/// </summary>
/// <remarks>
/// A culture is selected from, in order: explicit names in the operator file,
/// the launcher preferences, then <see cref="CultureInfo.CurrentUICulture"/>.
/// A name may be a culture (<c>zh-CN</c>), a neutral language (<c>zh</c>) or a
/// language name (<c>Chinese</c>). Lookups fall back through the normal
/// <see cref="ResourceManager"/> parent chain and finally to the English
/// resources embedded in the main assembly. Unknown names, missing satellite
/// assemblies and unreadable resources never block startup.
/// </remarks>
internal static class LauncherLanguage
{
    /// <summary>Neutral resources are English, so this language needs no satellite.</summary>
    public const string SourceLanguage = "English";

    private static readonly object sync = new();
    private static CultureInfo selectedCulture = CultureInfo.InvariantCulture;
    private static string selected = SourceLanguage;

    /// <summary>Culture in use, or the invariant culture when no translation matched.</summary>
    public static CultureInfo Culture { get { lock (sync) return selectedCulture; } }

    /// <summary>Culture name in use, or <see cref="SourceLanguage"/> for the neutral table.</summary>
    public static string Selected { get { lock (sync) return selected; } }

    /// <summary>
    /// Applies the first explicit name with usable resources, otherwise the UI
    /// culture chain. A missing or unreadable translation always degrades to
    /// English instead of throwing, so a bad language setting cannot block start.
    /// </summary>
    public static void Load(params string[] explicitNames)
    {
        CultureInfo chosen = null;
        bool decided = false;

        foreach (string name in explicitNames ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            string requested = name.Trim();

            // An explicit English choice is authoritative: do not let the
            // operating system UI culture pull a translation back in.
            if (IsEnglish(requested))
            {
                chosen = CultureInfo.InvariantCulture;
                decided = true;
                break;
            }

            CultureInfo culture = ResolveCulture(requested);
            if (culture == null || !HasTranslation(culture)) continue;

            chosen = culture;
            decided = true;
            break;
        }

        if (!decided)
        {
            CultureInfo uiCulture = CultureInfo.CurrentUICulture;
            chosen = HasTranslation(uiCulture) ? uiCulture : CultureInfo.InvariantCulture;
        }

        Apply(chosen);
    }

    /// <summary>
    /// Formats a translated template with its <c>{0}</c>-style placeholders. A
    /// malformed translation is shown unformatted instead of crashing the UI.
    /// </summary>
    public static string Format(string template, params object[] arguments)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;
        if (arguments == null || arguments.Length == 0) return template;

        try
        {
            return string.Format(template, arguments);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    private static void Apply(CultureInfo culture)
    {
        lock (sync)
        {
            selectedCulture = culture;
            selected = culture.Name.Length == 0 ? SourceLanguage : culture.Name;
            LauncherStrings.Culture = culture;
        }

        // Framework dialogs (message boxes, file pickers) and threads started
        // later follow the selected UI language. Only the UI culture changes;
        // number and date formatting keep the player's regional settings.
        try
        {
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch (InvalidOperationException) { }
        catch (ArgumentException) { }
    }

    private static bool IsEnglish(string name)
    {
        if (name.Equals(SourceLanguage, StringComparison.OrdinalIgnoreCase)) return true;

        CultureInfo culture = TryCulture(name);
        return culture != null && culture.TwoLetterISOLanguageName == "en";
    }

    /// <summary>
    /// Turns a configured name into the most specific culture that has usable
    /// resources. A neutral language or a language name selects a shipped
    /// regional culture of that language; a specific culture without a
    /// translation returns null so the next explicit name or the UI culture is
    /// used instead.
    /// </summary>
    private static CultureInfo ResolveCulture(string requested)
    {
        CultureInfo culture = TryCulture(requested);
        if (culture != null && HasTranslation(culture)) return culture;

        // "zh" and "Chinese" both name the shipped Chinese pack. A specific
        // culture such as zh-TW follows the native parent chain instead, which
        // does not cross into the sibling zh-Hans resources.
        bool neutral = culture != null && culture.Name.Length != 0 && culture.Name.IndexOf('-') < 0;
        if (culture != null && !neutral) return null;

        string language = culture != null ? LanguageName(culture.EnglishName) : requested;
        if (string.IsNullOrWhiteSpace(language)) return null;

        CultureInfo match = null;
        foreach (CultureInfo candidate in CultureInfo.GetCultures(CultureTypes.AllCultures))
        {
            if (candidate.Name.Length == 0 || !HasTranslation(candidate)) continue;
            if (!string.Equals(LanguageName(candidate.EnglishName), language, StringComparison.OrdinalIgnoreCase)) continue;

            // Prefer the alphabetically first regional pack (zh-Hans before
            // zh-Hant) so the choice is deterministic when several are shipped.
            if (match == null || string.CompareOrdinal(candidate.Name, match.Name) < 0) match = candidate;
        }
        return match;
    }

    /// <summary>True when this culture or one of its parents has a resource set.</summary>
    private static bool HasTranslation(CultureInfo culture)
    {
        if (culture == null) return false;
        if (culture.Name.Length == 0 || culture.Equals(CultureInfo.InvariantCulture)) return true;

        for (CultureInfo current = culture; current != null && current.Name.Length != 0; current = current.Parent)
        {
            try
            {
                // createIfNotExists must be true: with false the manager only
                // consults resource sets that are already loaded, so the first
                // startup would report every satellite as missing.
                if (LauncherStrings.ResourceManager.GetResourceSet(current, createIfNotExists: true, tryParents: false) != null) return true;
            }
            catch (MissingManifestResourceException) { }
            catch (MissingSatelliteAssemblyException) { }
        }
        return false;
    }

    private static CultureInfo TryCulture(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        try
        {
            return CultureInfo.GetCultureInfo(name.Trim());
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>"Chinese (Simplified, China)" becomes "Chinese"; a bare "Chinese" is unchanged.</summary>
    private static string LanguageName(string englishName)
    {
        if (string.IsNullOrEmpty(englishName)) return string.Empty;
        int parenthesis = englishName.IndexOf(" (", StringComparison.Ordinal);
        return (parenthesis < 0 ? englishName : englishName[..parenthesis]).Trim();
    }
}
