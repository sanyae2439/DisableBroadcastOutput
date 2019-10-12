using System;
using System.Reflection;
using Smod2;
using Smod2.Attributes;
using Harmony;
using UnityEngine;
using UnityEngine.Networking;

namespace DisableBroadcastOutput
{
    [PluginDetails(
    author = "sanyae2439",
    name = "DisableBroadcastOutput",
    description = "Disable Output for broadcast",
    id = "sanyae2439.DisableBroadcastOutput",
    version = "1.0",
    SmodMajor = 3,
    SmodMinor = 5,
    SmodRevision = 0
    )]
    class DisableBroadcastOutput : Plugin
    {
        internal static DisableBroadcastOutput instance;
        internal static Type Servermod;
        internal static MethodInfo Debuglog;
        internal static FieldInfo conn;
        public override void OnDisable()
        {
            this.Info("DisableServerPresense Disabled...");
        }

        public override void OnEnable()
        {
            this.Info("DisableServerPresense Enabled!");
            instance = this;
        }

        public override void Register()
        {
            this.Info("Find types...");
            Assembly asm = Assembly.GetAssembly(typeof(ServerConsole));
            Servermod = asm.GetType("ServerMod");
            Debuglog = Servermod.GetMethod("DebugLog", BindingFlags.Static | BindingFlags.Public);
            conn = typeof(ServerMod2.API.SmodPlayer).GetField("conn", BindingFlags.NonPublic | BindingFlags.Instance);

            this.Info("Patching...");
            HarmonyInstance.Create(this.Details.id).PatchAll();
        }
    }

    [HarmonyPatch(typeof(ServerMod2.API.SmodMap), "Broadcast")]
    class BroadcastPatch
    {
        static bool Prefix(ServerMod2.API.SmodMap __instance, ref uint duration, ref string message, ref bool isMonoSpaced)
        {
            if(duration < 1u)
            {
                ServerConsole.AddLog("API Attempted to Broadcast with a non-positive duration.");
            }
            else
            {
                PlayerManager.localPlayer.GetComponent<Broadcast>().CallRpcAddElement(message, duration, isMonoSpaced);
                DisableBroadcastOutput.Debuglog.Invoke(null, new object[] { "Broadcast", "Broadcasted: " + message });
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ServerMod2.API.SmodMap), "ClearBroadcasts")]
    class ClearBroadcastsPatch
    {
        static bool Prefix(ServerMod2.API.SmodMap __instance)
        {
            PlayerManager.localPlayer.GetComponent<Broadcast>().CallRpcClearElements();
            DisableBroadcastOutput.Debuglog.Invoke(null, new object[] { "Broadcast", "Broadcasts cleared." });
            return false;
        }
    }

    [HarmonyPatch(typeof(ServerMod2.API.SmodPlayer), "PersonalBroadcast")]
    class PersonalBroadcastPatch
    {
        static bool Prefix(ServerMod2.API.SmodPlayer __instance, ref uint duration, ref string message, ref bool isMonoSpaced)
        {
            NetworkConnection conn = DisableBroadcastOutput.conn.GetValue(__instance) as NetworkConnection;
            if(conn != null)
            {
                PlayerManager.localPlayer.GetComponent<Broadcast>().CallTargetAddElement(conn, message, duration, isMonoSpaced);
                DisableBroadcastOutput.Debuglog.Invoke(null, new object[] { "Broadcast", "Broadcasted: " + message + " to: " + __instance.Name });
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ServerMod2.API.SmodPlayer), "PersonalClearBroadcasts")]
    class PersonalClearBroadcasts
    {
        static bool Prefix(ServerMod2.API.SmodPlayer __instance)
        {
            NetworkConnection conn = DisableBroadcastOutput.conn.GetValue(__instance) as NetworkConnection;
            if(conn != null)
            {
                PlayerManager.localPlayer.GetComponent<Broadcast>().CallTargetClearElements(conn);
                DisableBroadcastOutput.Debuglog.Invoke(null, new object[] { "Broadcast", "Cleared broadcasts for: " + __instance.Name });
            }
            return false;
        }
    }
}
