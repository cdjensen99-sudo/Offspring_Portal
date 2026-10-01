using System;

namespace OffspringPortal;

public static class ZdoIdUtility
{
    public static bool IsNone(ZDOID id)
    {
        return id == ZDOID.None;
    }

    public static bool IsValidId(ZDOID id)
    {
        if (IsNone(id))
        {
            return false;
        }

        try
        {
            _ = id.ToString();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string Format(ZDOID id)
    {
        if (IsNone(id))
        {
            return "None";
        }

        try
        {
            return id.ToString();
        }
        catch (Exception)
        {
            return $"invalid:{id.ID}";
        }
    }

    public static int Compare(ZDOID left, ZDOID right)
    {
        return string.Compare(Format(left), Format(right), StringComparison.Ordinal);
    }

    public static ZDO TryGetZdo(ZDOID id)
    {
        if (!IsValidId(id) || ZDOMan.instance == null)
        {
            return null;
        }

        try
        {
            return ZDOMan.instance.GetZDO(id);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
