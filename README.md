# Điện Biên Phủ 1954

Game bắn súng góc nhìn thứ nhất (FPS) về 56 ngày đêm ở lòng chảo Mường Thanh (13/3 – 7/5/1954).
Người chơi nhập vai năm chiến sĩ hư cấu qua năm màn. Muốn lấy vũ khí trong hòm thì phải trả lời câu hỏi lịch sử,
và **thế giới đứng yên trong lúc trả lời**.

Đồ án môn PRU213 · Unity 6 (URP) · nhóm 5 người.

---

## Chạy thử trong 5 phút

1. Cài **Unity 6000.3.24f1** qua Unity Hub (thêm module *Windows Build Support (Mono)* nếu muốn build).
2. Cài [Git LFS](https://git-lfs.com) rồi clone:
   ```bash
   git lfs install
   git clone https://github.com/Phanbatien/dien-bien-phu-1954.git
   ```
3. Unity Hub → **Add project from disk** → chọn thư mục `dien-bien-phu-1954`.
4. Mở scene `Assets/_Project/Scenes/Sandbox.unity` → bấm **Play**.

| Phím | Tác dụng |
|---|---|
| WASD · Chuột | Đi · nhìn |
| Shift · Space · Ctrl/C | Chạy (tốn thể lực) · nhảy · ngồi/bò |
| Chuột trái · Chuột phải · R | Bắn · ngắm (ADS) · thay đạn |
| 1 / 2 · lăn chuột | Đổi súng |
| **Giữ E 1 giây** | Mở hòm vũ khí → câu hỏi (chọn bằng chuột hoặc phím 1–4) |
| E | Nhặt lại đồ để lại · lên/xuống ụ súng máy |
| Esc | Tạm dừng: tiếp tục / chơi lại / bỏ dở |
| F9 *(chỉ Editor/Development)* | Nổ mục tiêu đang nhắm (thay bộc phá tạm thời) |

---

## Cấu trúc thư mục

```
dien-bien-phu-1954/
├── Assets/
│   ├── _Project/                 ← mọi thứ của nhóm nằm ở đây
│   │   ├── Scripts/              ← code game (assembly DBP.Runtime)
│   │   │   ├── Core/             máu & sát thương, khóa điều khiển, "thế giới đứng yên"
│   │   │   ├── Player/           điều khiển góc nhìn thứ nhất, thể lực
│   │   │   ├── Weapons/          súng, túi đồ, bắn/ngắm/thay đạn, ụ súng máy
│   │   │   ├── Interaction/      giữ E, hòm vũ khí, đồ để lại
│   │   │   ├── Quiz/             câu hỏi ngoài trận, đồng hồ thời gian thực
│   │   │   ├── Missions/         luồng màn, mục tiêu, lượt chơi, cắm cờ
│   │   │   ├── Upgrades/         nâng cấp bằng Điểm Quyết Tâm, hồ sơ
│   │   │   └── DebugTools/       HUD/menu tạm bằng IMGUI (sẽ được thay)
│   │   ├── Editor/               công cụ: dựng scene greybox, build .exe
│   │   ├── Tests/                EditMode (logic) + PlayMode (chạy scene thật)
│   │   ├── Scenes/               M1…M5 + Sandbox
│   │   ├── Prefabs/  Art/  Audio/  UI/  Data/
│   └── StreamingAssets/content/  dữ liệu JSON (weapons.json, sau này questions/…)
├── docs/                         kế hoạch, quy tắc nghiệp vụ, kiến trúc, quyết định
├── Packages/  ProjectSettings/   cấu hình Unity (commit)
└── Library/  Builds/  Logs/      Unity tự sinh (KHÔNG commit)
```

## Tài liệu

| Tài liệu | Nội dung |
|---|---|
| [docs/kien-truc.md](docs/kien-truc.md) | **Đọc trước khi code:** các hệ thống, "hợp đồng" giữa các phần, chỗ mỗi người cắm vào |
| [docs/ke-hoach.md](docs/ke-hoach.md) · [docs/cong-viec.md](docs/cong-viec.md) | Lịch 10 tuần, phân vai, 75 việc chi tiết |
| [docs/business-rules.md](docs/business-rules.md) | Quy tắc nghiệp vụ BR-01…BR-40 (mã BR trong code trỏ về đây) |
| [docs/tieu-chi-cham.md](docs/tieu-chi-cham.md) | Tiêu chí chấm điểm của môn |
| [docs/build.md](docs/build.md) | Build `.exe`, kiểm tra hiệu năng, đóng gói bản nộp |
| [docs/quyet-dinh/](docs/quyet-dinh/) | Các quyết định nhóm đã chốt (ADR) |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Quy trình Git, đặt tên, ai sở hữu file nào |
| GDD v5 | https://claude.ai/artifact/R41iR43ggef6X8x8zM4keb |

## Nhóm

| Thành viên | Vai | Thư mục chính |
|---|---|---|
| Trưởng nhóm | FPS Lead + tích hợp + build | `Player/`, `Weapons/`, `Missions/`, `Upgrades/`, `Editor/` |
| Bùi Nguyễn Thành Đạt | AI & Combat | `Scripts/AI/` (mới), `Interaction/QuizCrate.cs`, mục tiêu địch |
| Lê Nguyễn Đình Khôi | 3D Artist | `Art/`, `Prefabs/`, dressing các scene |
| Thiên Trí | Quiz & Narrative | `Quiz/`, `StreamingAssets/content/questions/` |
| Trần Hoàng Long | Audio / Data / UI | `UI/`, `Audio/`, lưu hồ sơ, Validate Content |

## Trạng thái (cập nhật 07/10/2026 · làm trong 2 tuần, nộp 21/10)

**Cả 5 màn chơi được từ đầu đến cuối ở dạng greybox** (khối hộp), có mục tiêu, hòm ngẫu nhiên, điện thoại gọi pháo, súng rơi, lưu hồ sơ.
Kiểm tra tự động: **46/46 test qua** (39 EditMode + 7 PlayMode). Build Development thành công; bản nộp đang bị **Validate Content** chặn đúng vì còn thiếu câu hỏi đã duyệt và nguồn vũ khí.

| Đã có | Còn là bản tạm (ai thay, việc nào) |
|---|---|
| Đi/chạy/nhảy/bò, thể lực, ADS, độ giật, ống ngắm | Địch là hình nộm đứng yên → **AI của Đạt** (T07, T39, T52) |
| 5 súng trong `weapons.json`, túi 2 súng, đồ để lại (BR-36) | Bộc phá, lựu đạn ném, xe tăng chạy → **Đạt** (T14, T15, T30, T45). Tạm dùng F9 |
| Giữ E 1 s → thế giới đứng yên → đếm giờ thực → 0,5 s chuyển tiếp | Giao diện câu hỏi IMGUI → **UI của Thiên Trí** (T18, T25) |
| Ngân hàng câu JSON + bộ chọn không lặp; hòm rút theo seed; bảo hiểm 3 lần sai; súng rơi | Mới có 3 câu nháp → **Thiên Trí viết câu** (T26, T42, T47, T56, T61) |
| Điện thoại gọi pháo, ụ súng máy, cắm cờ Màn 5, mê cung hào Màn 4 | HUD, menu, màn kết quả IMGUI → **UI của Long** (T27, T35, T36) |
| Lưu hồ sơ JSON an toàn (`.tmp` + `.bak`), nâng cấp 4 nhánh × 3 cấp | Khối hộp, chưa có âm thanh → **Khôi, Long** |
| Validate Content (menu **DBP → Validate Content**), build + nén `.zip`, đo FPS (`-perf-probe`) | |

Lịch 2 tuần và người phụ trách từng việc: [docs/ke-hoach.md](docs/ke-hoach.md) · chương trình họp: [docs/quyet-dinh/0001-khoi-dong.md](docs/quyet-dinh/0001-khoi-dong.md).
