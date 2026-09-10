namespace OffspringPortal;

public static class BreedingCapData
{
    public static int GetCapLimit(SpeciesType species)
    {
        switch (species)
        {
            case SpeciesType.Boar:
                return 5;
            case SpeciesType.Wolf:
                return 4;
            case SpeciesType.Lox:
                return 4;
            case SpeciesType.Chicken:
                return 10;
            case SpeciesType.Asksvin:
                return 10;
            case SpeciesType.Moose:
                return 4;
            default:
                return 0;
        }
    }

    public static float GetCapRadius(SpeciesType species)
    {
        switch (species)
        {
            case SpeciesType.Boar:
            case SpeciesType.Wolf:
            case SpeciesType.Chicken:
            case SpeciesType.Asksvin:
                return 10f;
            case SpeciesType.Lox:
            case SpeciesType.Moose:
                return 20f;
            default:
                return 0f;
        }
    }
}
