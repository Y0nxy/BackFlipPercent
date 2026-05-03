using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace BackFlipPercent
{
    [BepInPlugin("YonDev.BackFlipPercent", "BackFlip%", "1.0.0")]
    public class BackFlipPercent : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        public static ConfigEntry<bool> enableFlipPatch;
        public static ConfigEntry<float> SuccessChance;
        public static ConfigEntry<KeyCode> BackFlipKey;
        public static ConfigEntry<bool> UseRPC;
        private static readonly MethodInfo playEmoteMethod = AccessTools.Method(typeof(CharacterAnimations), "PlayEmote");
        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            enableFlipPatch = Config.Bind("hi Mordz", "Enable", true,
               new ConfigDescription("Enable or disable the backflip patch"));
            SuccessChance = Config.Bind("hi Mordz", "Backflip Success", 50f,
               new ConfigDescription("Probability of backflip succeeding (0 - 100)", new AcceptableValueRange<float>(0f, 100f)));
            BackFlipKey = Config.Bind("hi Mordz", "Backflip Key", KeyCode.T,
               new ConfigDescription("Key to perform a backflip"));
            UseRPC = Config.Bind("hi Mordz", "Use RPC Directly for Backflip", false,
               new ConfigDescription("Use animation method instead of RPC "));
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(BackFlipPatch));
        }
        void Update()
        {
            if (GUIManager.instance != null && GUIManager.instance.windowBlockingInput) return; //no keypress when typing in chat or using menus
            var anims = Character.localCharacter?.refs?.animations;
            if (anims == null) return;

            if (enableFlipPatch.Value && Input.GetKeyDown(BackFlipKey.Value))
            {
                Character localCharacter = Character.localCharacter;
                //Logger.LogInfo("Backflip key pressed");
                if (UseRPC.Value)
                {
                    localCharacter.refs.view.RPC("RPCA_PlayRemove", RpcTarget.All, new object[] { "A_Scout_Emote_BackFlip", GetSucceeded()});
                    return;
                }
                
                if (playEmoteMethod != null)
                {
                    playEmoteMethod.Invoke(anims, new object[] { "A_Scout_Emote_BackFlip" });
                }
            }
        }
        static class BackFlipPatch
        {
            [HarmonyPatch(typeof(CharacterAnimations), "PlayEmote")]
            [HarmonyPrefix]
            private static bool Prefix(CharacterAnimations __instance, string emoteName)
            {
                //Logger.LogInfo($"Attempting to play emote: {emoteName}");
                if (emoteName != "A_Scout_Emote_BackFlip") return true; //only patch backflip emote
                if (!enableFlipPatch.Value) return true;
                Character character = Traverse.Create(__instance).Field<Character>("character").Value;
                character.refs.view.RPC("RPCA_PlayRemove", RpcTarget.All, new object[] { emoteName, GetSucceeded() });
                return false;
            }
        }
        static bool GetSucceeded()
        {
            bool succeeded = UnityEngine.Random.value * 100f > SuccessChance.Value;
            Logger.LogInfo($"Backflip {(!succeeded ? "succeeded" : "failed")} with chance {SuccessChance.Value}%");
            return succeeded;
        }
    }
}
