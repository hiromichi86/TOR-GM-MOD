using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.GameHistory;
using static TheOtherRoles.Patches.PlayerControlFixedUpdatePatch;
using System;
using Hazel;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class Akujo : RoleBase<Akujo>
    {
        private static CustomButton honmeiButton;
        private static CustomButton keepButton;

        public static TMPro.TMP_Text timeLimitText;
        public static TMPro.TMP_Text numKeepsText;

        public static Color color = new Color32(232, 57, 185, byte.MaxValue);

        public static List<Color> iconColors = new List<Color>
            {
                Akujo.color,                   // pink
                new Color32(255, 165, 0, 255), // orange
                new Color32(255, 255, 0, 255), // yellow
                new Color32(0, 255, 0, 255),   // green
                new Color32(0, 0, 255, 255),   // blue
                new Color32(0, 255, 255, 255), // light blue
                new Color32(255, 0, 0, 255),   // red
            };

        /// <summary>設定 制限時間</summary>
        public static float timeLimit { get { return CustomOptionHolder.akujoTimeLimit.getFloat() + 10f; } }
        /// <summary>設定 悪女から役職が分かる</summary>
        public static bool knowsRoles { get { return CustomOptionHolder.akujoKnowsRoles.getBool(); } }
        /// <summary>設定 キープ作成可能人数</summary>
        public static int numKeeps { get { return Math.Min(Mathf.RoundToInt(CustomOptionHolder.akujoNumKeeps.getFloat()), PlayerControl.AllPlayerControls.Count - 2); } }

        public PlayerControl currentTarget;
        public AkujoHonmei honmei = null;
        public List<AkujoKeep> keeps = new List<AkujoKeep>();

        public DateTime startTime = DateTime.UtcNow;
        /// <summary>経過時間</summary>
        public int timeLeft { get { return (int)Math.Ceiling(timeLimit - (DateTime.UtcNow - local.startTime).TotalSeconds); } }
        public string timeString { 
            get 
            {
                return String.Format(ModTranslation.getString("timeRemaining"), TimeSpan.FromSeconds(local.timeLeft).ToString(@"mm\:ss"));
            }
        }
        /// <summary>残りキープ作成人数</summary>
        public int keepsLeft { get { return numKeeps - keeps.Count; } }
        /// <summary>本命の生存数（0：本命が居ない。1：本命が生存）</summary>
        public static int numAlive
        {
            get
            {
                int alive = 0;
                foreach (var p in players)
                {
                    if (p.player.isAlive() && p.honmei != null && p.honmei.player.isAlive())
                    {
                        alive++;
                    }
                }
                return alive;
            }
        }

        /// <summary>
        /// 悪女のアイコン（♥）カラー
        /// </summary>
        public Color iconColor;

        public Akujo()
        {
            RoleType = roleId = RoleType.Akujo;
            startTime = DateTime.UtcNow;
            honmei = null;
            keeps = new List<AkujoKeep>();
            iconColor = getAvailableColor();
        }
        /// <summary>会議開始時処理</summary>
        public override void OnMeetingStart() { }
        /// <summary>会議終了時処理</summary>
        public override void OnMeetingEnd() { }

        /// <summary>
        /// 悪女の情報更新処理
        /// </summary>
        public override void FixedUpdate()
        {
            // 更新対象がプレイヤーの場合
            if (player == PlayerControl.LocalPlayer)
            {
                if (timeLimitText != null)
                    timeLimitText.enabled = false;

                // プレイヤーが生存中
                if (player.isAlive())
                {
                    // 制限時間が残っていて、本命またはキープの指定が完了していない場合
                    if (timeLeft > 0 && (honmei == null || keepsLeft > 0))
                    {
                        // ターゲットにできないプレイヤーをリスト化
                        List<PlayerControl> untargetablePlayers = new List<PlayerControl>();
                        if (honmei != null) untargetablePlayers.Add(honmei.player);
                        untargetablePlayers.AddRange(keeps.Select(x => x.player));
                        // ターゲットとなるプレイヤーを取得
                        currentTarget = setTarget(untargetablePlayers: untargetablePlayers);
                        setPlayerOutline(currentTarget, Akujo.color);

                        if (timeLimitText != null)
                        {
                            // 制限時間と能力ボタンを表示
                            timeLimitText.text = timeString;
                            timeLimitText.enabled = Helpers.ShowButtons;
                        }
                    }
                    // 制限時間が残っておらず、本命またはキープの指定が完了していない場合
                    else if (timeLeft <= 0 && (honmei == null || keepsLeft > 0))
                    {
                        // 悪女の最終結果を「自殺」に設定
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.AkujoSuicide, Hazel.SendOption.Reliable, -1);
                        writer.Write(player.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        // 悪女の自殺処理呼び出し
                        RPCProcedure.akujoSuicide(player.PlayerId);
                    }
                }
            }
        }

        /// <summary>
        /// キル実施時の処理
        /// </summary>
        /// <param name="target"></param>
        public override void OnKill(PlayerControl target) { }
        /// <summary>
        /// 死亡時の処理
        /// </summary>
        /// <param name="killer">キラー</param>
        public override void OnDeath(PlayerControl killer = null)
        {
            // 本命が指定済みで生存している場合
            if (honmei != null && honmei.player.isAlive())
            {
                // キルまたは自殺の場合
                if (killer != null)
                    // 本命を自殺させる
                    honmei.player.MurderPlayer(honmei.player);
                // 追放の場合
                else
                    // 本命を追放させる
                    honmei.player.Exiled();
                // 本命プレイヤーの最終ステータスを「自殺」に設定する
                finalStatuses[honmei.player.PlayerId] = FinalStatus.Suicide;
            }
        }

        /// <summary>
        /// 悪女のプレイヤーが切断した場合
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason)
        {
            if (player == this.player)
            {
                if (honmei != null)
                    AkujoHonmei.eraseModifier(honmei.player);

                foreach (var keep in keeps)
                    AkujoKeep.eraseModifier(keep.player);
            }

            if (player == honmei?.player)
            {
                AkujoHonmei.eraseModifier(honmei.player);
                honmei = null;
            }

            foreach (var keep in keeps)
            {
                AkujoKeep.eraseModifier(keep.player);
            }
            keeps.Clear();
        }

        private static Sprite honmeiSprite;
        /// <summary>
        /// 本命ボタンの画像取得
        /// </summary>
        /// <returns>本命ボタンの画像</returns>
        public static Sprite getHonmeiSprite()
        {
            if (honmeiSprite) return honmeiSprite;
            honmeiSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.AkujoHonmeiButton.png", 115f);
            return honmeiSprite;
        }

        private static Sprite keepSprite;
        /// <summary>
        /// キープボタンの画像取得
        /// </summary>
        /// <returns>キープボタンの画像</returns>
        public static Sprite getKeepSprite()
        {
            if (keepSprite) return keepSprite;
            keepSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.AkujoKeepButton.png", 115f);
            return keepSprite;
        }

        /// <summary>
        /// 本命・キープボタンの作成
        /// </summary>
        /// <param name="hm"></param>
        public static void MakeButtons(HudManager hm)
        {
            // Honmei Button
            honmeiButton = new CustomButton(
                () =>
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.AkujoSetHonmei, Hazel.SendOption.Reliable, -1);
                    writer.Write(local.player.PlayerId);
                    writer.Write(local.currentTarget.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    local.setHonmei(local.currentTarget);
                },
                () => { return PlayerControl.LocalPlayer.isRole(RoleType.Akujo) && !PlayerControl.LocalPlayer.Data.IsDead && local.honmei == null && local.timeLeft > 0; },
                () => { return PlayerControl.LocalPlayer.isRole(RoleType.Akujo) && !PlayerControl.LocalPlayer.Data.IsDead && local.currentTarget != null && local.honmei == null && local.timeLeft > 0; },
                () => { honmeiButton.Timer = honmeiButton.MaxTimer; },
                getHonmeiSprite(),
                new Vector3(0f, 1.0f, 0),
                hm,
                hm.AbilityButton,
                KeyCode.F
            );
            honmeiButton.buttonText = ModTranslation.getString("AkujoHonmeiText");

            timeLimitText = GameObject.Instantiate(honmeiButton.actionButton.cooldownTimerText, hm.transform);
            timeLimitText.text = "";
            timeLimitText.enableWordWrapping = false;
            timeLimitText.transform.localScale = Vector3.one * 0.45f;
            timeLimitText.transform.localPosition = honmeiButton.actionButton.cooldownTimerText.transform.parent.localPosition + new Vector3(-0.1f, 0.35f, 0f);

            // Keep Button
            keepButton = new CustomButton(
                () =>
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.AkujoSetKeep, Hazel.SendOption.Reliable, -1);
                    writer.Write(local.player.PlayerId);
                    writer.Write(local.currentTarget.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    local.setKeep(local.currentTarget);
                },
                () => { return PlayerControl.LocalPlayer.isRole(RoleType.Akujo) && !PlayerControl.LocalPlayer.Data.IsDead && local.keepsLeft > 0 && local.timeLeft > 0; },
                () => {
                    if (numKeepsText != null)
                    {
                        if (local.keepsLeft > 0)
                            numKeepsText.text = String.Format(ModTranslation.getString("akujoKeepsLeft"), local.keepsLeft);
                        else
                            numKeepsText.text = "";
                    }
                    return PlayerControl.LocalPlayer.isRole(RoleType.Akujo) && !PlayerControl.LocalPlayer.Data.IsDead && local.currentTarget != null && local.keepsLeft > 0 && local.timeLeft > 0;
                },
                () => { keepButton.Timer = keepButton.MaxTimer; },
                getKeepSprite(),
                new Vector3(-0.9f, 1.0f, 0),
                hm,
                hm.AbilityButton,
                KeyCode.K
            );
            keepButton.buttonText = ModTranslation.getString("AkujoKeepText");

            numKeepsText = GameObject.Instantiate(keepButton.actionButton.cooldownTimerText, keepButton.actionButton.cooldownTimerText.transform.parent);
            numKeepsText.text = "";
            numKeepsText.enableWordWrapping = false;
            numKeepsText.transform.localScale = Vector3.one * 0.66f;
            numKeepsText.transform.localPosition += new Vector3(-0.05f, 0.73f, 0);
        }

        public static void SetButtonCooldowns()
        {
            honmeiButton.MaxTimer = 0f;
            keepButton.MaxTimer = 0f;
        }

        /// <summary>
        /// 本命の指定
        /// </summary>
        /// <param name="target">指定対象プレイヤー</param>
        public void setHonmei(PlayerControl target)
        {
            // 本命を指定済みの場合、処理を抜ける
            if (honmei != null) return;

            // ターゲットに「本命」の役割を追加する
            honmei = AkujoHonmei.addModifier(target);

            // 本命に対する悪女を設定する
            honmei.akujo = this;
        }
        /// <summary>
        /// キープの指定
        /// </summary>
        /// <param name="target">指定対象プレイヤー</param>
        public void setKeep(PlayerControl target)
        {
            // キープの指定残数が0の場合、処理を抜ける
            if (keepsLeft <= 0) return;

            // ターゲットに「キープ」の役割を追加する
            var keep = AkujoKeep.addModifier(target);

            // キープに対する悪女を設定する
            keep.akujo = this;
            // キープのリストに追加する
            keeps.Add(keep);
        }

        public static bool isPartner(PlayerControl player, PlayerControl partner)
        {
            Akujo akujo = getRole(player);
            if (akujo != null)
            {
                return akujo.isPartner(partner);
            }
            return false;
        }

        public bool isPartner(PlayerControl partner)
        {
            return honmei?.player == partner || keeps.Any(x => x.player == partner);
        }

        /// <summary>
        /// 利用可能な色の取得
        /// </summary>
        /// <returns>色情報</returns>
        public static Color getAvailableColor()
        {
            var availableColors = new List<Color>(iconColors);
            foreach (var akujo in players)
            {
                availableColors.RemoveAll(x => x == akujo.iconColor);
            }
            return availableColors.Count > 0 ? availableColors[0] : Akujo.color;
        }

        /// <summary>
        /// 表示名の修正
        /// </summary>
        /// <param name="nameText">名前の文字列</param>
        /// <returns></returns>
        public override string modifyNameText(string nameText)
        {
            return nameText + Helpers.cs(iconColor, " ♥");
            //return Helpers.cs(iconColor, String.Format("{0} ♥", nameText));
        }

        public override string meetingInfoText()
        {
            if (player.isAlive() && timeLeft > 0 && (honmei == null || keepsLeft > 0))
                return timeString;

            return "";
        }

        public static void Clear()
        {
            players = new List<Akujo>();
            AkujoHonmei.Clear();
            AkujoKeep.Clear();
        }
    }

    [HarmonyPatch]
    public class AkujoHonmei : ModifierBase<AkujoHonmei>
    {
        public Color color { get { return akujo != null ? akujo.iconColor : Akujo.color; } }
        public Akujo akujo = null;

        public AkujoHonmei()
        {
            ModType = modId = ModifierType.AkujoHonmei;

            persistRoleChange = new List<RoleType>() {
                RoleType.Sidekick,
                RoleType.Immoralist,
                RoleType.Shifter
            };
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd() { }
        public override void FixedUpdate() { }
        public override void OnKill(PlayerControl target) { }

        public override void OnDeath(PlayerControl killer = null)
        {
            // player.clearAllTasks();
            if (akujo != null && akujo.player.isAlive())
            {
                if (killer != null)
                    akujo.player.MurderPlayer(akujo.player);
                else
                    akujo.player.Exiled();
                finalStatuses[akujo.player.PlayerId] = FinalStatus.Suicide;
            }
        }

        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public override string modifyNameText(string nameText)
        {
            return nameText + Helpers.cs(color, " ♥");
        }

        public override string modifyRoleText(string roleText, List<RoleInfo> roleInfo, bool useColors = true, bool includeHidden = false)
        {
            if (includeHidden)
            {
                string name = $" {ModTranslation.getString("akujoHonmei")}";
                roleText += useColors ? Helpers.cs(color, name) : name;
            }
            return roleText;
        }

        public static void Clear()
        {
            players = new List<AkujoHonmei>();
        }
    }

    [HarmonyPatch]
    public class AkujoKeep : ModifierBase<AkujoKeep>
    {
        public Color color { get { return akujo != null ? akujo.iconColor : Akujo.color; } }
        public Akujo akujo = null;

        public AkujoKeep()
        {
            ModType = modId = ModifierType.AkujoKeep;

            persistRoleChange = new List<RoleType>() {
                RoleType.Sidekick,
                RoleType.Immoralist,
                RoleType.Shifter
            };
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd() { }
        public override void FixedUpdate() { }
        public override void OnKill(PlayerControl target) { }
        public override void OnDeath(PlayerControl killer = null) { }

        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public override string modifyNameText(string nameText)
        {
            return nameText + Helpers.cs(color, " ♥");
        }
        /// <summary>
        /// 役職名の修正
        /// </summary>
        /// <param name="roleText">役職文字列</param>
        /// <param name="roleInfo">役職情報</param>
        /// <param name="useColors">色を使用するか（true:使用する、false:使用しない）</param>
        /// <param name="includeHidden">役職を表示するか（true:表示する、false:表示しない）</param>
        /// <returns></returns>
        public override string modifyRoleText(string roleText, List<RoleInfo> roleInfo, bool useColors = true, bool includeHidden = false)
        {
            if (includeHidden)
            {
                string name = $" {ModTranslation.getString("akujoKeep")}";
                roleText += useColors ? Helpers.cs(color, name) : name;
            }
            return roleText;
        }

        public static void Clear()
        {
            players = new List<AkujoKeep>();
        }
    }
}