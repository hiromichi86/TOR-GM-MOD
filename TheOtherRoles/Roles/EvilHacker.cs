using HarmonyLib;
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
    public class EvilHacker : RoleBase<EvilHacker>
    {
        /// <summary>役職カラー</summary>
        public static Color color = Palette.ImpostorRed;
        /// <summary>アドミンボタン</summary>
        private static CustomButton adminButton;
        /// <summary>バイタルボタン</summary>
        private static CustomButton vitalButton;
        public static Minigame vitals = null;

        public EvilHacker()
        {
            RoleType = roleId = RoleType.EvilHacker;
        }

        public override void FixedUpdate() { }

        /// <summary>アドミンボタン</summary>
        private static Sprite buttonAdminSprite;
        /// <summary>アドミンボタン画像の取得</summary>
        /// <returns></returns>
        public static Sprite getAdminButtonSprite()
        {
            if (buttonAdminSprite) return buttonAdminSprite;
            byte mapId = PlayerControl.GameOptions.MapId;
            TheOtherRolesPlugin.Logger.LogInfo(String.Format("mapId: {0}", mapId));
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
               // Action OnClick
               () =>
               {
                   PlayerControl.LocalPlayer.NetTransform.Halt();
                   Action<MapBehaviour> tmpAction = (MapBehaviour m) => { m.ShowCountOverlay(); };
                   DestroyableSingleton<HudManager>.Instance.ShowMap(tmpAction);
                   if (PlayerControl.LocalPlayer.AmOwner)
                   {
                       PlayerControl.LocalPlayer.MyPhysics.inputHandler.enabled = true;
                       ConsoleJoystick.SetMode_Task();
                   }
               },
               // Func<bool> HasButton
               () => { return PlayerControl.LocalPlayer.isRole(RoleType.EvilHacker); },
               // Func<bool> CouldUse
               () => { return true; },
               // Action OnMeetingEnds
               () => { PlayerControl.LocalPlayer.moveable = true; },
               // Sprite Sprite
               EvilHacker.getAdminButtonSprite(),
               // Vector3 PositionOffset
               new Vector3(-1.8f, -0.06f, 0),
               // HudManager hudManager
               hm,
               // ActionButton? textTemplate
               null,
               // KeyCode? hotkey
               null,
               // bool HasEffect
               false,
               // float EffectDuration
               0f,
               // Action OnEffectEnds
               () =>
               {
                   if (MapBehaviour.Instance && MapBehaviour.Instance.isActiveAndEnabled) MapBehaviour.Instance.Close();
                   PlayerControl.LocalPlayer.moveable = true;
               },
               // bool mirror = false
               PlayerControl.GameOptions.MapId == 3,
               // string buttonText = null
               DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Admin)
               );

            // バイタルボタン
            vitalButton = new CustomButton(
               // Action OnClick
               () =>
               {
                   if(EvilHacker.vitals == null)
                   {
                       var e = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault(x => x.gameObject.name.Contains("panel_vitals"));
                       if (e == null || Camera.main == null) return;
                       EvilHacker.vitals = UnityEngine.Object.Instantiate(e.MinigamePrefab, Camera.main.transform, false);
                   }
                   EvilHacker.vitals.transform.SetParent(Camera.main.transform, false);
                   EvilHacker.vitals.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                   EvilHacker.vitals.Begin(null);
                   PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement 
               },
               // Func<bool> HasButton
               () => { return PlayerControl.LocalPlayer.isRole(RoleType.EvilHacker); },
               // Func<bool> CouldUse
               () =>
               { return true; },
               // Action OnMeetingEnds
               () =>
               { PlayerControl.LocalPlayer.moveable = true; },
               // Sprite Sprite
               EvilHacker.getVitalButtonSprite(),
               // Vector3 PositionOffset
               new Vector3(-2.7f, -0.06f, 0),
               // HudManager hudManager
               hm,
               // ActionButton? textTemplate
               null,
               // KeyCode? hotkey
               null,
               // bool HasEffect
               false,
               // float EffectDuration
               0f,
               // Action OnEffectEnds
               () =>
               {
                   if (MapBehaviour.Instance && MapBehaviour.Instance.isActiveAndEnabled) MapBehaviour.Instance.Close();
                   PlayerControl.LocalPlayer.moveable = true;
               },
               // bool mirror = false
               false,
               // string buttonText = null
               TranslationController.Instance.GetString(StringNames.VitalsLabel)
               );
        }

        /// <summary>能力ボタンのクールダウンタイム設定</summary>
        public static void SetButtonCooldowns()
        {
            adminButton.MaxTimer = 0f;
            vitalButton.MaxTimer = 0f;
        }
        public static void Clear()
        {
            players = new List<EvilHacker>();
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
