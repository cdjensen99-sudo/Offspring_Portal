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
    Asksvin,
    Moose
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
        SpeciesType.Moose,
        SpeciesType.All
    };

    public static readonly IReadOnlyList<string> KnownAdultPrefabFragments = new[]
    {
        "Boar",
        "WolfCub",
        "Wolf",
        "Lox",
        "Hen",
        "Chicken",
        "Asksvin",
        "Moose",
        "Moose_Calf"
    };

    public static readonly IReadOnlyList<string> LegacyDestinationOptionKeys = new[]
    {
        "Boar",
        "Wolf",
        "Lox",
        "Chicken",
        "Asksvin",
        "Moose",
        SpeciesKey.All
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
            case SpeciesType.Moose:
                return "Moose";
            default:
                return "Breeder";
        }
    }
}
