# Kế hoạch triển khai: Điện Biên Phủ 1954 (FPS, Unity URP) — 2 tuần

Nguồn: GDD v5, `Business_rule_DBP1954_v2.md`, `Tiêu chí đánh giá dự án Unity Game.docx`, `Quản lý công việc - PRU213`.
Danh sách 75 việc chi tiết: [cong-viec.md](cong-viec.md). 
Repo: https://github.com/Phanbatien/dien-bien-phu-1954

## Khung thời gian

| Mục | Giá trị |
|---|---|
| Thời gian làm | **Thứ 5 08/10 → Thứ 3 20/10/2026** (13 ngày, làm cả Chủ nhật) |
| Nộp bài | **Thứ 4 21/10/2026** |
| Phạm vi | **Đủ 75 việc, không cắt việc nào.** 23 việc đã xong và 3 việc làm được một phần (tính đến 07/10); còn 49 việc |
| Đơn vị lịch | Mỗi ngày 2 buổi (sáng/chiều). Việc cỡ S = 1 buổi, M = 2 buổi, L = 3 buổi |
| Sức chứa | 26 buổi mỗi người. Tải thực tế: Bạn 25 · Đạt 26 · Khôi 24 · Trí 24 · Long 25 |

> ⚠️ Lịch gần như **không còn dự phòng**: mỗi người dùng 24–26 trên 26 buổi. Trễ một buổi là phải bù ngay tối hôm đó.
> Vì vậy họp nhanh 15 phút mỗi tối và hai buổi làm chung (16/10, 19/10) là bắt buộc.

## Các mốc

| Mốc | Thời điểm | Phải đạt |
|---|---|---|
| Họp khởi động (T03) | **Sáng 08/10** | Chốt vai, quy trình, các quyết định ở [chương trình họp](quyet-dinh/0001-khoi-dong.md) |
| Nền móng | Sáng 09/10 | Lính AI bắn trả, hào modular, câu hỏi đọc từ JSON |
| Lát cắt dọc | Chiều 11/10 | Hầm chuẩn bị, UI câu hỏi thật, lưu hồ sơ, hòm ngẫu nhiên |
| Màn 2 + Validate | Chiều 15/10 | Phản kích, xe tăng, Validate Content chạy được |
| **Playtest Màn 1 (cả nhóm)** | **Chiều 16/10** | 3 người ngoài nhóm chơi hết Màn 1 |
| Màn 3, 4 | Sáng 17/10 | Hai màn chơi trọn vẹn |
| **Đóng băng tính năng** | **Chiều 18/10** | Đủ 5 màn chơi được; từ đây chỉ sửa lỗi và hoàn thiện |
| **Sửa lỗi chung** | **Cả ngày 19/10** | Bạn, Đạt, Khôi cùng sửa lỗi; Trí viết báo cáo; Long làm video |
| Build + tập demo | 20/10 | Bản `.exe` cuối, tag v1.0, tập demo chiều 20/10 |
| **Nộp** | **21/10** | Build + báo cáo + video |

## Lịch từng người (sáng / chiều)

Ô có một mã nghĩa là cả ngày làm việc đó. "—" là buổi trống: dùng để bù trễ hoặc hỗ trợ người khác.

| Ngày | Bạn | Đạt | Khôi | Trí | Long |
|---|---|---|---|---|---|
| T5 08/10 | T03 / T11 | T07 | T04 / T09 | — / T10 | T35 |
| T6 09/10 | T11 / T17 | T08 / T14 | T09 / T16 | T10 / T26 | T36 |
| T7 10/10 | T17 / T20 | T15 | T16 / T24 | T34 | T19 / T28 |
| CN 11/10 | T20 / T22 | T39 | T24 / T32 | T18 | T48 |
| T2 12/10 | T22 / T23 | T52 | T32 / T33 | T25 | T27 |
| T3 13/10 | T23 / T31 | T30 | T33 / T41 | T42 | T49 |
| T4 14/10 | T31 / T40 | T45 | T41 / T46 | T47 | T57 |
| T5 15/10 | T40 | T45 / T59 | T46 / T55 | T56 | T62 |
| T6 16/10 | T54 / **T37** | T59 / **T37** | T55 / **T37** | T53 / **T37** | T62 / **T37** |
| T7 17/10 | T54 / T63 | T65 | — / T60 | T61 | T69 |
| CN 18/10 | T63 / — | T66 | T60 | T68 | — / T73 |
| T2 19/10 | **T71** | **T71** | **T71** | T72 | T73 / T67 |
| T3 20/10 | T74 / **T75** | T70 / **T75** | — / **T75** | — / **T75** | T67 / **T75** |

Thời gian chính xác (buổi bắt đầu → buổi kết thúc) của từng việc nằm trong [cong-viec.md](cong-viec.md).

## Chuyển việc để cân tải (không bỏ việc nào)

| Việc | Từ | Sang | Lý do |
|---|---|---|---|
| T11 JSON loader · T17 bộ chọn câu · T20 lưu hồ sơ · T40 Validate Content | Long, Trí | **Bạn** | Việc code thuần, nối thẳng vào hệ thống câu hỏi/hồ sơ bạn đã viết |
| T22 hòm ngẫu nhiên · T23 bảo hiểm 3 lần sai · T31 điện thoại gọi pháo | Đạt | **Bạn** | Đều đi qua `QuizCrate` / `FieldQuizSession` bạn đã viết |
| T54 môi trường Màn 4 | Khôi | **Bạn** | Ghép từ module hào có sẵn; Khôi đang kín lịch |
| T67 VFX | Khôi | **Long** | Long trống ngày 18/10 |
| T53 chỉ số + nguồn MAT-49/Kiểu 50 | Đạt | **Trí** | Chủ yếu là tra nguồn lịch sử |
| T70 chạy Validate cuối | Long | **Đạt** | Đạt trống sáng 20/10 |

## Phân vai

| Thành viên | Vai | Việc còn lại |
|---|---|---|
| **Bạn** (Trưởng nhóm) | FPS Lead + tích hợp + dữ liệu | ~~T11 T17 T20 T22 T23 T31 T40 T54~~ (đã xong) · còn T03 T63 T74 + T37 T71 T75 |
| **Bùi Nguyễn Thành Đạt** | AI & Combat | T07 T08 T14 T15 T30 T39 T45 T52 T59 T65 T66 T70 + T37 T71 T75 |
| **Lê Nguyễn Đình Khôi** | 3D Artist | T04 T09 T16 T24 T32 T33 T41 T46 T55 T60 + T37 T71 T75 |
| **Thiên Trí** | Quiz & Narrative | T10 T18 T25 T26 T34 T42 T47 T53 T56 T61 T68 T72 + T37 T75 |
| **Trần Hoàng Long** | Audio / UI | T19 T27 T28 T35 T36 T48 T49 T57 T62 T67 T69 T73 + T37 T75 |

## Đã xong (07/10)

| Đợt | Việc | Nội dung |
|---|---|---|
| Trước 07/10 | T01 T02 T05 T06 T12 T13 T21 T29 T38 T43 T44 T50 T51 T58 T64 | Project Unity, bộ điều khiển FPS, bắn/ngắm/thay đạn, thế giới đứng yên, túi đồ, luồng 5 màn (greybox), nâng cấp, Git/LFS/nhánh, build Windows |
| 07/10 (sớm hơn lịch) | T11 T17 T20 T22 T23 T31 T40 T54 | Đọc JSON, bộ chọn câu không lặp, lưu hồ sơ an toàn, hòm ngẫu nhiên theo seed, bảo hiểm 3 lần sai + súng rơi, điện thoại gọi pháo, Validate Content, môi trường Màn 4 |
| Một phần 🟡 | T03 · T63 · T74 | Chương trình họp đã soạn · công cụ đo FPS (máy trưởng nhóm 1155–1224 FPS ở bản greybox) · lệnh build + nén `.zip` (bản cuối chạy 20/10) |

Kiểm tra: **46/46 test qua** (39 EditMode + 7 PlayMode), build Development thành công, bản nộp bị Validate chặn đúng (còn 37 lỗi nội dung).

> **Lịch của trưởng nhóm giờ trống từ chiều 08/10 đến 16/10** (trừ T63, T74 và các buổi làm chung). Đề xuất dùng để: soát câu hỏi cùng Trí, hỗ trợ AI cho Đạt, nối UI của Long/Trí vào hệ thống. Chốt trong buổi họp.

> **Phát hiện từ Validate Content:** theo BR-28, mỗi màn cần khoảng **47 câu đã duyệt** (11 câu cấp 1–2, 10–11 câu cấp 3, 10 câu cấp 4, 10 câu cấp 5, 5 câu trong hầm), trong khi kế hoạch có 25 câu/màn. Câu dùng chung (`missionId: "CAMPAIGN"`) được tính cho mọi màn, nên viết khoảng **50 câu dùng chung** cộng thêm câu riêng từng màn là đủ, tổng vẫn khoảng 125 câu. Cần chốt trong buổi họp.

## Tiêu chí điểm → việc nào lấy điểm

| Tiêu chí | % | Việc chính |
|---|---|---|
| Ý tưởng & thiết kế | 20 | T03, T13, T34, T26/T42/T47/T56/T61, T68 |
| Kỹ thuật | 30 | T02, T11, T17, T19, T20, T40, T63, T64, T70 |
| Gameplay | 20 | T07–T08, T14–T15, T22–T23, T30–T31, T39, T45, T52, T59, T65–T66 |
| Giao diện & UX | 15 | T18, T25, T27, T35, T36, T48, T57, T69 |
| Đồ họa & âm thanh | 15 | T09, T16, T24, T28, T32–T33, T41, T46, T49, T54–T55, T60, T67 |
| Điểm cộng | +5 mỗi mục | T13 (tính năng nổi bật) · T57 + T62 (bảng xếp hạng) |

## Rủi ro

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| Không còn dự phòng: ai trễ là kéo cả nhóm | **Cao** | Họp 15 phút mỗi tối; buổi "—" dùng để bù; trễ quá một buổi thì báo ngay để người trống hỗ trợ |
| Khôi phải dựng 5 môi trường + nhiều model | Cao | Dùng asset low-poly miễn phí (giấy phép CC0) cho đồ phụ; môi trường ghép từ module hào |
| Trí viết khoảng 125 câu có nguồn trong 9 ngày | Cao | Mỗi màn 25 câu; dùng sách giáo khoa Lịch sử 12 và tài liệu bảo tàng làm nguồn chính; nhờ cả nhóm soát vào buổi trống |
| Hai người cùng sửa một scene → xung đột Git | Trung | Mỗi scene một người giữ (chốt trong buổi họp); làm trong prefab riêng |
| Nhánh `main` cần 1 người duyệt PR | Thấp | Làm hằng ngày trên `develop`; chỉ merge vào `main` ở các mốc |
