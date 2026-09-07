# HUD nông trại theo ảnh tham khảo

UI mới giữ scene Unity và vòng chơi hiện có. Các cụm điều khiển nổi thay thanh menu toàn chiều ngang:

- Góc trái: chân dung, tên game, mùa và tiến độ mùa; số ô đất, ô chờ thu; nút chăm cây.
- Góc phải: xu, số thợ và tổng số vật phẩm trong kho. Các số đều đọc từ GameState.
- Mép dưới: nhà, kho, trang trí, nâng cấp, sổ tay, xưởng, nhân sự và cài đặt.
- Giữa dưới: về toàn cảnh, hành động theo trạng thái cây, xem cận xưởng. Nút tròn mở ô trống để gieo, thu một ô đã chín qua GameSession, hoặc mở bảng chăm cây khi tất cả đang lớn.
- Thanh máy pha gọn phía trên giữ nguyên trạng thái công thức và tiến độ mẻ.
- Dưới nút chăm cây: thẻ dư nợ, chỉ hiện khi đang có khoản vay. Ba trạng thái của nó là ba việc khác nhau người chơi cần làm — đang trả bình thường, đang thiếu kỳ, đã bị niêm phong.

Sổ tay mở bản HTML người dùng cung cấp tại `Assets/StreamingAssets/Mo-phong-khoi-nghiep-tra.html` bằng trình duyệt mặc định. Bản này được giữ nguyên, bao gồm sáu mục tham khảo và các bộ mô phỏng JavaScript. HTML dùng Tailwind và Chart.js từ CDN nên phần đó cần mạng.

Sổ tay còn là **cửa vào năm bảng** dựng sáu mục của tài liệu thành cơ chế chơi được: vốn và nợ, nông học và mùa vụ, kỹ thuật căn lửa, pháp lý và ATTP, mô hình kinh doanh. Số cân bằng, khung tiền phạt và bảng phân hạng thu hái giờ **đều nằm trong luật chơi của Unity** — xem [Docs/Mo-phong-khoi-nghiep-tra.md](../Mo-phong-khoi-nghiep-tra.md), trong đó có cả những chỗ cố ý làm khác tài liệu và lý do.

Icon HUD là mesh vector trong `FarmHudIcon.cs`, không cần thư viện hay ảnh tải ngoài. `GameHud.Farm.cs` dựng các thẻ viền kem, nền ngà và bóng nhẹ. Camera mặc định 6.6 cho cảnh chiếm nhiều diện tích hơn; giới hạn kéo/zoom cũ được giữ lại.

## Kiểm tra

Bản `HudReview` có product name riêng, nên kiểm tra tự động không đọc hay ghi save của bản chơi chính. Kết quả kiểm tra và ảnh chụp được ghi sau khi build tại `Docs/Art` và `Docs/screenshots`.
