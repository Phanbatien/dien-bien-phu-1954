# Cốt truyện: mở đầu, nhật ký hầm, thoại vô tuyến, kết thúc (T34, T47, T61)

Lời dẫn nằm trong code để chạy được không cần tải thêm: `Scripts/UI/CampaignUI.Story.cs` (mở đầu, nhật ký, kết thúc) và `Scripts/Missions/RadioChatter.cs` (Màn 3). Sửa lời ở đó; test `StoryFlowTests` kiểm số khung và chữ không rỗng.

Nguyên tắc (BR-01, BR-02, BR-32):
- Nhân vật lịch sử thật (Đại tướng Võ Nguyên Giáp, anh hùng Tô Vĩnh Diện, tướng De Castries) **chỉ xuất hiện trong lời dẫn**, không điều khiển, không nói thoại bịa.
- Người chơi là **một chiến sĩ vô danh**. Nhật ký và vô tuyến là nhân vật/đơn vị hư cấu.
- Phần mở đầu không điều khiển, không tính vào bảng xếp hạng.
- Không máu me (BR-21).

## Mở đầu 90 giây (storyboard T34)

Vào từ nút **▶ Mở đầu chiến dịch** trên menu. 6 khung × 15 giây, tự chuyển, bấm **Tiếp** hoặc **Bỏ qua** được.

| # | Tiêu đề | Hình gợi ý | Lời dẫn (tóm tắt) |
|---|---|---|---|
| 1 | Cuối năm 1953 | Bản đồ lòng chảo, 3 phân khu | Pháp xây tập đoàn cứ điểm mạnh nhất Đông Dương: 49 cứ điểm, 3 phân khu, 2 sân bay |
| 2 | Quyết định lịch sử | Hầm chỉ huy, bản đồ tác chiến | 12/1953 Bộ Chính trị mở chiến dịch; Đại tướng Võ Nguyên Giáp làm Chỉ huy trưởng |
| 3 | Kéo pháo vào trận địa | Dây tời, sườn núi dốc | Kéo pháo bằng sức người; Tô Vĩnh Diện lấy thân chèn pháo |
| 4 | Đánh chắc, tiến chắc | Pháo được kéo ra | Cuối 1/1954 đổi phương châm “đánh nhanh, giải quyết nhanh” sang “đánh chắc, tiến chắc” |
| 5 | Hậu phương ra tiền tuyến | Đoàn xe đạp thồ, dân công | Hơn 260.000 dân công, gần 21.000 xe đạp thồ |
| 6 | 17 giờ 05, 13/3/1954 | Pháo khai hỏa | Nổ súng vào Him Lam; “Bạn là một chiến sĩ vô danh…” |

## Nhật ký trong hầm, Màn 1

Nút **Đọc nhật ký trong hầm** ở màn hướng dẫn trước khi vào Màn 1. Hai trang nhật ký của một chiến sĩ hư cấu, trưa và chiều 13/3/1954: đèn dầu, lau súng, thư mẹ trong túi áo, hào xung phong đã sát hàng rào Him Lam.

Màn chuẩn bị của mọi màn hiện **Hầm chữ A** phía sau giao diện (`Scenes/Bunker_HamChuA.unity`): mái gỗ chữ A, bàn bản đồ, điện đài, giá súng, đèn dầu chập chờn.

## Thoại vô tuyến, Màn 3 (T47)

5 đoạn hiện ở góc thông báo, đoạn đầu sau 4 giây, mỗi đoạn sau khi xong một mục tiêu:

1. Pháo ta bắn chuẩn bị vào đồi E, D; chờ pháo dứt thì xung phong.
2. Áp sát hàng rào; pháo sáng địch lên thì nằm im.
3. Đồi D1 đã cắm cờ; tiếp tục đánh vào trung tâm.
4. Địch bắn chặn, chuẩn bị phản kích; đào hầm cá nhân giữ trận địa.
5. Giữ chốt đến sáng, chờ lệnh mới.

## Kết thúc chiến dịch và Credits (T61)

Thắng Màn 5 → nút **Kết thúc chiến dịch** ở màn kết quả → 4 khung → màn **Nguồn tư liệu & Credits**. Màn Credits cũng mở được từ menu.

| # | Tiêu đề | Lời dẫn (tóm tắt) |
|---|---|---|
| 1 | 17 giờ 30, 7/5/1954 | Cờ “Quyết chiến — Quyết thắng” trên nóc hầm chỉ huy; De Castries và Bộ tham mưu bị bắt sống |
| 2 | 56 ngày đêm | Tiêu diệt và bắt sống toàn bộ tập đoàn cứ điểm, hơn 16.000 quân địch |
| 3 | Hiệp định Genève | 21/7/1954 ký kết, lập lại hòa bình ở Đông Dương |
| 4 | Tưởng nhớ | Tri ân những chiến sĩ đã ngã xuống trên lòng chảo |

Màn Credits liệt kê nhóm thực hiện, gói tài nguyên dùng trong game và danh sách nguồn đọc từ `StreamingAssets/content/sources.json` (cùng nguồn với ngân hàng câu hỏi).

## Cần Trí soát lại

Số liệu dùng các con số phổ biến trong tài liệu chính thống (49 cứ điểm, 260.000 dân công, 21.000 xe đạp thồ, 16.200 quân địch, 56 ngày đêm). Nếu sách giáo khoa/nguồn của nhóm ghi khác, sửa trực tiếp trong `CampaignUI.Story.cs` và thêm mã nguồn vào `sources.json`.
