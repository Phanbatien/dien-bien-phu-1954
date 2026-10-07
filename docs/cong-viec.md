# Danh sách công việc: Điện Biên Phủ 1954 (FPS) — kế hoạch 2 tuần

> **Làm từ 08/10 đến 20/10/2026, nộp 21/10/2026.** Đủ 75 việc, không cắt việc nào. Mỗi ngày chia 2 buổi (sáng/chiều); làm cả Chủ nhật. Cỡ việc: S = 1 buổi, M = 2 buổi, L = 3 buổi.
> ✅ đã xong · 🟡 làm được một phần · ☐ chưa làm. *(nhận từ …)* = việc được chuyển người để cân tải.

Tiêu chí điểm: **Ý tưởng** 20% · **KT** 30% · **Gameplay** 20% · **UI** 15% · **Đồ họa/Âm thanh** 15% · **Điểm cộng** +5%/mục. BR = mã quy tắc trong business rules; PL-A = Phụ lục A.


## Giai đoạn 0 · Khởi động

| ✔ | ID | Công việc | Phụ trách | Thời gian | Cấp độ | Cỡ | Kết quả cần có | Cách kiểm | Phụ thuộc | Tiêu chí | BR |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ✅ | T01 | Dựng project Unity URP + Starter Assets First Person | Bạn (Trưởng nhóm) | xong 07/10 | Khẩn cấp | S | Project mở được trên cả 5 máy; đi, nhìn, nhảy được trên mặt phẳng bùn | 5 người pull về, bấm Play không lỗi | - | KT | - |
| ✅ | T02 | Git/GitHub + Git LFS + quy ước nhánh/commit | Trần Hoàng Long | xong 07/10 | Khẩn cấp | S | Có .gitignore Unity, LFS cho fbx/png/wav, nhánh main/develop/feature/* | 1 PR thử được merge; 5 máy clone chạy được | - | KT | - |
| 🟡 | T03 | Họp chốt vai trò, phạm vi MVP, Phụ lục B (BR v2), Definition of Done | Bạn (Trưởng nhóm) | 08/10 sáng · đã soạn chương trình họp | Khẩn cấp | S | Biên bản chốt: persistence, giá nâng cấp, số câu/màn, ai làm gì; danh sách cắt giảm nếu trễ | Cả nhóm đọc và đồng ý | - | Ý tưởng | BR-40, PL-B |
| ☐ | T04 | Style guide low-poly + 1 module hào mẫu | Lê Nguyễn Đình Khôi | 08/10 sáng | Cao | S | Bảng màu, kích thước lưới module, 1 đoạn hào import đúng tỉ lệ | Nhân vật Starter Assets đi lọt hào | T01 | Đồ họa | - |

## Giai đoạn 1 · Nền móng FPS

| ✔ | ID | Công việc | Phụ trách | Thời gian | Cấp độ | Cỡ | Kết quả cần có | Cách kiểm | Phụ thuộc | Tiêu chí | BR |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ✅ | T05 | Mosin: bắn raycast, bắn phát một, hiệu ứng trúng đích | Bạn (Trưởng nhóm) | xong 07/10 | Cao | S | Bắn trúng vật thể, có vết đạn, trừ máu mục tiêu thử | Bắn 10 phát vào hộp thử, máu giảm đúng | T01 | Gameplay | - |
| ✅ | T06 | Thay đạn + cơ số đạn | Bạn (Trưởng nhóm) | xong 07/10 | Cao | S | Hết băng phải thay, đạn dự trữ giảm đúng, không bắn khi đang thay | Test thủ công + log số đạn | T05 | Gameplay | BR-36 |
| ☐ | T07 | AI lính sau bao cát (đứng → bắn → nấp khi bị bắn) | Bùi Nguyễn Thành Đạt | 08/10 sáng → 08/10 chiều | Cao | M | Lính bắn trả người chơi, hạ thấp người khi bị áp chế | Quay clip 30 giây lính hành xử đúng 3 trạng thái | T01 | KT, Gameplay | BR-20 |
| ☐ | T08 | Hệ máu/sát thương chung + địch ngã rồi mờ dần | Bùi Nguyễn Thành Đạt | 09/10 sáng | Cao | S | Người chơi và địch dùng chung IDamageable; chết thì ngã, mờ, không máu me | Hạ 5 lính: không lỗi, không rơi đồ hai lần | T05,T07 | Gameplay | BR-21 |
| ☐ | T09 | Bộ hào modular v1 (thẳng, góc, chữ T, bao cát, hầm chữ A) | Lê Nguyễn Đình Khôi | 08/10 chiều → 09/10 sáng | Cao | M | ≥6 prefab hào ghép được khít nhau, có collider | Ghép thử 1 đoạn hào 30 m, đi hết không kẹt | T04 | Đồ họa | - |
| ☐ | T10 | Chốt schema questions/sources + 20 câu đầu Màn 1 | Thiên Trí | 08/10 chiều → 09/10 sáng | Cao | M | Schema khớp GDD §8 (id đáp án, correct theo id, forms, level, source); 20 câu có nguồn | E đọc được file bằng loader (T11) | T03 | Ý tưởng | BR-27,28 |
| ✅ | T11 | JSON loader: manifest, difficulty, questions + lớp dữ liệu | Bạn (Trưởng nhóm) *(nhận từ Long)* | xong 07/10 (sớm; lịch cũ 08/10 chiều → 09/10 sáng) | Cao | M | Đọc StreamingAssets/content, báo lỗi rõ khi thiếu/hỏng file | Unit test đọc file mẫu + file hỏng | T02 | KT | BR-27 |

### ★ Checkpoint 1 (hết 09/10 sáng): đi trong hào greybox, bắn Mosin, 1 lính AI bắn trả; repo sạch.
- [ ] Cả nhóm chơi bản mới nhất trên `develop`, ghi lỗi; chưa đạt thì dồn sang buổi kế tiếp, không bỏ mốc.


## Giai đoạn 2 · Lát cắt dọc: hòm → đóng băng → câu hỏi

| ✔ | ID | Công việc | Phụ trách | Thời gian | Cấp độ | Cỡ | Kết quả cần có | Cách kiểm | Phụ thuộc | Tiêu chí | BR |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ✅ | T12 | ADS + giật súng + thanh thể lực | Bạn (Trưởng nhóm) | xong 07/10 | Cao | M | Ngắm chuột phải, giật có phục hồi, chạy hết thể lực thì phải nghỉ | Test thủ công 3 tình huống | T05 | Gameplay | - |
| ✅ | T13 | Thế giới đứng yên + giữ E 1 giây + 1 hòm thử | Bạn (Trưởng nhóm) | xong 07/10 | Khẩn cấp | M | Giữ E đủ 1 s mở câu; đạn/AI/pháo dừng; khóa camera; thả E hoặc chết thì hủy; 0,5 s chuyển tiếp | PL-A dòng 6–7: thả E ở 0,9 s không mở; địch không bắn trong 0,5 s | T08,T11 | Ý tưởng, Gameplay (tính năng nổi bật) | BR-08,09,12 |
| ☐ | T14 | Lựu đạn nổ theo vùng | Bùi Nguyễn Thành Đạt | 09/10 chiều | Cao | S | Lựu đạn gây sát thương theo bán kính, đẩy vật lý | Ném vào 3 lính: máu giảm theo khoảng cách | T08 | Gameplay | - |
| ☐ | T15 | Lô cốt (cung quét cố định, miễn súng cá nhân) + bộc phá phá được | Bùi Nguyễn Thành Đạt | 10/10 sáng → 10/10 chiều | Cao | M | Đạn súng không gây sát thương lô cốt; bộc phá/pháo phá được | Bắn 100 phát: không thủng; đặt bộc phá: sập | T08 | Gameplay | BR-20 |
| ☐ | T16 | Mosin góc nhìn thứ nhất: model + tay + hoạt ảnh bắn/nạp | Lê Nguyễn Đình Khôi | 09/10 chiều → 10/10 sáng | Cao | M | Model low-poly ≤3k tris, anim bắn, nạp, đi bộ | Gắn vào T05, khớp thời gian nạp | T04 | Đồ họa | - |
| ✅ | T17 | Bộ chọn câu không lặp theo cấp/hình thức/màn | Bạn (Trưởng nhóm) *(nhận từ Trí)* | xong 07/10 (sớm; lịch cũ 09/10 chiều → 10/10 sáng) | Cao | M | Không lặp mã câu trong lượt, lọc approved, dự trữ câu cho nhánh còn lại; báo lỗi khi thiếu | Unit test: gọi hết ngân hàng không lặp, thiếu câu thì báo lỗi | T10,T11 | KT | BR-31,28 |
| ☐ | T18 | UI câu hỏi ngoài trận (đếm ngược thời gian thực, đáp án không bị cắt) | Thiên Trí | 11/10 sáng → 11/10 chiều | Khẩn cấp | M | Hiện câu + 2–4 đáp án + đồng hồ chạy khi timeScale=0; hết giờ tính là sai | Chạy cùng T13; đo 12/15/18/20 s đúng | T13,T17 | UI | BR-10 |
| ☐ | T19 | Tính điểm kiến thức (cấp 1–5, thưởng tốc độ, làm tròn từng câu) | Trần Hoàng Long | 10/10 sáng | Cao | S | Hàm tính điểm đúng 3 ví dụ BR-25 (360, 300, 125) | Unit test 3 ví dụ + ca biên 0/12 giây | T11 | KT | BR-25 |
| ✅ | T20 | Lưu hồ sơ/tiến độ an toàn (ghi tạm rồi đổi tên) | Bạn (Trưởng nhóm) *(nhận từ Long)* | xong 07/10 (sớm; lịch cũ 10/10 chiều → 11/10 sáng) | Cao | M | Tạo/sửa hồ sơ, lưu progress.json; tắt đột ngột không hỏng file | Kill process lúc ghi, mở lại vẫn đọc được | T11 | KT | BR-22,37 |

### ★ Checkpoint 2 (hết 11/10 chiều): lát cắt dọc chạy được: giữ E mở hòm → đứng yên → trả lời → nhận thưởng đúng/sai.
- [ ] Cả nhóm chơi bản mới nhất trên `develop`, ghi lỗi; chưa đạt thì dồn sang buổi kế tiếp, không bỏ mốc.


## Giai đoạn 3 · Màn 1 Him Lam

| ✔ | ID | Công việc | Phụ trách | Thời gian | Cấp độ | Cỡ | Kết quả cần có | Cách kiểm | Phụ thuộc | Tiêu chí | BR |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ✅ | T21 | Túi đồ: tối đa 2 súng, đổi súng, đạn tương thích | Bạn (Trưởng nhóm) | xong 07/10 | Cao | M | Nhận súng khi đầy thì thay súng đang cầm; súng cũ nằm lại; đạn dư ở lại điểm nhận | PL-A dòng 14 | T06,T13 | Gameplay | BR-36 |
| ✅ | T22 | Hòm ngẫu nhiên: rút điểm theo seed, độ hiếm, câu hỏi theo cấp | Bạn (Trưởng nhóm) *(nhận từ Đạt)* | xong 07/10 (sớm; lịch cũ 11/10 chiều → 12/10 sáng) | Khẩn cấp | M | Mỗi lượt rút 5–7/10–15 điểm, lưu seed; hòm hiện đúng màu theo độ hiếm | Cùng seed ra cùng vị trí; 1000 lần rút khớp trọng số | T13 | Gameplay | BR-13,15 |
| ✅ | T23 | Bảo hiểm sau 3 hòm sai + súng rơi từ địch | Bạn (Trưởng nhóm) *(nhận từ Đạt)* | xong 07/10 (sớm; lịch cũ 12/10 chiều → 13/10 sáng) | Cao | M | Sai 3 hòm liên tiếp → hòm sau câu cấp 1/12 s; địch rơi súng theo xác suất, có giới hạn | PL-A dòng 4–5 | T22,T17 | Gameplay | BR-14,16 |
| ☐ | T24 | Môi trường Màn 1 (rào kẽm, hố, 10–15 điểm hòm, vị trí lô cốt) | Lê Nguyễn Đình Khôi | 10/10 chiều → 11/10 sáng | Cao | M | Màn đi được từ điểm xuất phát tới tập kết; có sương, ánh sáng pháo | Chạy hết màn greybox→dressed ≥60 FPS | T09 | Đồ họa | - |
| ☐ | T25 | UI hầm chuẩn bị: 3–5 câu, giải thích đáp án, Quyết Tâm chờ | Thiên Trí | 12/10 sáng → 12/10 chiều | Cao | M | Câu hầm không giới hạn giờ; sai thì hiện đáp án + giải thích; chưa cộng Quyết Tâm đến khi thắng | PL-A dòng 3 | T17,T20 | UI, Ý tưởng | BR-6,7,34 |
| ☐ | T26 | Hoàn thiện 25 câu Màn 1 (approved, có nguồn, thân ≤120 ký tự) | Thiên Trí | 09/10 chiều | Cao | S | 25 câu qua checklist; ≥6 câu dùng được cả hầm và ngoài trận | Validate (T40) không báo lỗi M1 | T10 | Ý tưởng | BR-28 |
| ☐ | T27 | Menu: tạo hồ sơ + xác nhận gửi thành tích + chọn màn + âm lượng | Trần Hoàng Long | 12/10 sáng → 12/10 chiều | Cao | M | Nhiều hồ sơ/máy; chỉ lưu tên, lớp, consentAt; màn khóa/mở đúng | Tạo 2 hồ sơ trùng tên không gộp | T20 | UI | BR-22 |
| ☐ | T28 | Âm thanh gói 1: Mosin, bước bùn, pháo xa, máy bay nền | Trần Hoàng Long | 10/10 chiều | Thường | S | ≥8 hiệu ứng có giấy phép dùng được; máy bay chỉ là âm thanh | Nghe thử toàn màn, không có mô hình máy bay | T11 | Âm thanh | BR-04 |
| ✅ | T29 | Luồng Màn 1: mục tiêu, thắng/thua/bỏ dở, mở khóa Màn 2, chơi lại | Bạn (Trưởng nhóm) | xong 07/10 | Khẩn cấp | M | Thắng = qua hướng dẫn + vượt rào + phá hết lô cốt + tới tập kết; chết thì ưu tiên thua; chơi lại tạo lượt mới | PL-A dòng 12; chỉ chốt kết quả 1 lần | T21,T23 | Gameplay | BR-32,33 |
| ☐ | T30 | Vật tư nhiệm vụ không qua câu hỏi + điểm cấp bổ sung | Bùi Nguyễn Thành Đạt | 13/10 sáng → 13/10 chiều | Khẩn cấp | M | Sai mọi câu vẫn có đủ bộc phá cho mọi lô cốt; hết vật tư thì điểm cấp bổ sung | PL-A dòng 1–2: chơi Màn 1 trả lời sai hết | T15,T22 | Gameplay | BR-17,30 |
| ✅ | T31 | Điện thoại gọi pháo: đúng → trúng, sai → trượt | Bạn (Trưởng nhóm) *(nhận từ Đạt)* | xong 07/10 (sớm; lịch cũ 13/10 chiều → 14/10 sáng) | Cao | M | Câu cấp 3, 15 s; mục tiêu đã phá thì không mở câu | PL-A dòng 15 | T13,T15 | Gameplay | BR-38 |
| ☐ | T32 | Lính Pháp low-poly + hoạt ảnh (đi, bắn, nấp, ngã) | Lê Nguyễn Đình Khôi | 11/10 chiều → 12/10 sáng | Cao | M | 1 model, ≥4 anim, rig dùng chung | Gắn vào AI T07 | T04 | Đồ họa | - |
| ☐ | T33 | Model lô cốt, bộc phá, lựu đạn chày | Lê Nguyễn Đình Khôi | 12/10 chiều → 13/10 sáng | Cao | M | 3 model low-poly có collider | Đặt vào Màn 1 | T04 | Đồ họa | - |
| ☐ | T34 | Storyboard mở đầu 90 s + nhật ký trong hầm Màn 1 | Thiên Trí | 10/10 sáng → 10/10 chiều | Thường | M | Kịch bản kéo pháo, Tô Vĩnh Diện, dân công; không điều khiển; nhân vật thật chỉ ở cốt truyện | Cả nhóm đọc duyệt | T03 | Ý tưởng | BR-01,02,32 |
| ☐ | T35 | HUD (máu, đạn, thể lực, mục tiêu) | Trần Hoàng Long | 08/10 sáng → 08/10 chiều | Cao | M | HUD cập nhật đúng; hiện mục tiêu còn lại | Test cùng T29 | T06,T12 | UI | - |
| ☐ | T36 | Hướng dẫn chơi Màn 1 + màn kết quả | Trần Hoàng Long | 09/10 sáng → 09/10 chiều | Cao | M | Popup phím WASD/chuột/E/R; kết quả hiện điểm, Quyết Tâm, nút chơi lại | Người ngoài nhóm chơi không cần hỏi | T35 | UI | - |
| ☐ | T37 | Playtest Màn 1 từ đầu tới cuối + sửa lỗi | Cả nhóm | 16/10 chiều | Khẩn cấp | S | 3 người ngoài nhóm chơi hết Màn 1; danh sách lỗi có độ ưu tiên | Ghi lại ≥5 lỗi/nhận xét | T29..T36 | Gameplay | - |

### ★ Checkpoint 3 (hết 16/10 chiều): Màn 1 chơi trọn vẹn, có hầm → chiến trường → kết quả → lưu.
- [ ] Cả nhóm chơi bản mới nhất trên `develop`, ghi lỗi; chưa đạt thì dồn sang buổi kế tiếp, không bỏ mốc.


## Giai đoạn 4 · Màn 2 + Công cụ kiểm tra

| ✔ | ID | Công việc | Phụ trách | Thời gian | Cấp độ | Cỡ | Kết quả cần có | Cách kiểm | Phụ thuộc | Tiêu chí | BR |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ✅ | T38 | Nâng cấp: mua trong hầm, áp dụng lúc xuất trận | Bạn (Trưởng nhóm) | xong 07/10 | Cao | M | 4 nhánh × 3 cấp theo giá BR-35; thiếu điểm thì từ chối không trừ; áp dụng vào chỉ số | Mua đủ rồi vào màn: đạn +30%, thay đạn ×0,7 | T20,T29 | Gameplay, Ý tưởng | BR-35 |
| ☐ | T39 | AI nhóm phản kích 4–8 lính + lính gác (tuần tra → nghi ngờ → báo động) | Bùi Nguyễn Thành Đạt | 11/10 sáng → 11/10 chiều | Khẩn cấp | M | Nhóm có yểm trợ + xung phong; rút khi mất HƠN một nửa; đẩy lùi chỉ tính 1 lần | Mất đúng 1/2 chưa rút | T07,T08 | Gameplay | BR-20 |
| ✅ | T40 | Validate Content (Editor): schema, mã duy nhất, tham chiếu, 120 ký tự, approved, vũ khí ≤1954, tổng trọng số = 1, ngân sách câu | Bạn (Trưởng nhóm) *(nhận từ Long)* | xong 07/10 (sớm; lịch cũ 14/10 chiều → 15/10 chiều) | Khẩn cấp | L | Menu Tools → Validate; còn lỗi thì chặn Build | Cố ý nhét K-54, câu 121 ký tự, câu nháp: đều bị chặn | T11,T26 | KT | BR-27..30 |
| ☐ | T41 | Môi trường Màn 2 Giữ Độc Lập + ụ súng máy | Lê Nguyễn Đình Khôi | 13/10 chiều → 14/10 sáng | Cao | M | Đồi vừa chiếm, 3 hướng phản kích, ụ súng máy | Chạy hết màn ≥60 FPS | T24 | Đồ họa | - |
| ☐ | T42 | 25 câu Màn 2 (có nguồn, cấp 1–5) + câu chiến dịch dùng chung | Thiên Trí | 13/10 sáng → 13/10 chiều | Cao | M | 25 câu approved; ngân sách câu đủ cho 3 sai liên tiếp lặp lại | Validate không lỗi M2 | T10 | Ý tưởng | BR-28,31 |
| ✅ | T43 | Luồng Màn 2: đợt phản kích + Chaffee đợt cuối + súng máy tại ụ | Bạn (Trưởng nhóm) | xong 07/10 | Khẩn cấp | M | Thắng khi đẩy lùi hết đợt và phá Chaffee (dùng xe tăng giả, thay bằng T45 trước Checkpoint 4) | Chơi 2 lần ra 2 seed khác | T38,T39 | Gameplay | BR-32 |
| ✅ | T44 | Mosin ống ngắm (ADS zoom) cho Màn 3 | Bạn (Trưởng nhóm) | xong 07/10 | Cao | S | Zoom 4×, hiện thước ngắm, giật | Bắn xa 80 m trúng | T12 | Gameplay | - |
| ☐ | T45 | Xe tăng Chaffee: đường cố định, bộ binh hộ tống, sát thương hiệu lực | Bùi Nguyễn Thành Đạt | 14/10 sáng → 15/10 sáng | Khẩn cấp | L | Đi đúng tuyến; chỉ bị hạ bằng bazooka/bộc phá/pháo; luôn có phương án chống tăng | PL-A dòng 1: sai hết câu vẫn hạ được xe tăng | T30 | Gameplay | BR-17,20 |
| ☐ | T46 | Môi trường Màn 3 đồi E, D ban đêm + pháo sáng | Lê Nguyễn Đình Khôi | 14/10 chiều → 15/10 sáng | Cao | M | Ánh sáng đêm, pháo sáng định kỳ, chỗ ẩn nấp | Chạy hết màn ≥60 FPS | T41 | Đồ họa | - |
| ☐ | T47 | 25 câu Màn 3 + thoại vô tuyến | Thiên Trí | 14/10 sáng → 14/10 chiều | Cao | M | 25 câu approved + 5 đoạn thoại | Validate không lỗi M3 | T10 | Ý tưởng | BR-28 |
| ☐ | T48 | Cửa hàng nâng cấp trong hầm (UI) | Trần Hoàng Long | 11/10 sáng → 11/10 chiều | Cao | M | Hiện giá/cấp/điều kiện; nút mua bị khóa khi thiếu điểm | Mua 1 lần không bị trừ hai lần | T38 | UI | BR-35 |
| ☐ | T49 | Âm thanh gói 2: MAT-49, Kiểu 50, pháo 105, xe tăng, loa dã chiến | Trần Hoàng Long | 13/10 sáng → 13/10 chiều | Thường | M | ≥10 hiệu ứng mới, có nhạc nền mỗi màn | Nghe thử từng màn | T28 | Âm thanh | - |

### ★ Checkpoint 4 (hết 15/10 chiều): Màn 2 chơi trọn vẹn; Validate chạy được; lưu/mở khóa ổn.
- [ ] Cả nhóm chơi bản mới nhất trên `develop`, ghi lỗi; chưa đạt thì dồn sang buổi kế tiếp, không bỏ mốc.


## Giai đoạn 5 · Màn 3, 4

| ✔ | ID | Công việc | Phụ trách | Thời gian | Cấp độ | Cỡ | Kết quả cần có | Cách kiểm | Phụ thuộc | Tiêu chí | BR |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ✅ | T50 | Luồng Màn 3 (hạ lính gác + tổ súng máy, tới điểm rút) | Bạn (Trưởng nhóm) | xong 07/10 | Khẩn cấp | M | Thắng khi hết mục tiêu đánh dấu; tổ súng máy KHÔNG miễn đạn | Chơi Màn 3 hết 1 lượt | T44,T46,T47 | Gameplay | BR-32 |
| ✅ | T51 | Luồng Màn 4: chiếm các đoạn hào theo thứ tự | Bạn (Trưởng nhóm) | xong 07/10 | Khẩn cấp | M | Không chiếm đoạn sau khi chưa chiếm đoạn trước; còn sống và sạch địch mới tính chiếm | Chiếm bỏ qua đoạn: không tính | T50 | Gameplay | BR-32 |
| ☐ | T52 | AI cận chiến trong hào: ném lựu đạn khi áp sát, đổi chỗ nấp | Bùi Nguyễn Thành Đạt | 12/10 sáng → 12/10 chiều | Cao | M | Lính né, ném lựu đạn khi người chơi <8 m | Clip 30 s trong hào | T39 | Gameplay | BR-20 |
| ☐ | T53 | Chỉ số súng MAT-49, Kiểu 50 + bảng độ hiếm (weapons.json) | Thiên Trí *(nhận từ Đạt)* | 16/10 sáng | Cao | S | 2 súng có damage/rpm/mag/reload, có nguồn + availableFrom | Validate không báo vũ khí | T40 | Gameplay | BR-18,29 |
| ✅ | T54 | Môi trường Màn 4 Trong lòng hào | Bạn (Trưởng nhóm) *(nhận từ Khôi)* | xong 07/10 (sớm; lịch cũ 16/10 sáng → 17/10 sáng) | Cao | M | Mê cung hào + 4 đoạn hào để chiếm | Chạy hết màn ≥60 FPS | T46 | Đồ họa | - |
| ☐ | T55 | Model MAT-49 + Kiểu 50 (tay + hoạt ảnh) | Lê Nguyễn Đình Khôi | 15/10 chiều → 16/10 sáng | Cao | M | 2 model + anim bắn/nạp | Gắn vào T21 | T16 | Đồ họa | - |
| ☐ | T56 | 25 câu Màn 4 (có câu cấp 4–5) | Thiên Trí | 15/10 sáng → 15/10 chiều | Cao | M | Câu cấp 4–5 đủ cho hòm hiếm/rất hiếm | Validate đủ câu mỗi độ hiếm | T10 | Ý tưởng | BR-28 |
| ☐ | T57 | Bảng xếp hạng tại máy (5 tiêu chí, đồng hạng 1-1-3) + UI | Trần Hoàng Long | 14/10 sáng → 14/10 chiều | Cao | M | 2 bảng: chiến đấu và kiến thức; chỉ lượt thắng; giữ thành tích tốt nhất mỗi hồ sơ | PL-A dòng 10–11 qua unit test | T19,T27 | UI, Điểm cộng | BR-24,25 |

### ★ Checkpoint 5 (hết 17/10 sáng): Màn 3, 4 chơi trọn vẹn; có bảng xếp hạng tại máy.
- [ ] Cả nhóm chơi bản mới nhất trên `develop`, ghi lỗi; chưa đạt thì dồn sang buổi kế tiếp, không bỏ mốc.


## Giai đoạn 6 · Màn 5 + đóng băng tính năng

| ✔ | ID | Công việc | Phụ trách | Thời gian | Cấp độ | Cỡ | Kết quả cần có | Cách kiểm | Phụ thuộc | Tiêu chí | BR |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ✅ | T58 | Luồng Màn 5: vượt cầu, ổ đề kháng, vào hầm chỉ huy, cắm cờ | Bạn (Trưởng nhóm) | xong 07/10 | Khẩn cấp | L | Kết thúc kích hoạt đúng cảnh cắm cờ; epilogue Genève | Chơi hết Màn 5, thắng/thua đúng | T51 | Gameplay, Ý tưởng | BR-3,32 |
| ☐ | T59 | Màn 5: địch ổ đề kháng + cảnh nổ A1 theo kịch bản duyệt | Bùi Nguyễn Thành Đạt | 15/10 chiều → 16/10 sáng | Cao | M | Nổ A1 luôn xảy ra khi tới đoạn tương ứng, không phụ thuộc đáp án | Trả lời sai hết: A1 vẫn nổ | T52 | Gameplay | BR-3 |
| ☐ | T60 | Môi trường Màn 5 cầu Mường Thanh + hầm chỉ huy + cờ | Lê Nguyễn Đình Khôi | 17/10 chiều → 18/10 chiều | Cao | L | Cầu, hầm chỉ huy, cờ Quyết chiến Quyết thắng | Chạy hết màn ≥60 FPS | T54 | Đồ họa | - |
| ☐ | T61 | 25 câu Màn 5 + epilogue + màn Nguồn/Credits | Thiên Trí | 17/10 sáng → 17/10 chiều | Cao | M | 25 câu approved; màn credits ghi nguồn tư liệu | Validate không lỗi M5 | T10 | Ý tưởng | BR-2,28 |
| ☐ | T62 | (Điểm cộng) Gửi điểm online: outbox + thử lại + server chấm lại | Trần Hoàng Long | 15/10 sáng → 16/10 sáng | Thấp | L | Outbox giữ qua khởi động lại; thử lại 5 s/15 s/60 s/5 phút; gửi lại không nhân đôi | PL-A dòng 13, 16. Cắt nếu trễ | T57 | Điểm cộng | BR-26,39 |

### ★ Checkpoint 6 (hết 18/10 chiều): ĐÓNG BĂNG TÍNH NĂNG, đủ 5 màn chơi được.
- [ ] Cả nhóm chơi bản mới nhất trên `develop`, ghi lỗi; chưa đạt thì dồn sang buổi kế tiếp, không bỏ mốc.


## Giai đoạn 7 · Hoàn thiện

| ✔ | ID | Công việc | Phụ trách | Thời gian | Cấp độ | Cỡ | Kết quả cần có | Cách kiểm | Phụ thuộc | Tiêu chí | BR |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 🟡 | T63 | Tối ưu hiệu năng (≥60 FPS máy yếu nhất) + sửa lỗi nặng | Bạn (Trưởng nhóm) | 17/10 chiều → 18/10 sáng · đã có công cụ đo; máy trưởng nhóm 1155–1224 FPS (greybox) | Khẩn cấp | M | Profiler: không giật ở Màn 4–5; không lỗi chặn | FPS đo trên máy yếu nhất nhóm | T58 | KT | - |
| ✅ | T64 | Build Windows + chạy thử trên 3 máy | Bạn (Trưởng nhóm) | xong 07/10 | Khẩn cấp | S | Bản .exe chơi hết 5 màn | 3 máy khác nhau chạy được | T63 | KT | - |
| ☐ | T65 | Cân bằng độ khó + tỉ lệ hòm theo playtest | Bùi Nguyễn Thành Đạt | 17/10 sáng → 17/10 chiều | Cao | M | Số liệu playtest → chỉnh weights, máu địch, tăng bản nội dung nếu đổi | 3 người chơi hết mỗi màn ≤15 phút | T58 | Gameplay | BR-40 |
| ☐ | T66 | Kiểm thử 17 tình huống Phụ lục A | Bùi Nguyễn Thành Đạt | 18/10 sáng → 18/10 chiều | Khẩn cấp | M | Bảng đạt/chưa đạt cho từng dòng; lỗi chuyển cho người phụ trách | ≥15/17 đạt, còn lại ghi rõ vì sao | T65 | KT, Gameplay | BR-40 |
| ☐ | T67 | VFX: lửa nòng, nổ, khói, sương + chuyển cảnh | Trần Hoàng Long *(nhận từ Khôi)* | 19/10 chiều → 20/10 sáng | Cao | M | Hiệu ứng nhẹ, không tụt FPS; chuyển cảnh mờ dần | FPS không giảm >5 khi bắn | T60 | Đồ họa | - |
| ☐ | T68 | Rà soát cả ngân hàng câu + chơi thử với lớp | Thiên Trí | 18/10 sáng → 18/10 chiều | Khẩn cấp | M | ≥120 câu approved có nguồn + giải thích; 5 người ngoài nhóm chơi và góp ý | Validate không lỗi; bảng phản hồi | T61 | Ý tưởng | BR-28 |
| ☐ | T69 | Trộn âm + hiệu ứng thông báo UI + sửa độ phân giải | Trần Hoàng Long | 17/10 sáng → 17/10 chiều | Cao | M | Âm lượng cân bằng; thông báo khi nhận thưởng, hết giờ, lưu thành công | Chạy ở 1366×768 và 1920×1080 | T49 | Âm thanh, UI | - |
| ☐ | T70 | Chạy Validate Content, xóa hết lỗi | Bùi Nguyễn Thành Đạt *(nhận từ Long)* | 20/10 sáng | Khẩn cấp | S | 0 lỗi bắt buộc | Ảnh chụp kết quả | T40,T68 | KT | BR-27..30 |

## Giai đoạn 8 · Nộp bài

| ✔ | ID | Công việc | Phụ trách | Thời gian | Cấp độ | Cỡ | Kết quả cần có | Cách kiểm | Phụ thuộc | Tiêu chí | BR |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | T71 | Sửa lỗi cuối (khóa code 10/12) | Bạn, Đạt, Khôi | 19/10 sáng → 19/10 chiều | Khẩn cấp | M | Không còn lỗi chặn; chỉ sửa lỗi, không thêm tính năng | Danh sách lỗi về 0 | T66 | KT | - |
| ☐ | T72 | Báo cáo + slide (chia theo 5 nhóm tiêu chí chấm) | Thiên Trí | 19/10 sáng → 19/10 chiều | Khẩn cấp | M | Mỗi nhóm tiêu chí có trang minh chứng + ảnh chụp | Trưởng nhóm duyệt | T64 | Ý tưởng | - |
| ☐ | T73 | Quay video demo + trailer 60 giây | Trần Hoàng Long | 18/10 chiều → 19/10 sáng | Cao | M | Video chơi Màn 1 + khoảnh khắc đóng băng + bảng xếp hạng | Xem lại trên điện thoại | T64 | Đồ họa, Âm thanh | - |
| 🟡 | T74 | Build nộp, gắn tag v1.0, hướng dẫn cài | Bạn (Trưởng nhóm) | 20/10 sáng · đã có lệnh build + nén .zip; chạy bản cuối 20/10 | Khẩn cấp | S | File .zip + README cài đặt + tag git | Máy sạch giải nén chạy được | T71 | KT | - |
| ☐ | T75 | Tập demo (2 lượt) + phân người thao tác/thuyết trình | Cả nhóm | 20/10 chiều | Khẩn cấp | S | Demo ≤10 phút không lỗi | Tập 2 lượt có bấm giờ | T72,T73,T74 | Ý tưởng | - |

### ★ Nộp bài: 21/10/2026 — bản build + báo cáo + video demo.

