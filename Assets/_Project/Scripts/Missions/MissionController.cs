using System;
using System.Collections.Generic;
using DBP.Core;
using DBP.Quiz;
using DBP.Upgrades;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DBP.Missions
{
    /// Luồng một màn (T29, T43, T50, T51, T58; BR-32, BR-33):
    /// kích hoạt mục tiêu theo thứ tự → thắng khi xong hết, thua khi hết máu, bỏ dở khi người chơi thoát.
    /// Kết quả chỉ chốt một lần; thắng thì mở khóa màn sau.
    public class MissionController : MonoBehaviour
    {
        public static MissionController Current { get; private set; }
        public static event Action<Attempt> Ended;

        [SerializeField] string missionId = "M1";
        [SerializeField] string missionTitle = "Mở màn Him Lam";
        [Tooltip("Màn được mở khi thắng (BR-32). Trống = màn cuối.")]
        [SerializeField] string nextMissionId = "M2";
        [SerializeField] string nextSceneName = "M2_GiuDocLap";
        [SerializeField] Health player;
        [Tooltip("Mục tiêu bắt buộc. Sequential: làm lần lượt; không: làm theo thứ tự bất kỳ.")]
        [SerializeField] List<Objective> objectives = new List<Objective>();
        [SerializeField] bool sequential = true;
        [Header("Súng rơi từ địch (BR-14) — chốt khi cân bằng")]
        [Range(0f, 1f)] [SerializeField] float enemyDropChance = 0.08f;
        [SerializeField] int maxEnemyDrops = 3;

        public UnityEvent onWon = new UnityEvent();
        public UnityEvent onLost = new UnityEvent();

        public Attempt Attempt { get; private set; }
        public string MissionId => missionId;
        public string Title => missionTitle;
        public string NextSceneName => nextSceneName;
        public IReadOnlyList<Objective> Objectives => objectives;
        public bool PlayerDead => player && player.IsDead;
        public Health Player => player;

        public Objective CurrentObjective
        {
            get
            {
                foreach (var o in objectives) if (o && !o.Done) return o;
                return null;
            }
        }

        public void Setup(string id, string title, string nextId, string nextScene, Health playerHealth, List<Objective> list, bool inOrder)
        {
            missionId = id;
            missionTitle = title;
            nextMissionId = nextId;
            nextSceneName = nextScene;
            player = playerHealth;
            objectives = list;
            sequential = inOrder;
        }

        void Awake()
        {
            Current = this;
            Time.timeScale = 1f;
            InputLock.ResetAll();
            Attempt = new Attempt(missionId, Guid.NewGuid().GetHashCode()); // lượt mới, seed mới (BR-33)
        }

        void OnEnable()
        {
            Health.AnyDied += OnAnyDied;
            FieldQuizSession.Answered += OnAnswered;
            FieldQuizSession.QuestionShortage += OnShortage;
        }

        void OnDisable()
        {
            Health.AnyDied -= OnAnyDied;
            FieldQuizSession.Answered -= OnAnswered;
            FieldQuizSession.QuestionShortage -= OnShortage;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void Start()
        {
            if (!player) Debug.LogError($"[Mission {missionId}] Chưa gán người chơi.");
            Attempt.BattleStart = Time.realtimeSinceStartupAsDouble;
            if (!sequential)
                foreach (var o in objectives) o.Activate(this);
        }

        void Update()
        {
            if (Attempt.Outcome != MissionOutcome.None) return;
            var outcome = MissionRules.Evaluate(PlayerDead, EvaluateObjectives());
            if (outcome != MissionOutcome.None) End(outcome);
        }

        bool EvaluateObjectives()
        {
            bool all = true;
            foreach (var o in objectives)
            {
                if (!o) continue;
                if (!o.Active) o.Activate(this);
                if (o.Evaluate()) continue;
                all = false;
                if (sequential) break;
            }
            return all;
        }

        public float EnemyDropChance => enemyDropChance;
        public int MaxEnemyDrops => maxEnemyDrops;

        /// Mỗi địch bị hạ chỉ xét rơi súng một lần; đạt giới hạn của màn thì không sinh thêm (BR-14).
        public bool TryRollDrop()
        {
            if (Attempt.Outcome != MissionOutcome.None || Attempt.WeaponDrops >= maxEnemyDrops) return false;
            if (Attempt.Rng.NextDouble() >= enemyDropChance) return false;
            Attempt.WeaponDrops++;
            return true;
        }

        /// Chủ động kết thúc trước khi thắng (menu tạm dừng): bỏ dở, không lên bảng (BR-32, BR-33).
        public void Abandon() => End(MissionOutcome.Abandoned);

        void End(MissionOutcome outcome)
        {
            if (!Attempt.Settle(outcome)) return;
            Attempt.BattleEnd = Time.realtimeSinceStartupAsDouble;
            if (player) Attempt.DamageTaken = player.TotalDamageTaken;

            if (outcome == MissionOutcome.Won)
            {
                if (!ProfileService.OnMissionWon(nextMissionId)) Attempt.SaveFailed = true;
                onWon.Invoke();
            }
            else if (outcome == MissionOutcome.Lost) onLost.Invoke();

            Ended?.Invoke(Attempt);
        }

        /// Chơi lại = lượt mới hoàn toàn (BR-33).
        // ponytail: nạp lại scene màn; khi có hầm chuẩn bị (T25) thì chuyển về scene hầm của màn.
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadNext()
        {
            Time.timeScale = 1f;
            if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
        }

        public bool PlayerInside(Collider zone)
        {
            if (!player) return false;
            var p = player.transform.position + Vector3.up * 0.5f;
            return zone.bounds.Contains(p);
        }

        void OnAnyDied(Health h)
        {
            if (Attempt.Outcome != MissionOutcome.None) return;
            switch (h.Kind)
            {
                case TargetKind.Infantry:
                case TargetKind.MachineGunNest:
                case TargetKind.Tank:
                    Attempt.Kills++;
                    break;
                case TargetKind.Bunker:
                    Attempt.BunkersDestroyed++;
                    break;
            }
        }

        void OnAnswered(QuizResult r) => Attempt.Answers.Add(r);
        void OnShortage(int level) => Attempt.RankEligible = false;
    }
}
