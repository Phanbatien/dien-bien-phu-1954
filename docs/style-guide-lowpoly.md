# Style guide low-poly (T04)

Dùng cho hào, công sự và prop của Điện Biên Phủ 1954. Model mới để trong `Assets/_Project/Art/`, prefab trong `Assets/_Project/Prefabs/`. Bật **Batching Static** trên mesh đứng yên.

## Bảng màu

Lấy từ material greybox đã có (`Assets/_Project/Art/Materials/Greybox/`). Không thêm màu bão hòa.

| Việc | Màu | Material |
|---|---|---|
| Bùn, sàn hào | `#5C4530` | `Mud.mat` |
| Vách đất | `#734D33` | `Trench.mat` |
| Gỗ (sàn, hầm chữ A, cầu) | `#735433` | `Wood.mat` |
| Bao cát | `#998C66` | `Sandbag.mat` |
| Bê tông lô cốt | `#807F7A` | `Concrete.mat` |
| Kim loại súng, dây thép | `#333833` | `Metal.mat` |
| Rào kẽm | `#4D4D4D` | `Wire.mat` |

Sương Màn 1: Exponential Squared, mật độ `0.012`, màu `#8C8573`. Ánh sáng pháo là đèn điểm ấm `#FF9E59`, không đổ bóng.

## Lưới module hào

Khớp nhân vật trong `Player.prefab`: bán kính `0,35 m`, cao `1,8 m`, mắt `1,65 m`.

| Thông số | Giá trị |
|---|---|
| Bước lưới | 4 m (module nối dài 2 m) |
| Lòng hào | rộng 2,2 m (mỗi bên 1,1 m) |
| Vách | dày 0,45 m, cao 1,9 m |
| Sàn | mặt trên tại y = 0, dày 0,2 m |
| Trục | tâm miệng phía nam, nhìn +Z |

Module mẫu là `Hao_Thang` (4 m). Ghép thẳng bằng cách đặt module sau tại z += 4. Góc rẽ sang +X: miệng thoát tại `(4, 0, 4)`, module kế xoay yaw 90°. Chữ T có thêm miệng thoát tại `(4, 0, 2)`.

Bộ prefab (T09): `Hao_Thang`, `Hao_Goc`, `Hao_ChuT`, `Hao_BaoCat`, `Hao_HamChuA`, `Hao_Noi`. Đoạn thử 30 m là `Hao_DoanThu_30m` (7 thẳng + 1 nối), scene `Scenes/Dev/T09_HaoThu.unity`.

Súng góc nhìn thứ nhất giữ dưới 3.000 tam giác cho riêng model súng. Hoạt ảnh nạp bám `ReloadProgress01`, hết đúng lúc đạn vào băng.
