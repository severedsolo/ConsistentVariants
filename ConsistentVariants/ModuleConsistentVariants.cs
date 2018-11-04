using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ConsistentVariants
{
    class ModuleConsistentVariants : PartModule
    {
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Default Variant: ")]
        public string _defaultTheme = "None";

        [KSPEvent(active = true, guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, externalToEVAOnly = false, guiName = "Apply Default Variant")]
        public void ApplyVariantToPart()
        {
            ConsistentVariants.instance._knownParts.Remove(part.persistentId);
            ConsistentVariants.instance.EditorPartPlaced(part);
        }

        [KSPEvent(active = true, guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, externalToEVAOnly = false, guiName = "Apply Default Variant To All")]
        public void ApplyVariantToAllParts()
        {
            List<Part> editorParts = EditorLogic.SortedShipList;
            for (int i = 0; i < editorParts.Count(); i++)
            {
                Part p = editorParts.ElementAt(i);
                ConsistentVariants.instance._knownParts.Remove(p.persistentId);
                ConsistentVariants.instance.EditorPartPlaced(p);
            }
        }

        void Start()
        {
            if (ConsistentVariants.instance == null) return;
            _defaultTheme = ConsistentVariants.instance._defaultTheme;
        }
    }
}
