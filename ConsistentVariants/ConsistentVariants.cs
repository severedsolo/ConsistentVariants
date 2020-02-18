using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using System.Linq;

namespace ConsistentVariants
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class ConsistentVariants : MonoBehaviour
    {
        public static ConsistentVariants Instance;
        public string defaultTheme = "None";
        private string fallbackVariant;
        private ApplicationLauncherButton toolbarButton;
        private bool variantListenerEnabled = true;
        private bool overrideVariant = true;
        public List<uint> knownParts = new List<uint>();

        public void Awake()
        {
            Instance = this;
            GameEvents.onEditorPartPlaced.Add(EditorPartPlaced);
            GameEvents.onVariantApplied.Add(EditorVariantApplied);
            GameEvents.onEditorDefaultVariantChanged.Add(DefaultVariantSet);
            GameEvents.onGUIApplicationLauncherReady.Add(GuiReady);
            GameEvents.onEditorPodPicked.Add(EditorPartPlaced);
            Debug.Log("[ConsistentVariants]: Awake");
        }

        public void Start()
        {
            ConfigNode[] themeNodes = GameDatabase.Instance.GetConfigNodes("CONSISTENT_VARIANT");
            for(int partCount = 0; partCount<PartLoader.Instance.loadedParts.Count; partCount++)
            {
                AvailablePart part = PartLoader.Instance.loadedParts.ElementAt(partCount);
                for(int partNodeCount = 0; partNodeCount<themeNodes.Length; partNodeCount++)
                {
                    ConfigNode partNode = themeNodes.ElementAt(partNodeCount);
                    if (partNode.GetValue("PartName") != part.partPrefab.name) continue;
                    if (part.Variants == null || part.Variants.Count == 0) continue;
                    ConfigNode[] variantNodes = partNode.GetNodes("VARIANT");
                    for(int variantNodeCount = 0; variantNodeCount<variantNodes.Length; variantNodeCount++)
                    {
                        ConfigNode variantNode = variantNodes.ElementAt(variantNodeCount);
                        if (variantNode.GetValue("Theme") == "N/A") continue;
                        for(int variantCount = 0; variantCount<part.Variants.Count; variantCount++)
                        {
                            PartVariant variant = part.Variants.ElementAt(variantCount);
                            if (variant.Name != variantNode.GetValue("VariantName")) continue;
                            variant.DisplayName = variantNode.GetValue("Theme");
                        }
                    }
                }
            }
            Debug.Log("ConsistentVariants]: Variant Descriptions Updated");
        }

        private void DefaultVariantSet(AvailablePart part, PartVariant variant)
        {
            string themeToApply = DefineDefaultTheme(variant.Name, part.partPrefab.name);
            if (themeToApply != null)
            {
                UpdateDefaultTheme(themeToApply);
            }
            fallbackVariant = variant.Name;
        }
        
        public void SetDefaultVariant(Part part, PartVariant variant)
        {
            string themeToApply = DefineDefaultTheme(variant.Name, part.name);
            if (themeToApply != null)
            {
                UpdateDefaultTheme(themeToApply);
            }
            fallbackVariant = variant.Name;
        }

        private void UpdateDefaultTheme(string themeToApply)
        {
            defaultTheme = themeToApply;
            for(int i = 0; i<EditorLogic.SortedShipList.Count; i++)
            {
                Part p = EditorLogic.SortedShipList.ElementAt(i);
                p.FindModulesImplementing<ModuleConsistentVariants>().FirstOrDefault().defaultTheme = defaultTheme;
            }
            Debug.Log("[ConsistentVariants]: " + defaultTheme + " is now the new default theme");
        }

        private void GuiReady()
        {
            toolbarButton = ApplicationLauncher.Instance.AddModApplication(ToggleVariantListener, ToggleVariantListener, null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, GameDatabase.Instance.GetTexture("ConsistentVariants/IconON", false));
            Debug.Log("[ConsistentVariants]: GUIReady");
        }

        private void ToggleVariantListener()
        {
            variantListenerEnabled = !variantListenerEnabled;
            if (variantListenerEnabled) toolbarButton.SetTexture(GameDatabase.Instance.GetTexture("ConsistentVariants/IconON", false));
            else toolbarButton.SetTexture(GameDatabase.Instance.GetTexture("ConsistentVariants/IconOFF", false));
        }

        private void EditorVariantApplied(Part p, PartVariant v)
        {
            if (!variantListenerEnabled) return;
            if (!overrideVariant)
            {
                overrideVariant = true;
                return;
            }
            if (!EditorLogic.fetch.ship.parts.Contains(p)) return;
            string themeToApply = DefineDefaultTheme(v.Name, p.name);
            if (themeToApply != null && themeToApply != defaultTheme)
            {
                UpdateDefaultTheme(themeToApply);
            }
            fallbackVariant = v.Name;
            Debug.Log("[ConsistentVariants]: Fallback variant is " + fallbackVariant);
        }

        public void EditorPartPlaced(Part part)
        {
            if (!variantListenerEnabled) return;
            if (part == null || part.variants == null) return;
            if (knownParts.Contains(part.persistentId)) return;
            if (defaultTheme == null)
            {
                string themeToApply = DefineDefaultTheme(part.variants.SelectedVariant.Name, part.name);
                if (themeToApply != null && themeToApply != defaultTheme) UpdateDefaultTheme(themeToApply);
                    fallbackVariant = part.variants.SelectedVariant.Name;
                Debug.Log("[ConsistentVariants]: " + defaultTheme + " is now the default theme");
                Debug.Log("[ConsistentVariants]: Fallback variant is " + fallbackVariant);
                if(defaultTheme != null) return;
            }
            if (part == null || part.variants == null) return;
            string variantToApply = GetVariantToApply(defaultTheme, part);
            if (variantToApply == null) return;
            if (!part.variants.HasVariant(variantToApply))
            {
                Debug.Log("[ConsistentVariants]: " + part.partInfo.title + " has no suitable variants available. Skipping");
                overrideVariant = false;
                return;
            }
            part.variants.SetVariant(variantToApply);
            foreach (Part p in part.symmetryCounterparts)
            {
                p.variants.SetVariant(variantToApply);
            }
            Debug.Log("[ConsistentVariants]: Applied " + variantToApply + " to " + part.partInfo.title);
            if(!knownParts.Contains(part.persistentId))knownParts.Add(part.persistentId);
        }

        private string GetVariantToApply(string variantName, Part p)
        {
            ConfigNode[] partNodes = GameDatabase.Instance.GetConfigNodes("CONSISTENT_VARIANT");
            for (int i = 0; i < partNodes.Length; i++)
            {
                ConfigNode partNode = partNodes.ElementAt(i);
                if (partNode.GetValue("PartName") != p.name) continue;
                ConfigNode[] variantNodes = partNode.GetNodes("VARIANT");
                for (int variantCount = 0; variantCount < variantNodes.Length; variantCount++)
                {
                    ConfigNode selectedVariantNode = variantNodes.ElementAt(variantCount);
                    if (selectedVariantNode.GetValue("Theme") != variantName) continue;
                    return selectedVariantNode.GetValue("VariantName");
                }
            }

            for (int i = 0; i < p.variants.variantList.Count; i++)
            {
                PartVariant pv = p.variants.variantList.ElementAt(i);
                if (pv.DisplayName == fallbackVariant) return pv.Name;
            }
            return null;
        }

        private string DefineDefaultTheme(string variantName, string partName)
        {
            ConfigNode[] variantParts = GameDatabase.Instance.GetConfigNodes("CONSISTENT_VARIANT");
            for (int i = 0; i < variantParts.Length; i++)
            {
                ConfigNode cn = variantParts.ElementAt(i);
                if (cn.GetValue("PartName") != partName) continue;
                ConfigNode[] variantList = cn.GetNodes("VARIANT");
                for (int variantCount = 0; variantCount < variantList.Length; variantCount++)
                {
                    ConfigNode selectedVariant = variantList.ElementAt(variantCount);
                    if (selectedVariant.GetValue("VariantName") != variantName) continue;
                    if (selectedVariant.GetValue("Theme") == "N/A") return defaultTheme;
                    return selectedVariant.GetValue("Theme");
                }
            }
            return null;
        }

        public void OnDisable()
        {
            GameEvents.onEditorPartPlaced.Remove(EditorPartPlaced);
            GameEvents.onVariantApplied.Remove(EditorVariantApplied);
            GameEvents.onEditorDefaultVariantChanged.Remove(DefaultVariantSet);
            GameEvents.onGUIApplicationLauncherReady.Remove(GuiReady);
            GameEvents.onEditorPodPicked.Remove(EditorPartPlaced);
            if (toolbarButton != null) ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
        }
    }
}
