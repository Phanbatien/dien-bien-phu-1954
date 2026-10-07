# 0001 · Họp khởi động: vai trò, phạm vi, Phụ lục B (T03)

- **Trạng thái:** DỰ THẢO. Cả nhóm cần đọc, sửa và đánh dấu ✔ từng dòng.
- **Ngày dự kiến họp:** 03/10/2026 · **Chủ trì:** Trưởng nhóm

Code hiện tại đã cài theo cột "Mặc định đang dùng". Nếu nhóm chốt khác, ghi vào cột "Nhóm chốt" và báo người phụ trách sửa.

## 1. Vai trò

| ✔ | Thành viên | Vai |
|---|---|---|
| ☐ | Trưởng nhóm | FPS Lead + tích hợp + build |
| ☐ | Bùi Nguyễn Thành Đạt | AI & Combat |
| ☐ | Lê Nguyễn Đình Khôi | 3D Artist |
| ☐ | Thiên Trí | Quiz & Narrative |
| ☐ | Trần Hoàng Long | Audio / Data / UI |

## 2. Phạm vi MVP và thứ tự cắt khi trễ

MVP: 5 màn + hai hình thức câu hỏi + hòm ngẫu nhiên + bảng xếp hạng **tại máy**.
Thứ tự cắt: (1) gửi điểm online, (2) Mosin ống ngắm và nhạc nền riêng mỗi màn, (3) Màn 4 thu gọn còn 2 đoạn hào.
**Không cắt:** thế giới đứng yên, vật tư nhiệm vụ, Validate Content, kiểm thử Phụ lục A.

## 3. Phụ lục B của Business Rules v2

| ✔ | Quyết định | Mặc định đang dùng trong code | Nhóm chốt |
|---|---|---|---|
| ☐ | Vũ khí nhặt có mang sang màn sau? | **Không**: chỉ trong lượt (BR-19). `Inventory` mới mỗi lượt | |
| ☐ | Số súng mang theo | 2 súng; lựu đạn ngăn riêng (BR-36) | |
| ☐ | Bảng nâng cấp | 4 nhánh × 3 cấp, giá 100/200/300, +10%/20%/30% (BR-35) | |
| ☐ | Thời gian câu ngoài trận | Thường 12 s · Khá 15 s · Hiếm 18 s · Rất hiếm 20 s (BR-15) | |
| ☐ | Câu trong hầm | Không giới hạn giờ, không thưởng tốc độ (BR-06) | |
| ☐ | Điểm kiến thức | Cấp 1–5 = 100–500, ngoài trận × (1 + 0,5 × phần giờ còn lại) (BR-25) | |
| ☐ | Thắng/thua | Hết máu = thua (ưu tiên hơn thắng); không có điểm lưu giữa màn (BR-32, BR-33) | |
| ☐ | Bảng xếp hạng | Chỉ lượt thắng; online là điểm cộng, làm sau cùng | |
| ☐ | Starter Assets | **Không dùng**, tự viết bộ điều khiển → [0002](0002-tu-viet-bo-dieu-khien.md) | |

## 4. Definition of Done

Xem [CONTRIBUTING.md](../../CONTRIBUTING.md#definition-of-done-một-việc-chỉ-tính-là-xong-khi).

## 5. Việc cần làm sau họp

| Ai | Việc | Hạn |
|---|---|---|
| Mỗi người | Clone repo, mở Unity, chạy Sandbox, báo lại máy chạy được | |
| Mỗi người | Đọc [kien-truc.md](../kien-truc.md), phần "Mỗi người cắm vào đâu" | |
