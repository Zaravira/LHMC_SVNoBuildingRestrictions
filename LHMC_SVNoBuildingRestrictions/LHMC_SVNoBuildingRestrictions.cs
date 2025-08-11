using BepInEx;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace LHMC_SVNoBuildingRestrictions
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class LHMC_SVNoBuildingRestrictions : BaseUnityPlugin
    {
        public const string pluginGuid = "LHMC_SVNoBuildingRestrictions";
        public const string pluginName = "LHMC_SVNoBuildingRestrictions";
        public const string pluginVersion = "0.0.2";

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

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CallOpenInventory))]
        [HarmonyPrefix]
        private static bool PCOpenInv_Pre(PlayerControl __instance)
        {
            StationCapture.allowClosing = true;
            int num = MenuControl.ClosePanels(forceCloseAll: false);
            if (num != 1 && num != 2)
            {
                if (__instance.inStationRange)
                {
                    // Get nearest station whose docking area is colliding with player
                    Station station = null;
                    float distCur = 99999;
                    CapsuleCollider[] playerShipColliders = __instance.GetSpaceShip.GetComponentsInChildren<CapsuleCollider>();

                    foreach (CapsuleCollider col in playerShipColliders)
                    {
                        Vector3 direction = new Vector3 { [col.direction] = 1 };
                        float offset = col.height / 2 - col.radius;
                        Vector3 localPoint0 = col.center - direction * offset;
                        Vector3 localPoint1 = col.center + direction * offset;
                        Vector3 r = __instance.GetSpaceShip.transform.TransformVector(col.radius, col.radius, col.radius);
                        float radius = Enumerable.Range(0, 3).Select(xyz => xyz == col.direction ? 0 : r[xyz])
                            .Select(Mathf.Abs).Max();

                        Collider[] otherColliders = Physics.OverlapCapsule(
                            __instance.GetSpaceShip.transform.TransformPoint(localPoint0),
                            __instance.GetSpaceShip.transform.TransformPoint(localPoint1),
                            radius, 2147483647, QueryTriggerInteraction.Collide);

                        for (int i = 0; i < otherColliders.Length; i++)
                        {
                            Collider otherCol = otherColliders[i];
                            if (otherCol.name == "DockingControl")
                            {
                                AIStationControl aisc = otherCol.gameObject.transform.parent.GetComponentInChildren<AIStationControl>();
                                if (aisc != null)
                                {
                                    if (station != null)
                                    {

                                        if ((aisc.station.HasDocking(false) && !DockingUI.inst.dockingPanel.activeSelf) &&
                                          (GameData.data.gameMode == 1 || aisc.station.IsPlayerFriendly || aisc.station.Disabled) &&
                                          (aisc == null || (aisc.ss != null && !aisc.ss.ffSys.TargetIsEnemy(__instance.GetSpaceShip.ffSys))))
                                        {
                                            float distNew = Vector3.Distance(__instance.GetSpaceShip.transform.position, aisc.gameObject.transform.position);
                                            if (distNew < distCur)
                                            {
                                                station = aisc.station;
                                                distCur = distNew;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        station = aisc.station;
                                        distCur = Vector3.Distance(__instance.GetSpaceShip.transform.position, aisc.gameObject.transform.position);
                                    }
                                }
                            }
                        }
                    }

                    if (station != null)
                        DockingUI.inst.station = station;

                    __instance.dockingUI.StartDockingStation(0);
                    FleetControl.instance.DockFleet(forceDock: false);
                }
                else
                {
                    ShipInfo si = (ShipInfo)AccessTools.Field(typeof(PlayerControl), "shipInfo").GetValue(__instance);
                    AccessTools.Method(typeof(ShipInfo), nameof(ShipInfo.OpenClose)).Invoke(si, null);
                }
            }
            return false;
        }
    }
}
