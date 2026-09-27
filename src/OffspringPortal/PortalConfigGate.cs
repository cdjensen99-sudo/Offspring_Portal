namespace OffspringPortal;

/// <summary>
/// Portals must be configured once (E → save) before automation and registry sync treat them as active.
/// </summary>
public static class PortalConfigGate
{
    public static bool IsConfiguredForAutomation(ZDO zdo)
    {
        if (zdo == null)
        {
            return false;
        }

        string role = zdo.GetString(ZdoFields.PortalRole, string.Empty);
        return !string.IsNullOrWhiteSpace(role);
    }
}
