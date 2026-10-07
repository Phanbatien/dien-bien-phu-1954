# Build, hiệu năng và bản nộp (T63, T64, T74)

## Build Windows

Trong Unity: **DBP → Build → Windows (bản nộp)**. Kết quả: `Builds/Windows/DienBienPhu1954.exe`.

- *Development* giữ đồng hồ FPS và phím F9; dùng khi chơi thử với lớp.
- Thứ tự màn lấy từ **File → Build Settings** (SceneBuilder đã đặt M1 → M5, Sandbox cuối).

Dòng lệnh (không mở Editor, dùng khi build bản nộp):

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe" -batchmode -nographics -projectPath . -executeMethod DBP.EditorTools.BuildTools.BuildWindowsCI -logFile build.log
```

- **Bản nộp** chạy Validate Content trước; còn lỗi bắt buộc thì **không build** (BR-27–BR-30). Build xong tự nén `Builds/DienBienPhu1954_v<version>.zip`, đã bỏ thư mục debug.
- **Bản Development** vẫn build khi còn lỗi nội dung (chỉ cảnh báo), để chơi thử trong lúc đang viết câu hỏi.
- Đã có bản build, chỉ muốn nén lại: `-executeMethod DBP.EditorTools.BuildTools.PackageCI`.

Ngày 07/10/2026: bản Development build thành công (156 MB, 30 giây); bản nộp bị chặn đúng vì còn 37 lỗi nội dung (thiếu câu đã duyệt, thiếu nguồn vũ khí); đóng gói thử ra file `.zip` 292 file.

## Kiểm tra hiệu năng (T63, 17–18/10)

Mục tiêu: **≥ 60 FPS trên máy yếu nhất nhóm**, ở Màn 4 và Màn 5 (nhiều địch và hiệu ứng nhất).

1. Đo nhanh mọi màn bằng một lệnh (tự chạy từng màn, ghi FPS vào log rồi thoát):
   ```bash
   Builds/Windows/DienBienPhu1954.exe -perf-probe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile perf.log
   ```
   Kết quả nằm ở các dòng `[PERF]` trong `perf.log`. Hoặc build *Development*, chơi hết màn, đọc FPS ở góc trái trên.
2. Nếu tụt: **Window → Analysis → Profiler**, chơi lại trong Editor, xem CPU (Scripts / Physics / Rendering).
3. Cách giảm thường gặp:
   - Khối môi trường đã đánh dấu *Batching Static*. Model mới của Khôi cũng phải bật cờ này.
   - Giảm *Shadow Distance* trong `Assets/Settings/PC_RPAsset`.
   - AI: không gọi `GetComponent` / `FindObjectOfType` trong `Update`; raycast tối đa vài lần/giây.
   - Hiệu ứng: dùng pool thay vì `Instantiate`/`Destroy` liên tục.
4. Ghi kết quả đo (máy, màn, FPS trước/sau) vào cuối file này.

## Đóng gói bản nộp (T74, 20/10)

1. `develop` → `main` qua Pull Request; mọi test xanh; Validate Content 0 lỗi.
2. Build *bản nộp*, nén thư mục `Builds/Windows/` thành `DienBienPhu1954_v1.0.zip`. **Bỏ** thư mục `*_BurstDebugInformation_DoNotShip`.
3. Thử trên một máy sạch: giải nén → chạy `DienBienPhu1954.exe`.
4. Gắn tag: `git tag -a v1.0 -m "Bản nộp PRU213"` rồi `git push origin v1.0`.

### Hướng dẫn cài cho người chơi (dán vào trang nộp bài)

> Yêu cầu: Windows 10/11 64-bit, card đồ họa hỗ trợ DirectX 11.
> Giải nén `DienBienPhu1954_v1.0.zip` → chạy `DienBienPhu1954.exe`. Không cần cài đặt, không cần mạng.

## Nhật ký đo hiệu năng

| Ngày | Máy | Màn | FPS | Ghi chú |
|---|---|---|---|---|
| 07/10/2026 | Ryzen 9 7945HX · RTX 4060 Laptop · 32 GB (máy trưởng nhóm) | M1 · M2 · M3 · M4 · M5 · Sandbox | 1191 · 1224 · 1182 · 1155 · 1203 · 1209 (trung bình) | Bản greybox, 1280×720, tắt VSync. **Chưa đại diện**: phải đo lại trên máy yếu nhất khi có model thật (17–18/10) |
