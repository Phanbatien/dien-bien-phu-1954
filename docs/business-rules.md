# BẢN QUY TẮC NGHIỆP VỤ (BUSINESS RULES) — V2
### DỰ ÁN: ĐIỆN BIÊN PHỦ 1954

Ngày lập: 20/09/2026  
Đối chiếu: `Business_rule_DBP1954.md`, `Business_rule_DBP1954_review_handoff.md`, `dien-bien-phu-1954-gdd-v5.md`.

## Phạm vi và trạng thái

- Đây là bản sửa độc lập, giữ mã BR-01–BR-30 và bổ sung BR-31–BR-40. Tài liệu gốc và GDD chưa được sửa.
- **[Kế thừa]**: giữ nguyên quyết định chính trong bản gốc. **[Làm rõ]**: bổ sung ranh giới và tình huống ngoại lệ. **[Đề xuất v2]**: lựa chọn mới để bộ quy tắc có thể áp dụng nhất quán, chưa phải quyết định đã được nhóm phê duyệt. Một quy tắc có thể mang nhiều nhãn.
- Khi dùng v2 làm cơ sở triển khai, phải chốt các đề xuất ở phụ lục B và đồng bộ GDD. Các con số mới là mặc định đề xuất, chưa được kiểm chứng cân bằng bằng chơi thử.
- Tài liệu quy định hành vi cần có; không bắt buộc một cách lập trình cụ thể. Các chi tiết lịch sử kế thừa vẫn cần người phụ trách nội dung xác minh nguồn trước khi phát hành.
- **Lượt chơi màn** bắt đầu khi vào hầm chuẩn bị và kết thúc bằng thắng, thua hoặc bỏ dở. Mỗi lượt có một mã riêng (`attemptId`), gắn với hồ sơ, màn, phiên bản nội dung và phiên bản quy tắc.
- **Vật tư nhiệm vụ** là vật tư bảo đảm hoàn thành mục tiêu bắt buộc, khác với phần thưởng ngẫu nhiên. **Điểm kiến thức** dùng xếp hạng; **Điểm Quyết Tâm** dùng mua nâng cấp; không chuyển đổi giữa hai loại.

---

## I. QUY TẮC VỀ NHÂN VẬT VÀ BỐI CẢNH LỊCH SỬ

### BR-01 — Nhân vật điều khiển [Kế thừa]
Người chơi điều khiển năm chiến sĩ hư cấu, mỗi màn một người ở một vị trí chiến đấu. Không cho phép điều khiển nhân vật lịch sử có thật.

### BR-02 — Sự xuất hiện của nhân vật lịch sử [Kế thừa]
Nhân vật lịch sử có thật chỉ xuất hiện trong cốt truyện, hoạt cảnh dẫn dắt hoặc nội dung câu hỏi. Mọi thông tin về họ phải được kiểm chứng trước khi phát hành.

### BR-03 — Bảo toàn diễn biến lịch sử [Kế thừa, Làm rõ]
Câu trả lời chỉ ảnh hưởng đến trang bị và lợi thế chiến đấu, không tạo kết cục lịch sử khác. Các sự kiện như vụ nổ A1 và cắm cờ ở hầm chỉ huy vẫn được thể hiện theo kịch bản đã duyệt khi người chơi đến đoạn tương ứng. Thua hoặc bỏ dở màn kết thúc lượt của người chơi, không được diễn giải thành thay đổi kết quả chiến dịch.

### BR-04 — Quy tắc không quân [Kế thừa]
Không dựng mô hình máy bay trực tiếp tham chiến trên màn hình. Máy bay địch chỉ được thể hiện bằng âm thanh nền.

---

## II. QUY TẮC HỆ THỐNG CÂU HỎI

### 1. Câu hỏi trong hầm — Chuẩn bị trước trận

### BR-05 — Số lượng và cấp độ [Kế thừa, Làm rõ]
Mỗi lượt có 3–5 câu trong hầm, cấp 1–3. Số câu và phân bố cấp phải được khai báo cho màn; không thay đổi sau khi lượt bắt đầu. Chọn câu theo BR-31, tính cả các câu dùng được ở hai hình thức.

### BR-06 — Thời gian trong hầm [Đề xuất v2]
Câu trong hầm không giới hạn thời gian, không có thưởng trả lời nhanh. Người chơi phải trả lời hết phần chuẩn bị mới ra chiến trường; có thể bỏ dở lượt theo BR-33.

### BR-07 — Kết quả trong hầm [Kế thừa, Đề xuất v2]
- Đúng: nhận điểm kiến thức cơ bản theo BR-25; ghi nhận Điểm Quyết Tâm chờ quyết toán theo BR-34.
- Sai: không nhận điểm của câu đó; hiển thị đáp án đúng và giải thích ngay. Điểm đã có không bị trừ.
- Mỗi câu chỉ nhận một đáp án cuối cùng. Không trả lời lại để đổi kết quả trong cùng lượt.
- Điểm Quyết Tâm chờ quyết toán chưa được dùng mua nâng cấp; chỉ cộng vào số dư khi thắng màn. Đây là thay đổi đề xuất để tránh kiếm điểm bằng cách vào hầm rồi thoát.

### 2. Câu hỏi ngoài chiến trường — Hòm, súng rơi và điện thoại

### BR-08 — Điều kiện kích hoạt [Làm rõ]
Nhân vật phải còn sống, trong khoảng tương tác của đối tượng chưa dùng và giữ E liên tục **ít nhất 1 giây**. Thả phím, ra khỏi khoảng tương tác, mất đối tượng hoặc chết sẽ hủy tiến trình; lần sau phải giữ lại từ đầu. Trong thời gian giữ E, chiến trường vẫn hoạt động và nhân vật vẫn nhận sát thương. Bị trúng đạn nhưng còn sống không tự hủy tương tác. Hủy trước khi câu hỏi mở không tiêu thụ đối tượng hoặc đánh dấu câu đã gặp.

### BR-09 — Trạng thái thế giới khi trả lời [Kế thừa, Làm rõ]
Khi câu hỏi hiển thị thành công, dừng đạn, AI, vật lý, pháo, hiệu ứng chiến trường và các bộ đếm nhiệm vụ chịu ảnh hưởng của thời gian chiến đấu. Khóa di chuyển, ngắm, bắn và xoay camera; làm mờ nền, ngả nâu xám và tối viền. Âm thanh chiến trường tạm dừng hoặc được lọc trầm. Giao diện và đồng hồ câu hỏi vẫn hoạt động theo thời gian thực. Không được chỉ dừng hình ảnh trong khi địch vẫn gây sát thương.

### BR-10 — Nội dung và thời hạn ngoài trận [Kế thừa, Làm rõ]
- Mỗi tương tác hiển thị đúng một câu; phần thân câu hỏi không quá 120 ký tự theo BR-28.
- Đồng hồ bắt đầu khi câu và toàn bộ lựa chọn đã hiển thị, sẵn sàng nhận thao tác. Cấp câu và thời hạn theo BR-15, trừ bảo hiểm BR-16 và điện thoại BR-38.
- Khi thời gian còn lại bằng 0, kết quả là hết giờ; đáp án đến đúng hoặc sau hạn không được chấp nhận. Chỉ chấp nhận lựa chọn đầu tiên trước hạn.
- Không cho tạm dừng đồng hồ câu ngoài trận bằng menu, chuyển cửa sổ hoặc mất tiêu điểm. Nếu ứng dụng bị đóng, áp dụng BR-37; không mở lại chính câu đó để trả lời tiếp.

### BR-11 — Kết quả tương tác [Kế thừa, Làm rõ]
| Đối tượng | Trả lời đúng | Sai hoặc hết giờ |
|---|---|---|
| Hòm / súng rơi | Nhận phần thưởng đã gắn với đối tượng, xử lý túi đồ theo BR-36 | Nhận gói đạn thông thường đã cấu hình, tương thích với súng khởi đầu của màn; không nhận vũ khí chính của đối tượng |
| Điện thoại | Pháo đánh trúng mục tiêu được chỉ định | Pháo trượt, không gây sát thương cho mục tiêu; người chơi còn phương án bắt buộc theo BR-17 |

Mỗi kết quả chỉ phát thưởng hoặc yêu cầu pháo một lần. Sau khi chốt đáp án, đối tượng không thể thử lại trong lượt đó. Không cộng Điểm Quyết Tâm cho câu ngoài trận. Sai ở mọi câu vẫn phải có đường hoàn thành các mục tiêu bắt buộc.

### BR-12 — Chống lạm dụng và chuyển tiếp [Kế thừa, Làm rõ]
- Mỗi hòm, súng rơi và điện thoại chỉ dùng một lần trong một lượt, bất kể đúng, sai hay hết giờ. Không thể mở nhiều câu cùng lúc.
- Trong 0,5 giây chuyển tiếp sau câu hỏi, chiến trường tiếp tục đứng yên, nhân vật và camera vẫn bị khóa; đồng hồ câu hỏi đã dừng. Khôi phục hình ảnh và âm thanh dần.
- Chỉ cho chiến trường tiếp tục khi điều khiển người chơi đã được mở lại trong cùng thời điểm; địch không được tấn công trong lúc người chơi còn bị khóa.
- Menu tạm dừng ngoài câu hỏi dừng chiến đấu và khóa quan sát/bắn. Cách tính thời gian xếp hạng tuân theo BR-24, độc lập với thời gian chiến đấu.
- Chơi lại tạo đối tượng và trạng thái tương tác mới theo BR-33, không khôi phục quyền nhận thưởng của lượt cũ.

---

## III. QUY TẮC HÒM ĐỒ VÀ TRANG BỊ VŨ KHÍ

### BR-13 — Vị trí hòm ngẫu nhiên [Kế thừa, Làm rõ]
Mỗi màn khai báo 10–15 vị trí hợp lệ; mỗi lượt rút 5–7 vị trí khác nhau. Lưu hạt ngẫu nhiên (`seed`) cùng phiên bản nội dung để tái hiện cách phân bổ khi kiểm tra. Cùng seed không đồng nghĩa toàn bộ diễn biến chiến đấu sẽ giống nhau. Vật tư nhiệm vụ bảo đảm theo BR-17 được bố trí riêng, không bị thay thế bởi phép rút hòm. Lượt mới dùng seed mới.

### BR-14 — Súng rơi từ địch [Kế thừa, Đề xuất v2]
Mỗi địch chỉ được xét rơi súng một lần khi bị hạ; xác suất thuộc khoảng 0–1 và được khai báo theo màn. Giá trị 0,08 ở Màn 2 là ví dụ cần chơi thử. Súng rơi phải có độ hiếm và áp dụng BR-08–BR-12, BR-15, BR-36. Không được nhặt trực tiếp để bỏ qua câu hỏi. Mỗi màn khai báo giới hạn số súng rơi tối đa trong một lượt; đạt giới hạn thì không sinh thêm, nhằm bảo đảm ngân hàng câu đủ dùng. Giới hạn này là thông số nội dung phải chốt trước phát hành.

### BR-15 — Độ hiếm, cấp câu hỏi và thời gian [Kế thừa, Làm rõ]
| Độ hiếm | Nhận diện hòm | Ví dụ phần thưởng, chỉ dùng khi đủ điều kiện BR-18 | Cấp câu | Thời hạn |
|---|---|---|---|---|
| Thường | Gỗ mộc | Đạn Mosin, lựu đạn chày | 1 hoặc 2 | 12 giây |
| Khá | Dây xanh | Mauser, băng đạn tiểu liên | 3 | 15 giây |
| Hiếm | Dây đỏ | MAT-49, Kiểu 50, bộc phá | 4 | 18 giây |
| Rất hiếm | Đánh dấu sao | Mosin có ống ngắm, Bazooka | 5 | 20 giây |

Súng rơi dùng độ hiếm của chính vũ khí. Với hòm Thường, rút trong tập câu cấp 1–2 còn hợp lệ; không bắt buộc có cả hai cấp ở từng lần rút. Phần thưởng được xác định trước khi hiện câu hỏi và không đổi theo đáp án. Điện thoại áp dụng BR-38.

### BR-16 — Bảo hiểm sau ba lần sai [Đề xuất v2]
- Chỉ tính các **hòm** đã chốt kết quả; không tính câu trong hầm, súng rơi hoặc điện thoại. Tương tác các loại khác không tăng và không xóa chuỗi sai của hòm.
- Sai hoặc hết giờ tăng chuỗi sai thêm một. Đúng trước khi đạt ba lần sai đặt chuỗi về 0.
- Sau ba lần sai liên tiếp, hòm tiếp theo dùng câu **cấp 1, 12 giây**, chưa từng hiển thị trong lượt. Phần thưởng và độ hiếm hòm giữ nguyên; điểm kiến thức tính theo cấp 1 thực tế.
- Sau câu bảo hiểm, chuỗi về 0 dù đúng, sai hay hết giờ. Nếu hủy giữ E trước lúc mở câu, bảo hiểm vẫn chờ cho hòm tiếp theo.
- Lượt mới đặt chuỗi về 0. Không đủ câu bảo hiểm là lỗi nội dung, xử lý theo BR-31; không tự chuyển sang câu cấp khác.

### BR-17 — Luôn có phương án hoàn thành mục tiêu [Làm rõ, Đề xuất v2]
Mỗi mục tiêu bắt buộc phải có ít nhất một phương án không phụ thuộc vào câu trả lời hoặc kết quả ngẫu nhiên. Phương án phải tiếp cận được trước khi phá mục tiêu, không nằm phía sau chính vật cản cần phá.

Mặc định đề xuất: có điểm cấp **vật tư nhiệm vụ không qua câu hỏi** phù hợp với từng mục tiêu còn lại, gồm bộc phá cho lô cốt và phương tiện chống tăng nếu xe tăng là mục tiêu bắt buộc. Khi người chơi không còn đủ vật tư cần thiết, điểm cấp bổ sung phần thiếu; vật tư đã đánh rơi hoặc nằm ở nơi không thể tiếp cận không được tính là nguồn dự phòng khả dụng. Cơ chế phải đáp ứng cả trường hợp bắn trượt, đặt sai chỗ hoặc tiêu hao vào mục tiêu khác, và phải đủ cho tất cả mục tiêu bắt buộc còn lại.

Vật tư nhiệm vụ không chiếm chỗ súng thông thường, không quy đổi thành điểm, không mang sang lượt khác; chỉ gây tác dụng với nhóm mục tiêu nhiệm vụ phù hợp. Nếu phương án cần một vũ khí, phải bảo đảm cả vũ khí lẫn đạn tương thích. Hòm hiếm và pháo gọi bằng câu hỏi chỉ tạo lợi thế bổ sung. Màn chỉ được phát hành sau khi kiểm tra thực tế đường đi và tình huống dùng hết vật tư.

### BR-18 — Điều kiện lịch sử của vũ khí [Làm rõ]
Chỉ đưa vũ khí vào màn khi có nguồn chứng minh phù hợp với thời điểm, chiến trường và lực lượng sử dụng hoặc thu được. Năm ra đời hay năm sản xuất không tự chứng minh vũ khí đã có ở trận đánh. `availableFrom` phải có ý nghĩa rõ trong dữ liệu; nếu chỉ lưu năm thì phải bổ sung căn cứ sử dụng theo màn. Không chấp nhận vũ khí xuất hiện sau thời điểm màn hoặc sau năm 1954. K-54 và K-50M thuộc danh sách loại theo chủ ý thiết kế hiện tại, kể cả khi giá trị năm vượt qua kiểm tra số học.

### BR-19 — Lưu giữ trang bị [Kế thừa, Đề xuất v2]
Chốt mặc định v2: súng, đạn, lựu đạn và vật tư nhặt trong màn chỉ tồn tại trong lượt hiện tại, không chuyển sang màn sau hoặc lượt chơi lại. Nâng cấp mua bằng Điểm Quyết Tâm tồn tại trên hồ sơ và áp dụng theo BR-35. Ghi nhận “đã khám phá vũ khí” chỉ phục vụ bộ sưu tập, không cấp lại súng ở màn khác.

---

## IV. QUY TẮC HÀNH VI KẺ ĐỊCH VÀ HÌNH ẢNH

### BR-20 — Hành vi kẻ địch [Kế thừa, Làm rõ]
- Lính gác: tuần tra → nghi ngờ khi phát hiện tiếng động → báo động khu vực.
- Lính sau bao cát: bắn từ chỗ nấp, hạ thấp người khi bị áp chế, ném lựu đạn khi người chơi áp sát.
- Nhóm phản kích: 4–8 lính phối hợp yểm trợ và xung phong; rút lui khi mất **hơn** một nửa quân số ban đầu. Mất đúng một nửa chưa kích hoạt điều kiện này.
- Lô cốt: quét hỏa lực theo cung cố định; miễn nhiễm súng cá nhân, chỉ bị phá bằng bộc phá hoặc pháo.
- Xe tăng Chaffee: đi theo đường định sẵn, có bộ binh hộ tống. Màn có xe tăng bắt buộc phải khai báo loại sát thương có hiệu lực và phương án bảo đảm theo BR-17.
- Một đợt phản kích chỉ tính là đẩy lùi khi tất cả thành viên đã bị hạ hoặc rút khỏi khu vực giao chiến; mỗi đợt chỉ tính một lần. Địch còn đang rút không được coi là đã hoàn thành đợt.

### BR-21 — Hình ảnh bạo lực [Kế thừa]
Không hiển thị máu me. Địch bị hạ ngã xuống rồi mờ dần. Hiệu ứng biến mất không được tạo thêm lượt ghi nhận hạ địch hoặc rơi đồ.

---

## V. QUY TẮC HỒ SƠ VÀ BẢNG XẾP HẠNG

### BR-22 — Hồ sơ trên máy [Làm rõ]
Một máy có thể có nhiều hồ sơ không mật khẩu. Thông tin cá nhân lưu trữ giới hạn gồm tên hiển thị, tên lớp và thời điểm xác nhận (`consentAt`); ngoài ra lưu thiết lập, tiến độ, số dư Điểm Quyết Tâm, nâng cấp, bộ sưu tập và lịch sử chơi cần thiết. Mỗi hồ sơ có mã riêng; trùng tên không gộp hồ sơ. Mặc định đề xuất: chỉ đưa kết quả lên bảng công khai khi người chơi đã xác nhận việc gửi thành tích; không xác nhận vẫn chơi và lưu kết quả tại máy.

### BR-23 — Ngoại tuyến và trực tuyến [Kế thừa, Làm rõ]
Toàn bộ phần chơi và chấm điểm tại máy hoạt động không cần mạng. Kết quả kết thúc lượt được lưu cục bộ; kết quả đủ điều kiện gửi được đưa vào hàng đợi (`outbox`) và tự gửi khi có mạng. Mất mạng không làm mất điểm, chặn qua màn hoặc buộc chơi lại. Quy trình xác nhận và thử lại theo BR-39.

### BR-24 — Xếp hạng chiến đấu [Kế thừa, Đề xuất v2]
Chỉ nhận lượt **thắng** hợp lệ. So sánh lần lượt, tiêu chí trước bằng nhau mới xét tiêu chí sau:

1. Số địch bị hạ: nhiều hơn xếp trên.
2. Số lô cốt bị phá: nhiều hơn xếp trên.
3. Số đợt phản kích đẩy lùi: nhiều hơn xếp trên.
4. Tổng sát thương thực tế đã nhận: ít hơn xếp trên; hồi máu không xóa sát thương đã ghi.
5. Thời gian hoàn thành: ngắn hơn xếp trên, so sánh tới mili giây.

Thời gian hoàn thành tính từ lúc được điều khiển ngoài chiến trường đến khi chốt thắng; **bao gồm** thời gian câu hỏi ngoài trận, 0,5 giây chuyển tiếp, menu tạm dừng và mất tiêu điểm. Không tính phần trong hầm, tải màn, hoạt cảnh bắt buộc khóa điều khiển và màn kết quả. Các khoảng loại trừ không được trùng hoặc trừ hai lần. Bộ đếm nhiệm vụ trong BR-09 không phải đồng hồ này.

Mỗi bảng chỉ so sánh cùng màn, độ khó, phiên bản nội dung, phiên bản quy tắc và cấu hình nâng cấp lúc xuất trận. Giữ thành tích tốt nhất của mỗi hồ sơ trong từng bảng. Nếu mọi tiêu chí bằng nhau, đồng hạng theo kiểu 1, 1, 3; thứ tự hiển thị ổn định bằng mã hồ sơ, không dùng thời điểm gửi mạng để phân thắng thua. Với hai lượt ngang nhau của cùng hồ sơ, giữ bản đã lưu trước.

Seed và phần thưởng ngẫu nhiên có thể khác nhau trong cùng bảng và phải lưu để đối chiếu. Bảng phản ánh thành tích ở chế độ ngẫu nhiên, không khẳng định điều kiện chơi hoàn toàn ngang nhau hoặc chống mọi gian lận ngoại tuyến.

### BR-25 — Điểm và xếp hạng kiến thức [Làm rõ, Đề xuất v2]
Mặc định điểm cơ bản: cấp 1 = **100**, cấp 2 = **200**, cấp 3 = **300**, cấp 4 = **400**, cấp 5 = **500**. Đây là thông số đề xuất cần cân bằng.

- Câu trong hầm đúng: nhận đúng điểm cơ bản, không có hệ số thời gian.
- Câu ngoài trận đúng: `điểm cơ bản × (1 + 0,5 × r)`, với `r = thời gian còn lại / thời hạn`, giới hạn trong khoảng 0–1. Dùng cấp câu và thời hạn **thực tế**, kể cả câu bảo hiểm.
- Sai hoặc hết giờ: 0 điểm, không trừ điểm cũ.
- Làm tròn từng câu đến số nguyên gần nhất, phần lẻ 0,5 làm tròn lên, rồi cộng tất cả câu để có tổng lượt. Dùng thời gian mili giây trước khi làm tròn.
- Ví dụ: câu cấp 3 đúng còn 6/15 giây nhận 360 điểm; cấp 3 trong hầm nhận 300 điểm; câu bảo hiểm cấp 1 đúng còn 6/12 giây nhận 125 điểm.
- Cả hai hình thức đóng góp vào tổng; Điểm Quyết Tâm không đóng góp vào tổng này.
- Mặc định v2: chỉ đưa lượt thắng hợp lệ lên bảng kiến thức; lượt thua/bỏ dở vẫn xem được điểm tại máy. Xếp riêng theo cùng các nhóm điều kiện của BR-24, giữ tổng cao nhất của mỗi hồ sơ, bằng tổng thì đồng hạng 1, 1, 3. Không gộp điểm từ nhiều lượt. Thứ tự hiển thị và cách giữ lượt ngang nhau giống BR-24.

### BR-26 — Chấm lại và kiểm duyệt [Làm rõ]
Máy chủ tính lại điểm kiến thức bằng đáp án ứng với phiên bản nội dung của lượt chơi, kiểm tra câu, cấp, lựa chọn, thời hạn, số lần trả lời và phạm vi không lặp. Phiên bản không được hỗ trợ hoặc dữ liệu không hợp lệ không được tự chấm bằng phiên bản mới; trả trạng thái từ chối kèm lý do. Máy chủ kiểm duyệt tên trước khi công bố.

Game ngoại tuyến vẫn cần đáp án để chấm tại máy; bộ đáp án phía máy chủ là nguồn đối chiếu độc lập, không có nghĩa đáp án hoàn toàn vắng mặt ở máy người chơi. Tự chấm lại không chứng minh tính xác thực của toàn bộ chiến đấu hoặc thời gian gửi từ máy. Kết quả đang chờ, được nhận và bị từ chối phải phân biệt rõ.

---

## VI. QUY TẮC KIỂM TRA DỮ LIỆU TRƯỚC PHÁT HÀNH

Validate Content phải chặn bản phát hành khi có lỗi sau đây; cảnh báo không được thay cho lỗi bắt buộc.

### BR-27 — Cấu trúc, mã và tham chiếu [Làm rõ]
Mọi dữ liệu phải đúng cấu trúc đã khai báo. Mã câu hỏi duy nhất trên toàn ngân hàng; mã màn, nhân vật, vũ khí, nâng cấp, nguồn và sự kiện duy nhất trong từng danh mục tương ứng. Mã đáp án chỉ cần duy nhất **trong một câu**, nên `a`, `b` có thể dùng lại ở câu khác. Mã điểm sinh, hòm và điện thoại duy nhất trong màn; khi lưu một lần tương tác phải gắn thêm mã lượt. Tham chiếu phải trỏ tới đúng loại đối tượng tồn tại. Không dùng vị trí trong danh sách thay cho mã ổn định.

### BR-28 — Câu hỏi và khả năng chọn câu [Làm rõ]
- Mỗi câu có 2–4 lựa chọn không rỗng; `correct` khớp một mã đáp án của chính câu đó; cấp nằm trong 1–5.
- Mọi câu được đóng gói để chơi trong bản phát hành phải có trạng thái `approved`, nguồn hợp lệ và giải thích đáp án; không đưa câu nháp vào tập chọn.
- Mọi câu có `field` trong `forms`, kể cả `["bunker", "field"]`, phải có phần thân không quá 120 ký tự hiển thị. Chuẩn hóa Unicode trước khi đếm; khoảng trắng và dấu câu được tính, đáp án không nằm trong giới hạn này. Phải kiểm tra riêng đáp án không bị cắt hoặc che trên các kích thước màn hình hỗ trợ.
- Tách danh mục phần thưởng khỏi ngân hàng câu. Kiểm tra đủ câu sau khi lọc màn, hình thức, cấp, trạng thái và loại trừ câu đã gặp.
- Tính ngân sách câu cho số câu trong hầm, số hòm tối đa, số súng rơi tối đa, mọi điện thoại và các nhánh bảo hiểm có thể xảy ra. Câu dùng được ở nhiều tập không được đếm như nhiều câu độc lập. Phải bảo đảm có cách phân bổ câu không lặp cho mọi nhánh hợp lệ, kể cả ba lần sai rồi bảo hiểm lặp lại.

### BR-29 — Vũ khí, tỉ lệ và âm thanh [Làm rõ]
Vũ khí có nguồn và đủ bằng chứng theo BR-18; chặn K-54, K-50M bằng danh sách loại rõ ràng. Mọi trọng số độ hiếm không âm, tổng bằng 1 ở độ chính xác thập phân công bố trong cấu hình; không tự sửa tổng sai. Tỉ lệ rơi nằm trong 0–1, danh mục thưởng không rỗng ở độ hiếm có thể chọn. Mọi âm thanh tham chiếu phải tồn tại. Đạn, súng, giới hạn mang và vật tư nhiệm vụ phải tham chiếu loại tương thích.

### BR-30 — Màn chơi và khả năng hoàn thành [Làm rõ]
Chặn phát hành nếu thiếu nhân vật hư cấu điều khiển, điều kiện thắng/thua, danh sách mục tiêu bắt buộc, giới hạn rơi súng, cấp/thời hạn điện thoại hoặc phương án dự phòng BR-17. Mục tiêu cần vật tư tiêu hao phải có phương án bổ sung phần thiếu cho tất cả mục tiêu còn lại. Không coi chỉ có hòm hoặc điện thoại là đủ.

Kiểm tra dữ liệu chỉ xác nhận cấu hình và liên kết; không chứng minh đường đi, điểm nhận vật tư hoặc thao tác phá mục tiêu thực sự hoạt động. Phải kiểm tra trong từng màn: sai mọi câu, pháo trượt, hết vật tư, túi đầy và tiếp cận phương án dự phòng. Chưa qua các kiểm tra này thì chưa đủ điều kiện nghiệm thu màn.

---

## VII. QUY TẮC BỔ SUNG CHO TOÀN BỘ VÒNG CHƠI

### BR-31 — Chọn câu không lặp và thiếu câu [Đề xuất v2]
- Không hiển thị lại cùng mã câu trong **một lượt chơi màn**, tính chung hầm, hòm, súng rơi và điện thoại. Đánh dấu đã gặp ngay khi câu hiển thị thành công, kể cả sau đó hết giờ.
- Chỉ chọn câu đã duyệt, đúng hình thức, cấp và màn hoặc tập chiến dịch được màn cho phép. Câu liên quan sự kiện chưa được phép trong màn không được chọn chỉ để bù số lượng.
- Cách chọn phải giữ đủ câu cho các tương tác còn lại; không tùy ý tiêu thụ hết câu dùng chung khiến phần sau thiếu câu dù tổng ngân hàng ban đầu đủ.
- Nếu thiếu câu trước lượt: không cho bắt đầu, báo lỗi nội dung rõ ràng. Nếu phát sinh thiếu câu giữa lượt: không phát thưởng miễn phí, không lặp câu, không hạ cấp âm thầm; hủy lần mở, khôi phục điều khiển và khóa tương tác lỗi trong lượt. Không tiêu thụ bảo hiểm đang chờ. Lượt vẫn có thể hoàn thành nhờ BR-17 nhưng không đủ điều kiện lên cả hai bảng xếp hạng; vẫn xét tiến độ và Quyết Tâm khi thắng.
- Lượt mới đặt lại tập không lặp, có thể gặp lại câu từ lượt cũ. Lịch sử toàn hồ sơ dùng để xem lại học tập và kiểm soát thưởng BR-34, không cấm câu đó xuất hiện mãi mãi.

### BR-32 — Thắng, thua và mở khóa màn [Đề xuất v2]
Điều kiện dưới đây cụ thể hóa mô tả GDD, chưa phải thiết kế màn đã được kiểm nghiệm:

| Màn | Điều kiện thắng bắt buộc |
|---|---|
| 1 — Him Lam | Hoàn thành đoạn hướng dẫn, vượt tuyến rào bắt buộc, phá toàn bộ lô cốt mục tiêu và tới điểm tập kết |
| 2 — Giữ Độc Lập | Đẩy lùi toàn bộ đợt phản kích đã khai báo và phá xe tăng Chaffee ở đợt cuối |
| 3 — Đồi E, D | Hạ toàn bộ lính gác và tổ súng máy được đánh dấu là mục tiêu, tới điểm rút; không đồng nhất tổ súng máy với lô cốt miễn nhiễm đạn |
| 4 — Trong lòng hào | Chiếm các đoạn hào theo thứ tự đã khai báo và đẩy lùi các đợt phản kích bắt buộc |
| 5 — Chiều mùng Bảy | Vượt các mốc tiến công, vượt cầu, loại bỏ ổ đề kháng bắt buộc và vào hầm chỉ huy để kích hoạt kết thúc |

Số mục tiêu, số đợt và ranh giới khu vực phải có trong cấu hình màn. Chiếm đoạn hào nghĩa là người chơi còn sống trong vùng chiếm và không còn địch thuộc nhóm giữ đoạn đó; kiểm tra theo thứ tự, không bỏ qua đoạn trước. Không thêm điều kiện thua ngầm vì hết giờ hoặc trả lời sai.

Nhân vật hết máu là thua; chủ động kết thúc trước khi thắng là bỏ dở. Nếu hết máu và hoàn thành mục tiêu đồng thời, ưu tiên thua. Chỉ chốt kết quả một lần. Màn 1 mở sẵn; thắng màn N mở màn N+1, không khóa lại màn đã mở khi thua. Được chọn chơi lại mọi màn đã mở. Phần mở đầu không điều khiển và không tính là lượt xếp hạng.

### BR-33 — Chết, chơi lại và bỏ dở [Đề xuất v2]
Mặc định v2 không có tiếp tục giữa màn hoặc điểm lưu chiến đấu. Chơi lại luôn tạo lượt mới từ hầm, seed mới, hòm/súng/điện thoại mới; đặt lại câu đã gặp trong lượt, chuỗi bảo hiểm, điểm kiến thức, Điểm Quyết Tâm chờ, mục tiêu, địch và trang bị tạm. Không đặt lại tiến độ mở màn, nâng cấp đã mua, số dư đã quyết toán và dấu thưởng toàn hồ sơ. Lượt thua/bỏ dở không được lên bảng và không nhận Quyết Tâm chờ. Việc chọn chơi lại phải kết thúc lượt cũ trước, không có hai lượt hoạt động cùng hồ sơ.

### BR-34 — Thưởng Điểm Quyết Tâm [Đề xuất v2]
Câu trong hầm đúng ở cấp 1/2/3 lần lượt tạo **10/20/30** điểm chờ. Khi thắng, cộng điểm chờ và ghi dấu đã nhận thưởng cùng một lần lưu an toàn. Mỗi mã câu trong hầm chỉ cấp Quyết Tâm **một lần trên mỗi hồ sơ**, kể cả xuất hiện ở màn khác; thay đổi thứ tự đáp án hoặc sửa phiên bản câu không làm mất dấu này. Trả lời lại vẫn nhận điểm kiến thức của lượt mới theo BR-25.

Nếu lượt trước thua hoặc bỏ dở thì chưa ghi dấu thưởng; người chơi có thể trả lời đúng câu đó và thắng ở lượt sau để nhận lần đầu. Không thưởng thêm khi đọc lại kết quả, mở lại game hoặc gửi lại dữ liệu. Giữ mã câu ổn định khi sửa nội dung; không đổi mã chỉ để cấp lại thưởng. Chính sách này giới hạn tổng Quyết Tâm theo ngân hàng câu; nhóm phải kiểm tra tổng điểm có thể kiếm so với chi phí nâng cấp mong muốn trước phát hành.

### BR-35 — Mua và áp dụng nâng cấp [Đề xuất v2]
Nâng cấp dùng chung cho cả năm nhân vật trên hồ sơ, chỉ mua trong hầm trước khi ra trận. Mỗi mã nâng cấp mua một lần; cấp tiếp theo cần cấp trước. Thiếu điểm, chưa đủ điều kiện hoặc đã đạt trần thì từ chối và không trừ điểm. Mua thành công phải trừ điểm và lưu nâng cấp cùng một lần; áp dụng từ lúc xuất trận của lượt hiện tại. Không hoàn điểm tự động và không mua giữa chiến đấu.

| Nhánh | Số cấp tối đa đề xuất | Giá từng cấp I / II / III | Hiệu lực ở cấp I / II / III so với chỉ số gốc |
|---|---|---|---|
| Cơ số đạn dự trữ | 3 | 100 / 200 / 300 | +10% / +20% / +30% |
| Tốc độ thay đạn | 3 | 100 / 200 / 300 | Thời gian thay đạn ×0,9 / ×0,8 / ×0,7 |
| Thể lực | 3 | 100 / 200 / 300 | Thể lực tối đa +10% / +20% / +30% |
| Sức chứa lựu đạn | 3 | 100 / 200 / 300 | +1 / +2 / +3 quả |

Hiệu lực ở cấp hiện tại thay thế cấp trước, không cộng dồn ba mức; sức chứa đạn làm tròn xuống. Thể lực không phải máu tối đa. Tăng sức chứa không tự cấp lại vật phẩm khi đang ở chiến trường. Toàn bộ bảng là giá trị thử nghiệm, cần đồng bộ cấu hình và phiên bản xếp hạng nếu đổi.

### BR-36 — Mang, đổi súng và phần thưởng khi đầy [Đề xuất v2]
- Tối đa hai súng cá nhân; lựu đạn và vật tư nhiệm vụ có ngăn riêng. Súng tại ụ là đối tượng sử dụng tại chỗ, không chiếm chỗ mang súng.
- Nhận súng mới khi đầy: thay khẩu đang cầm; khẩu cũ nằm tại điểm tương tác và có thể nhặt lại không qua câu hỏi vì đã thuộc sở hữu trong lượt. Đổi súng không tạo thêm đạn hoặc lượt tính điểm.
- Nhận súng trùng loại đang mang: đổi thành lượng đạn tương thích khai báo cho phần thưởng, không tạo bản sao súng.
- Đạn chỉ dùng cho súng có loại đạn tương thích theo cấu hình; cùng nhãn độ hiếm không có nghĩa dùng chung đạn. Đạn/lựu đạn vượt sức chứa ở lại tại điểm nhận để nhặt sau, không đổi thành điểm, không phát lại toàn bộ gói khi quay lại.
- Thao tác nhận/đổi thưởng diễn ra trong lượt giải quyết kết quả và không thêm thời gian đóng băng ngoài BR-12. Đối tượng đã chốt câu không được sinh câu mới khi nhặt phần dư.

### BR-37 — Lưu và phục hồi [Đề xuất v2]
Lưu hồ sơ sau tạo/sửa hồ sơ, mua nâng cấp và kết thúc lượt. Ghi nhận câu đã trả lời và giao dịch thưởng đủ để phát hiện trùng. Khi thắng, kết quả, mở khóa, Quyết Tâm, dấu đã thưởng và mục chờ đồng bộ phải được ghi nhất quán: phục hồi chỉ thấy toàn bộ lần quyết toán đã hoàn tất hoặc trạng thái trước quyết toán, không thấy điểm đã cộng nhưng dấu thưởng chưa ghi.

Đóng giữa lượt hoặc ứng dụng dừng đột ngột: lần mở sau đánh dấu lượt chưa có kết quả là bỏ dở, cho bắt đầu lượt mới từ hầm; không khôi phục trạng thái chiến đấu. Nếu kết quả thắng đã lưu hoàn tất thì giữ nguyên, không chuyển thành bỏ dở. Nếu chưa lưu thắng hoàn tất thì không tự cấp thưởng dựa trên màn hình thắng từng hiện. Hàng đợi đã lưu phải tồn tại qua khởi động lại. Lỗi ghi đĩa phải thông báo, không hiển thị “đã lưu” khi chưa thành công.

### BR-38 — Điện thoại gọi pháo [Đề xuất v2]
Mỗi điện thoại gắn với một mục tiêu cụ thể và dùng câu **cấp 3, 15 giây**, không hưởng hoặc tác động bảo hiểm. Nếu mục tiêu đã bị phá trước khi mở câu, điện thoại không khả dụng, không tiêu thụ câu và không thưởng đạn hoặc điểm.

Đúng: ghi một yêu cầu pháo, thực hiện khi chiến trường chạy lại; sai/hết giờ: một đòn trượt, không gây sát thương. Nếu mục tiêu bị phá sau khi câu đã chốt nhưng trước lúc đạn pháo đến, vẫn giữ điểm câu trả lời đúng nhưng không tính phá mục tiêu lần thứ hai. Mục tiêu không hợp lệ hoặc bị thiếu là lỗi cấu hình, không tự chọn mục tiêu khác. Mỗi điện thoại chỉ tạo một kết quả gọi pháo trong lượt; BR-17 luôn cung cấp phương án khác khi gọi pháo thất bại.

### BR-39 — Đồng bộ không trùng [Đề xuất v2]
Một kết quả dùng cùng `attemptId` trong mọi lần gửi lại; không tạo mã mới khi lỗi mạng. Máy chủ ghi nhận tối đa một kết quả cho mã lượt đó. Gửi lại nội dung giống nhau trả xác nhận đã nhận, không cộng điểm lần nữa; cùng mã nhưng nội dung khác bị từ chối và lưu lý do.

Chỉ xóa mục khỏi hàng đợi gửi sau xác nhận tương ứng của máy chủ; giữ bản kết quả tại máy. Mất mạng, hết thời gian chờ hoặc lỗi tạm thời thì giữ nguyên và thử lại sau 5 giây, 15 giây, 60 giây, rồi tối đa mỗi 5 phút khi còn mạng. Sau khởi động lại được thử lại nhưng vẫn giữ mã cũ. Lỗi dữ liệu hoặc phiên bản không hỗ trợ chuyển sang trạng thái bị từ chối có lý do, giữ bản cục bộ và không gửi dồn vô hạn. Đồng bộ hai bảng cùng một lượt không được làm nhân đôi kết quả gốc.

### BR-40 — Điều kiện đủ để nghiệm thu v2 [Đề xuất v2]
Phải có kiểm tra dữ liệu, kiểm tra cách tính điểm và kiểm tra trong game cho các tình huống ở phụ lục A. Khi thay đổi điểm, nâng cấp, cách xếp hạng hoặc nội dung ảnh hưởng so sánh, tăng phiên bản tương ứng, không gộp kết quả cũ/mới vào cùng bảng. Kiểm tra tài liệu này không thay thế kiểm thử game hoặc thẩm định lịch sử.

---

## PHỤ LỤC A — TÌNH HUỐNG KIỂM TRA KHI TRIỂN KHAI

Các dòng dưới là tiêu chí nghiệm thu, **chưa phải kết quả đã kiểm thử trong game**.

| Tình huống | Kết quả bắt buộc | Quy tắc |
|---|---|---|
| Sai mọi câu, pháo trượt, cần phá nhiều lô cốt hoặc xe tăng | Vẫn tiếp cận được vật tư/phương án đủ cho mọi mục tiêu bắt buộc | BR-17, BR-30 |
| Tiêu hao hết hoặc đánh rơi vật tư ở nơi không tới được | Điểm cấp bổ sung phần thiếu; không mắc kẹt vì hết vật tư | BR-17 |
| Câu trong hầm cấp 2 đúng sau thời gian suy nghĩ dài | 200 điểm kiến thức, không thưởng tốc độ; 20 Quyết Tâm chờ nếu chưa nhận thưởng câu đó | BR-06, BR-25, BR-34 |
| Ba hòm sai, xen giữa một điện thoại đúng, rồi mở hòm hiếm | Hòm tiếp theo cấp 1, 12 giây, thưởng hiếm giữ nguyên; sau câu đó chuỗi về 0 | BR-16 |
| Hết giờ tại hòm hoặc súng rơi | 0 điểm kiến thức, chỉ thưởng đạn; đối tượng không cho trả lời lại | BR-11, BR-14 |
| Thả E ở 0,9 giây hoặc chết trước khi đủ 1 giây | Không mở câu, không tiêu thụ đối tượng/câu | BR-08 |
| Kết thúc câu hỏi và còn 0,5 giây chuyển tiếp | Địch chưa gây sát thương, người chơi được mở điều khiển cùng lúc chiến trường tiếp tục | BR-12 |
| Thiếu câu sau khi lọc, kể cả thiếu câu cấp 1 bảo hiểm | Chặn trước lượt hoặc xử lý lỗi giữa lượt, không lặp/hạ cấp; không xếp hạng lượt lỗi | BR-28, BR-31 |
| Câu dùng chung hai hình thức dài 121 ký tự; câu nháp có nguồn | Đều không được dùng trong bản phát hành | BR-28 |
| Hai thành tích bằng ba chỉ số đầu nhưng sát thương khác | Lượt nhận ít sát thương xếp trên, chưa cần so thời gian | BR-24 |
| Hai thành tích bằng mọi tiêu chí; gửi mạng khác thời điểm | Đồng hạng; thời điểm gửi không quyết định thắng thua | BR-24, BR-25 |
| Đúng trong hầm rồi thoát, hoặc chết rồi chơi lại | Không cộng Quyết Tâm; không mang theo đồ/điểm tạm từ lượt cũ | BR-33, BR-34 |
| Thắng rồi khởi động lại, đọc lại kết quả và gửi lại nhiều lần | Điểm thưởng và kết quả mỗi lượt chỉ ghi một lần | BR-34, BR-37, BR-39 |
| Túi đủ hai súng hoặc đạn đầy khi nhận thưởng | Thay súng đang cầm, giữ súng cũ/phần dư tại điểm nhận; không tạo thêm đồ khi nhặt lại | BR-36 |
| Điện thoại có mục tiêu đã bị phá | Không mở câu hoặc ghi công phá mục tiêu lần hai | BR-38 |
| Mất mạng sau khi máy chủ nhận nhưng trước khi máy nhận xác nhận | Gửi lại cùng mã, máy chủ trả xác nhận mà không tạo bản trùng | BR-39 |
| Đóng ứng dụng trong lúc trả lời hoặc quyết toán | Bỏ dở lượt chưa lưu kết quả; bảo toàn toàn bộ lượt thắng đã lưu, không thưởng nửa chừng | BR-37 |

## PHỤ LỤC B — ĐỀ XUẤT CẦN NHÓM CHỐT VÀ ĐỒNG BỘ GDD

| Nhóm quyết định | Mặc định trong v2 | Cần đồng bộ |
|---|---|---|
| Không mắc kẹt nhiệm vụ | Vật tư nhiệm vụ không qua câu hỏi, bổ sung phần thiếu, áp dụng cả chống tăng | GDD phần vũ khí, năm màn, Validate và ví dụ cấu hình bảo đảm |
| Thời gian và điểm | Hầm không hạn giờ; cấp 1–5 nhận 100–500; ngoài trận thưởng tốc độ, làm tròn từng câu | GDD phần câu hỏi, dữ liệu cấp độ và bảng kiến thức |
| Bảo hiểm và điện thoại | Bảo hiểm chỉ hòm: cấp 1/12 giây; điện thoại cấp 3/15 giây | GDD phần tương tác, cấu hình điện thoại |
| Ngân hàng câu và súng rơi | Không lặp trong lượt; đặt giới hạn rơi súng mỗi màn; dự trữ đủ câu cho mọi nhánh | GDD phần chọn câu, cấu hình màn và Validate |
| Xếp hạng | Chỉ lượt thắng cho cả hai bảng; chiến đấu so năm tiêu chí theo thứ tự; phân nhóm điều kiện chơi | GDD phần hồ sơ và bảng xếp hạng |
| Thắng/thua và chơi lại | Điều kiện năm màn ở BR-32; không tiếp tục giữa màn, lượt mới từ hầm | GDD phần năm màn và lưu tiến độ |
| Quyết Tâm và nâng cấp | Quyết toán khi thắng; thưởng một lần/mã câu/hồ sơ; nâng cấp dùng chung năm nhân vật, bảng giá BR-35 | GDD phần chuẩn bị, hồ sơ và nâng cấp; kiểm tra đủ ngân sách điểm |
| Trang bị | Hai súng cá nhân, không mang đồ nhặt sang lượt khác, vật tư nhiệm vụ riêng | GDD phần kho vũ khí và persistence |
| Hồ sơ và đồng bộ | Xác nhận trước khi công khai; lưu nhất quán, gửi lại cùng mã, có trạng thái từ chối | GDD phần dữ liệu người chơi và online |
| Nội dung lịch sử | Chứng cứ sử dụng theo màn và danh sách loại rõ ràng | GDD phần kho vũ khí, nguồn và Validate; cần thẩm định nội dung riêng |

**Giới hạn của bản bàn giao:** Đã cụ thể hóa quy tắc để nhóm có thể thảo luận, triển khai và kiểm thử thống nhất. Chưa triển khai game, chưa chơi thử các giá trị cân bằng, chưa xác minh độc lập các khẳng định lịch sử và chưa cập nhật GDD.
