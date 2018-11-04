using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CVConfigCreator
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class CVConfigCreator : MonoBehaviour
    {
        public void Start()
        {
            ConfigNode[] loadedNodes = GameDatabase.Instance.GetConfigNodes("CONSISTENT_VARIANT");
            ConfigNode masterNode = new ConfigNode();
            for(int partCount = 0; partCount<PartLoader.LoadedPartsList.Count(); partCount++)
            {
                bool configAlreadyExists = false;
                AvailablePart ap = PartLoader.LoadedPartsList.ElementAt(partCount);
                if (ap.Variants == null || ap.Variants.Count == 0) continue;
                for (int i = 0; i<loadedNodes.Count(); i++)
                {
                    ConfigNode node = loadedNodes.ElementAt(i);
                    if(node.GetValue("PartName") == ap.partPrefab.name)
                    {
                        configAlreadyExists = true;
                        break;
                    }
                }
                if (configAlreadyExists) continue;
                ConfigNode cn = new ConfigNode("CONSISTENT_VARIANT");
                cn.SetValue("PartName", ap.partPrefab.name, true);
                cn.SetValue("PartDisplayName", ap.partPrefab.partInfo.title, true);
                for(int variantCount = 0; variantCount<ap.Variants.Count; variantCount++)
                {
                    ConfigNode vnode = new ConfigNode("VARIANT");
                    PartVariant v = ap.Variants.ElementAt(variantCount);
                    vnode.SetValue("VariantName", v.Name, true);
                    vnode.SetValue("DisplayName", v.DisplayName, true);
                    vnode.SetValue("Theme", "", true);
                    cn.AddNode(vnode);
                }
                masterNode.AddNode(cn);
            }
            masterNode.Save(KSPUtil.ApplicationRootPath + "GameData/ConsistentVariants/GeneratedConfig.cfg");
        }
    }
}
