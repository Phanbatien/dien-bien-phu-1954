# T57 — Chuẩn bảng xếp hạng tại máy

Ngày đối chiếu: **15/10/2026**. Phạm vi: hai bảng tại máy, chiến đấu và kiến thức.
Tài liệu này là chuẩn T57 đã được duyệt và triển khai. [Biên bản kiểm tra ngày 15/10/2026](t57-verification.md) ghi riêng minh chứng chạy Unity.

## Nguồn và thứ tự ưu tiên

1. [business-rules.md](business-rules.md), BR-24 và BR-25: chuẩn chi tiết cho T57; BR-22, BR-31, BR-32, BR-33, BR-37, BR-40 bổ sung điều kiện lưu và tính hợp lệ.
2. `../Business_rule_DBP1954_v2.md` tính từ gốc repository (nằm ở thư mục dự án cha): bản nguồn giống hệt `docs/business-rules.md` tại thời điểm đối chiếu.
3. `../dien-bien-phu-1954-gdd-v5.md` tính từ gốc repository, mục 9: mô tả tổng quan. Công thức viết gọn ở GDD phải được đọc theo các ngoại lệ và cách làm tròn của BR-25.
4. [cong-viec.md](cong-viec.md), T57; `../../tasks/todo.md` và `../../tasks/long-progress.md`: phạm vi, phụ thuộc và tiêu chí nghiệm thu.

Phạm vi BR-24/25 dùng cho T57 đã được người dùng duyệt ngày 15/10/2026; hai bản business rules và mục 9 của GDD đã được đồng bộ ghi nhận phạm vi này. Việc chốt T57 không đồng nghĩa phê duyệt toàn bộ đề xuất v2 hoặc thay đổi công việc của người khác.

## 1. Điều kiện vào bảng

- Có hồ sơ với mã riêng; trùng tên không gộp thành tích.
- Chỉ lượt **thắng hợp lệ** mới vào cả hai bảng. Thua, bỏ dở, phần mở đầu không điều khiển không vào bảng.
- Thiếu câu giữa lượt làm lượt mất điều kiện xếp hạng ở cả hai bảng, kể cả sau đó thắng. Tiến độ và Quyết Tâm vẫn xét theo quy tắc riêng.
- Bảng tại máy hoạt động ngoại tuyến. Việc đồng ý gửi thành tích công khai thuộc luồng online; không ngăn lưu kết quả tại máy.
- Chỉ so sánh các lượt cùng **màn, độ khó, phiên bản nội dung, phiên bản quy tắc, cấu hình nâng cấp lúc xuất trận**. Hiển thị rõ nhóm đang xem.
- Chụp các điều kiện này lúc bắt đầu lượt; mua nâng cấp sau lượt hoặc đổi hồ sơ không được viết lại điều kiện của kết quả cũ.
- Seed và phần thưởng ngẫu nhiên lưu để đối chiếu, không phải tiêu chí phụ để phân thắng thua. Không tuyên bố bảng offline chống được mọi gian lận.

## 2. Năm tiêu chí chiến đấu, đúng thứ tự

| Ưu tiên | Tiêu chí | Thành tích tốt hơn |
|---|---|---|
| 1 | Số địch bị hạ | Nhiều hơn |
| 2 | Số lô cốt bị phá | Nhiều hơn |
| 3 | Số đợt phản kích đẩy lùi | Nhiều hơn |
| 4 | Tổng sát thương thực tế đã nhận | Ít hơn; hồi máu không xóa phần đã nhận |
| 5 | Thời gian hoàn thành | Ngắn hơn, so sánh tới mili giây |

Chỉ xét tiêu chí tiếp theo khi tất cả tiêu chí trước bằng nhau. Không cộng thành điểm tổng, không dùng điểm kiến thức, tên hoặc thời điểm gửi làm tiêu chí thứ sáu. Không làm tròn sát thương chỉ vì giao diện hiển thị ít chữ số.

Thời gian bắt đầu khi người chơi được điều khiển ngoài chiến trường và kết thúc khi chốt thắng:

- **Tính:** câu hỏi ngoài trận, 0,5 giây chuyển tiếp, menu tạm dừng, mất tiêu điểm.
- **Loại:** trong hầm, tải màn, hoạt cảnh bắt buộc khóa điều khiển, màn kết quả.
- Các khoảng loại trừ chỉ tính một lần, kể cả chồng nhau. Đồng hồ nhiệm vụ BR-09 là đồng hồ khác.
- T57 lưu số mili giây nguyên, làm tròn gần nhất và phần lẻ 0,5 lên, chốt đúng một lần khi kết thúc lượt theo thiết kế đã duyệt. BR-24 quy định độ chính xác mili giây; cách làm tròn này là quyết định triển khai. Không dùng số giây đã làm tròn trên giao diện để xếp hạng.

Ví dụ: 10 địch, 0 lô cốt luôn đứng trên 9 địch, 5 lô cốt. Khi ba tiêu chí đầu bằng nhau, nhận 30 sát thương đứng trên nhận 50 sát thương, dù lượt thứ hai nhanh hơn.

## 3. Đồng hạng và thứ tự hiển thị

- Bằng mọi tiêu chí của bảng thì cùng hạng, dùng cách **1, 1, 3**, không phải 1, 1, 2.
- Hạng của một dòng bằng 1 cộng số hồ sơ có thành tích tốt hơn trong cùng nhóm điều kiện.
- Trong nhóm đồng hạng, hiển thị ổn định bằng mã hồ sơ theo thứ tự tăng dần, so sánh mã trực tiếp, không theo ngôn ngữ tên hiển thị.
- Mã hồ sơ chỉ ổn định vị trí dòng, không phá đồng hạng. Thời điểm chơi, lưu hoặc gửi mạng không quyết định ai thắng.
- Cắt trang hiển thị không làm tính lại hạng và không xóa bản ghi nằm ngoài trang.

Ví dụ: A và B bằng cả năm tiêu chí; C kém hơn. A và B cùng hạng 1, C hạng 3. D đứng sau C thì hạng 4.

## 4. Giữ thành tích tốt nhất

- Mỗi hồ sơ chỉ có **một bản tốt nhất trong mỗi bảng của mỗi nhóm điều kiện**.
- Chiến đấu: thay bản cũ chỉ khi lượt mới tốt hơn theo đủ thứ tự ở mục 2.
- Kiến thức: thay bản cũ chỉ khi tổng điểm của lượt mới cao hơn.
- Nếu lượt mới ngang bằng hoặc kém hơn, giữ nguyên bản đã lưu trước, gồm mã lượt và dữ liệu đối chiếu.
- Hai bảng xét độc lập: cùng một hồ sơ có thể dùng hai lượt khác nhau làm kỷ lục chiến đấu và kiến thức.
- Không ghép chỉ số tốt nhất từ nhiều lượt thành một kết quả; không cộng dồn điểm kiến thức qua nhiều lượt.
- Gửi lại cùng một lượt không tạo thêm dòng. Đổi nhóm điều kiện tạo bảng riêng, không ghi đè kỷ lục nhóm cũ.
- Lưu phải tồn tại sau khi tắt/mở game. Lỗi ghi phải được báo và cho thử lại; không thông báo đã lưu khi ghi thất bại, không làm mất bản tốt nhất trước đó.

Ví dụ: lượt X có chiến đấu tốt hơn nhưng 800 điểm kiến thức, lượt Y chiến đấu kém hơn nhưng 1.000 điểm. Giữ X ở bảng chiến đấu và Y ở bảng kiến thức.

## 5. Bảng kiến thức

- Điểm cơ bản theo cấp 1–5: 100, 200, 300, 400, 500.
- Trong hầm đúng: điểm cơ bản, không nhân thời gian.
- Ngoài trận đúng: `điểm cơ bản × (1 + 0,5 × r)`, `r = thời gian còn / thời hạn`, giới hạn 0–1; dùng cấp và thời hạn thực tế, kể cả bảo hiểm.
- Sai/hết giờ: 0. Làm tròn từng câu tới số nguyên gần nhất, 0,5 lên; dùng thời gian mili giây trước khi làm tròn, rồi cộng trong một lượt.
- Tổng cao hơn đứng trên. Bằng tổng thì đồng hạng, không xét thêm chiến đấu hay tốc độ. Điểm Quyết Tâm không tham gia.
- Ví dụ chuẩn: cấp 3 ngoài trận còn 6/15 giây = 360; cấp 3 trong hầm = 300; bảo hiểm cấp 1 còn 6/12 giây = 125.

## 6. Đối chiếu sau triển khai

| Phần | Kết quả ngày 15/10/2026 |
|---|---|
| Hai bản business rules | Trùng nội dung; cùng ghi nhận chuẩn T57 |
| GDD mục 9 | Đã làm rõ công thức trong hầm/ngoài trận và dẫn chuẩn T57 |
| `Attempt` và `LeaderboardService` | Chụp hồ sơ, độ khó, nhóm nâng cấp và phiên bản đầu lượt; chốt chỉ số, điểm, seed và nhật ký phần thưởng cuối lượt |
| `Leaderboard` | Hai phép so sánh riêng; hạng 1–1–3; mã hồ sơ chỉ ổn định dòng |
| `LeaderboardRepository` | Lưu hai kỷ lục độc lập; kiểm tra tính toàn vẹn, ghi tạm/thay thế, backup, báo lỗi không xóa dữ liệu |
| `RankingClock` | Đồng hồ thực riêng; pause/câu hỏi/chuyển tiếp tính, hướng dẫn đầu lượt và khởi tạo scene loại; hợp các khoảng loại trừ có tên |
| `CampaignUI` | Có bảng từ menu/kết quả, tab chiến đấu/kiến thức, đổi nhóm, phân trang, bảng trống, thông báo lỗi và thử lại |
| TH-10/11 | Đã thay kiểm tra giả bằng phép so sánh và xếp hạng thật; có thêm unit/PlayMode test |

## 7. Thiết kế đã triển khai

- Kết quả lưu tại `profiles.leaderboards.json` cạnh `profiles.json`; không sửa định dạng hồ sơ hoặc dữ liệu mua nâng cấp. File có payload JSON và checksum SHA-256 để phát hiện thiếu/hỏng dữ liệu, không phải cơ chế chống sửa điểm offline.
- Chỉ ghi khi ít nhất một bảng có kỷ lục mới. Nếu lỗi ghi, dữ liệu trong bộ nhớ và kỷ lục trước đó giữ nguyên; màn kết quả có thử lại. Backup tốt không bị thay bằng file chính đã hỏng.
- Nội dung dùng fingerprint SHA-256 của đường dẫn tương đối và toàn bộ JSON trong `StreamingAssets/content`; quy tắc dùng `br24-25-v2-t57-1`; nâng cấp dùng bốn cấp hiệu lực lúc xuất trận theo thứ tự đạn/thay đạn/thể lực/lựu đạn.
- `MissionController.rankingDifficulty` là độ khó được cấu hình cho scene, mặc định 1 trong chiến dịch hiện tại. T57 không bổ sung bộ chọn độ khó hoặc giả định gameplay đã có năm biến thể.
- `QuizCrate` chỉ bổ sung ghi nhật ký, không đổi cách chọn hoặc phát thưởng: phần thưởng hòm/súng rơi được đề nghị, kết quả câu hỏi, gói thực tế, đồ dư và vũ khí bị đổi ra. Nhật ký được sao chép tách biệt khi lưu.
- Hầm/menu chuẩn bị và tải màn nằm ngoài scene chiến trường nên không vào đồng hồ. Luồng khóa điều khiển bắt buộc mới trong trận phải dùng `SetRankingExclusion(reason, excluded)`; các khoảng chồng nhau chỉ loại một lần. Chiến dịch hiện tại chưa có hoạt cảnh khóa bắt buộc trong trận.
- Không đổi chữ ký hợp đồng dùng chung. Gửi mạng T62 vẫn là phần riêng.

## 8. Điều kiện nghiệm thu T57

- [x] Cả năm tiêu chí được kiểm tra riêng, chứng minh tiêu chí trước luôn ưu tiên; kiểm tra chênh 1 ms.
- [x] PL-A dòng 10: ba tiêu chí đầu bằng nhau, ít sát thương hơn đứng trên.
- [x] PL-A dòng 11: bằng cả năm tiêu chí thì 1–1–3; đổi thứ tự đầu vào/thời điểm gửi vẫn giữ hạng và thứ tự dòng ổn định.
- [x] Lượt tốt hơn thay bản cũ; ngang bằng/kém hơn giữ bản cũ; gửi lại không nhân đôi; hai bảng giữ được hai lượt khác nhau.
- [x] Kiến thức kiểm tra tổng cao hơn, đồng tổng, ba ví dụ BR-25, không cộng nhiều lượt.
- [x] Thua/bỏ dở/lượt lỗi câu không vào cả hai bảng; nhóm điều kiện khác không so chung.
- [x] Khởi động lại vẫn có kỷ lục; lỗi ghi báo đúng, thử lại an toàn; dùng dữ liệu thử riêng.
- [x] Pause và WorldFreeze/chuyển tiếp được kiểm tra trong PlayMode; đồng hồ dùng thời gian thực nên mất tiêu điểm không bị trừ. Hầm/tải trước trận nằm ngoài đồng hồ; kiểm tra unit xác nhận hợp các khoảng loại trừ không trừ hai lần.
- [x] Trong Unity: mở Play từ MainMenu, mở/chuyển bảng, đổi nhóm, xem trang trống và nhiều trang, quay về menu; kiểm tra đường từ kết quả.
- [x] Cập nhật tracker và đánh dấu T57 sau khi có kết quả kiểm tra thực tế, không dùng TH-11 hiện tại để nghiệm thu.
