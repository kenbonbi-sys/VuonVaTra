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

## Lớp token và component

Ba file mới ở `Assets/Scripts/Views/`:

- **`UiTokens.cs`** — khoảng cách, cỡ chữ, bán kính, và màu theo **sắc thái** chứ không theo tên
  màu. Chỗ gọi nói "cái này đang sai" (`UiTone.Bad`), còn màu là thứ suy ra. Khoảng cách đi theo
  bậc 4: 4, 8, 12, 16, 24, 32 — mắt đọc *quan hệ* giữa các khoảng cách chứ không đọc giá trị tuyệt
  đối, nên một thang bậc đều quan trọng hơn việc từng con số có "đúng" hay không. Những hằng số
  `UiFactory` đã có từ trước thì **trỏ về chính chúng** thay vì khai báo lại: hai nguồn số cho
  cùng một thứ là hai nguồn sẽ lệch nhau, và cái lệch đó không bao giờ báo lỗi.
- **`UiKit.cs`** — sáu component: `Section`, `Card`, `StatRow`, `Badge`, `Meter`, `Callout`.
- **`UiMotion.cs`** — lớp chuyển động.

Năm bảng của bản mô phỏng ban đầu đều là những khối chữ nhiều dòng. Đọc được, nhưng **đọc như một
trang tài liệu**: mắt không bắt được con số nào trước, không biết cái nào đang tốt cái nào đang
hỏng, và phải đọc hết cả đoạn mới tìm ra dòng mình cần. Giờ mỗi con số là một hàng có nhãn trái giá
trị phải, mỗi trạng thái là một thẻ mang màu của nó, và mỗi thông số căn lửa có thang đo riêng với
khoảng chuẩn vẽ sẵn trên nền.

Thang đo của bốn nút căn lửa lấy **khoảng chấp nhận được** làm trục, không lấy cả dải chỉnh được.
Nhiệt độ chỉnh được từ 80 đến 320 độ, mà khoảng chuẩn của trà xanh chỉ là 250–260 — vẽ trên trục
240 độ thì vệt sáng đó rộng bốn phần trăm, tức một sợi chỉ không nhìn ra. Trên trục 235–275 thì nó
chiếm một phần tư thanh, và nó trở thành thứ đọc được.

## Chuyển động

Trước đây mọi thay đổi đều tức thì — `SetActive` bật tắt, số nhảy một phát từ 0 sang 4.569, thanh
tiến độ gán thẳng tới vị trí mới. Mắt người đọc chuyển động tốt hơn đọc một khung hình đứng yên:
một cái bảng trượt vào nói cho người chơi biết nó **đến từ đâu** và vì thế đóng lại thì về đâu, còn
một con số chạy lên nói cho họ biết **vừa cộng bao nhiêu**.

| Chỗ | Chuyển động |
|---|---|
| Mở bảng | mờ dần vào, trượt từ phải 26 px, nở từ 98% — 0,22 s |
| Đóng bảng | mờ dần ra và trượt về phải — 0,17 s |
| Bấm nút | lún xuống 94% khi nhấn, bật lại có vượt đà khi thả |
| Toast, thẻ tên | nở từ 92% khi hiện, mờ dần khi tắt |
| Số xu | chạy tới giá trị mới trong 0,45 s |
| Thanh mùa vụ, thanh đo | trượt tới mức mới, chậm dần khi gần đích |

Ba luật của lớp đó: **không bao giờ chặn** (mọi thứ bấm được ngay từ khung hình đầu của chuyển
động), **dùng `unscaledDeltaTime`** (công cụ tua thời gian đổi `Time.timeScale`, và giao diện không
được chậm đi theo nó), và **kết thúc đúng điểm đặt** (ghi thẳng giá trị cuối chứ không để lại phần
lẻ của phép nội suy — một thanh tiến độ dừng ở 0,997 là một lỗi nhìn thấy được).

Phản hồi khi bấm gắn qua `EventTrigger` chứ không qua `onClick`: `onClick` chỉ bắn khi thả chuột
**trong** nút, còn phản hồi phải có ngay lúc nhấn xuống.

Bộ QA **đợi chuyển động dừng hẳn** rồi mới đo bố cục, thay vì chạy ở một chế độ không chuyển động
riêng. Đo giữa lúc một cái bảng đang trượt vào thì nó nằm sai chỗ, và phép đo "bảng có chặn click
xuống vườn không" sẽ báo sai theo — bảng chưa tới nơi thì click đi lọt qua thật.

## Icon

Icon HUD là mesh vector trong `FarmHudIcon.cs`, không cần thư viện hay ảnh tải ngoài. Mười tám icon
vẽ trên lưới 100 × 100 bằng năm hàm dựng hình: `Box`, `RoundBox`, `Ellipse`, `Line`, `Poly`.
`GameHud.Farm.cs` dựng các thẻ viền kem, nền ngà và bóng nhẹ. Camera mặc định 6.6 cho cảnh chiếm
nhiều diện tích hơn; giới hạn kéo/zoom cũ được giữ lại.

## Kiểm tra

Bản `HudReview` có product name riêng, nên kiểm tra tự động không đọc hay ghi save của bản chơi
chính. Kết quả kiểm tra và ảnh chụp được ghi sau khi build tại `Docs/Art` và `Docs/screenshots`.

190 mục kiểm ở cả 1366 × 768 và 1920 × 1080. Trong đó có một mục gửi thẳng `PointerEnter` và
`PointerExit` qua EventSystem vào từng nút cột trái rồi đọc lại thẻ tên: ảnh chụp không bắt được
trạng thái rê chuột nên nếu không kiểm ở đó thì không ai kiểm.

**Mục "Rê chuột nhận đủ sáu máy" cần cửa sổ game có focus** — `StationHoverController.IsBlocked`
trả về true khi `Application.isFocused` là false, và đó là hành vi đúng: chuột đứng yên trên một
cửa sổ không active thì không phải là một cú rê chuột. Chạy bộ QA trong lúc có cửa sổ khác giữ
foreground thì mục này báo trượt kèm đúng câu giải thích đó, và sáu mục kiểm chữ tràn của bảng
hover cũng bị bỏ theo (190 xuống 184 mục chạy). Đóng bớt cửa sổ rồi chạy lại.
