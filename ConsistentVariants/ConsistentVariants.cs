using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using System.Linq;
#if DEBUG
using System.IO;
#endif

namespace ConsistentVariants
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class ConsistentVariants : MonoBehaviour
    {
        string _defaultTheme = "ConsistentVariants Theme: None";
        string _fallbackVariant;
        ApplicationLauncherButton _toolbarButton;
        bool _variantListenerEnabled = true;
        bool _overrideVariant = true;
        List<uint> _knownParts = new List<uint>();

        public void Awake()
        {
            GameEvents.onEditorPartPlaced.Add(EditorPartPlaced);
            GameEvents.onVariantApplied.Add(EditorVariantApplied);
            GameEvents.onEditorDefaultVariantChanged.Add(DefaultVariantSet);
            GameEvents.onGUIApplicationLauncherReady.Add(GUIReady);
            GameEvents.onEditorPodPicked.Add(EditorPartPlaced);
            Debug.Log("[ConsistentVariants]: Awake");
        }

        public void Start()
        {
            ConfigNode[] themeNodes = GameDatabase.Instance.GetConfigNodes("CONSISTENT_VARIANT");
            ConfigNode[] variantNodes;
            ConfigNode partNode;
            ConfigNode variantNode;
            PartVariant variant;
            AvailablePart part;
            for(int partCount = 0; partCount<PartLoader.Instance.loadedParts.Count(); partCount++)
            {
                part = PartLoader.Instance.loadedParts.ElementAt(partCount);
                for(int partNodeCount = 0; partNodeCount<themeNodes.Count(); partNodeCount++)
                {
                    partNode = themeNodes.ElementAt(partNodeCount);
                    if (partNode.GetValue("PartName") != part.partPrefab.name) continue;
                    if (part.Variants == null || part.Variants.Count == 0) continue;
                    variantNodes = partNode.GetNodes("VARIANT");
                    for(int variantNodeCount = 0; variantNodeCount<variantNodes.Count(); variantNodeCount++)
                    {
                        variantNode = variantNodes.ElementAt(variantNodeCount);
                        if (variantNode.GetValue("Theme") == "N/A") continue;
                        for(int variantCount = 0; variantCount<part.Variants.Count; variantCount++)
                        {
                            variant = part.Variants.ElementAt(variantCount);
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
                _defaultTheme = themeToApply;
                Debug.Log("[ConsistentVariants]: DefaultSet: " + _defaultTheme + " is now the new default theme");
            }
            _fallbackVariant = variant.Name;
        }

        private void GUIReady()
        {
            _toolbarButton = ApplicationLauncher.Instance.AddModApplication(ToggleVariantListener, ToggleVariantListener, null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, GameDatabase.Instance.GetTexture("ConsistentVariants/IconON", false));
            Debug.Log("[ConsistentVariants]: GUIReady");
        }

        private void ToggleVariantListener()
        {
            _variantListenerEnabled = !_variantListenerEnabled;
            if (_variantListenerEnabled) _toolbarButton.SetTexture(GameDatabase.Instance.GetTexture("ConsistentVariants/IconON", false));
            else _toolbarButton.SetTexture(GameDatabase.Instance.GetTexture("ConsistentVariants/IconOFF", false));
        }

        private void EditorVariantApplied(Part p, PartVariant v)
        {
            if (!_variantListenerEnabled) return;
            if (!_overrideVariant)
            {
                _overrideVariant = true;
                return;
            }
            if (!EditorLogic.fetch.ship.parts.Contains(p)) return;
            string themeToApply = DefineDefaultTheme(v.Name, p.name);
            if (themeToApply != null && themeToApply != _defaultTheme)
            {
                Debug.Log("[ConsistentVariants]: " + _defaultTheme + " is now the default theme");
                _defaultTheme = themeToApply;
            }
            _fallbackVariant = v.Name;
            Debug.Log("[ConsistentVariants]: Fallback variant is " + _fallbackVariant);
        }

        private void EditorPartPlaced(Part part)
        {
            if (!_variantListenerEnabled) return;
            if (part == null || part.variants == null) return;
            if (_knownParts.Contains(part.persistentId)) return;
            if (_defaultTheme == null)
            {
                string themeToApply = DefineDefaultTheme(part.variants.SelectedVariant.Name, part.partName);
                if (themeToApply != null && themeToApply != _defaultTheme)
                {
                    _defaultTheme = themeToApply;
                    Debug.Log("[ConsistentVariants]: " + _defaultTheme + " is now the default theme");
                }
                    _fallbackVariant = part.variants.SelectedVariant.Name;
                Debug.Log("[ConsistentVariants]: " + _defaultTheme + " is now the default theme");
                Debug.Log("[ConsistentVariants]: Fallback variant is " + _fallbackVariant);
                if(_defaultTheme != null) return;
            }
            if (part == null || part.variants == null) return;
            string variantToApply = GetVariantToApply(_defaultTheme, part);
            if (variantToApply == null) return;
            if (!part.variants.HasVariant(variantToApply))
            {
                Debug.Log("[ConsistentVariants]: " + part.partInfo.title + " has no suitable variants available. Skipping");
                _overrideVariant = false;
                return;
            }
            part.variants.SetVariant(variantToApply);
            Debug.Log("[ConsistentVariants]: Applied " + variantToApply + " to " + part.partInfo.title);
            _knownParts.Add(part.persistentId);
        }

        private string GetVariantToApply(string variantName, Part p)
        {
            List<PartVariant> availableVariants = p.variants.variantList;
            ConfigNode[] partNodes = GameDatabase.Instance.GetConfigNodes("CONSISTENT_VARIANT");
            ConfigNode[] variantNodes;
            ConfigNode partNode;
            ConfigNode selectedVariantNode;
            for (int i = 0; i < partNodes.Count(); i++)
            {
                partNode = partNodes.ElementAt(i);
                if (partNode.GetValue("PartName") != p.partName) continue;
                variantNodes = partNode.GetNodes("VARIANT");
                for (int variantCount = 0; variantCount < variantNodes.Count(); variantCount++)
                {
                    selectedVariantNode = variantNodes.ElementAt(variantCount);
                    if (selectedVariantNode.GetValue("Theme") != _defaultTheme) continue;
                    return selectedVariantNode.GetValue("VariantName");
                }
            }
            PartVariant pv;
            for (int i = 0; i < p.variants.variantList.Count(); i++)
            {
                pv = p.variants.variantList.ElementAt(i);
                if (pv.DisplayName == _fallbackVariant) return pv.Name;
            }
            return null;
        }
        
        private string DefineDefaultTheme(string variantName, string partName)
        {
            ConfigNode[] variantParts = GameDatabase.Instance.GetConfigNodes("CONSISTENT_VARIANT");
            ConfigNode cn;
            ConfigNode[] variantList;
            ConfigNode selectedVariant;
            for (int i = 0; i < variantParts.Count(); i++)
            {
                cn = variantParts.ElementAt(i);
                if (cn.GetValue("PartName") != partName) continue;
                variantList = cn.GetNodes("VARIANT");
                for (int variantCount = 0; variantCount < variantList.Count(); variantCount++)
                {
                    selectedVariant = variantList.ElementAt(variantCount);
                    if (selectedVariant.GetValue("VariantName") != variantName) continue;
                    if (selectedVariant.GetValue("Theme") == "N/A") return _defaultTheme;
                    return selectedVariant.GetValue("Theme");
                }
            }
            return null;
        }
    }
}
