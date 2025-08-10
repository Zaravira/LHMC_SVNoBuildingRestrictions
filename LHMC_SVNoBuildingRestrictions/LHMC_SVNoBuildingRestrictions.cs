using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace LHMC_SVNoBuildingRestrictions
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class LHMC_SVNoBuildingRestrictions : BaseUnityPlugin
    {
        public const string pluginGuid = "LHMC_SVNoBuildingRestrictions";
        public const string pluginName = "LHMC_SVNoBuildingRestrictions";
        public const string pluginVersion = "0.0.1";

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(LHMC_SVNoBuildingRestrictions));
        }

        [HarmonyPatch(typeof(GhostModelControl), "Setup")]
        [HarmonyPostfix]
        private static void GMCSetup_Post(GhostModelControl __instance)
        {
            __instance.requiresClearSpace = false;
        }

        [HarmonyPatch(typeof(GhostModelControl), "SetColliders")]
        [HarmonyPostfix]
        private static void GMCSetcolliders_Post(GhostModelControl __instance, bool mode)
        {
            if (__instance.colliders != null)
            {
                for (int i = 0; i < __instance.colliders.Count; i++)
                {
                    if (__instance.colliders[i].name == "PlacementCollider")
                        __instance.colliders[i].enabled = false;
                }
            }
        }

        [HarmonyPatch(typeof(GhostModelControl), nameof(GhostModelControl.OnTriggerEnter))]
        [HarmonyPrefix]
        private static bool GMCOnTriggerEnt_Pre(Collider other)
        {
            if (other.gameObject.transform.name == "DockingControl")
                return false;
            return true;
        }
    }
}
