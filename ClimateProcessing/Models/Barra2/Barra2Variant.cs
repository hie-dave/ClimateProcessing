namespace ClimateProcessing.Models.Barra2;

/// <summary>
/// The nature of the ERA5 data used.
/// </summary>
public enum Barra2Variant
{
    /// <summary>
    /// High resolution ERA5 data.
    /// </summary>
    HRes,

    /// <summary>
    /// ERA5 data with eda (Earth Data Access??) processing.
    /// </summary>
    Eda
}

/// <summary>
/// Extension methods for the <see cref="Barra2Variant"/> enum.
/// </summary>
public static class Barra2VariantExtensions
{
    /// <summary>
    /// The string representation of the <see cref="Barra2Variant.HRes"/> variant.
    /// </summary>
    private const string hres = "hres";

    /// <summary>
    /// The string representation of the <see cref="Barra2Variant.Eda"/> variant.
    /// </summary>
    private const string eda = "eda";

    /// <summary>
    /// Convert a <see cref="Barra2Variant"/> to a string.
    /// </summary>
    /// <param name="variant">The variant to convert.</param>
    /// <returns>The string representation of the variant.</returns>
    /// <exception cref="ArgumentException">Thrown when the variant is not a valid <see cref="Barra2Variant"/>.</exception>
    public static string ToString(this Barra2Variant variant) => variant switch
    {
        Barra2Variant.HRes => hres,
        Barra2Variant.Eda => eda,
        _ => throw new ArgumentException($"Unknown variant: {variant}")
    };

    /// <summary>
    /// Convert a string to a <see cref="Barra2Variant"/>.
    /// </summary>
    /// <param name="variantStr">The string to convert.</param>
    /// <returns>The <see cref="Barra2Variant"/> representation of the string.</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid <see cref="Barra2Variant"/>.</exception>
    public static Barra2Variant FromString(string variantStr) => variantStr switch
    {
        hres => Barra2Variant.HRes,
        eda => Barra2Variant.Eda,
        _ => throw new ArgumentException($"Unknown variant string: {variantStr}")
    };
}
