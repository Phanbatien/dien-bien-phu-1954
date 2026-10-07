# Kiến trúc code

Một assembly game (`DBP.Runtime`), một assembly Editor (`DBP.Editor`), hai assembly test.
Không có framework riêng: MonoBehaviour + vài lớp C# thuần để test được.

## Sơ đồ một lượt chơi

```
Scene màn (M1…M5)
├── GameSystems
│   ├── WorldFreeze ─────────── Freeze(): timeScale=0, khóa điều khiển, nền nâu xám, tắt tiếng
│   ├── FieldQuizSession ────── TryOpen(cấp, thời hạn, onResolved) → hỏi → chốt → Resume(0,5 s)
│   │     ├── IQuestionProvider   (Thiên Trí – T17 thay PlaceholderQuestionProvider)
│   │     └── IQuizView           (Thiên Trí – T18 thay DebugQuizView)
│   └── DebugOverlay ────────── HUD/menu/kết quả tạm (Long – T27, T35, T36 thay)
├── Player (prefab)
│   ├── FirstPersonController   đi, chạy, nhảy, bò, nhìn, thể lực, độ giật
│   ├── Health                  máu, tổng sát thương nhận (BR-24)
│   ├── Interactor              nhìn vào IInteractable, nhấn/giữ E
│   ├── UpgradeApplier          áp nâng cấp lúc xuất trận (BR-35)
│   └── MainCamera
│       └── WeaponController    bắn raycast, ADS, ống ngắm, thay đạn, đổi súng → Inventory
├── Mission
│   ├── MissionController       kích hoạt mục tiêu theo thứ tự, thắng/thua/bỏ dở, mở khóa màn
│   └── các Objective           DestroyTargets / ReachZone / RepelWaves / CaptureZone / Tutorial
├── Crates                      QuizCrate (giữ E 1 s → FieldQuizSession)
└── Enemies / Waves             hiện là hình nộm có Health (Đạt thay bằng AI)
```

## Hợp đồng giữa các phần (đừng đổi chữ ký nếu chưa báo nhóm)

| Hợp đồng | File | Ai dùng / ai cài |
|---|---|---|
| `IDamageable.TakeDamage(lượng, DamageType, điểm)` | `Core/Health.cs` | Súng, lựu đạn, pháo gọi; `Health` cài. `DamageType`: Bullet, Explosive, Artillery, AntiTank |
| `Health.Retreated` | `Core/Health.cs` | **AI của Đạt đặt `true` khi lính rút** → đợt phản kích tính là đẩy lùi (BR-20) |
| `Health.AnyDied` (static event) | `Core/Health.cs` | MissionController đếm địch hạ, lô cốt phá theo `TargetKind` |
| `IInteractable` (Prompt, HoldSeconds, CanInteract, Interact) | `Interaction/Interaction.cs` | Hòm, súng rơi, điện thoại gọi pháo (T31), đồ để lại, ụ súng |
| `FieldQuizSession.TryOpen(level, timeLimit, onResolved)` | `Quiz/FieldQuizSession.cs` | **Mọi câu hỏi ngoài trận đi qua đây.** Trả false nếu đang có câu khác hoặc thiếu câu |
| `IQuestionProvider.Next(level, form)` → `Question` hoặc null | `Quiz/QuizTypes.cs` | Thiên Trí cài (không lặp trong lượt, chỉ câu approved – BR-31) |
| `IQuizView.Show / SetRemaining / Hide` | `Quiz/QuizTypes.cs` | Thiên Trí cài; **phải chạy bằng thời gian thực** (`Time.unscaledDeltaTime`) |
| `FieldQuizSession.Answered` (static event, `QuizResult`) | `Quiz/FieldQuizSession.cs` | Long tính điểm kiến thức (T19, BR-25); Attempt lưu lại |
| `Objective` (Activate, Check, onCompleted) | `Missions/Objective.cs` | Thêm loại mục tiêu mới bằng cách kế thừa |
| `MissionController.Ended` (static event, `Attempt`) | `Missions/MissionController.cs` | Màn kết quả, bảng xếp hạng (Long – T36, T57) |
| `ProfileService.Data / Save / Buy` | `Upgrades/ProfileService.cs` | Long thay phần lưu bằng file JSON an toàn (T20), giữ nguyên hàm public |
| `Inventory.GiveWeapon / TakeBack / AddAmmo / AddGrenades` | `Weapons/Inventory.cs` | Mọi phần thưởng đều đi qua đây (BR-36) |

## Mỗi người cắm vào đâu

**Đạt (AI & Combat)**
- AI lính: tạo `Scripts/AI/`, gắn lên hình nộm (đã có `Health`). Khi lính rút thì đặt `health.Retreated = true`.
- Hình nộm hiện dùng `DebugTools/DisableOnDeath` (biến mất ngay). Thay bằng ngã rồi mờ dần (T08, BR-21).
- Hòm ngẫu nhiên (T22): sinh `QuizCrate` theo seed `MissionController.Current.Attempt.Seed`. Bảng độ hiếm → cấp/thời hạn đã có trong `RarityRules` (BR-15). Bảo hiểm 3 lần sai (BR-16) thì thêm vào `QuizCrate.Interact`.
- Bộc phá/lựu đạn/pháo: gọi `TakeDamage(..., DamageType.Explosive/Artillery/AntiTank, ...)`. Lô cốt và xe tăng đã miễn `Bullet`.
- Điện thoại gọi pháo (T31): một `IInteractable` có `HoldSeconds = 1`, gọi `FieldQuizSession.Instance.TryOpen(3, 15f, …)` (BR-38).

**Thiên Trí (Quiz)**
- Cài `IQuestionProvider` đọc `StreamingAssets/content/questions/*.json` theo schema `Question` (GDD §8). Gán vào `FieldQuizSession.Instance.Questions` trong `Awake`.
- UI câu hỏi: một MonoBehaviour cài `IQuizView`, gắn lên **cùng GameObject `GameSystems`** rồi xóa `DebugQuizView`. FieldQuizSession tự tìm.

**Long (Data/UI/Audio)**
- `ProfileService`: thay `Load/Save` (PlayerPrefs) bằng ghi file tạm rồi đổi tên (BR-37). Giữ `Data`, `Save`, `Buy`, `OnMissionWon`.
- HUD/menu/kết quả: đọc như `DebugOverlay` đang đọc (Health, Stamina, Inventory, Objective.Progress, Attempt). Xong thì gỡ `DebugOverlay` khỏi scene.
- Âm thanh súng: kéo clip vào `WeaponController` (fireClip, reloadClip, dryClip) trên prefab Player. Âm UI câu hỏi: `AudioSource.ignoreListenerPause = true`.
- Validate Content (T40): gọi trong `Editor/BuildTools.cs` trước khi build (đã để sẵn chỗ).

**Khôi (3D)**
- Mỗi scene là khối hộp dựng bởi `Editor/SceneBuilder.cs`. Thay khối trong nhóm `Environment` bằng model; **giữ nguyên** `Mission`, `GameSystems`, các Zone.
- Súng góc nhìn thứ nhất: thay `ViewModel_Placeholder` trong prefab Player; gán vào trường `viewModel` của `WeaponController`.
- Hiệu ứng trúng đạn: prefab gán vào `impactPrefab`.

## Dựng lại scene greybox

Menu **DBP → Greybox → Tạo scene còn thiếu** (không đụng scene đã có).
*Dựng lại tất cả* sẽ **ghi đè** scene: chỉ dùng khi chưa ai dressing.

## Vì sao thiết kế như vậy

- **Tự viết bộ điều khiển thay Starter Assets**: Starter Assets phải tải qua Asset Store bằng tài khoản từng người. Bộ điều khiển tự viết khoảng 180 dòng, nằm sẵn trong repo, clone về là chạy. Xem [quyet-dinh/0002](quyet-dinh/0002-tu-viet-bo-dieu-khien.md).
- **Thế giới đứng yên bằng `Time.timeScale = 0`**: mọi thứ dùng thời gian game tự dừng; chỉ UI câu hỏi dùng thời gian thực.
- **Logic tách thành lớp C# thuần** (`Weapon`, `Inventory`, `HoldProgress`, `QuizTimer`, `StaminaModel`, `UpgradeService`, `MissionRules`) để test không cần mở scene.
