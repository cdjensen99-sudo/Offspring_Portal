using System.Collections.Generic;
using Jotunn.Managers;

namespace OffspringPortal;

internal static class OffspringPortalLocalization
{
    internal static void Register()
    {
        LocalizationManager.Instance.GetLocalization().AddTranslation("English", new Dictionary<string, string>
        {
            { "piece_offspring_portal", "Offspring Portal" },
            {
                "piece_offspring_portal_description",
                "Routes tamed juveniles and breedable eggs between configured breeding pens."
            }
        });
    }
}
