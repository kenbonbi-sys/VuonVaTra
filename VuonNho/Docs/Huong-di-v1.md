# Vườn và Trà — hướng đi v1

Ghi lại thay đổi định hướng so với [kế hoạch MVP](../../Ke-hoach-MVP-Vuon-Nho.md), để sau này còn
đối chiếu được vì sao số liệu và cảm giác game khác bản v0.

## Hai thay đổi định hướng

| | Kế hoạch v0 | Hướng v1 |
|---|---|---|
| Tên | Vườn Nhỏ: Quán Trà | **Vườn và Trà** |
| Cảm xúc chủ đạo | Ấm áp, thư giãn, không phạt | **Thắng nhanh, thưởng nhanh, dopamine cao** |
| Cấu trúc | Một vòng tycoon phẳng, mở khóa tuyến tính | **Hai pha: dẫn dắt dựng farm → mở rộng và tùy biến** |

Đổi trụ cảm xúc là thay đổi lớn nhất: mục 2 kế hoạch chốt "ấm áp, thư giãn", và toàn bộ balance v0
được dựng theo đó. v1 đi hướng khác nên bộ số phải làm lại từ đầu, không phải chỉnh nhẹ.

## Balance v1 — đo được, không phải ước lượng

Số mới nằm trong `Assets/Scripts/Core/DefaultContent.cs`. Các mốc dưới đây do bộ QA trong bản build
tự chơi và đo, không phải tính tay:

| Mốc | v0 | v1 |
|---|---:|---:|
| Xu đầu tiên từ trà | 48 giây | **18 giây** |
| Đủ tiền mua robot | 5,5 phút · 44 lần bấm thu | **1,6 phút · 36 lần bấm thu** |
| Mua hết thang nâng cấp | 27,2 phút | **19,3 phút** |

Chu kỳ: bạc hà 8 giây, cúc 16, dâu 26, sả 40, nhài 60; pha 4 / 6 / 9 / 13 / 18 giây. Thời gian pha
được chọn để 4 ô cùng loại cấp vừa đủ cho máy — giữ nguyên bài học thiết kế của v0 là "mở thêm đất
không tự tăng tiền trà khi máy đang là giới hạn".

### Nội dung phải dài thêm, không chỉ đắt thêm

Rút nhịp xuống làm thu nhập tăng nhanh hơn nội dung, nên lần đo đầu **thang nâng cấp bị mua hết ở
phút 13** — sớm hơn cả v0. Cách lấp đã chọn là thêm nội dung thật chứ không nâng giá:

| | v0 | v1 |
|---|---:|---:|
| Loại cây | 3 | **5** (thêm sả, nhài) |
| Bậc nâng cấp | 7 | **11** (thêm Tốc độ máy II, Tốc độ cây II và hai lần mở cây) |
| Tổng chi phí | 1 840 xu | **7 490 xu** |
| Phiên đo được | 27,2 phút | **19,3 phút** |

Vẫn ngắn hơn v0 khoảng 8 phút. Đó là cái giá của nhịp nhanh, và pha 2 gánh phần còn lại.

## Hai pha

**Pha 1 — dựng farm có dẫn dắt (đã chạy được, ~13 phút).**
Hướng dẫn kéo người chơi qua đúng một đường: gieo → thu → thấy trà thành xu → mua robot → chọn
cây/công thức → mở đất → tăng tốc. Hết thang nâng cấp là hoàn thành pha 1. Toàn bộ phần này đã
chạy và đo được trong bản build hiện tại.

**Pha 2 — trang trí (đã chạy được).**
Mở ra khi người chơi đã có robot **và** đã mở hết 12 ô — tức là cái farm đã dựng xong theo đúng
nghĩa của luồng dẫn dắt. Lúc đó nút "Trang trí" mới hiện trên thanh HUD.

Đã chốt là **thuần thẩm mỹ**: trang trí không xuất hiện trong `FarmSimulation`, không đổi năng suất,
không đổi thời gian. Có một test khẳng định điều này — cùng một snapshot, chạy 10 phút có và không
có trang trí, kết quả kinh tế phải giống nhau từng đồng.

Năm món khởi điểm: phiến đá 60 xu, chậu hoa 120, đèn lồng 260, ghế gỗ 320, bảng hiệu 480.
**Gỡ ra hoàn đủ xu**, nên người chơi thử bố cục thoải mái mà không sợ mất tiền — đây là lựa chọn
có chủ ý, và vì trang trí không sinh lợi nên hoàn đủ không tạo kẽ hở kinh tế nào.

Cách chơi: chọn món trong panel → bấm vào khoảng đất trống để đặt (bám lưới 20 cm), giữ nguyên
chế độ để đặt liền mấy món; chuột phải thoát. Nút "Gỡ đồ đã đặt" chuyển sang chế độ gỡ.

Kỹ thuật:

- `PlacedDecoration` lưu toạ độ bằng **milimet nguyên**, nên save đọc lại đúng y nguyên vị trí,
  giữ đúng kỷ luật số nguyên của mục 8 kế hoạch.
- **Save lên schema 2** kèm migration 1 → 2 (thêm danh sách rỗng). Save của bản cũ vẫn nạp được;
  có test cho đúng đường đi này.
- Đặt và gỡ là **transaction có lưu** giống lệnh mua nâng cấp: lỗi ghi thì không trừ tiền.
- `DecorationLayer` dựng lại toàn bộ từ state khi danh sách đổi. View mất đi dựng lại vẫn y nguyên.
- Model lấy từ `GardenSkin.Decorations`; ô nào chưa có prefab thì rơi về primitive, đúng cách bộ
  art hiện tại đang làm với cây và quầy.

### Giới hạn đã biết của pha 2

Core chỉ chặn đặt ra ngoài khu vườn và đặt chồng lên món khác. **Nó không chặn đặt đè lên ô đất
hay quầy trà** — trong game thì click vào ô đất bị chính collider của ô nuốt trước nên hiếm khi
xảy ra, nhưng vẫn đặt sát mép được. Chưa xử lý vì trang trí không ảnh hưởng gameplay; nếu thấy
xấu khi chơi thì thêm vùng cấm vào `CanPlaceDecoration`.

## Hướng mở tiếp

Đã chọn **trang trí thuần thẩm mỹ** cho pha 2 và **thêm cây/công thức** để kéo dài phiên.
Hai hướng còn lại vẫn để ngỏ, và giờ chúng rẻ hơn trước vì save đã có schema 2 và đã có tiền lệ
migration:

- **Bố trí farm ảnh hưởng năng suất** — nặng nhất: `FarmSimulation` phải hiểu không gian và toàn
  bộ test balance phải làm lại. Toạ độ của trang tri đã mở đường, nhưng ô đất thì vẫn chưa có.
- **Nhiều khu vườn** — mỗi khu một bộ cây và công thức riêng.

Nếu sau này làm hướng bố trí, thứ phải đổi là `PlotState` — hiện ô đất chỉ có id, chưa có toạ độ.
