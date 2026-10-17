# T57 — Biên bản hoàn thành 15/10/2026

Chuẩn nghiệm thu: [t57-bang-xep-hang.md](t57-bang-xep-hang.md), BR-24/25, PL-A dòng 10–11. Unity **6000.3.24f1**, nhánh `Long`, kiểm tra trực tiếp trong Editor đang mở.

## Kết quả

| Kiểm tra | Kết quả |
|---|---|
| Toàn bộ EditMode | **98/98 đạt**, 0 lỗi, 0 bỏ qua |
| Toàn bộ PlayMode | **22/22 đạt**, 0 lỗi, 0 bỏ qua |
| Riêng T57 | 28 test quy tắc/lưu trữ + 8 test luồng UI/nhiệm vụ, nằm trong tổng trên |
| Biên dịch | `recompile_status`: completed, failed=false, errors=[] |
| Kiểm tra hình ảnh | Đã mở và xem 7 ảnh chụp từ game thật: bảng trống, chiến đấu, kiến thức, trang 2, nhóm độ khó 2, kết quả, bảng từ kết quả |
| Rà soát độc lập | Hai lượt; đã sửa lỗi xử lý dữ liệu hỏng, bổ sung nhật ký phần thưởng và kiểm tra tên dài; lượt cuối không còn vấn đề được xác nhận |

Đã thấy test thất bại trước khi bổ sung mô hình xếp hạng, nút menu, nhật ký phần thưởng và sửa khôi phục file hỏng. Sau đó chạy lại đạt. Test TH-11 cũ báo đạt vô điều kiện đã được thay bằng kiểm tra bảng thật.

## Phạm vi được kiểm tra

- Năm tiêu chí đúng thứ tự, chênh 1 mili giây, ít sát thương dù lâu hơn, hạng 1–1–3 và thứ tự mã hồ sơ ổn định khi đảo đầu vào.
- Hai bảng giữ hai lượt khác nhau; ngang bằng giữ lượt trước; không cộng nhiều lượt, không nhân đôi khi gửi lại; tách đủ năm chiều điều kiện.
- Thua/bỏ dở/lượt lỗi câu không xếp hạng; thay hồ sơ/nâng cấp sau lúc xuất trận không sửa chủ sở hữu hoặc nhóm của kết quả.
- Mở lại kho dữ liệu vẫn có kỷ lục. Khóa file tạm gây lỗi ghi được báo và thử lại thành công. File chính hỏng dùng backup; hai file hỏng hiển thị lỗi, không chặn màn kết quả và không xóa dữ liệu gốc. JSON thiếu trường không được nhận thành bảng trống.
- Đồng hồ loại thời gian hướng dẫn, tính pause, WorldFreeze và chuyển tiếp 0,5 giây; unit test cho khoảng loại trừ chồng nhau và chốt một lần. Không có hoạt cảnh bắt buộc trong trận hiện tại để chạy kiểm tra cảnh đó.
- Seed và nhật ký phần thưởng hòm/súng rơi, kết quả phát thưởng và đồ dư được lưu, không dùng làm tiêu chí phụ.
- Đã Play từ MainMenu đang mở, xác nhận frame tăng, chuyển tab/nhóm/trang, kiểm tra tên 40 ký tự không tràn dòng, đi từ kết quả vào bảng rồi quay lại kết quả/menu.

## Báo cáo và ảnh tại máy

- [EditMode JSON](../Evidence/T57/editmode.json)
- [PlayMode JSON](../Evidence/T57/playmode.json)
- [Visual check JSON](../Evidence/T57/visual-check.json)
- [Bảng chiến đấu](../Evidence/T57/combat.png), [kiến thức](../Evidence/T57/knowledge.png), [trang 2](../Evidence/T57/page-2.png), [nhóm khác](../Evidence/T57/difficulty-2.png)
- [Bảng trống](../Evidence/T57/empty.png), [kết quả](../Evidence/T57/results.png), [bảng từ kết quả](../Evidence/T57/from-results.png)

`Evidence/` là minh chứng cục bộ đang được repository bỏ qua; mã test được giữ trong `Assets/_Project/Tests` để chạy lại. Các tên và số liệu trong ảnh là dữ liệu thử, không phải thành tích thật.

## Bảo toàn dữ liệu và giới hạn

Các test/ảnh T57 dùng kho tạm. Hai suite scene có sẵn cũng đã được bổ sung kho tạm khi kiểm tra toàn bộ. Lượt chạy đầu của test Sandbox cũ từng tạo một file xếp hạng chỉ chứa `SANDBOX`, 5 địch, 72 ms; file đó đã được chuyển nguyên vẹn vào `Temp/t57-first-suite-sandbox-only.json` để lưu minh chứng. Không sửa hồ sơ thật; SHA-256 `profiles.json` giữ nguyên trước/sau kiểm tra hình ảnh: `E3B9258481D36E2A8CA97A2C8CE98F19BA1C2B6579B9A2AE98CA7F83C00A1C36`.

Đây là nghiệm thu T57 tại máy trong Editor; không phải nghiệm thu T62 online hoặc chơi thử T36 bởi người ngoài nhóm. Không commit/push trong yêu cầu ngày 15/10. Các sửa có sẵn ở `.gitignore` và `Assets/Settings/Mobile_RPAsset.asset` được giữ nguyên ở lần nghiệm thu đó.

## Kiểm tra trước khi đưa lên nhánh Long — 17/10/2026

- Theo yêu cầu commit/push của người dùng, chạy lại trên Unity 6000.3.24f1: EditMode **98/98 đạt**; PlayMode trong Editor thông thường **22/22 đạt**, không lỗi hoặc bỏ qua.
- Hai lần chạy PlayMode ở batch mode (có và không có đồ họa) đều đạt 20/22; hai test menu về tên dài và chuột không đạt trong môi trường này. Chạy lại toàn bộ suite trong Editor thông thường xác nhận cả hai đạt, cùng toàn bộ 8 test luồng T57. Không sửa mã để bỏ qua các test này.
- Kết quả chạy lại nằm cục bộ trong `Temp/publish-editmode.xml`, `Temp/publish-playmode.xml`, `Temp/publish-playmode-graphics.xml` và `Temp/pipeline_test_status.json`; các file này không đưa vào Git.
- Đưa mã T57, test và tài liệu vào commit tính năng; cập nhật `.gitignore` ở commit riêng. Giữ thay đổi `Assets/Settings/Mobile_RPAsset.asset` tại máy, ngoài các commit này.
