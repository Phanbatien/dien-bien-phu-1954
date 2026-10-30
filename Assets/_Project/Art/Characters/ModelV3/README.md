# Model nhân vật v3 — dành cho làm hoạt ảnh

Nhánh `feature/model-v3`. Mở scene **`TrungBay_NhanVat.unity`** (thư mục này) để thấy cả 5 nhân vật đứng thành hàng.

| Prefab | Nhân vật | Model gốc | Controller |
|---|---|---|---|
| `BoDoi` | Bộ đội ta (đồng đội) | `Art/Animations/Allies/Firing_Rifle.fbx` (Jungle Sentinel – Long) | `Art/Animations/Allies/AlliedSoldier.controller` |
| `NhanVatNguoiChoi` | Nhân vật người chơi (góc nhìn thứ ba) | như bộ đội | `AlliedSoldier.controller` |
| `LinhPhap` | Lính Pháp | `tt-3d/LowPolyWarStarterPack/Character/Models/Infantry.fbx` | `Art/Characters/EnemySoldier.controller` |
| `SiQuanPhap` | Sĩ quan / chỉ huy (to hơn 15%) | `tt-3d/.../Officer.fbx` | `EnemySoldier.controller` |
| `TrumPhap` | Trùm mỗi màn (to hơn 44%) | `tt-3d/.../Officer.fbx` | `EnemySoldier.controller` |

Mọi model đều là **rig Humanoid**, nên một clip hoạt ảnh dùng được cho cả 5 nhân vật (Unity tự retarget).
Prefab chỉ có hình + Animator + súng / lưỡi lê, **không có script AI**, kéo thả vào scene là xem được.

## Cấu trúc controller (cả hai controller giống nhau)

- **Lớp gốc** (toàn thân): `Locomotion` (blend theo `Speed`: đứng 0 · đi 1,6 · chạy 4,5) ↔ `Crouch` (bool) ↔ `Climb` (bool); `Death` (trigger `Dead`, từ mọi trạng thái).
- **Lớp `ThanTren`** (mask nửa người trên `Art/Animations/Generated/ThanTren.mask`): trigger `Shoot`, `Stab`, `Punch`, `Throw`, `Hit`, `Command`, `Rally` — chạy xong tự về trạng thái rỗng `Trong`, nên vừa đi vừa bắn / đâm được.

| Động tác | Clip hiện tại | Ai dùng |
|---|---|---|
| Đứng / đi / chạy | Starter Assets (`Stand--Idle`, `Locomotion--Walk_N`, `Locomotion--Run_N`) | tất cả |
| Bắn | `Firing_Rifle.fbx` (Long) | tất cả |
| Ngồi nấp, trèo thang, gục, đâm lê, đấm, ném lựu đạn, trúng đạn, chỉ tay, hô hào | `Art/Animations/Generated/*.anim` (dựng bằng đường cong cơ, khá thô) | xem bảng dưới |

Ai dùng động tác nào trong game: bộ đội (đi, bắn, ngồi, trèo, đâm, đấm, trúng đạn, gục) · lính Pháp (thêm ném lựu đạn) ·
sĩ quan (thêm `Command` khi dùng kỹ năng) · trùm (thêm `Rally` khi vào trận) · người chơi (bắn, đâm V, ném G, ngồi, trèo).

## Thay / thêm hoạt ảnh

1. Import clip (Mixamo, mocap…) → Inspector của FBX → **Rig: Humanoid** → Apply.
2. Mở controller → chọn trạng thái (vd. `Stab` trên lớp `ThanTren`) → kéo clip mới vào ô **Motion**. Giữ nguyên **tên trạng thái và tên tham số** — code gọi theo tên (`EnemyAnimatorDriver.Play(soldier, "Stab")`).
3. Clip thân trên (bắn, đâm, ném…) nên đặt vào lớp `ThanTren`; clip toàn thân (ngồi, trèo, gục) đặt ở lớp gốc.
4. Bấm Play trong scene trưng bày, chọn một nhân vật → cửa sổ Animator → bấm trigger để xem thử.

**Lưu ý:** menu `DBP → Animations → Dựng lại hoạt ảnh nhân vật` (và bước nối dây `WireAll`) **dựng lại hai controller từ code** —
nếu sửa controller bằng tay, báo trưởng nhóm để sửa luôn `Assets/_Project/Editor/ProjectWiring.Animations.cs`, không thì lần nối dây sau sẽ ghi đè.
Muốn xuất lại các prefab này sau khi đổi model: menu `DBP → Model v3 → Xuất model nhân vật`.

Cần **Git LFS** để có model / texture (xem README chính).
