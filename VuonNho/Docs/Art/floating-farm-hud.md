# HUD nông trại theo ảnh tham khảo

UI giữ nguyên scene Unity và vòng chơi hiện có. Các cụm điều khiển nổi thay thanh menu toàn chiều
ngang:

- **Góc trái trên** — thẻ hồ sơ: chân dung, tên game, mùa và tiến độ mùa.
- **Cột dọc mép trái**, ngay dưới thẻ hồ sơ — năm nút vuông **chỉ có icon**: trang trí, nâng cấp,
  sổ tay, khu vườn, kho. Rê chuột lên một nút thì tên hiện ra thành một thẻ xanh bên phải nó.
- **Góc phải trên** — thẻ xu. Chỉ một con số, đọc từ GameState.
- **Giữa mép phải** — ba nút có chữ: xưởng, nhân sự, cài đặt.
- **Giữa dưới** — về toàn cảnh, hành động theo trạng thái cây, xem cận xưởng. Nút tròn mở ô trống
  để gieo, thu một ô đã chín qua GameSession, hoặc mở bảng chăm cây khi tất cả đang lớn.
- **Trên cùng giữa** — thanh máy pha, giữ nguyên trạng thái công thức và tiến độ mẻ.
- **Dưới cùng cột trái** — thẻ dư nợ, chỉ hiện khi đang có khoản vay. Ba trạng thái của nó là ba
  việc khác nhau người chơi cần làm: đang trả bình thường, đang thiếu kỳ, đã bị niêm phong.

## Vì sao cột trái chỉ có icon

Năm nút xếp dọc mà mỗi cái mang một dòng chữ là một cột chữ chạy suốt mép trái màn hình, che mất
khu vườn — mà khu vườn mới là thứ người chơi nhìn. Thẻ tên chỉ hiện **một cái một lúc**, và chỉ khi
người chơi đang hỏi đến nó.

Cột dựng bằng `VerticalLayoutGroup` chứ không đặt toạ độ cứng cho từng nút: nút trang trí chỉ hiện
sau khi người chơi dựng xong vườn ở pha 1, nên toạ độ cứng sẽ để lại một cái lỗ ở đầu cột trước đó,
còn layout thì tự khép lại. Thêm nút mới về sau cũng không phải tính lại toạ độ của những nút bên
dưới.

`childControl` phải **bật** và `childForceExpand` phải **tắt**: layout mới lấy kích thước 68 × 68 từ
`LayoutElement` của từng nút. Tắt `childControl` thì layout đọc `sizeDelta` của nút, mà nút vừa tạo
ra chưa có kích thước nào — nó sẽ ra 100 × 100 mặc định của `RectTransform`, tức một cột hình chữ
nhật cao thay vì năm ô vuông.

## Góc phải trên chỉ còn xu

Thẻ này từng mang cả số thợ và số hàng trong kho. Bỏ cả hai vì chúng **đã có đường vào riêng**: nút
"Nhân sự" ở mép phải và nút "Kho" ở cột trái. Một con số vừa hiện ở góc màn hình vừa có một cái nút
riêng là hai chỗ nói cùng một chuyện, và cái ở góc màn hình thì không bấm được để làm gì sâu hơn.

Xu ở lại vì nó khác hẳn: nó đổi sau **mọi** hành động — gieo, thu, mua, trả nợ — nên nó là con số
duy nhất cần nhìn thấy liên tục mà không phải mở gì cả.

Thẻ cao 68 px từ mốc −24, nên `GameHud.ContentTop` phải là 100 — các bảng bên phải bắt đầu dưới nó.
Để 76 như cũ thì bảng đè lên đáy thẻ, và có một mục kiểm bắt được điều đó.

## Những gì đã bỏ khỏi HUD

Thẻ "12/12 ô đất · 0 ô chờ thu" và nút "Chăm cây" riêng đã bỏ. Cả hai việc đó giờ nằm ở nút tròn
giữa dưới và dòng nhắc ngay trên nó: dòng nhắc nói mấy ô có sâu, mấy ô chờ thu hay chưa gieo gì, và
cái nút chính là nút làm việc đó — nên câu nhắc và cái nút luôn nói về cùng một việc. Số ô đã mở
đọc ở bảng Nâng cấp.

## Sổ tay

Mở bản HTML người dùng cung cấp tại `Assets/StreamingAssets/Mo-phong-khoi-nghiep-tra.html` bằng
trình duyệt mặc định. Bản này được giữ nguyên, bao gồm sáu mục tham khảo và các bộ mô phỏng
JavaScript. HTML dùng Tailwind và Chart.js từ CDN nên phần đó cần mạng.

Sổ tay còn là **cửa vào năm bảng** dựng sáu mục của tài liệu thành cơ chế chơi được: vốn và nợ,
nông học và mùa vụ, kỹ thuật căn lửa, pháp lý và ATTP, mô hình kinh doanh. Số cân bằng, khung tiền
phạt và bảng phân hạng thu hái đều nằm trong luật chơi của Unity — xem
[Docs/Mo-phong-khoi-nghiep-tra.md](../Mo-phong-khoi-nghiep-tra.md), trong đó có cả những chỗ cố ý
làm khác tài liệu và lý do.

## Icon

Icon HUD là mesh vector trong `FarmHudIcon.cs`, không cần thư viện hay ảnh tải ngoài. Mười tám icon
vẽ trên lưới 100 × 100 bằng năm hàm dựng hình: `Box`, `RoundBox`, `Ellipse`, `Line`, `Poly`.
`GameHud.Farm.cs` dựng các thẻ viền kem, nền ngà và bóng nhẹ. Camera mặc định 6.6 cho cảnh chiếm
nhiều diện tích hơn; giới hạn kéo/zoom cũ được giữ lại.

## Kiểm tra

Bản `HudReview` có product name riêng, nên kiểm tra tự động không đọc hay ghi save của bản chơi
chính. Kết quả kiểm tra và ảnh chụp được ghi sau khi build tại `Docs/Art` và `Docs/screenshots`.

196 mục kiểm, đạt hết ở cả 1366 × 768 và 1920 × 1080. Trong đó có một mục gửi thẳng `PointerEnter`
và `PointerExit` qua EventSystem vào từng nút cột trái rồi đọc lại thẻ tên: ảnh chụp không bắt được
trạng thái rê chuột nên nếu không kiểm ở đó thì không ai kiểm.
