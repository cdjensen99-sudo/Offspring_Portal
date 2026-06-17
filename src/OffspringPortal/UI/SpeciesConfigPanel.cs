using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OffspringPortal.UI;

public sealed class SpeciesConfigPanel
{
    private static SpeciesConfigPanel instance;

    private const float Padding = 24f;
    private const float LabelWidth = 160f;
    private const float InputWidth = 460f;
    private const float RowHeight = 32f;
    private const float ButtonWidth = 110f;
    private const float ButtonHeight = 48f;
    private const float FirstColumnLeft = Padding;
    private const float SecondColumnLeft = FirstColumnLeft + LabelWidth + Padding;
    private const float NameRowTop = -84f;
    private const float TypeRowTop = -140f;
    private const float ReceivesRowTop = -196f;
    private const float DestinationRowTop = -252f;
    private const float ButtonRowHeight = ButtonHeight + Padding;

    private GameObject mainPanel;
    private GameObject receivesLabelObject;
    private GameObject destinationLabelObject;
    private InputField nameInputField;
    private Dropdown typeDropdown;
    private Dropdown speciesDropdown;
    private Dropdown destinationDropdown;
    private readonly Dictionary<int, PortalRole> indexToRole = new Dictionary<int, PortalRole>();
    private readonly Dictionary<int, SpeciesType> indexToSpecies = new Dictionary<int, SpeciesType>();
    private readonly Dictionary<int, AdultDestination> indexToDestination = new Dictionary<int, AdultDestination>();
    private ZDOID portalId;
    private SpeciesType savedSpecies = SpeciesType.Boar;
    private AdultDestination savedDestination = AdultDestination.None;
    private bool initialized;

    public static SpeciesConfigPanel Instance => instance ?? (instance = new SpeciesConfigPanel());

    public void Open(
        ZDOID id,
        PortalRole currentRole,
        SpeciesType currentSpecies,
        string currentName,
        AdultDestination adultDestination)
    {
        EnsureInitialized();
        if (mainPanel == null)
        {
            return;
        }

        portalId = id;
        savedSpecies = currentSpecies == SpeciesType.None ? SpeciesType.Boar : currentSpecies;
        savedDestination = adultDestination;
        nameInputField.text = currentName ?? string.Empty;
        PopulateTypeDropdown(currentRole);
        PopulateSpeciesDropdown(savedSpecies);
        PopulateDestinationDropdown(savedDestination);
        UpdatePanelState();
        mainPanel.SetActive(true);
        GUIManager.BlockInput(true);
    }

    public void Close()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        GUIManager.BlockInput(false);
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        GameObject guiRoot = GameObject.Find("_GameMain/LoadingGUI/CustomGUIFront");
        if (guiRoot == null)
        {
            OffspringPortalPlugin.Log.LogError("Could not find Valheim custom GUI root.");
            return;
        }

        float panelWidth = Padding + LabelWidth + Padding + InputWidth + Padding;
        float contentBottom = -DestinationRowTop + RowHeight + Padding;
        float panelHeight = contentBottom + ButtonRowHeight + Padding;

        mainPanel = GUIManager.Instance.CreateWoodpanel(
            guiRoot.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f),
            panelWidth,
            panelHeight,
            false);
        mainPanel.name = "OffspringPortal_ConfigPanel";
        mainPanel.AddComponent<CanvasGroup>();
        if (mainPanel.GetComponentInParent<Localize>() == null)
        {
            mainPanel.AddComponent<Localize>();
        }

        GameObject header = GUIManager.Instance.CreateText(
            "Configure Portal",
            mainPanel.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(20f, -15f),
            GUIManager.Instance.AveriaSerifBold,
            32,
            GUIManager.Instance.ValheimOrange,
            true,
            Color.black,
            250f,
            50f,
            false);
        header.GetComponent<Text>().alignment = TextAnchor.UpperCenter;
        header.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);

        CreateLabel("Name", NameRowTop, out _);
        GameObject nameInputObject = GUIManager.Instance.CreateInputField(
            mainPanel.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(SecondColumnLeft, NameRowTop),
            InputField.ContentType.Standard,
            "e.g. Boar Farm",
            18,
            InputWidth,
            RowHeight);
        nameInputObject.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
        nameInputField = nameInputObject.GetComponent<InputField>();
        nameInputField.characterLimit = PortalDisplayHelper.MaxNameLength;

        CreateLabel("Type", TypeRowTop, out _);
        GameObject typeDropdownObject = GUIManager.Instance.CreateDropDown(
            mainPanel.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(SecondColumnLeft, TypeRowTop),
            18,
            InputWidth,
            RowHeight);
        typeDropdownObject.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
        typeDropdown = typeDropdownObject.GetComponent<Dropdown>();
        typeDropdown.onValueChanged.AddListener(new UnityAction<int>(OnTypeChanged));
        ApplyDropdownStyle(typeDropdown);

        CreateLabel("Receives", ReceivesRowTop, out receivesLabelObject);
        GameObject speciesDropdownObject = GUIManager.Instance.CreateDropDown(
            mainPanel.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(SecondColumnLeft, ReceivesRowTop),
            18,
            InputWidth,
            RowHeight);
        speciesDropdownObject.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
        speciesDropdown = speciesDropdownObject.GetComponent<Dropdown>();
        speciesDropdown.onValueChanged.AddListener(new UnityAction<int>(OnSpeciesChanged));
        ApplyDropdownStyle(speciesDropdown);

        CreateLabel("Destination", DestinationRowTop, out destinationLabelObject);
        GameObject destinationDropdownObject = GUIManager.Instance.CreateDropDown(
            mainPanel.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(SecondColumnLeft, DestinationRowTop),
            18,
            InputWidth,
            RowHeight);
        destinationDropdownObject.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
        destinationDropdown = destinationDropdownObject.GetComponent<Dropdown>();
        destinationDropdown.onValueChanged.AddListener(new UnityAction<int>(OnDestinationChanged));
        ApplyDropdownStyle(destinationDropdown);

        GameObject okButtonObject = GUIManager.Instance.CreateButton(
            Localization.instance.Localize("$menu_ok"),
            mainPanel.transform,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-Padding, Padding),
            ButtonWidth,
            ButtonHeight);
        okButtonObject.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);
        okButtonObject.GetComponent<Button>().onClick.AddListener(new UnityAction(OnOkClicked));

        GameObject cancelButtonObject = GUIManager.Instance.CreateButton(
            Localization.instance.Localize("$menu_cancel"),
            mainPanel.transform,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-Padding - ButtonWidth - Padding, Padding),
            ButtonWidth,
            ButtonHeight);
        cancelButtonObject.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);
        cancelButtonObject.GetComponent<Button>().onClick.AddListener(new UnityAction(Close));

        mainPanel.SetActive(false);
        initialized = true;
    }

    private void CreateLabel(string text, float rowTop, out GameObject labelObject)
    {
        labelObject = GUIManager.Instance.CreateText(
            text,
            mainPanel.transform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(FirstColumnLeft, rowTop),
            GUIManager.Instance.AveriaSerif,
            18,
            GUIManager.Instance.ValheimOrange,
            true,
            Color.black,
            LabelWidth,
            RowHeight,
            false);
        labelObject.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
        Text labelText = labelObject.GetComponent<Text>();
        labelText.alignment = TextAnchor.UpperLeft;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    private static void ApplyDropdownStyle(Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return;
        }

        Image captionImage = dropdown.captionImage;
        if (captionImage != null)
        {
            captionImage.color = Color.white;
        }
    }

    private void PopulateTypeDropdown(PortalRole currentRole)
    {
        typeDropdown.ClearOptions();
        indexToRole.Clear();

        int selectedIndex = 0;
        foreach (PortalRole role in Enum.GetValues(typeof(PortalRole)))
        {
            indexToRole[typeDropdown.options.Count] = role;
            typeDropdown.options.Add(new Dropdown.OptionData(PortalRoleCatalog.GetDisplayName(role)));
            if (role == currentRole)
            {
                selectedIndex = typeDropdown.options.Count - 1;
            }
        }

        typeDropdown.value = selectedIndex;
        typeDropdown.RefreshShownValue();
    }

    private void PopulateSpeciesDropdown(SpeciesType currentSpecies)
    {
        speciesDropdown.ClearOptions();
        indexToSpecies.Clear();

        int selectedIndex = 0;
        int index = 0;
        foreach (SpeciesType species in SpeciesCatalog.DestinationOptions)
        {
            indexToSpecies[index] = species;
            speciesDropdown.options.Add(new Dropdown.OptionData(SpeciesCatalog.GetDisplayName(species)));
            if (species == currentSpecies)
            {
                selectedIndex = index;
            }

            index++;
        }

        speciesDropdown.value = selectedIndex;
        speciesDropdown.RefreshShownValue();
    }

    private void PopulateDestinationDropdown(AdultDestination currentDestination)
    {
        destinationDropdown.ClearOptions();
        indexToDestination.Clear();

        int selectedIndex = 0;
        foreach (AdultDestination destination in Enum.GetValues(typeof(AdultDestination)))
        {
            indexToDestination[destinationDropdown.options.Count] = destination;
            destinationDropdown.options.Add(new Dropdown.OptionData(PortalRoleCatalog.GetDisplayName(destination)));
            if (destination == currentDestination)
            {
                selectedIndex = destinationDropdown.options.Count - 1;
            }
        }

        destinationDropdown.value = selectedIndex;
        destinationDropdown.RefreshShownValue();
    }

    private void OnTypeChanged(int index)
    {
        if (indexToRole.TryGetValue(typeDropdown.value, out PortalRole role)
            && role != PortalRole.Breeder)
        {
            PopulateSpeciesDropdown(savedSpecies);
        }

        UpdatePanelState();
    }

    private void OnSpeciesChanged(int index)
    {
        if (indexToSpecies.TryGetValue(speciesDropdown.value, out SpeciesType species))
        {
            savedSpecies = species;
        }
    }

    private void OnDestinationChanged(int index)
    {
        if (indexToDestination.TryGetValue(destinationDropdown.value, out AdultDestination destination))
        {
            savedDestination = destination;
        }
    }

    private void UpdatePanelState()
    {
        if (!indexToRole.TryGetValue(typeDropdown.value, out PortalRole role))
        {
            role = PortalRole.Breeder;
        }

        bool showReceives = role == PortalRole.Maturing || role == PortalRole.Farm;
        bool showDestination = role == PortalRole.Maturing;
        receivesLabelObject.SetActive(showReceives);
        speciesDropdown.gameObject.SetActive(showReceives);
        destinationLabelObject.SetActive(showDestination);
        destinationDropdown.gameObject.SetActive(showDestination);

        if (role == PortalRole.Breeder)
        {
            speciesDropdown.interactable = false;
            speciesDropdown.ClearOptions();
            speciesDropdown.options.Add(new Dropdown.OptionData("All juveniles"));
            speciesDropdown.value = 0;
            speciesDropdown.RefreshShownValue();
            return;
        }

        if (role == PortalRole.Cull)
        {
            speciesDropdown.interactable = false;
            speciesDropdown.ClearOptions();
            speciesDropdown.options.Add(new Dropdown.OptionData("All adults"));
            speciesDropdown.value = 0;
            speciesDropdown.RefreshShownValue();
            return;
        }

        speciesDropdown.interactable = true;
        if (speciesDropdown.options.Count <= 1)
        {
            PopulateSpeciesDropdown(savedSpecies);
        }

        if (showDestination && destinationDropdown.options.Count <= 1)
        {
            PopulateDestinationDropdown(savedDestination);
        }
    }

    private PortalRole GetSelectedRole()
    {
        return indexToRole.TryGetValue(typeDropdown.value, out PortalRole role)
            ? role
            : PortalRole.Breeder;
    }

    private SpeciesType GetSelectedSpecies()
    {
        return indexToSpecies.TryGetValue(speciesDropdown.value, out SpeciesType species)
            ? species
            : SpeciesType.Boar;
    }

    private AdultDestination GetSelectedDestination()
    {
        return indexToDestination.TryGetValue(destinationDropdown.value, out AdultDestination destination)
            ? destination
            : AdultDestination.None;
    }

    private void OnOkClicked()
    {
        PortalRole role = GetSelectedRole();
        SpeciesType species = PortalRoleCatalog.ResolveSpecies(role, GetSelectedSpecies());
        AdultDestination destination = GetSelectedDestination();
        PortalRpc.SetPortalConfig(portalId, role, species, nameInputField.text, destination);
        Close();
    }
}
