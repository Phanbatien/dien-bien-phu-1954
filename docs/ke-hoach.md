# Kế hoạch triển khai: Điện Biên Phủ 1954 (FPS, Unity URP)

Nguồn: GDD v5 (artifact), `Business_rule_DBP1954_v2.md`, `Tiêu chí đánh giá dự án Unity Game.docx`, `Quản lý công việc - PRU213`.
Danh sách 75 việc chi tiết: [cong-viec.md](cong-viec.md). 

## Giả định (sửa nếu sai, lịch dịch theo)

| Giả định | Giá trị | Vì sao |
|---|---|---|
| Hạn nộp/demo | **CN 13/12/2026** (10 tuần từ 5/10) | Chưa có hạn trong tài liệu; GDD nói 5 tháng nhưng môn PRU213 là 1 học kỳ |
| Ngày làm việc | Thứ 2 → Thứ 7. **Chủ nhật = họp checkpoint + dự phòng** | Mỗi người tối đa 1 việc/ngày |
| Phân vai | Theo 5 vai của GDD, gán tên theo ảnh nhóm | Chưa biết ai giỏi gì; đổi tên là đổi 1 cột |
| Phạm vi | 5 màn + mở đầu, câu hỏi 2 hình thức, hòm ngẫu nhiên, bảng xếp hạng **tại máy**; online là điểm cộng | Bám GDD v5 và BR v2 |

## Phân vai

| Thành viên | Vai (theo GDD) | Phụ trách chính | Số việc riêng |
|---|---|---|---|
| **Bạn** (Trưởng nhóm) | FPS Gameplay Lead + tích hợp | Điều khiển, ngắm/giật/thể lực, **thế giới đứng yên**, túi đồ, luồng thắng/thua từng màn, nâng cấp, build | 17 |
| **Bùi Nguyễn Thành Đạt** | AI & Combat | AI lính/phản kích, lô cốt, xe tăng, lựu đạn, hòm ngẫu nhiên + bảo hiểm, vật tư nhiệm vụ, gọi pháo, cân bằng, kiểm thử Phụ lục A | 15 |
| **Lê Nguyễn Đình Khôi** | 3D Artist | Hào modular, môi trường 5 màn, model súng/lính/lô cốt, VFX | 12 |
| **Thiên Trí** | Quiz & Narrative | Ngân hàng ≥120 câu có nguồn, bộ chọn câu, UI câu hỏi + hầm, kịch bản, báo cáo | 12 |
| **Trần Hoàng Long** | Audio / Data / UI | JSON loader, lưu hồ sơ, điểm, Validate Content, menu/HUD/cửa hàng, âm thanh, bảng xếp hạng, video | 16 |

Cùng 3 việc chung: T37 (playtest Màn 1), T71 (sửa lỗi cuối, Bạn+Đạt+Khôi), T75 (tập demo).

## Lịch 10 tuần

| Tuần | Ngày | Mục tiêu | Mốc cuối tuần (Chủ nhật) |
|---|---|---|---|
| 0 | 2–3/10 | Project, Git, chốt vai/phạm vi/Phụ lục B | họp chốt |
| 1 | 5–10/10 | Mosin bắn được, AI lính, hào modular, schema câu hỏi, JSON loader | **CP1 11/10** đi hào greybox, bắn, lính bắn trả |
| 2 | 12–17/10 | ADS, **giữ E → đứng yên → câu hỏi**, lô cốt, bộ chọn câu, tính điểm, lưu hồ sơ | **CP2 18/10** lát cắt dọc chạy được |
| 3 | 19–24/10 | Túi đồ, hòm ngẫu nhiên + bảo hiểm, môi trường Màn 1, UI hầm, menu hồ sơ | |
| 4 | 26–31/10 | Luồng Màn 1, vật tư nhiệm vụ, gọi pháo, HUD, hướng dẫn, playtest | **CP3 1/11** Màn 1 trọn vẹn |
| 5 | 2–7/11 | Nâng cấp, AI phản kích, **Validate Content**, môi trường + câu hỏi Màn 2 | |
| 6 | 9–14/11 | Luồng Màn 2, Chaffee, Màn 3 môi trường + câu hỏi, cửa hàng, âm thanh gói 2 | **CP4 15/11** Màn 2 xong, Validate chạy |
| 7 | 16–21/11 | Luồng Màn 3–4, AI hào, MAT-49/Kiểu 50, bảng xếp hạng | **CP5 22/11** Màn 3–4 xong |
| 8 | 23–28/11 | Màn 5 (cầu, hầm chỉ huy, A1, cờ), gửi điểm online (cộng điểm) | **CP6 29/11 đóng băng tính năng** |
| 9 | 30/11–5/12 | Tối ưu, cân bằng, VFX, rà ngân hàng câu, build, kiểm thử Phụ lục A | build ứng viên 5/12 |
| 10 | 7–12/12 | Sửa lỗi (khóa code 10/12), báo cáo, video, tập demo | **Nộp CN 13/12** |

## Tiêu chí điểm → việc nào lấy điểm

| Tiêu chí | % | Việc chính | Cách chứng minh |
|---|---|---|---|
| Ý tưởng & thiết kế | 20 | T03, T13 (đóng băng thế giới), T34, T42–T61, T68 | Báo cáo + câu hỏi có nguồn lịch sử |
| Kỹ thuật | 30 | T02, T11, T17, T19, T20, T40, T63, T64, T70 | Unit test, Validate = 0 lỗi, ≥60 FPS, build chạy 3 máy |
| Gameplay | 20 | T05–T08, T12–T15, T21–T23, T29–T31, T38–T39, T43–T45, T50–T52, T58–T59, T65–T66 | 5 màn thắng được dù trả lời sai hết (BR-17) |
| Giao diện & UX | 15 | T18, T25, T27, T35, T36, T48, T57, T69 | Người ngoài nhóm chơi không cần hỏi (có hướng dẫn) |
| Đồ họa & âm thanh | 15 | T04, T09, T16, T24, T28, T32–T33, T41, T46, T49, T54–T55, T60, T67 | Style low-poly nhất quán, ≥18 hiệu ứng âm |
| Điểm cộng +5% mỗi mục | | Tính năng nổi bật (T13) · bảng xếp hạng (T57, T62) · đa nền tảng (**chưa lên kế hoạch**, xem câu hỏi mở) | |

## Thứ tự cắt giảm nếu trễ (quyết ngay ở checkpoint, không để dồn)

1. T62 gửi điểm online (chỉ là điểm cộng, tuần 8)
2. T44 + T53 và nhạc nền riêng mỗi màn
3. Màn 4 thu gọn còn 2 đoạn hào (T51, T54)
4. **Không cắt**: T13 đóng băng, T30 vật tư nhiệm vụ, T40 Validate, T66 kiểm thử — mất điểm kỹ thuật/gameplay

## Rủi ro

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| Khôi (3D) là nút cổ chai: 5 môi trường + nhiều model trong 10 tuần | Cao | Chỉ dựng hào module (T09), môi trường là ghép; dùng asset low-poly miễn phí cho props |
| Câu hỏi: cần ≥120 câu có nguồn đáng tin; Thiên Trí làm một mình | Cao | Mỗi tuần nộp 25 câu; cả nhóm cùng tìm tài liệu lịch sử; câu cấp 4–5 để cuối |
| Chưa chốt Phụ lục B (giá nâng cấp, vũ khí mang sang màn sau) | Trung | T03 họp 3/10; ghi mặc định BR v2 |
| Thế giới đứng yên (`timeScale=0`) làm hỏng UI/animation | Trung | T13 làm sớm (tuần 2), kiểm ở checkpoint 2 |
| Long gánh nhiều việc (dữ liệu + UI + âm) | Trung | T62 là việc đầu tiên bị cắt; Bạn nhận lại việc build/Git khi trễ |
| BR v2 là *đề xuất*, chưa kiểm cân bằng | Thấp | T65 cân bằng ở tuần 9, tăng phiên bản nội dung nếu đổi (BR-40) |

## Câu hỏi mở

1. Hạn nộp thật là ngày nào? (đang giả định CN 13/12/2026)
2. Phân vai trên có đúng sở trường từng người không?
3. Có làm bản **WebGL/Android** để lấy điểm cộng đa nền tảng không? (hiện bỏ ngoài kế hoạch)
4. Chọn dịch vụ cho bảng xếp hạng online (Firebase, Supabase…) trước tuần 8 nếu muốn giữ T62.
