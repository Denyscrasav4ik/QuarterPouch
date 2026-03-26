using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.SaveSystem;
using UnityEngine;
using static QuarterPouch.PouchIOWriter;

namespace QuarterPouch
{
    [BepInPlugin("denyscrasav4ik.basicallyukrainian.quarterpouch", "Quarter Pouch", "1.0.3")]
    public class QuarterPouchPlugin : BaseUnityPlugin
    {
        public static QuarterPouchPlugin Instance;

        public static ConfigEntry<int> QuarterSizeLimit;

        public static Dictionary<string, double> savedPouches = new Dictionary<string, double>();

        public static event Action<PouchManager> InitializePouches;

        public static void CallPouchInit(PouchManager pm)
        {
            InitializePouches?.Invoke(pm);
        }

        void Awake()
        {
            Instance = this;

            QuarterSizeLimit = Config.Bind(
                "General",
                "QuarterPouchSize",
                4,
                new ConfigDescription(
                    "Maximum number of quarters the pouch can hold",
                    new AcceptableValueRange<int>(1, 30)
                )
            );

            InitializePouches += InitQuarterPouch;

            Harmony harmony = new Harmony("denyscrasav4ik.basicallyukrainian.quarterpouch");
            harmony.PatchAll();

            ModdedSaveGame.AddSaveHandler(new PouchIOWriter());

        }

        void InitQuarterPouch(PouchManager pouchM)
        {
            pouchM.Add(new QuarterPouch());
        }
    }

    public static class Extensions
    {
        public static PouchManager GetPouchManager(this PlayerManager me)
        {
            if (me == null) return null;

            PouchManager pouchM = Singleton<CoreGameManager>.Instance
                .gameObject
                .GetComponents<PouchManager>()
                .Where(x => x.playerIndex == me.playerNumber)
                .FirstOrDefault();

            if (pouchM == null)
            {
                pouchM = Singleton<CoreGameManager>.Instance.gameObject.AddComponent<PouchManager>();
                pouchM.playerIndex = me.playerNumber;
            }

            return pouchM;
        }
    }


    public class PouchManager : MonoBehaviour
    {
        public int playerIndex = -1;

        public Pouch[] Pouches => pouches.ToArray();

        private List<Pouch> pouches = new List<Pouch>();

        public void Backup()
        {
            if (playerIndex > 0)
                throw new NotImplementedException("More players than the first not implemented!");

            Singleton<PouchManagerSaveStorage>.Instance.storedValues.Clear();

            foreach (Pouch p in pouches)
            {
                Singleton<PouchManagerSaveStorage>.Instance.storedValues.Add(p.id, p.amount);
            }
        }

        public void ReloadBackup()
        {
            if (playerIndex > 0)
                throw new NotImplementedException("More players than the first not implemented!");

            foreach (KeyValuePair<string, double> pvd in Singleton<PouchManagerSaveStorage>.Instance.storedValues)
            {
                pouches.Find(x => x.id == pvd.Key).ResetAmountTo(pvd.Value);
            }
        }

        public void Add(Pouch p)
        {
            pouches.Add(p);

            if (!Singleton<PouchManagerSaveStorage>.Instance)
                return;

            if (Singleton<PouchManagerSaveStorage>.Instance.storedValues.ContainsKey(p.id))
            {
                p.ResetAmountTo(Singleton<PouchManagerSaveStorage>.Instance.storedValues[p.id]);
                return;
            }

            Singleton<PouchManagerSaveStorage>.Instance.storedValues.Add(p.id, p.amount);
        }
    }
}
