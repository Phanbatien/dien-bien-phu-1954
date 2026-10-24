# Kịch bản demo ≤ 10 phút (T75) và video (T73)

Tài liệu nộp: [bao-cao.md](bao-cao.md) · slide [nop-bai/bao-cao.pptx](nop-bai/bao-cao.pptx) · trailer [nop-bai/trailer.mp4](nop-bai/trailer.mp4) (56 giây).

## Phân người (đề xuất, nhóm đổi nếu muốn)

| Người | Việc trong buổi demo |
|---|---|
| Trưởng nhóm | Mở đầu, slide 1–2 và 5–6 (kỹ thuật, kiểm thử), trả lời câu hỏi kỹ thuật |
| Thiên Trí | Slide 3–4 (ý tưởng, 5 màn, cốt truyện), giải thích nguồn câu hỏi |
| Trần Hoàng Long | **Cầm máy chơi demo**, slide 8 (giao diện) |
| Bùi Nguyễn Thành Đạt | Slide 7 (gameplay, AI), chỉ AI khi Long chơi |
| Lê Nguyễn Đình Khôi | Slide 9 (đồ họa, không khí), slide 10–11 |

## Trình tự và thời gian

| Phút | Nội dung | Ai | Thao tác |
|---|---|---|---|
| 0:00–0:40 | Chiếu trailer | Trưởng nhóm | Mở `docs/nop-bai/trailer.mp4` toàn màn hình |
| 0:40–2:30 | Slide 1–4: ý tưởng, 5 màn | Trưởng nhóm, Trí | |
| 2:30–6:30 | **Demo trực tiếp** | Long chơi, Đạt nói | Xem các bước dưới |
| 6:30–8:30 | Slide 5–9: kỹ thuật, kiểm thử, gameplay, giao diện, đồ họa | Trưởng nhóm, Đạt, Long, Khôi | |
| 8:30–9:30 | Slide 10–11: điểm cộng, hạn chế, cảm ơn | Khôi | |
| 9:30– | Hỏi đáp | Cả nhóm | |

### Các bước demo trực tiếp (4 phút)

Chuẩn bị trước: chạy `DienBienPhu1954.exe`, tạo sẵn hồ sơ "Demo" có khoảng 300 Quyết Tâm (thắng Màn 1 một lần hôm trước), âm lượng 80%.

1. **Menu** (15 s): chỉ nhiều hồ sơ, bấm **▶ Mở đầu chiến dịch**, xem 1 khung rồi **Bỏ qua**.
2. **Hầm chuẩn bị Màn 1** (40 s): chỉ Hầm chữ A phía sau, mua 1 nâng cấp, **Vào họp**, trả lời 3 câu (cố ý sai 1 câu để hiện giải thích).
3. **Xuất trận** (15 s): màn hướng dẫn, bấm **Đọc nhật ký trong hầm** 1 trang, rồi **Bắt đầu**.
4. **Chiến đấu** (1 phút 30): bắn vài lính (chỉ đường đạn, lính ngã), ra hòm tiếp tế, **giữ E 1 giây → chiến trường đứng yên**, trả lời đúng → nhận súng. Ném lựu đạn (G).
5. **Phá lô cốt** (40 s): nhặt bộc phá cạnh lô cốt, đặt và lùi ra.
6. **Kết quả + bảng xếp hạng** (30 s): mở bảng xếp hạng, chỉ hai tab chiến đấu/kiến thức.

Dự phòng nếu lỗi hoặc hết giờ: chiếu lại trailer và ảnh trong `docs/nop-bai/anh/`.

## Tập 2 lượt

| Lượt | Ngày | Tổng thời gian | Lỗi gặp | Ghi chú |
|---|---|---|---|---|
| 1 | | | | |
| 2 | | | | |

## Video (T73)

Trailer quay tự động từ chính game, không cần phần mềm quay màn hình:

```bash
TRAILER_DIR=<thư mục ảnh> Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter TrailerCaptureTests
```

Test `TrailerCaptureTests` chạy 5 màn, camera lướt qua trận địa, ghi 24 khung/giây ra JPG. Sau đó ghép bằng ffmpeg (chữ phụ đề, thẻ tiêu đề đầu và cuối) thành `docs/nop-bai/trailer.mp4`.

Video demo dài (chơi Màn 1 có khoảnh khắc đóng băng và bảng xếp hạng): dùng **Win + G** (Xbox Game Bar) quay lúc tập demo lượt 2, theo đúng các bước trên.
