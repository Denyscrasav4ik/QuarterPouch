using BepInEx;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QuarterPouch
{
    public class PouchIOWriter : ModdedSaveGameIOBinary
    {
        public override PluginInfo pluginInfo => QuarterPouchPlugin.Instance.Info;

        public override void Load(BinaryReader reader)
        {
            QuarterPouchPlugin.savedPouches.Clear();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                QuarterPouchPlugin.savedPouches.Add(reader.ReadString(), reader.ReadDouble());
            }
        }

        public override void OnCGMCreated(CoreGameManager instance, bool isFromSavedGame)
        {
            PouchManagerSaveStorage stupidHack = instance.gameObject.AddComponent<PouchManagerSaveStorage>();
            if (!isFromSavedGame) return;
            foreach (KeyValuePair<string, double> kvp in QuarterPouchPlugin.savedPouches)
            {
                stupidHack.storedValues.Add(kvp.Key, kvp.Value);
            }
        }

        public override void Reset()
        {
            QuarterPouchPlugin.savedPouches.Clear();
        }

        public override void Save(BinaryWriter writer)
        {
            if (Singleton<PouchManagerSaveStorage>.Instance)
            {
                QuarterPouchPlugin.savedPouches.Clear();
                foreach (KeyValuePair<string, double> kvp in Singleton<PouchManagerSaveStorage>.Instance.storedValues)
                {
                    QuarterPouchPlugin.savedPouches.Add(kvp.Key, kvp.Value);
                }
            }

            writer.Write(QuarterPouchPlugin.savedPouches.Count);
            foreach (KeyValuePair<string, double> kvp in QuarterPouchPlugin.savedPouches)
            {
                writer.Write(kvp.Key);
                writer.Write((double)kvp.Value);
            }

        }

        public class PouchManagerSaveStorage : Singleton<PouchManagerSaveStorage>
        {
            public Dictionary<string, double> storedValues = new Dictionary<string, double>();
        }
    }
}
