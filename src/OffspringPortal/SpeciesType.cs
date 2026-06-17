using System;
using System.Collections.Generic;

namespace OffspringPortal;

public enum SpeciesType
{
    None = 0,
    All,
    Boar,
    Wolf,
    Lox,
    Chicken,
    Asksvin
}

public static class SpeciesCatalog
{
    public static readonly IReadOnlyList<SpeciesType> DestinationOptions = new[]
    {
        SpeciesType.Boar,
        SpeciesType.Wolf,
        SpeciesType.Lox,
        SpeciesType.Chicken,
        SpeciesType.Asksvin,
        SpeciesType.All
    };

    public static string ToStorageValue(SpeciesType species)
    {
        return species == SpeciesType.None ? string.Empty : species.ToString();
    }

    public static SpeciesType FromStorageValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SpeciesType.None;
        }

        return Enum.TryParse(value, ignoreCase: true, out SpeciesType parsed)
            ? parsed
            : SpeciesType.None;
    }

    public static string GetDisplayName(SpeciesType species)
    {
        switch (species)
        {
            case SpeciesType.All:
                return "All";
            case SpeciesType.Boar:
                return "Boar";
            case SpeciesType.Wolf:
                return "Wolf";
            case SpeciesType.Lox:
                return "Lox";
            case SpeciesType.Chicken:
                return "Hen";
            case SpeciesType.Asksvin:
                return "Asksvin";
            default:
                return "Breeder";
        }
    }
}
