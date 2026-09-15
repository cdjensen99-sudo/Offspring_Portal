using BepInEx.Bootstrap;
using Jotunn.Managers;
using UnityEngine;

namespace OffspringPortal;

internal static class PortalHammerIcon
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // Slight isometric tilt — same family as Jotunn's default, tuned for the portal frame.
    private static readonly Quaternion IconRotation = Quaternion.Euler(28f, -135f, 0f);

    internal static Sprite Create(GameObject prefab)
    {
        if (prefab == null || RenderManager.Instance == null)
        {
            return null;
        }

        GameObject iconSubject = Object.Instantiate(prefab);
        iconSubject.name = prefab.name + "_IconRender";
        iconSubject.hideFlags = HideFlags.HideAndDontSave;
        iconSubject.SetActive(true);

        try
        {
            PrepareIconSubject(iconSubject);

            RenderManager.RenderRequest request = new RenderManager.RenderRequest(iconSubject)
            {
                Rotation = IconRotation,
                ParticleSimulationTime = -1f,
                Width = 256,
                Height = 256,
                UseCache = true
            };

            if (Chainloader.PluginInfos.TryGetValue(OffspringPortalPlugin.PluginGuid, out BepInEx.PluginInfo pluginInfo))
            {
                request.TargetPlugin = pluginInfo.Metadata;
            }

            return RenderManager.Instance.Render(request);
        }
        finally
        {
            Object.Destroy(iconSubject);
        }
    }

    private static void PrepareIconSubject(GameObject root)
    {
        DisableVisualNoise(root);
        DimEmissiveMaterials(root);
        HideAutomationTriggers(root);
    }

    private static void DisableVisualNoise(GameObject root)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            switch (component.GetType().Name)
            {
                case "ParticleSystem":
                    component.gameObject.SetActive(false);
                    break;
                case "Light":
                case "LineRenderer":
                case "TrailRenderer":
                    if (component is Behaviour behaviour)
                    {
                        behaviour.enabled = false;
                    }

                    break;
            }
        }
    }

    private static void DimEmissiveMaterials(GameObject root)
    {
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material material = renderer.material;
            if (material != null && material.HasProperty(EmissionColorId))
            {
                material.SetColor(EmissionColorId, Color.black);
            }
        }
    }

    private static void HideAutomationTriggers(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            if (collider.isTrigger)
            {
                collider.enabled = false;
            }
        }
    }
}
