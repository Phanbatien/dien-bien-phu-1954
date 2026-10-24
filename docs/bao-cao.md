# Báo cáo dự án: Điện Biên Phủ 1954 — Tiếp bước lịch sử (T72)

Môn PRU213 · FPT University · Bản nộp v1.0 · 24/10/2026
Repo: https://github.com/Phanbatien/dien-bien-phu-1954 · Trailer: [nop-bai/trailer.mp4](nop-bai/trailer.mp4) · Slide: [nop-bai/bao-cao.pptx](nop-bai/bao-cao.pptx)

Báo cáo chia theo 5 nhóm tiêu chí chấm của môn ([tieu-chi-cham.md](tieu-chi-cham.md)). Mỗi mục có minh chứng (file, test, ảnh) để giảng viên kiểm tra.

![Màn 2 nhìn từ chỗ người chơi](nop-bai/anh/m2-goc-nhin-nguoi-choi.png)

## Nhóm thực hiện

| Thành viên | Vai chính |
|---|---|
| Phanbatien (trưởng nhóm) | Bộ điều khiển FPS, súng, nhiệm vụ, nâng cấp, công cụ Editor, tích hợp, build |
| Bùi Nguyễn Thành Đạt | AI địch (lính, lính gác, đánh hào, ổ súng máy, lô cốt, xe tăng), máu/sát thương |
| Lê Nguyễn Đình Khôi | Môi trường, bộ hào modular, mô hình, vật liệu URP |
| Thiên Trí | 146 câu hỏi, 16 nguồn tư liệu, giao diện câu hỏi, cốt truyện |
| Trần Hoàng Long | Menu, HUD, âm thanh, bảng xếp hạng |

---

## 1. Ý tưởng và thiết kế (20%)

**Ý tưởng.** Một game bắn súng góc nhìn thứ nhất về chiến dịch Điện Biên Phủ, trong đó kiến thức lịch sử là vũ khí: mở hòm tiếp tế, gọi pháo qua điện thoại dã chiến hay họp tác chiến trong hầm đều phải trả lời câu hỏi. **Khi câu hỏi hiện lên, cả chiến trường đứng yên** (đạn, lính, âm thanh) nhưng đồng hồ câu hỏi vẫn chạy. Trả lời đúng nhanh thì súng hiếm hơn, pháo trúng đích.

**Đối tượng.** Học sinh, sinh viên 15–22 tuổi; chơi 10–15 phút mỗi màn; không cần biết trước lịch sử (câu sai có giải thích).

**Thế giới và cốt truyện.** 5 màn bám ba đợt tiến công của chiến dịch:

| Màn | Tên | Lối chơi chính |
|---|---|---|
| 1 | Mở màn Him Lam (13/3/1954) | Đánh vào cứ điểm, phá lô cốt bằng bộc phá |
| 2 | Giữ Độc Lập | Đẩy lùi phản kích, diệt xe tăng Chaffee bằng Bazooka |
| 3 | Đêm đồi E, D | Đánh đêm dưới pháo sáng, thoại vô tuyến |
| 4 | Trong lòng hào | Cận chiến trong hào, địch ném lựu đạn |
| 5 | Chiều mùng Bảy | Ổ đề kháng cuối, vụ nổ đồi A1, cầu Mường Thanh |

Mở đầu 90 giây (kéo pháo, Tô Vĩnh Diện, dân công), nhật ký chiến sĩ trong hầm, kết thúc chiến dịch và màn tưởng nhớ: [cot-truyen.md](cot-truyen.md). Nhân vật lịch sử thật chỉ có trong lời dẫn, người chơi là chiến sĩ vô danh; không máu me.

**Thiết kế có văn bản.** GDD v5, 40 quy tắc nghiệp vụ ([business-rules.md](business-rules.md)), kiến trúc ([kien-truc.md](kien-truc.md)), kế hoạch 75 việc ([ke-hoach.md](ke-hoach.md), [cong-viec.md](cong-viec.md)), biên bản quyết định ([quyet-dinh/](quyet-dinh/)).

## 2. Cài đặt kỹ thuật (30%)

**Cấu trúc code.** Unity 6000.3.24f1, URP, Input System. 94 script runtime (~10.400 dòng) chia 12 namespace theo hệ thống (`AI`, `Core`, `Interaction`, `Missions`, `Player`, `Quiz`, `Ranking`, `UI`, `Upgrades`, `Weapons`…), 4 assembly (`DBP.Runtime`, `DBP.Editor`, test EditMode, test PlayMode). Một MonoBehaviour mỗi file.

**Kỹ thuật Unity dùng đúng chỗ.**
- Prefab người chơi, súng, lựu đạn; NavMeshSurface bake riêng từng màn cho AI.
- Humanoid retarget: hoạt ảnh Starter Assets chạy trên mô hình lính low-poly, blend tree theo tốc độ thật.
- Vật lý: raycast cho đạn, va chạm theo vùng cho lựu đạn và bộc phá, đất địa hình một mặt được xử lý riêng khi đặt hòm.
- Hậu kỳ URP (Volume): màu phim bi tráng, hiệu ứng đóng băng khi trả lời.
- Pool hiệu ứng (lửa nòng, khói, tia lửa, đường đạn), không `Instantiate` trong lúc bắn.
- Nội dung tách khỏi code: câu hỏi, nguồn, vũ khí là JSON trong `StreamingAssets`.

**Không lỗi nghiêm trọng.**
- 134 test EditMode + 68 test PlayMode, đều đạt (1 test cần Game view có focus được bỏ qua khi chạy dòng lệnh). Danh sách và đối chiếu 17 tình huống nghiệm thu: [kiem-thu-phu-luc-a.md](kiem-thu-phu-luc-a.md).
- Test chạy thật cả 5 màn với mọi địch bật: không lỗi, lính đứng trên NavMesh.
- **Validate Content** (Editor) chặn build nếu câu quá 120 ký tự, câu nháp, vũ khí sau 1954, thiếu nguồn, thiếu câu: bản nộp **0 lỗi**.
- Hồ sơ người chơi ghi qua file tạm + bản sao lưu, chống hỏng file khi tắt máy đột ngột.

**Chạy mượt.** Đo bằng `-perf-probe` trên bản build thật: thấp nhất **531 FPS** (Màn 1), khung chậm nhất 17 ms (Màn 4) ở 1280×720 ([build.md](build.md)).

## 3. Gameplay (20%)

- **Điều khiển quen thuộc:** WASD, chuột ngắm/bắn, R thay đạn, 1/2 đổi súng, G lựu đạn, giữ E 1 giây để tương tác (thả sớm không mở, chống bấm nhầm).
- **Phản hồi rõ:** đường đạn vạch cho mọi khẩu súng ta và địch, lửa nòng, bụi/tia lửa chỗ trúng, lính trúng đạn gục ngã rồi mờ dần, thông báo nhận đồ, HUD máu/thể lực/đạn/mục tiêu.
- **Đa dạng:** 5 màn 5 lối chơi; 7 loại súng lịch sử (Mosin, MAT-49, Kiểu 50, Mauser, Bazooka…) với độ hiếm; lựu đạn, bộc phá, gọi pháo; nâng cấp 4 nhánh × 3 cấp mua bằng Điểm Quyết Tâm.
- **Cân bằng và luôn qua được màn:** sai 3 hòm liền thì hòm sau là câu dễ "bảo hiểm"; vật tư nhiệm vụ (bộc phá, Bazooka) không phụ thuộc câu hỏi; câu không lặp trong một lượt.
- **AI có hành vi:** lính nấp sau bao cát, lính gác tuần tra → nghi ngờ → báo động, nhóm phản kích rút lui khi mất quá nửa quân, lính trong hào ném lựu đạn khi người chơi áp sát, xe tăng chạy theo tuyến có bộ binh hộ tống.

![Bộ binh và xe tăng Màn 2](nop-bai/anh/m2-bo-binh-xe-tang.png)

## 4. Giao diện và trải nghiệm (15%)

- Menu: tạo nhiều hồ sơ (tên trùng vẫn tách biệt), chọn màn (thắng mới mở màn sau), âm lượng, đồng ý công khai thành tích.
- **Hầm chuẩn bị** hiện cảnh Hầm chữ A 3D phía sau giao diện: mua nâng cấp, họp tác chiến 3–5 câu (sai thì hiện đáp án + giải thích).
- **Hướng dẫn cách chơi** trước Màn 1 + ESC mở tạm dừng có hướng dẫn.
- Màn kết quả: điểm kiến thức, Quyết Tâm, thống kê, bảng xếp hạng chiến đấu và kiến thức.
- Mở đầu, nhật ký, kết thúc, Nguồn tư liệu & Credits.

## 5. Đồ họa và âm thanh (15%)

- Phong cách low-poly flat-shaded thống nhất ([style-guide-lowpoly.md](style-guide-lowpoly.md)), vật liệu URP.
- Không khí chiến trường bi tráng: dãy núi bao quanh lòng chảo, sương bụi, cột khói, xác pháo cháy, hố bom, mộ súng cắm mũ sắt, pháo nổ xa, đạn phòng không vạch trời; Màn 3 đêm có pháo sáng; Màn 5 chiều tà đỏ cam.
- Âm thanh: tiếng súng riêng từng loại, pháo xa, máy bay nền, bước chân, nhạc mỗi màn; nhạc tự giảm khi giao tranh.
- Chuyển cảnh mờ dần; thế giới đóng băng ngả nâu xám khi trả lời; 0,5 giây chuyển tiếp trước khi chiến trường chạy lại.

![Màn 5 chiều tà](nop-bai/anh/m5-chieu-ta.png)

## Điểm cộng

- **Tính năng nổi bật:** thế giới đứng yên khi trả lời câu hỏi; câu hỏi gắn trực tiếp vào phần thưởng chiến đấu.
- **Bảng xếp hạng:** hai bảng tại máy (chiến đấu 5 tiêu chí, kiến thức) theo nhóm điều kiện công bằng; hàng chờ gửi online có thử lại và chống gửi trùng (T62).

## Hạn chế và hướng phát triển

- Máy chủ chấm lại điểm online chưa triển khai (máy khách đã sẵn sàng).
- Hoạt ảnh tay góc nhìn thứ nhất còn đơn giản (cẳng tay khối).
- Cần thêm buổi chơi thử với người ngoài nhóm để cân lại độ khó.

## Cài đặt và chạy

Windows 10/11 64-bit, DirectX 11. Giải nén `DienBienPhu1954_v1.0.zip`, chạy `DienBienPhu1954.exe`. Không cần cài đặt, không cần mạng.
