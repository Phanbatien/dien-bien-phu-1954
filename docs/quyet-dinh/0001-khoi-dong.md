# 0001 · Họp khởi động: chương trình và quyết định cần chốt (T03)

- **Thời gian:** sáng Thứ 5, 08/10/2026 · khoảng 75 phút
- **Chủ trì:** Trưởng nhóm · **Thư ký:** (chọn đầu buổi), ghi vào cột "Nhóm chốt"
- **Trạng thái:** CHƯA HỌP. Sau buổi họp, sửa dòng này thành "Đã chốt 08/10" và commit lên `develop`.

## Trước buổi họp, mỗi người làm sẵn (khoảng 30 phút)

- [ ] Gửi **tên GitHub** cho trưởng nhóm để được mời vào repo.
- [ ] Cài Unity **6000.3.24f1** và Git LFS (`winget install GitHub.GitLFS`, rồi `git lfs install`).
- [ ] Clone repo, mở scene `Sandbox`, bấm Play, thử giữ E mở hòm. Báo máy mình chạy được hay lỗi gì.
- [ ] Đọc lịch của mình trong [ke-hoach.md](../ke-hoach.md) và phần "Mỗi người cắm vào đâu" trong [kien-truc.md](../kien-truc.md).

---

## Chương trình

### 1. Khung 2 tuần và cách làm việc (10 phút)

Bàn và chốt:

| ✔ | Nội dung | Đề xuất | Nhóm chốt |
|---|---|---|---|
| ☐ | Ngày nộp chính xác, hình thức nộp (file, link, thuyết trình?) | Nộp 21/10; build `.exe` + báo cáo + video | |
| ☐ | Làm cả Chủ nhật 11/10 và 18/10? | Có. Lịch đang xếp kín 13 ngày | |
| ☐ | Họp hằng ngày | 15 phút lúc 21h: hôm qua làm gì / hôm nay làm gì / vướng gì | |
| ☐ | Kênh liên lạc, báo trễ | Nhóm chat; trễ hơn một buổi là báo ngay để người có buổi trống ("—") vào hỗ trợ | |
| ☐ | Hai buổi làm chung bắt buộc | Chiều 16/10 playtest Màn 1 · cả ngày 19/10 sửa lỗi · chiều 20/10 tập demo | |

### 2. Phân vai và chuyển việc (10 phút)

Đủ 75 việc, không cắt việc nào. Để cân tải, một số việc **đổi người làm** (xem bảng "Chuyển việc" trong [ke-hoach.md](../ke-hoach.md)):

- Trưởng nhóm nhận thêm: T11, T17, T20, T40 (dữ liệu/hồ sơ/Validate), T22, T23, T31 (hòm, bảo hiểm, gọi pháo), T54 (môi trường Màn 4).
- Long nhận T67 (VFX) · Trí nhận T53 (nguồn vũ khí) · Đạt nhận T70 (chạy Validate cuối).

| ✔ | Câu hỏi | Nhóm chốt |
|---|---|---|
| ☐ | Mỗi người đồng ý với vai và lịch của mình? Ai thấy không kịp việc nào thì nói **ngay hôm nay** | |
| ☐ | Ai mạnh mảng nào mà đang bị xếp sai chỗ? (đổi bây giờ còn dễ) | |
| ☐ | Trưởng nhóm đã làm xong sớm 8 việc nhận thêm (T11 T17 T20 T22 T23 T31 T40 T54) nên **trống từ chiều 08/10 đến 16/10**. Dùng thời gian đó hỗ trợ ai? (đề xuất: soát câu hỏi cùng Trí, AI cùng Đạt, nối UI của Long/Trí) | |

### 3. Quy trình Git và quyền sở hữu scene (10 phút)

Đề xuất (chi tiết trong [CONTRIBUTING.md](../../CONTRIBUTING.md)):
- Làm trên nhánh `feature/Txx-...` tách từ `develop` → mở PR vào `develop` → tự merge được.
- `main` được bảo vệ: chỉ merge từ `develop` ở các mốc (11/10, 15/10, 18/10, 20/10), cần 1 người duyệt.
- **Mỗi scene một người giữ tại một thời điểm.**

| ✔ | Scene | Ai giữ (đề xuất) | Nhóm chốt |
|---|---|---|---|
| ☐ | `M1_HimLam` | Khôi (dressing 10–11/10), sau đó Đạt (AI, lô cốt) | |
| ☐ | `M2_GiuDocLap` | Khôi (13–14/10), sau đó Đạt (phản kích, xe tăng) | |
| ☐ | `M3_DoiED` | Khôi (14–15/10) | |
| ☐ | `M4_LongHao` | Trưởng nhóm (16–17/10) | |
| ☐ | `M5_ChieuMungBay` | Khôi (17–18/10), Đạt (ổ đề kháng, nổ A1) | |
| ☐ | `Sandbox` | Ai cũng được thử; **không** sửa rồi commit | |
| ☐ | Prefab `Player` | Trưởng nhóm; Khôi chỉ thay model súng/tay | |
| ☐ | Ai duyệt PR vào `main`? | | |

### 4. Quyết định thiết kế cần chốt — Phụ lục B (20 phút)

Code hiện tại đã cài theo cột "Mặc định". Đổi gì thì ghi vào cột "Nhóm chốt" và báo người phụ trách.

| ✔ | Quyết định | Mặc định đang dùng | Nhóm chốt |
|---|---|---|---|
| ☐ | Tên, quê quán, tính cách 5 chiến sĩ hư cấu (một người mỗi màn) | Chưa có, Trí đề xuất trong T34 | |
| ☐ | Vũ khí nhặt được có mang sang màn sau? | Không, chỉ dùng trong lượt (BR-19) | |
| ☐ | Mang tối đa mấy súng | 2 súng; lựu đạn và vật tư ngăn riêng (BR-36) | |
| ☐ | Giá và hiệu lực nâng cấp | 4 nhánh × 3 cấp, giá 100/200/300, +10/20/30% (BR-35) | |
| ☐ | Thời gian câu ngoài trận | Thường 12 s · Khá 15 s · Hiếm 18 s · Rất hiếm 20 s (BR-15) | |
| ☐ | Câu trong hầm | 3–5 câu, không giới hạn giờ (BR-05, BR-06) | |
| ☐ | Điểm kiến thức | Cấp 1–5 = 100–500, ngoài trận nhân thêm theo thời gian còn lại (BR-25) | |
| ☐ | Thắng/thua | Hết máu = thua, không có điểm lưu giữa màn (BR-32, BR-33) | |
| ☐ | Bảng xếp hạng online (T62) | Làm sau cùng (14–16/10); dịch vụ nào? (Firebase / Supabase / …) | |
| ☐ | Bộ điều khiển FPS | Tự viết, không dùng Starter Assets ([0002](0002-tu-viet-bo-dieu-khien.md)) | |

### 5. Nội dung lịch sử và tài nguyên (15 phút)

| ✔ | Câu hỏi | Đề xuất | Nhóm chốt |
|---|---|---|---|
| ☐ | **Ngân sách câu hỏi** (Validate Content báo): mỗi màn cần khoảng 47 câu đã duyệt theo BR-28, kế hoạch chỉ có 25/màn | Viết khoảng 50 câu dùng chung (`missionId: "CAMPAIGN"`, tính cho mọi màn) + câu riêng từng màn; tổng vẫn khoảng 125 | |
| ☐ | Nguồn chính cho khoảng 125 câu hỏi | Sách giáo khoa Lịch sử 12, tài liệu Bảo tàng Chiến thắng Điện Biên Phủ; mỗi câu ghi nguồn + trang | |
| ☐ | Ai soát câu hỏi cho Trí (một người đọc lại mỗi đợt 25 câu)? | Người có buổi trống ("—") cùng ngày | |
| ☐ | Model/âm thanh lấy từ đâu | Tự làm hoặc asset miễn phí giấy phép **CC0**; ghi nguồn vào `docs/nguon-tai-nguyen.md` | |
| ☐ | Chiến sĩ thật xuất hiện thế nào | Chỉ trong cốt truyện và câu hỏi, không điều khiển (BR-01, BR-02) | |
| ☐ | Không máu me, máy bay chỉ là âm thanh | Giữ nguyên (BR-04, BR-21) | |

### 6. Báo cáo, video, thuyết trình (5 phút)

| ✔ | Câu hỏi | Đề xuất | Nhóm chốt |
|---|---|---|---|
| ☐ | Báo cáo + slide | Trí viết (T72, 19/10), chia theo 5 nhóm tiêu chí chấm | |
| ☐ | Video demo | Long (T73, 18–19/10), khoảng 60 giây, có cảnh thế giới đứng yên | |
| ☐ | Ai thuyết trình, ai cầm máy demo | | |

### 7. Chốt và việc ngay sau họp (5 phút)

| Ai | Việc | Hạn |
|---|---|---|
| Trưởng nhóm | Mời 4 bạn vào repo; commit file này với cột "Nhóm chốt" đã điền | Trưa 08/10 |
| Mỗi người | Bắt đầu việc đầu tiên trong lịch của mình | Chiều 08/10 |
| Cả nhóm | Họp 15 phút lần đầu | 21h 08/10 |
