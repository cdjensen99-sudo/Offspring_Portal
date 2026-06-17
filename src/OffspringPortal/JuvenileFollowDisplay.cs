namespace OffspringPortal;

public static class JuvenileFollowDisplay
{
    public static void ApplyFollowHover(Character character, ref string hoverText)
    {
        if (!ModConfig.EnableFollowCommand.Value
            || character == null
            || !SpeciesHelper.IsEligibleJuvenile(character)
            || !character.IsTamed())
        {
            return;
        }

        if (character.GetComponent<MonsterAI>() == null && character.GetComponent<AnimalAI>() == null)
        {
            return;
        }

        string commandKey = JuvenileFollow.IsFollowing(character) ? "$hud_tamestay" : "$hud_tamefollow";
        string localizedCommand = Localization.instance.Localize(commandKey);
        string localizedPet = Localization.instance.Localize("$hud_pet");

        if (!string.IsNullOrEmpty(hoverText) && hoverText.Contains(localizedPet))
        {
            hoverText = hoverText.Replace(localizedPet, localizedCommand);
            return;
        }

        if (!string.IsNullOrEmpty(hoverText))
        {
            hoverText += Localization.instance.Localize(
                $"\n[<color=yellow><b>$KEY_Use</b></color>] {commandKey}");
            return;
        }

        hoverText = Localization.instance.Localize(
            $"{character.GetHoverName()}\n[<color=yellow><b>$KEY_Use</b></color>] {commandKey}");
    }
}
