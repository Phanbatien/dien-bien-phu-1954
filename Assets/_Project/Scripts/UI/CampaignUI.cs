using System;
using System.Linq;
using DBP.Core;
using DBP.Interaction;
using DBP.Missions;
using DBP.Player;
using DBP.Quiz;
using DBP.Ranking;
using DBP.Upgrades;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using W = DBP.UI.UIWidgets;
namespace DBP.UI
{
    public enum CampaignScreen { Menu, CreateProfile, Preparation, Tutorial, Gameplay, Pause, Results, Leaderboard, Story, Credits, Settings, BunkerView }

    /// Runtime Canvas UI leaves authored mission scenes and shared player prefabs intact.
    public sealed partial class CampaignUI : MonoBehaviour
    {
        public static CampaignUI Current { get; private set; }
        public static readonly string[] Scenes = { "M1_HimLam", "M2_GiuDocLap", "M3_DoiED", "M4_LongHao", "M5_ChieuMungBay", "M6_ThuThach" };
        static readonly string[] Titles = { "Mở màn Him Lam", "Giữ Độc Lập", "Đêm đồi E, D", "Trong lòng hào", "Chiều mùng Bảy", "Thử thách vô hạn" };
        static readonly string[] BranchNames = { "CƠ SỐ ĐẠN", "THAY ĐẠN", "THỂ LỰC", "LỰU ĐẠN" };
        static int pendingPreparation = -1;
        public CampaignScreen ScreenState { get; private set; }
        public int SelectedMission { get; private set; }
        public string LastError { get; private set; }
        Transform canvas, page;
        Text status, healthText, staminaText, ammoText, objectiveText, interactionText, scoreText;
        Image healthFill, staminaFill;
        MissionController mission;
        FirstPersonController player;
        WeaponController weapons;
        Interactor interactor;
        bool ownsLock, loading;
        int profilePage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            Current = null; pendingPreparation = -1;
            SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneLoaded += OnSceneLoaded;
        }
        // Entering Play from an already-open Editor scene must also create the UI,
        // independently of how Unity reports the initial sceneLoaded event.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallInitialScene() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            if (scene.name == "MainMenu" || Array.IndexOf(Scenes, scene.name) >= 0)
                if (!FindAnyObjectByType<CampaignUI>()) new GameObject("CampaignUI").AddComponent<CampaignUI>();
        }
        void Start()
        {
            Current = this;
            var go = new GameObject("CampaignCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false); canvas = go.transform;
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (!FindAnyObjectByType<EventSystem>())
            {
                var events = new GameObject("UIEvents", typeof(EventSystem), typeof(InputSystemUIInputModule)); events.transform.SetParent(transform);
            }
            mission = MissionController.Current;
            player = FindAnyObjectByType<FirstPersonController>(); weapons = FindAnyObjectByType<WeaponController>(); interactor = FindAnyObjectByType<Interactor>();
            MissionController.Ended += OnEnded;
            try
            {
                AudioListener.volume = ProfileService.Data.volume;
                if (mission) Show(mission.MissionId == "M1" ? CampaignScreen.Tutorial : CampaignScreen.Gameplay);
                else if (pendingPreparation >= 0 && ProfileService.HasProfile)
                { SelectedMission = pendingPreparation; pendingPreparation = -1; Show(CampaignScreen.Preparation); }
                else Show(CampaignScreen.Menu);
            }
            catch (Exception e) { ShowStorageError(e); }
        }
        void OnDestroy()
        {
            MissionController.Ended -= OnEnded;
            if (ownsLock) { InputLock.Pop(); Time.timeScale = 1f; }
            if (Current == this) Current = null;
        }
        void LateUpdate()
        {
            if (ScreenState != CampaignScreen.Gameplay) FirstPersonController.UnlockCursor();
            if (ScreenState == CampaignScreen.Gameplay) RefreshHud();
            TickStory();
            if (mission && mission.Attempt.Outcome == MissionOutcome.None && !WorldFreeze.IsFrozen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (ScreenState == CampaignScreen.Gameplay) Show(CampaignScreen.Pause);
                else if (ScreenState == CampaignScreen.Pause) Show(CampaignScreen.Gameplay);
            }
        }
        void ClearPage()
        {
            if (page) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            page = W.Rect(canvas, "Page", 0, 0, 1600, 900);
            var r = (RectTransform)page; r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f,.5f); r.anchoredPosition = Vector2.zero;
            status = null;
        }
        void Show(CampaignScreen screen)
        {
            ScreenState = screen;
            if (mission) mission.SetRankingExclusion("tutorial",screen == CampaignScreen.Tutorial);
            bool lockNow = mission && screen != CampaignScreen.Gameplay;
            if (lockNow && !ownsLock) { InputLock.Push(); ownsLock = true; Time.timeScale = 0; }
            if (!lockNow && ownsLock)
            {
                InputLock.Pop(); ownsLock = false; Time.timeScale = 1;
            }
            if (screen == CampaignScreen.Gameplay && !InputLock.Locked) FirstPersonController.LockCursor(); else FirstPersonController.UnlockCursor();
            ClearPage();
            // Hầm chữ A sau màn chuẩn bị: nền mờ để thấy ánh đèn dầu, bản đồ, điện đài
            bool inBunker = (screen == CampaignScreen.Preparation || screen == CampaignScreen.BunkerView) && !mission;
            if (inBunker) BunkerBackdrop.Show(); else BunkerBackdrop.Hide();
            if (screen != CampaignScreen.Gameplay)
            {
                var shade = W.Panel(page, "Backdrop", -2000, -2000, 5600, 4900, mission ? new Color(.055f,.075f,.065f,.96f) : inBunker && BunkerBackdrop.Available ? new Color(.05f, .06f, .05f, screen == CampaignScreen.BunkerView ? 0f : .28f) : W.Ink);
                shade.transform.SetAsFirstSibling();
                W.Label(page, "Eyebrow", "ĐIỆN BIÊN PHỦ 1954     /     QUYẾT CHIẾN • QUYẾT THẮNG", 64, 28, 1450, 40, 18, W.Gold);
                W.Panel(page, "Rule", 64, 88, 1472, 2, W.Gold);
                status = W.Label(page, "Status", "", 64, 838, 1470, 45, 21, W.Gold);
            }
            switch (screen)
            {
                case CampaignScreen.Menu: Menu(); break;
                case CampaignScreen.CreateProfile: CreateProfile(); break;
                case CampaignScreen.Preparation: Preparation(); break;
                case CampaignScreen.Tutorial: Tutorial(); break;
                case CampaignScreen.Gameplay: Hud(); break;
                case CampaignScreen.Pause: Pause(); break;
                case CampaignScreen.Results: Results(); break;
                case CampaignScreen.Leaderboard: LeaderboardPage(); break;
                case CampaignScreen.Story: StoryPage(); break;
                case CampaignScreen.Credits: CreditsPage(); break;
                case CampaignScreen.Settings: SettingsPage(); break;
                case CampaignScreen.BunkerView: BunkerViewPage(); break;
            }
        }
        void Guard(Action action)
        {
            try { LastError = null; action(); }
            catch (Exception e) { LastError = e.Message; if (status) status.text = "Không thể lưu thay đổi: " + e.Message; }
        }
        void ShowStorageError(Exception e)
        {
            ClearPage(); W.Panel(page, "Backdrop", 0, 0, 1600, 900, W.Ink);
            W.Label(page, "StorageError", "Không thể đọc hồ sơ\n" + e.Message + "\nDữ liệu gốc vẫn được giữ nguyên.", 100, 200, 1400, 350, 28);
            FirstPersonController.UnlockCursor(); LastError = e.Message;
        }
        void Menu()
        {
            W.Label(page, "Title", "Tiếp bước lịch sử", 64, 108, 1200, 76, 48);
            W.Label(page, "Subtitle", "Chọn hồ sơ và nhiệm vụ. Tiến độ được lưu riêng trên máy này.", 64, 188, 1400, 40, 24);
            W.Panel(page, "ProfilesPanel", 64, 254, 490, 560);
            W.Label(page, "ProfilesHeading", "HỒ SƠ TRÊN MÁY", 88, 268, 440, 46, 24, W.Gold);
            var profiles = ProfileService.Repository.Profiles;
            profilePage = Mathf.Clamp(profilePage, 0, Math.Max(0, (profiles.Count - 1) / 4));
            for (int i = profilePage * 4; i < Math.Min(profiles.Count, profilePage * 4 + 4); i++)
            {
                var p = profiles[i]; bool active = ProfileService.HasProfile && p.id == ProfileService.Data.id;
                W.Button(page, "Profile_" + p.id, (active ? "●  " : "") + ShortLabel(p.displayName, 18) + "\n" + ShortLabel(p.className, 12) + "  ·  #" + p.id.Substring(0, 6), 88, 330 + (i % 4) * 78, 442, 68,
                    () => Guard(() => { ProfileService.Repository.Select(p.id); AudioListener.volume = p.volume; Show(CampaignScreen.Menu); }));
                // Tên dài toàn chữ rộng (W, M) vẫn nằm gọn 2 dòng trong nút
                var profileLabel = page.Find("Profile_" + p.id).GetComponentInChildren<Text>();
                profileLabel.fontSize = 19; profileLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            if (profiles.Count == 0) W.Label(page, "NoProfiles", "Chưa có hồ sơ.\nTạo hồ sơ để bắt đầu chiến dịch.", 88, 340, 420, 120, 26);
            if (profiles.Count > 4)
            {
                W.Button(page, "PrevProfiles", "←", 88, 656, 80, 44, () => { profilePage--; Show(CampaignScreen.Menu); }, profilePage > 0);
                W.Button(page, "NextProfiles", "→", 450, 656, 80, 44, () => { profilePage++; Show(CampaignScreen.Menu); }, (profilePage + 1) * 4 < profiles.Count);
            }
            W.Button(page, "NewProfile", "+ Tạo hồ sơ mới", 88, 730, 442, 58, () => Show(CampaignScreen.CreateProfile));
            W.Label(page, "MissionsHeading", "CHỌN NHIỆM VỤ", 604, 260, 570, 48, 24, W.Gold);
            W.Button(page,"OpenLeaderboard","Bảng xếp hạng",1190,260,346,48,()=>OpenLeaderboard(false));
            for (int i = 0; i < 5; i++)
            {
                int index = i; bool unlocked = ProfileService.HasProfile && ProfileService.IsUnlocked("M" + (i + 1));
                W.Button(page, "Mission" + (i + 1), $"0{i + 1}    {Titles[i]}" + (unlocked ? "    →" : i == 0 ? "    [Chọn hồ sơ]" : $"    [Thắng màn {i} để mở]"),
                    604, 320 + i * 58, 932, 50, () => SelectMission(index), unlocked);
            }
            // Màn 6 ngoài chiến dịch: luôn mở khi đã có hồ sơ
            W.Button(page, "Mission6", $"∞    THỬ THÁCH VÔ HẠN  ·  kỷ lục {DBP.Missions.EndlessObjective.Best} địch    →",
                604, 612, 932, 46, () => SelectMission(5), ProfileService.HasProfile);
            if (ProfileService.HasProfile)
            {
                W.Toggle(page, "Consent", "Đồng ý gửi tên, lớp và thành tích lên bảng công khai (tùy chọn)", !string.IsNullOrEmpty(ProfileService.Data.consentAt), 604, 665, 920,
                    value => Guard(() => ProfileService.Repository.SetConsent(value)));
                W.Label(page, "VolumeLabel", "Âm lượng", 604, 742, 170, 44, 24);
                W.Volume(page, ProfileService.Data.volume, 786, 742, value => Guard(() => { ProfileService.Repository.SetVolume(value); AudioListener.volume = value; }));
                W.Label(page, "LocalOnly", "Không đồng ý vẫn chơi và lưu tại máy.", 604, 790, 900, 32, 20);
            }
            StoryMenuButtons();
            W.Button(page, "OpenSettings", "Cài đặt", 1190, 800, 346, 44, OpenSettings);
            W.Button(page, "ExitGame", "Thoát game (Exit)", 1190, 730, 346, 58, ExitGame);
            if (ProfileService.Repository.RecoveredBackup) status.text = "Đã đọc bản sao lưu hồ sơ sau lỗi lưu trước đó.";
        }
        static void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        static string ShortLabel(string value, int length) => string.IsNullOrEmpty(value) ? "" : value.Length <= length ? value : value.Substring(0, length - 1) + "…";
        void CreateProfile()
        {
            W.Label(page, "Title", "Hồ sơ mới", 260, 130, 1000, 80, 48);
            W.Label(page, "Hint", "Tên giống nhau vẫn là hai hồ sơ riêng biệt.", 260, 224, 1050, 44, 25);
            var name = W.Input(page, "DisplayName", "Tên hiển thị (bắt buộc)", 260, 316, 1080);
            var className = W.Input(page, "ClassName", "Tên lớp (không bắt buộc)", 260, 402, 1080);
            bool consent = false;
            W.Toggle(page, "NewConsent", "Tôi đồng ý gửi tên, lớp và thành tích lên bảng công khai.", false, 260, 494, 1080, value => consent = value);
            W.Label(page, "Privacy", "Tùy chọn. Bạn luôn có thể chơi ngoại tuyến và thay đổi lựa chọn này trong menu.", 260, 564, 1080, 70, 23);
            W.Button(page, "CancelCreate", "Quay lại", 260, 690, 360, 64, () => Show(CampaignScreen.Menu));
            var create = W.Button(page, "ConfirmCreate", "Tạo hồ sơ", 660, 690, 680, 64, () => Guard(() => { ProfileService.Repository.Create(name.text, className.text, consent); Show(CampaignScreen.Menu); }), false);
            name.onValueChanged.AddListener(value => create.interactable = !string.IsNullOrWhiteSpace(value));
        }
        public void SelectMission(int index)
        {
            if (loading || index < 0 || index >= Scenes.Length || !ProfileService.HasProfile || !ProfileService.IsUnlocked("M" + (index + 1))) return;
            SelectedMission = index; Show(CampaignScreen.Preparation);
        }
        void Preparation()
        {
            W.Label(page, "Title", "Hầm chuẩn bị", 64, 110, 1000, 76, 48);
            W.Label(page, "MissionTitle", $"MÀN {SelectedMission + 1}  /  {Titles[SelectedMission]}", 64, 192, 1080, 45, 25, W.Gold);
            W.Label(page, "Balance", $"QUYẾT TÂM\n{ProfileService.Data.resolve} điểm", 1180, 112, 356, 112, 30, W.Gold);
            W.Label(page, "ShopHint", "Nâng cấp dùng chung các màn • Áp dụng khi xuất trận • Mỗi cấp chỉ mua một lần", 64, 248, 1472, 50, 23);
            for (int b = 0; b < 4; b++)
            {
                var branch = (UpgradeBranch)b; int current = UpgradeService.Level(ProfileService.Data, branch); int next = current + 1;
                float x = 64 + b * 374;
                W.Panel(page, "UpgradeCard" + b, x, 328, 350, 356);
                W.Label(page, "Branch", BranchNames[b], x + 22, 346, 306, 52, 27, W.Gold);
                W.Label(page, "Level", $"Cấp hiện tại: {current} / 3", x + 22, 407, 306, 44, 24);
                string effect = b == 0 ? $"Đạn dự trữ +{Math.Min(next,3) * 10}%" : b == 1 ? $"Thay đạn ×{1f - Math.Min(next,3) * .1f:0.0}" : b == 2 ? $"Thể lực +{Math.Min(next,3) * 10}%" : $"Sức chứa +{Math.Min(next,3)} quả";
                W.Label(page, "Effect", effect, x + 22, 458, 306, 60, 24);
                string id = next <= 3 ? UpgradeCatalog.Id(branch, next) : "";
                int cost = next * 100; bool canBuy = next <= 3 && ProfileService.Data.resolve >= cost;
                W.Label(page, "Condition", current == 3 ? "Đã đạt cấp tối đa" : next == 1 ? "Không cần cấp trước" : $"Cần cấp {current}: đã có", x + 22, 532, 306, 40, 20);
                W.Button(page, "Buy" + b, current == 3 ? "ĐÃ ĐỦ 3 CẤP" : $"MUA CẤP {next} • {cost} ĐIỂM", x + 22, 600, 306, 60, () => Purchase(id), canBuy);
                if (!canBuy && current < 3) W.Label(page, "Shortfall", $"Thiếu {cost - ProfileService.Data.resolve} điểm", x + 22, 568, 306, 30, 18, W.Gold);
            }
            W.Label(page, "CrateHint", "Câu hỏi nằm trong các hòm phát sáng giữa trận: trả lời đúng nhận súng + Quyết Tâm (cộng khi thắng màn).", 64, 704, 1472, 30, 20, W.Gold);
            if (BunkerBackdrop.Available) W.Button(page, "ViewBunker", "Xem Hầm chữ A", 1180, 236, 356, 46, () => Show(CampaignScreen.BunkerView));
            W.Button(page, "BackToMenu", "← Chọn nhiệm vụ", 64, 740, 440, 66, () => { briefing = null; Show(CampaignScreen.Menu); });
            W.Button(page, "Deploy", "XUẤT TRẬN →", 1096, 740, 440, 66, Deploy);
        }
        public void Purchase(string upgradeId)
        {
            if (ScreenState != CampaignScreen.Preparation || loading || mission) return;
            Guard(() => { var result = ProfileService.Buy(upgradeId, true); Show(CampaignScreen.Preparation); status.text = result == BuyResult.Ok ? "Đã mua và lưu nâng cấp." : "Không mua: " + result; });
        }
        public void Deploy()
        {
            if (ScreenState != CampaignScreen.Preparation || loading || !ProfileService.HasProfile || !ProfileService.IsUnlocked("M" + (SelectedMission + 1))) return;
            // Không còn họp tác chiến bắt buộc (sửa 28/10): câu hỏi chỉ nằm trong hòm giữa trận
            if (BriefingReady) briefing.Commit();
            briefing = null;
            loading = SceneTransition.Load(Scenes[SelectedMission]);
        }
        void Tutorial()
        {
            W.Label(page, "Title", "Trước giờ xuất trận", 200, 126, 1200, 80, 48);
            string[] rows = { "W A S D     Di chuyển   ·   Chuột: nhìn xung quanh", "CHUỘT TRÁI     Bắn   ·   CHUỘT PHẢI: ngắm", "E     Giữ 1 giây: mở hòm tiếp tế (hòm đạn có quầng sáng)", "R  Thay đạn  ·  1 / 2: đổi súng  ·  V: đâm dao  ·  G: lựu đạn", "SHIFT  Chạy  ·  CTRL / C: cúi  ·  SPACE: nhảy  ·  W/S: trèo thang  ·  B: độ sáng  ·  T: góc nhìn 1 / 3" };
            for (int i = 0; i < rows.Length; i++) W.Label(page, "Control" + i, rows[i], 200, 242 + i * 68, 1200, 58, 28);
            W.Label(page, "CrateColors", "Hòm có cột sáng — màu là độ khó: xanh lá = dễ · xanh dương = vừa · tím = khó · vàng = rất khó. Đúng: súng + Quyết Tâm.", 200, 576, 1200, 26, 18, W.Gold);
            W.Label(page, "TutorialHint", "Theo mục tiêu trên HUD để tiến quân. ESC mở tạm dừng và hướng dẫn.\nKhi trả lời câu hỏi, chiến trường dừng nhưng đồng hồ câu hỏi vẫn chạy.", 200, 602, 1200, 100, 25, W.Gold);
            W.Button(page, "BeginMission", "ĐÃ RÕ • BẮT ĐẦU", 200, 736, 1200, 68, () => Show(CampaignScreen.Gameplay));
            if (mission && mission.MissionId == "M1")
                W.Button(page, "ReadDiary", "Đọc nhật ký trong hầm", 1060, 126, 340, 56, () => PlayStory(Diary, () => Show(CampaignScreen.Tutorial)));
        }
        void Hud()
        {
            W.Panel(page, "MissionPanel", 36, 28, 910, 156, new Color(.06f,.09f,.07f,.87f));
            W.Label(page, "MissionHeading", mission.Title.ToUpperInvariant(), 58, 40, 866, 42, 23, W.Gold);
            objectiveText = W.Label(page, "ObjectiveText", "", 58, 86, 866, 86, 25);
            W.Panel(page, "ScorePanel", 1160, 28, 404, 102, new Color(.06f,.09f,.07f,.87f));
            scoreText = W.Label(page, "ScoreText", "", 1182, 40, 360, 76, 22);
            W.Panel(page, "StatsPanel", 36, 716, 540, 150, new Color(.06f,.09f,.07f,.87f));
            healthText = W.Label(page, "HealthText", "", 58, 725, 490, 44, 25);
            healthFill = W.Meter(page, "HealthMeter", 58, 773, 490, new Color32(174,83,71,255));
            staminaText = W.Label(page, "StaminaText", "", 58, 789, 490, 40, 23);
            staminaFill = W.Meter(page, "StaminaMeter", 58, 842, 490, W.Gold);
            W.Panel(page, "AmmoPanel", 1096, 728, 468, 138, new Color(.06f,.09f,.07f,.87f));
            ammoText = W.Label(page, "AmmoText", "", 1120, 739, 420, 112, 27);
            var cross = W.Label(page, "Crosshair", "+", 780, 430, 40, 40, 30); cross.alignment = TextAnchor.MiddleCenter;
            interactionText = W.Label(page, "InteractionText", "", 500, 540, 600, 80, 25, W.Gold); interactionText.alignment = TextAnchor.MiddleCenter;
            W.Label(page, "PauseHint", "ESC  Tạm dừng / Hướng dẫn", 596, 836, 490, 34, 20);
            RefreshHud();
        }
        void RefreshHud()
        {
            if (!mission || !healthText) return;
            var hp = mission.Player; var stamina = player ? player.Stamina : null;
            healthText.text = hp ? $"MÁU   {Mathf.CeilToInt(hp.Current)} / {hp.Max:0}" : "MÁU   —";
            healthFill.rectTransform.sizeDelta = new Vector2(hp ? 490 * Mathf.Clamp01(hp.Current / hp.Max) : 0, 7);
            staminaText.text = stamina != null ? $"THỂ LỰC   {stamina.Current:0} / {stamina.Max:0}" + (stamina.Exhausted ? "  • Kiệt sức" : "") : "THỂ LỰC   —";
            staminaFill.rectTransform.sizeDelta = new Vector2(stamina != null ? 490 * Mathf.Clamp01(stamina.Current / stamina.Max) : 0, 7);
            var weapon = weapons ? weapons.Inventory.Current : null;
            ammoText.text = weapon == null ? "Chưa có vũ khí" : $"{weapon.Def.name}\n{weapon.InMag} / {weapon.Reserve}" + (weapons.IsReloading ? "   Đang thay đạn…" : "") + $"   •   Lựu đạn {weapons.Inventory.Grenades}";
            int remaining = mission.Objectives.Count(o => o && !o.Done); var objective = mission.CurrentObjective;
            objectiveText.text = $"Còn {remaining} mục tiêu" + (objective ? $"   /   {objective.Title}\n{objective.Progress}" : "   /   Đã hoàn thành");
            scoreText.text = $"KIẾN THỨC  {KnowledgeScore.Total(mission.Attempt.Answers)}\nQUYẾT TÂM  {ProfileService.Data.resolve}";
            interactionText.text = !WorldFreeze.IsFrozen && interactor && interactor.Focus != null ? interactor.Focus.Prompt + (interactor.HoldProgress01 > 0 ? $"  {interactor.HoldProgress01:P0}" : "") : "";
        }
        void Pause()
        {
            W.Label(page, "Title", "Tạm dừng", 200, 135, 1200, 80, 48);
            W.Label(page, "Help", "WASD: đi • Chuột: nhìn / bắn / ngắm • V: đâm dao • G: lựu đạn\nGiữ E: tương tác • R: thay đạn • 1 / 2: đổi súng • B: độ sáng • T: góc nhìn 1 / 3\nSHIFT: chạy • CTRL / C: cúi • SPACE: nhảy • W/S khi đứng ở thang: trèo", 200, 250, 1200, 190, 28);
            W.Button(page, "Resume", "Tiếp tục", 200, 496, 1200, 66, () => Show(CampaignScreen.Gameplay));
            W.Button(page, "Restart", "Chơi lại từ hầm", 200, 592, 1200, 66, () => ReturnToMenu(true));
            W.Button(page, "QuitMission", "Bỏ dở và về menu", 200, 688, 1200, 66, () => ReturnToMenu(false));
            W.Button(page, "OpenSettingsPause", "Cài đặt âm thanh và nhạc nền", 200, 784, 1200, 56, OpenSettings);
        }
        void OnEnded(Attempt attempt) { if (mission) Show(CampaignScreen.Results); }
        void Results()
        {
            var attempt = mission.Attempt;
            W.Label(page, "Title", attempt.Outcome == MissionOutcome.Won ? "Hoàn thành nhiệm vụ" : attempt.Outcome == MissionOutcome.Lost ? "Nhiệm vụ thất bại" : "Đã bỏ dở nhiệm vụ", 160, 128, 1300, 80, 48);
            W.Label(page, "ResultMission", mission.Title, 160, 219, 1250, 52, 28, W.Gold);
            W.Panel(page, "ResultCard", 160, 302, 1280, 302);
            W.Button(page,"ResultLeaderboard","Bảng xếp hạng",1060,229,380,52,()=>OpenLeaderboard(true));
            W.Label(page, "KnowledgeResult", $"ĐIỂM KIẾN THỨC\n{KnowledgeScore.Total(attempt.Answers)}", 192, 324, 550, 116, 34, W.Gold);
            W.Label(page, "ResolveResult", $"QUYẾT TÂM HIỆN CÓ\n{ProfileService.Data.resolve}", 830, 324, 550, 116, 34, W.Gold);
            W.Label(page, "Stats", $"Hạ địch: {attempt.Kills}   •   Phá lô cốt: {attempt.BunkersDestroyed}   •   Đẩy lùi: {attempt.WavesRepelled} đợt\nSát thương nhận: {attempt.DamageTaken:0}   •   Thời gian: {Math.Max(0, attempt.CompletionSeconds):0.0}s\nTrả lời đúng: {attempt.Answers.Count(a => a.outcome == QuizOutcome.Correct)} / {attempt.Answers.Count}", 192, 454, 1210, 132, 25);
            bool unsaved = attempt.Outcome == MissionOutcome.Won && !string.IsNullOrEmpty(mission.ProgressSaveError);
            W.Label(page, "ResultNote", unsaved ? "Chưa lưu được tiến độ. Hãy thử lưu lại trước khi rời màn." : attempt.Outcome == MissionOutcome.Won ? "Đã lưu tiến độ mở màn. Điểm kiến thức không dùng để mua nâng cấp." : "Lượt này không nhận Quyết Tâm và không được xếp hạng.", 160, 628, unsaved ? 880 : 1280, 40, 21);
            if (unsaved) W.Button(page, "RetrySave", "Thử lưu lại", 1060, 630, 380, 42, () => { mission.TrySaveProgress(); Show(CampaignScreen.Results); });
            RankingResultStatus(attempt);
            if (attempt.Outcome == MissionOutcome.Won && mission.MissionId == "M5")
                W.Button(page, "Epilogue", "Kết thúc chiến dịch →", 1060, 150, 380, 62, () => PlayStory(Epilogue, () => Show(CampaignScreen.Credits)));
            W.Button(page, "Replay", "Chơi lại từ hầm", 160, 734, 600, 68, () => ReturnToMenu(true));
            W.Button(page, "ResultMenu", "Về menu / Chọn màn", 800, 734, 640, 68, () => ReturnToMenu(false));
        }
        public void ReturnToMenu(bool prepareAgain)
        {
            if (loading || !mission) return;
            loading = true;
            if (mission.Attempt.Outcome == MissionOutcome.None) mission.Abandon();
            pendingPreparation = prepareAgain ? Array.IndexOf(Scenes, SceneManager.GetActiveScene().name) : -1;
            if (!SceneTransition.Load("MainMenu")) loading = false;
        }
    }
}
