using DBP.Core;
using DBP.Interaction;
using DBP.Missions;
using DBP.Player;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DBP.DebugTools
{
    // ponytail: HUD + menu tạm dừng + màn kết quả bằng IMGUI để chơi thử từng màn.
    // HUD thật T35, màn kết quả T36, menu T27 (Long) thay thế; FPS (T63) giữ lại cho bản Development.
    // Esc: tạm dừng · F9 (Editor/Development): nổ mục tiêu đang nhắm, thay bộc phá tới khi có T15/T30.
    public class DebugOverlay : MonoBehaviour
    {
        FirstPersonController body;
        WeaponController weapons;
        Interactor interactor;
        Health health;
        bool paused;
        float fps;
        GUIStyle big;

        void Start()
        {
            body = FindAnyObjectByType<FirstPersonController>();
            if (body)
            {
                weapons = body.GetComponentInChildren<WeaponController>();
                interactor = body.GetComponent<Interactor>();
                health = body.GetComponent<Health>();
            }
            MissionController.Ended += OnEnded;
        }

        void OnDestroy() => MissionController.Ended -= OnEnded;

        void OnEnded(Attempt attempt)
        {
            if (paused) SetPaused(false);
            InputLock.Push();
            FirstPersonController.UnlockCursor();
        }

        void Update()
        {
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            var mission = MissionController.Current;
            bool ended = mission && mission.Attempt.Outcome != MissionOutcome.None;

            // BR-10: không cho tạm dừng đồng hồ câu ngoài trận bằng menu.
            if (keyboard.escapeKey.wasPressedThisFrame && !WorldFreeze.IsFrozen && !ended) SetPaused(!paused);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (keyboard.f9Key.wasPressedThisFrame && !InputLock.Locked) BlastAimedTarget();
#endif
        }

        void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            Time.timeScale = paused ? 0f : 1f;
            if (paused) { InputLock.Push(); FirstPersonController.UnlockCursor(); }
            else { InputLock.Pop(); FirstPersonController.LockCursor(); }
        }

        void BlastAimedTarget()
        {
            var cam = Camera.main;
            if (cam && Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 300f, ~0, QueryTriggerInteraction.Ignore))
                hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(99999f, DamageType.Explosive, hit.point);
        }

        void OnGUI()
        {
            big ??= new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
            var mission = MissionController.Current;

            GUILayout.BeginArea(new Rect(12, 12, 460, 400));
            GUILayout.Label($"FPS {fps:0}");
            if (health) GUILayout.Label($"Máu {health.Current:0}/{health.Max:0}");
            if (body) GUILayout.Label($"Thể lực {body.Stamina.Current:0}/{body.Stamina.Max:0}{(body.Stamina.Exhausted ? " (kiệt sức)" : "")}");
            var w = weapons ? weapons.Inventory.Current : null;
            if (w != null)
                GUILayout.Label($"{w.Def.name}  {w.InMag}/{w.Reserve}{(weapons.IsReloading ? "  đang thay đạn…" : "")}  · Lựu đạn {weapons.Inventory.Grenades}/{weapons.Inventory.GrenadeCapacity}");
            if (mission)
            {
                var o = mission.CurrentObjective;
                GUILayout.Label($"[{mission.MissionId}] {mission.Title}");
                if (o) GUILayout.Label($"▶ {o.Title}  {o.Progress}", big);
            }
            GUILayout.EndArea();

            if (interactor && interactor.Focus != null && !InputLock.Locked)
            {
                var text = interactor.Focus.Prompt;
                if (interactor.HoldProgress01 > 0f) text += $"  {interactor.HoldProgress01 * 100f:0}%";
                GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 30, 300, 30), text, big);
            }

            if (paused) PauseMenu(mission);
            if (mission && mission.Attempt.Outcome != MissionOutcome.None) ResultPanel(mission);
        }

        void PauseMenu(MissionController mission)
        {
            var r = new Rect(Screen.width / 2f - 140, Screen.height / 2f - 90, 280, 180);
            GUI.Box(r, "Tạm dừng");
            GUILayout.BeginArea(new Rect(r.x + 20, r.y + 30, r.width - 40, r.height - 40));
            if (GUILayout.Button("Tiếp tục", GUILayout.Height(36))) SetPaused(false);
            if (mission && GUILayout.Button("Chơi lại (lượt mới)", GUILayout.Height(36))) mission.Restart();
            if (mission && GUILayout.Button("Bỏ dở màn", GUILayout.Height(36))) mission.Abandon();
            GUILayout.EndArea();
        }

        void ResultPanel(MissionController mission)
        {
            var a = mission.Attempt;
            string title = a.Outcome switch
            {
                MissionOutcome.Won => "Hoàn thành nhiệm vụ",
                MissionOutcome.Lost => "Hi sinh — nhiệm vụ thất bại",
                _ => "Đã bỏ dở màn",
            };
            int correct = 0;
            foreach (var ans in a.Answers) if (ans.outcome == Quiz.QuizOutcome.Correct) correct++;

            var r = new Rect(Screen.width / 2f - 200, Screen.height / 2f - 160, 400, 320);
            GUI.Box(r, "");
            GUILayout.BeginArea(new Rect(r.x + 20, r.y + 16, r.width - 40, r.height - 32));
            GUILayout.Label(title, big);
            GUILayout.Label($"Địch bị hạ: {a.Kills}   Lô cốt phá: {a.BunkersDestroyed}   Đợt đẩy lùi: {a.WavesRepelled}");
            GUILayout.Label($"Sát thương nhận: {a.DamageTaken:0}   Thời gian: {a.CompletionSeconds:0.0}s");
            GUILayout.Label($"Câu ngoài trận đúng: {correct}/{a.Answers.Count}{(a.RankEligible ? "" : "  (lượt lỗi câu hỏi: không xếp hạng)")}");
            if (a.SaveFailed) GUILayout.Label("⚠ Chưa lưu được tiến độ (lỗi ghi đĩa)");
            GUILayout.Space(10);
            if (GUILayout.Button("Chơi lại", GUILayout.Height(36))) mission.Restart();
            if (a.Outcome == MissionOutcome.Won && !string.IsNullOrEmpty(mission.NextSceneName)
                && GUILayout.Button("Màn tiếp theo", GUILayout.Height(36))) mission.LoadNext();
            GUILayout.EndArea();
        }
    }
}
