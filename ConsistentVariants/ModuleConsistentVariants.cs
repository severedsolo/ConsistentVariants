using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace ConsistentVariants
{
    internal class ModuleConsistentVariants : PartModule
    {
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Default Variant: ")] [UsedImplicitly]
        public string defaultTheme = "None";

        [KSPEvent(active = true, guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, externalToEVAOnly = false, guiName = "Apply Default Variant")]
        public void ApplyVariantToPart()
        {
            ConsistentVariants.Instance.knownParts.Remove(part.persistentId);
            ConsistentVariants.Instance.EditorPartPlaced(part);
        }

        [KSPEvent(active = true, guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, externalToEVAOnly = false, guiName = "Apply Default Variant To All")]
        public void ApplyVariantToAllParts()
        {
            List<Part> editorParts = EditorLogic.SortedShipList;
            for (int i = 0; i < editorParts.Count; i++)
            {
                Part p = editorParts.ElementAt(i);
                ConsistentVariants.Instance.knownParts.Remove(p.persistentId);
                ConsistentVariants.Instance.EditorPartPlaced(p);
            }
        }

        private void Start()
        {
            if (ConsistentVariants.Instance == null) return;
            defaultTheme = ConsistentVariants.Instance.defaultTheme;
        }
    }
}
