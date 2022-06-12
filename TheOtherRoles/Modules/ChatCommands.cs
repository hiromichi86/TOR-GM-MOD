using System;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using UnhollowerBaseLib;
using static TheOtherRoles.TheOtherRoles;

namespace TheOtherRoles.Modules {
    [HarmonyPatch]
    public static class ChatCommands {

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
        private static class SendChatPatch {
            static bool Prefix(ChatController __instance) {
                string text = __instance.TextArea.text;
                bool handled = false;
                if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) {
                    if (text.ToLower().StartsWith("/kick ")) {
                        string playerName = text.Substring(6);
                        PlayerControl target = PlayerControl.AllPlayerControls.ToArray().ToList().FirstOrDefault(x => x.Data.PlayerName.Equals(playerName));
                        if (target != null && AmongUsClient.Instance != null && AmongUsClient.Instance.CanBan()) {
                            var client = AmongUsClient.Instance.GetClient(target.OwnerId);
                            if (client != null) {
                                AmongUsClient.Instance.KickPlayer(client.Id, false);
                                handled = true;
                            }
                        }
                    } else if (text.ToLower().StartsWith("/ban ")) {
                        string playerName = text.Substring(5);
                        PlayerControl target = PlayerControl.AllPlayerControls.ToArray().ToList().FirstOrDefault(x => x.Data.PlayerName.Equals(playerName));
                        if (target != null && AmongUsClient.Instance != null && AmongUsClient.Instance.CanBan()) {
                            var client = AmongUsClient.Instance.GetClient(target.OwnerId);
                            if (client != null) {
                                AmongUsClient.Instance.KickPlayer(client.Id, true);
                                handled = true;
                            }
                        }
                    }
                }

                if (AmongUsClient.Instance.GameMode == GameModes.FreePlay) {
                    if (text.ToLower().Equals("/murder")) {
                        PlayerControl.LocalPlayer.Exiled();
                        HudManager.Instance.KillOverlay.ShowKillAnimation(PlayerControl.LocalPlayer.Data, PlayerControl.LocalPlayer.Data);
                        handled = true;
                    } else if (text.ToLower().StartsWith("/color ")) {
                        handled = true;
                        int col;
                        if (!Int32.TryParse(text.Substring(7), out col)) {
                            __instance.AddChat(PlayerControl.LocalPlayer, "Unable to parse color id\nUsage: /color {id}");
                        }
                        col = Math.Clamp(col, 0, Palette.PlayerColors.Length - 1);
                        PlayerControl.LocalPlayer.SetColor(col);
                        __instance.AddChat(PlayerControl.LocalPlayer, "Changed color succesfully"); ;
                    } else if (text.ToLower().StartsWith("/playerinfo")) {
                        foreach (var p in PlayerControl.AllPlayerControls)
                        {
                            TheOtherRolesPlugin.Logger.LogInfo(JsonUtility.ToJson(p, true));
                        }
                    } else if (text.ToLower().StartsWith("/setakujo ")) {
                        TheOtherRolesPlugin.Logger.LogInfo(AmongUsClient.Instance.GameState.ToString());
                        TheOtherRolesPlugin.Logger.LogInfo(text.ToLower());
                        string playerId = text.Substring(10);
                        TheOtherRolesPlugin.Logger.LogInfo(String.Format("playerId: {0}", playerId));
                        foreach (var p in PlayerControl.AllPlayerControls)
                        {
                            TheOtherRolesPlugin.Logger.LogInfo(String.Format("p.PlayerId: {0}", p.PlayerId.ToString()));
                            if (p.PlayerId.ToString() == playerId)
                            {
                                TheOtherRolesPlugin.Logger.LogInfo(String.Format("setRole playerId: {0}", playerId));
                                TheOtherRolesPlugin.Logger.LogInfo(String.Format("Akujo time limit: {0}", Akujo.timeLimit));
                                TheOtherRolesPlugin.Logger.LogInfo(String.Format("Akujo knows roles: {0}", Akujo.knowsRoles.ToString()));
                                TheOtherRolesPlugin.Logger.LogInfo(String.Format("Akujo num keeps: {0}", Akujo.numKeeps.ToString()));
                                PlayerControl.LocalPlayer.eraseAllRoles();
                                p.setRole(RoleType.Akujo);
                                //p.RpcSetRole(RoleTypes.Impostor);
                            }
                        }
                    } else if (text.ToLower().StartsWith("/sethonmei "))
                    {
                        TheOtherRolesPlugin.Logger.LogInfo(text.ToLower());
                        var list = text.Split(" ");
                        string akujoId = list[1];
                        string targetId = list[2];
                        TheOtherRolesPlugin.Logger.LogInfo(String.Format("playerId: {0}, targetId: {1}", akujoId, targetId));
                        foreach (var p in PlayerControl.AllPlayerControls)
                        {
                            TheOtherRolesPlugin.Logger.LogInfo(String.Format("p.PlayerId: {0}", p.PlayerId.ToString()));
                            if (p.PlayerId.ToString() == akujoId)
                            {
                                TheOtherRolesPlugin.Logger.LogInfo(String.Format("setHonmei AkujoId: {0}", akujoId));
                                Akujo akujo = Akujo.getRole(p);
                                foreach(var p2 in PlayerControl.AllPlayerControls)
                                {
                                    if(p.PlayerId.ToString() == targetId)
                                    {
                                        akujo.setHonmei(p2);
                                    }
                                }
                            }
                        }
                    } else if (text.ToLower().StartsWith("/setkeep "))
                    {
                        TheOtherRolesPlugin.Logger.LogInfo(text.ToLower());
                        string playerId = text.Substring(9);
                        TheOtherRolesPlugin.Logger.LogInfo(String.Format("playerId: {0}", playerId));
                        foreach (var p in PlayerControl.AllPlayerControls)
                        {
                            TheOtherRolesPlugin.Logger.LogInfo(String.Format("p.PlayerId: {0}", p.PlayerId.ToString()));
                            if (p.PlayerId.ToString() == playerId)
                            {
                                TheOtherRolesPlugin.Logger.LogInfo(String.Format("setHonmei AkujoId: {0}", playerId));
                                Akujo akujo = Akujo.getRole(p);
                                akujo.setKeep(PlayerControl.LocalPlayer);
                            }
                        }
                    } else if (text.ToLower().StartsWith("/setcrew "))
                    {
                        TheOtherRolesPlugin.Logger.LogInfo(text.ToLower());
                        string playerId = text.Substring(9);
                        TheOtherRolesPlugin.Logger.LogInfo(String.Format("playerId: {0}", playerId));
                        foreach (var p in PlayerControl.AllPlayerControls)
                        {
                            TheOtherRolesPlugin.Logger.LogInfo(String.Format("p.PlayerId: {0}", p.PlayerId.ToString()));
                            if (p.PlayerId.ToString() == playerId)
                            {
                                TheOtherRolesPlugin.Logger.LogInfo(String.Format("setCrewmate playerId: {0}", playerId));
                                p.RpcSetRole(RoleTypes.Crewmate);
                            }
                        }
                    }
                    else if (text.ToLower().StartsWith("/setengi "))
                    {
                        TheOtherRolesPlugin.Logger.LogInfo(text.ToLower());
                        string playerId = text.Substring(9);
                        TheOtherRolesPlugin.Logger.LogInfo(String.Format("playerId: {0}", playerId));
                        foreach (var p in PlayerControl.AllPlayerControls)
                        {
                            TheOtherRolesPlugin.Logger.LogInfo(String.Format("p.PlayerId: {0}", p.PlayerId.ToString()));
                            if (p.PlayerId.ToString() == playerId)
                            {
                                TheOtherRolesPlugin.Logger.LogInfo(String.Format("setEngineer playerId: {0}", playerId));
                                p.RpcSetRole(RoleTypes.Engineer);
                            }
                        }
                    }
                    else if(text.ToLower().StartsWith("/admin"))
                    {
                        TheOtherRolesPlugin.Logger.LogInfo(text.ToLower());
                        DestroyableSingleton<HudManager>.Instance.ShowMap((System.Action<MapBehaviour>)(m => m.ShowCountOverlay()));
                        //if (!MapBehaviour.Instance || !MapBehaviour.Instance.isActiveAndEnabled)
                        //    DestroyableSingleton<HudManager>.Instance.ShowMap((System.Action<MapBehaviour>)(m => m.ShowCountOverlay()));

                        //PlayerControl.LocalPlayer.moveable = false;
                        PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement 
                    }
                    else if(text.ToLower().StartsWith("/vitals"))
                    {
                        var e = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault(x => x.gameObject.name.Contains("panel_vitals"));
                        EvilHacker.vitals = UnityEngine.Object.Instantiate(e.MinigamePrefab, Camera.main.transform, false);
                        EvilHacker.vitals.transform.SetParent(Camera.main.transform, false);
                        EvilHacker.vitals.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                        EvilHacker.vitals.Begin(null);
                        PlayerControl.LocalPlayer.moveable = false;
                        PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement 
                    }
                }

                if (text.ToLower().StartsWith("/tp ") && PlayerControl.LocalPlayer.Data.IsDead) {
                    string playerName = text.Substring(4).ToLower();
                    PlayerControl target = PlayerControl.AllPlayerControls.ToArray().ToList().FirstOrDefault(x => x.Data.PlayerName.ToLower().Equals(playerName));
                    if (target != null) {
                        PlayerControl.LocalPlayer.transform.position = target.transform.position;
                        handled = true;
                    }
                }

                if (handled) {
                    __instance.TextArea.Clear();
                    __instance.quickChatMenu.ResetGlyphs();
                }
                return !handled;
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public static class EnableChat {
            public static void Postfix(HudManager __instance) {
                if (__instance?.Chat?.isActiveAndEnabled == false && (AmongUsClient.Instance?.GameMode == GameModes.FreePlay || (PlayerControl.LocalPlayer.isLovers() && Lovers.enableChat)))
                    __instance?.Chat?.SetVisible(true);
            }
        }

        [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
        public static class SetBubbleName { 
            public static void Postfix(ChatBubble __instance, [HarmonyArgument(0)] string playerName) {
                PlayerControl sourcePlayer = PlayerControl.AllPlayerControls.ToArray().ToList().FirstOrDefault(x => x.Data.PlayerName.Equals(playerName));
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data.Role.IsImpostor && Spy.spy != null && sourcePlayer.PlayerId == Spy.spy.PlayerId && __instance != null) __instance.NameText.color = Palette.ImpostorRed;
            }
        }

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
        public static class AddChatPatch {
            public static bool Prefix(ChatController __instance, [HarmonyArgument(0)] PlayerControl sourcePlayer) {
                if (__instance != DestroyableSingleton<HudManager>.Instance.Chat)
                    return true;
                PlayerControl localPlayer = PlayerControl.LocalPlayer;
                return localPlayer == null ||
                    (MeetingHud.Instance != null || LobbyBehaviour.Instance != null ||
                    localPlayer.isDead() || localPlayer.PlayerId == sourcePlayer.PlayerId ||
                    (Lovers.enableChat && localPlayer.getPartner() == sourcePlayer));
            }
        }
    }
}
