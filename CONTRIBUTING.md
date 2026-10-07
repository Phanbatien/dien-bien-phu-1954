# Cách làm việc chung

## Nhánh Git

```
main       ← chỉ chứa bản chạy được; merge qua Pull Request, trưởng nhóm duyệt
develop    ← tích hợp hằng ngày
feature/<mã-task>-<mô-tả-ngắn>   vd: feature/T07-ai-bao-cat, feature/T18-ui-cau-hoi
```

1. `git checkout develop && git pull`
2. `git checkout -b feature/T18-ui-cau-hoi`
3. Làm, commit nhỏ, đẩy lên: `git push -u origin feature/T18-ui-cau-hoi`
4. Mở Pull Request vào `develop`, ghi mã task (T18) và cách kiểm (cột "Cách kiểm" trong [docs/cong-viec.md](docs/cong-viec.md)).

**Commit:** `T18: hiện đồng hồ đếm ngược thời gian thực`. Bắt đầu bằng mã task, viết tiếng Việt, một việc một commit.

## Tránh xung đột trong Unity

Scene và prefab là YAML, hai người cùng sửa một scene gần như chắc chắn xung đột.

| Quy tắc | Vì sao |
|---|---|
| **Mỗi scene một người giữ tại một thời điểm.** Báo trong nhóm trước khi sửa scene | Merge scene rất khó |
| Làm phần của mình trong **prefab riêng** rồi kéo vào scene | Prefab ít xung đột hơn scene |
| Muốn thử nghiệm: tạo scene riêng `Scenes/Dev/<tên>.unity` | Không đụng scene chung |
| Luôn commit file `.meta` đi kèm | Thiếu `.meta` là mất liên kết |
| Không commit `Library/`, `Builds/`, `Logs/`, `UserSettings/` | Unity tự sinh, rất nặng |

## Đặt tên

| Loại | Quy ước | Ví dụ |
|---|---|---|
| Lớp C#, file | PascalCase, **1 MonoBehaviour = 1 file cùng tên** | `QuizCrate.cs` |
| Namespace | `DBP.<Thư mục>` | `DBP.Weapons` |
| Scene | `M<số>_<TenKhongDau>` | `M3_DoiED` |
| Mã dữ liệu | Theo GDD: `W-…`, `Q-M1-L2-006`, `U-AMMO-1` | `W-MAT49` |
| Asset | `<Loai>_<Ten>` | `Mat_Mud`, `SFX_Mosin_Fire` |

## Definition of Done (một việc chỉ tính là xong khi…)

- [ ] Đạt cột **Kết quả cần có** và **Cách kiểm** của task.
- [ ] Không có lỗi đỏ trong Console khi chơi Sandbox và màn liên quan.
- [ ] Logic có nhánh (if/else, tính điểm…) có ít nhất 1 test trong `Assets/_Project/Tests/`.
- [ ] Test Runner (EditMode + PlayMode) xanh.
- [ ] Đã merge vào `develop` và người khác pull về chạy được.

## Chạy test

Unity → **Window → General → Test Runner** → EditMode / PlayMode → **Run All**.
Hoặc dòng lệnh (không mở Editor):

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults test-results.xml
```
