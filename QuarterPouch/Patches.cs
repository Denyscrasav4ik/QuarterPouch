using HarmonyLib;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;

namespace QuarterPouch
{
    [HarmonyPatch(typeof(CoreGameManager), "SpawnPlayers")]
    public class SpawnPlayerPatch
    {
        static void Postfix(PlayerManager[] ___players)
        {
            for (int i = 0; i < ___players.Length; i++)
            {
                PouchManager pouchM = ___players[i].GetPouchManager();

                if (pouchM != null && pouchM.Pouches.Length == 0)
                {
                    QuarterPouchPlugin.CallPouchInit(pouchM);
                }
            }
        }
    }

    [HarmonyPatch(typeof(CoreGameManager), "DestroyPlayers")]
    public class DestroyPlayersPatch
    {
        static void Postfix(CoreGameManager __instance)
        {
            foreach (PouchManager p in __instance.gameObject.GetComponents<PouchManager>())
            {
                UnityEngine.Object.Destroy(p);
            }
        }
    }

    [HarmonyPatch(typeof(ItemManager), "UseItem")]
    public class UseItemPatch
    {
        static FieldInfo audUse = AccessTools.DeclaredField(typeof(ITM_Acceptable), "audUse");
        static MethodInfo UpdateSelect = AccessTools.DeclaredMethod(typeof(ItemManager), "UpdateSelect");

        static bool Prefix(ItemManager __instance, PlayerManager ___pm, ItemObject[] ___items, int ___selectedItem)
        {
            RaycastHit hit;

            if (Physics.Raycast(
                ___pm.transform.position,
                Singleton<CoreGameManager>.Instance.GetCamera(___pm.playerNumber).transform.forward,
                out hit,
                ___pm.pc.reach,
                ___pm.pc.ClickLayers))
            {
                foreach (IItemAcceptor component in hit.transform.GetComponents<IItemAcceptor>())
                {
                    var pouchManager = ___pm.GetPouchManager();
                    if (pouchManager == null) return true;

                    foreach (Pouch p in pouchManager.Pouches)
                    {
                        for (int i = 0; i < p.actingItems.Length; i++)
                        {
                            if (component.ItemFits(p.actingItems[i]))
                            {
                                if (___items[___selectedItem].itemType == p.actingItems[i])
                                    return true;

                                if (p.Spend(p.actingItems[i]))
                                {
                                    Item itmF = ItemMetaStorage.Instance.FindByEnum(p.actingItems[i]).value.item;

                                    if (itmF is ITM_Acceptable itm)
                                    {
                                        Singleton<CoreGameManager>.Instance.audMan.PlaySingle(
                                            (SoundObject)audUse.GetValue(itm));
                                    }

                                    UpdateSelect.Invoke(__instance, null);
                                    component.InsertItem(___pm, ___pm.ec);

                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CoreGameManager), "BackupPlayers")]
    public class BackupPatch
    {
        static void Postfix(CoreGameManager __instance, PlayerManager[] ___players)
        {
            for (int i = 0; i < __instance.setPlayers; i++)
            {
                ___players[i].GetPouchManager()?.Backup();
            }
        }
    }

    [HarmonyPatch(typeof(BaseGameManager), "LoadNextLevel")]
    public class LoadNextLevelPatch
    {
        static void Prefix()
        {
            Singleton<CoreGameManager>.Instance
                .GetPlayer(0)
                .GetPouchManager()?
                .Backup();
        }
    }

    [HarmonyPatch(typeof(CoreGameManager), "RestorePlayers")]
    public class RestorePatch
    {
        static void Postfix(CoreGameManager __instance, PlayerManager[] ___players)
        {
            __instance.StartCoroutine(RestoreCoroutine(__instance, ___players));
        }

        static IEnumerator RestoreCoroutine(CoreGameManager __instance, PlayerManager[] ___players)
        {
            yield return null;

            for (int i = 0; i < __instance.setPlayers; i++)
            {
                ___players[i].GetPouchManager()?.ReloadBackup();
            }
        }
    }

    [HarmonyPatch(typeof(BaseGameManager), "BeginPlay")]
    public class BeginPlayPatch
    {
        static MethodInfo UpdateSelect = AccessTools.DeclaredMethod(typeof(ItemManager), "UpdateSelect");

        static void Prefix()
        {
            UpdateSelect.Invoke(
                Singleton<CoreGameManager>.Instance.GetPlayer(0).itm,
                null
            );
        }
    }

    [HarmonyPatch(typeof(ItemManager), "AddItem", new Type[] { typeof(ItemObject), typeof(Pickup) })]
    public class AddItemPickupPatch
    {
        static MethodInfo UpdateSelect = AccessTools.DeclaredMethod(typeof(ItemManager), "UpdateSelect");

        static bool Prefix(ItemManager __instance, ItemObject item, Pickup pickup, PlayerManager ___pm, ref bool __result)
        {
            var pouchManager = ___pm.GetPouchManager();
            if (pouchManager == null) return true;

            foreach (Pouch pouch in pouchManager.Pouches)
            {
                if (pouch.itemConversionRates.TryGetValue(item.itemType, out double value))
                {
                    if (pouch.CanFit(item.itemType))
                    {
                        pouch.AddAmount(value);

                        UpdateSelect.Invoke(__instance, null);

                        __result = true;
                        return false;
                    }
                    else
                    {
                        pickup.AssignItem(item);
                        __result = false;
                        return false;
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ItemManager), "AddItem", new Type[] { typeof(ItemObject) })]
    public class AddItemPatch
    {
        static MethodInfo UpdateSelect = AccessTools.DeclaredMethod(typeof(ItemManager), "UpdateSelect");

        static bool Prefix(ItemManager __instance, ItemObject item, PlayerManager ___pm)
        {
            var pouchManager = ___pm.GetPouchManager();
            if (pouchManager == null) return true;

            foreach (Pouch pouch in pouchManager.Pouches)
            {
                if (pouch.itemConversionRates.TryGetValue(item.itemType, out double value))
                {
                    if (pouch.CanFit(item.itemType))
                    {
                        pouch.AddAmount(value);

                        UpdateSelect.Invoke(__instance, null);

                        return false;
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(HudManager), "SetItemSelect")]
    public class ItemSelectHudPatch
    {
        static void Postfix(TMP_Text ___itemTitle)
        {
            if (___itemTitle == null) return;

            var pouchM = Singleton<CoreGameManager>.Instance
                .GetPlayer(0)
                .GetPouchManager();

            if (pouchM == null) return;

            foreach (Pouch p in pouchM.Pouches)
            {
                ___itemTitle.text += "\n" + p.DisplayString();
            }
        }
    }
}
