﻿using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;

namespace TheOtherRoles
{
    [HarmonyPatch]
    internal class EvilHacker : RoleBase<EvilHacker>
    {
        /// <summary>アドミンボタン</summary>
        private static CustomButton adminButton;
        /// <summary>役職カラー</summary>
        public static Color color = Palette.ImpostorRed;
        /// <summary>アドミンボタン</summary>
        private static CustomButton adminbutton;
        /// <summary>バイタルボタン</summary>
        private static CustomButton vitalButton;
        private static Minigame vitals = null;

        public EvilHacker()
        {
            RoleType = roleId = RoleType.EvilHacker;
        }

        public override void FixedUpdate()
        {
        }

        /// <summary>アドミンボタン</summary>
        private static Sprite buttonAdminSprite;
        /// <summary>アドミンボタン画像の取得</summary>
        /// <returns></returns>
        public static Sprite getAdminButtonSprite()
        {
            if (buttonAdminSprite) return buttonAdminSprite;
            byte mapId = PlayerControl.GameOptions.MapId;
            UseButtonSettings button = HudManager.Instance.UseButton.fastUseSettings[ImageNames.PolusAdminButton]; // Polus
            if (mapId == 0 || mapId == 3) button = HudManager.Instance.UseButton.fastUseSettings[ImageNames.AdminMapButton]; // Skeld
            else if (mapId == 1) button = HudManager.Instance.UseButton.fastUseSettings[ImageNames.MIRAAdminButton]; // Mira HQ
            else if (mapId == 4) button = HudManager.Instance.UseButton.fastUseSettings[ImageNames.AirshipAdminButton]; // Airship
            buttonAdminSprite = button.Image;
            return buttonAdminSprite;
        }

        /// <summary>バイタルボタン</summary>
        private static Sprite buttonVitalSprite;
        /// <summary>バイタルボタン画像の取得</summary>
        /// <returns></returns>
        public static Sprite getVitalButtonSprite()
        {
            if (buttonVitalSprite) return buttonVitalSprite;
            buttonVitalSprite = HudManager.Instance.UseButton.fastUseSettings[ImageNames.VitalsButton].Image;
            return buttonVitalSprite;
        }

        public static void MakeButtons(HudManager hm)
        {
            // アドミンボタン
            adminButton = new CustomButton(
               () =>
               {
                   if (!MapBehaviour.Instance || !MapBehaviour.Instance.isActiveAndEnabled)
                       DestroyableSingleton<HudManager>.Instance.ShowMap((System.Action<MapBehaviour>)(m => m.ShowCountOverlay()));

                   PlayerControl.LocalPlayer.moveable = false;
                   PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement 
                   //Hacker.chargesAdminTable--;
               },
               () => { return PlayerControl.LocalPlayer.isRole(RoleType.EvilHacker) && PlayerControl.LocalPlayer.isAlive(); },
               () =>
               {
                   return true;
                   //if (hackerAdminTableChargesText != null)
                   //    hackerAdminTableChargesText.text = hackerVitalsChargesText.text = String.Format(ModTranslation.getString("hackerChargesText"), Hacker.chargesAdminTable, Hacker.toolsNumber);
                   //return Hacker.chargesAdminTable > 0 && MapOptions.canUseAdmin; ;
               },
               () =>
               {
                   //hackerAdminTableButton.Timer = hackerAdminTableButton.MaxTimer;
                   //hackerAdminTableButton.isEffectActive = false;
                   //hackerAdminTableButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
               },
               getAdminButtonSprite(),
               new Vector3(-1.8f, -0.06f, 0),
                hm,
                hm.AbilityButton,
               KeyCode.Q,
               true,
               0f,
               () =>
               {
                   //hackerAdminTableButton.Timer = hackerAdminTableButton.MaxTimer;
                   //if (!hackerVitalsButton.isEffectActive) PlayerControl.LocalPlayer.moveable = true;
                   if (MapBehaviour.Instance && MapBehaviour.Instance.isActiveAndEnabled) MapBehaviour.Instance.Close();
               },
               PlayerControl.GameOptions.MapId == 3,
               DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Admin)
                );
            adminButton.buttonText = ModTranslation.getString("EvilHackerAdminText");

            // バイタルボタン
            vitalButton = new CustomButton(
               () =>
               {
                   var e = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault(x => x.gameObject.name.Contains("panel_vitals"));
                   if (e == null || Camera.main == null) return;
                   EvilHacker.vitals = UnityEngine.Object.Instantiate(e.MinigamePrefab, Camera.main.transform, false);
                   EvilHacker.vitals.transform.SetParent(Camera.main.transform, false);
                   EvilHacker.vitals.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                   EvilHacker.vitals.Begin(null);
                   PlayerControl.LocalPlayer.moveable = false;
                   PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement 
               },
               () => { return PlayerControl.LocalPlayer.isRole(RoleType.EvilHacker) && PlayerControl.LocalPlayer.isAlive(); },
               () =>
               {
                   return true;
                   //if (hackerAdminTableChargesText != null)
                   //    hackerAdminTableChargesText.text = hackerVitalsChargesText.text = String.Format(ModTranslation.getString("hackerChargesText"), Hacker.chargesAdminTable, Hacker.toolsNumber);
                   //return Hacker.chargesAdminTable > 0 && MapOptions.canUseAdmin; ;
               },
               () =>
               {
                   //hackerAdminTableButton.Timer = hackerAdminTableButton.MaxTimer;
                   //hackerAdminTableButton.isEffectActive = false;
                   //hackerAdminTableButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
               },
               getVitalButtonSprite(),
               new Vector3(-2.7f, -0.06f, 0),
                hm,
                hm.AbilityButton,
               KeyCode.Q,
               true,
               0f,
               () =>
               {
                   //hackerAdminTableButton.Timer = hackerAdminTableButton.MaxTimer;
                   //if (!hackerVitalsButton.isEffectActive) PlayerControl.LocalPlayer.moveable = true;
                   if (MapBehaviour.Instance && MapBehaviour.Instance.isActiveAndEnabled) MapBehaviour.Instance.Close();
               },
               PlayerControl.GameOptions.MapId == 3,
               DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Admin)
                );
            adminButton.buttonText = ModTranslation.getString("EvilHackerAdminText");
        }

        /// <summary>能力ボタンのクールダウンタイム設定</summary>
        public static void SetButtonCooldowns()
        {
            EvilHacker.adminbutton.MaxTimer = 0f;
            EvilHacker.vitalButton.MaxTimer = 0f;
        }
        /// <summary>アドミンボタン処理</summary>
        public void openAdmin()
        {

        }
        /// <summary>バイタルボタン処理</summary>
        public void openVital()
        {

        }

        /// <summary>
        /// キル時処理
        /// </summary>
        /// <param name="target"></param>
        public override void OnKill(PlayerControl target) { }
        /// <summary>
        /// 死亡時処理
        /// </summary>
        /// <param name="killer"></param>
        public override void OnDeath(PlayerControl killer = null) { }
        /// <summary>会議開始時処理</summary>
        public override void OnMeetingStart() { }
        /// <summary>会議終了時処理</summary>
        public override void OnMeetingEnd() { }
        /// <summary>
        /// 切断時処理
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }
    }
}
