# 0002 · Tự viết bộ điều khiển FPS thay Starter Assets

- **Trạng thái:** Đã áp dụng (07/10/2026)
- **Liên quan:** T01, T12

## Bối cảnh

Kế hoạch ban đầu dùng gói *Starter Assets – First Person* trên Unity Asset Store.
Gói này phải tải bằng tài khoản Unity của từng người rồi import vào máy. Nó đi kèm Cinemachine, một file Input Actions và các script mẫu mà nhóm sẽ phải sửa nhiều chỗ: khóa điều khiển khi đóng băng thế giới, ADS, độ giật, thể lực.

## Quyết định

Tự viết `FirstPersonController` (khoảng 180 dòng) dùng `CharacterController` và đọc thẳng bàn phím/chuột qua Input System.

## Hệ quả

- Clone repo về là chạy, không phải tải gói ngoài.
- Khóa điều khiển (`InputLock`), thể lực, độ giật và ụ súng máy nằm gọn trong một file, dễ đọc.
- Chưa hỗ trợ tay cầm và chưa đổi phím trong game. Nếu cần (vd. bản đa nền tảng), thêm file Input Actions sau.
