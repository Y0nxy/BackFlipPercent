using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace BackFlipPercent
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class BackFlipPercent : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        //BackFlip Success
        public static ConfigEntry<float> SuccessChance;
        public static ConfigEntry<KeyCode> BackFlipKey;
        public static ConfigEntry<bool> enableFlipPatch;
        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            SuccessChance = Config.Bind("hi Mordz", "Backflip Success", 50f,
               new ConfigDescription("Probability of backflip succeeding (0 - 100)", new AcceptableValueRange<float>(0f, 100f)));
            BackFlipKey = Config.Bind("hi Mordz", "Backflip Key", KeyCode.T,
               new ConfigDescription("Key to perform a backflip"));
            enableFlipPatch = Config.Bind("hi Mordz", "Enable Flip Patch", true,
               new ConfigDescription("Enable or disable the backflip patch"));
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(BackFlipPercent));
        }
        void Update()
        {
            if (GUIManager.instance != null && GUIManager.instance.windowBlockingInput) return; //no keypress when typing in chat or using menus

            if (enableFlipPatch.Value && Input.GetKeyDown(BackFlipKey.Value))
            {
                Character localCharacter = Traverse.Create(this).Field<Character>("localCharacter").Value;
                if (localCharacter != null)
                {
                    localCharacter.refs.view.RPC("RPCA_PlayRemove", RpcTarget.All, new object[] { "A_Scout_Emote_BackFlip", SuccessChance.Value });
                }
            }
        }
        [HarmonyPatch(typeof(CharacterAnimations), "PlayEmote")]
        static class BackFlipPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(CharacterAnimations __instance, string emoteName)
            {
                if (emoteName != "A_Scout_Emote_BackFlip") return true; //only patch backflip emote
                Character character = Traverse.Create(__instance).Field<Character>("character").Value;
                character.refs.view.RPC("RPCA_PlayRemove", RpcTarget.All, new object[] { emoteName, SuccessChance.Value });
                return false;
            }
        }
    }
}
