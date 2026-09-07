# Mô phỏng khởi nghiệp trà — đưa vào game

Bản `Assets/StreamingAssets/Mo-phong-khoi-nghiep-tra.html` là một bảng mô phỏng kinh tế, kỹ thuật
và pháp lý cho một cơ sở chè thủ công 200 triệu đồng. Trang này ghi lại **sáu mục của bản đó đã
thành cơ chế gì trong game**, và những chỗ nào cố ý làm khác.

Nguyên tắc chung: toàn bộ luật nằm ở `VuonNho.Core` dưới dạng **hàm thuần trên số nguyên** — cùng
đầu vào cho cùng đầu ra, không đồng hồ thật, không `Random`. Nhờ vậy bảng người chơi xem trong HUD
và số tiền thật bị trừ khi đến hạn là cùng một phép tính, và chạy bù offline cho đúng kết quả của
chạy online. Có một bài test giữ điều đó (`MotBuocDaiBangNhieuBuocNgan_CoTaiChinhVaPhapLy`).

## Quy đổi đơn vị

Một xu là **100.000 đồng** (`BalanceConfig.CoinsPerMillionVnd = 10`). Vốn tự có 200 triệu là 2.000
xu, hạn mức thế chấp 350 triệu là 3.500 xu. Đây là chỗ duy nhất quy đổi giữa đơn vị của game và đơn
vị của bản mô phỏng — đổi số đó là đổi hết mọi con số tài chính hiện ra màn hình.

## Mục 1 — Vốn mồi và vay ngân hàng

`Core/Finance.cs`, bảng HUD **Vốn và nợ**.

| Bản mô phỏng | Trong game |
|---|---|
| Hạn mức tín chấp 200tr, 6,5–8,5%/năm | `UnsecuredLoanCapCoins = 2000`, cần UBND xã xác nhận |
| Hạn mức thế chấp 350tr, 7,5–9,0%/năm | `SecuredLoanCapCoins = 3500`, cần xong giấy ATTP |
| Gốc mỗi tháng $L_0/n$ | `Finance.PrincipalDue` — kỳ cuối gánh phần lẻ |
| Lãi tháng trên dư nợ | `Finance.InterestDue`, làm tròn lên |
| Đồ thị dòng tiền 12 chu kỳ | `GameState.CashHistory`, vẽ bằng chính dòng chữ trong HUD |
| Game over: 3 chu kỳ âm liên tiếp | 3 kỳ liền không trả đủ → ngân hàng niêm phong nương chè |

Lãi suất là một **khoảng**: vay dài hơn thì ngân hàng đòi cao hơn (`Finance.RateBpsFor` nội thẳng
giữa hai đầu). Nhờ vậy chọn thời hạn là một đánh đổi thật — dài thì trả nhẹ mỗi kỳ nhưng tổng lãi
nhiều hơn — chứ không phải một ô nhập liệu chỉ có một đáp án đúng.

Một kỳ trả nợ là một "tháng" trong game: `SeasonLengthMs / MonthsPerSeason` = 80 giây. Mùa là một
quý, nên ba tháng một mùa.

**Chỗ làm khác bản mô phỏng.** Bản mô phỏng nói ngân hàng "siết nợ và niêm phong nương chè" — một
game over. Ở đây niêm phong dừng **xưởng, quầy trà và robot**, tức toàn bộ phần làm ra tiền nhanh,
nhưng không dừng việc làm tay trong vườn. Trả hết nợ quá hạn là mở lại. Xoá sạch tiến độ của một
người chơi cozy sau ba kỳ thiếu tiền là mất nhiều hơn được, mà cái răng của luật — phải giữ dòng
tiền dương — vẫn còn nguyên khi cả dây chuyền đứng im.

Vườn phải chạy tiếp, không được dừng theo. Bản đầu dừng cả vườn, và nó tạo ra một ngõ cụt thật:
một người hết sạch xu, kho rỗng và đang bị niêm phong thì không còn đường nào kiếm ra một đồng để
trả nợ — ván chơi kẹt ở đó vĩnh viễn. Một cái bẫy không lối ra thì không dạy được người chơi điều
gì cả. Đường về là đường chậm nhất của game: hái bằng tay, bán lá tươi, mà lá tươi rẻ hơn trà đã
đóng gói hàng chục lần — nên món nợ vẫn là một cái giá thật phải trả bằng thời gian. Có một bài
test chạy trọn con đường đó (`BiNiemPhongVoiKhoRongVanConDuongTraNo`).

Tiền vay **không** tính là doanh thu (`Finance.Borrow` cộng thẳng vào `Coins`, không qua
`EarnCoins`). Một khoản vay không làm vườn giàu hơn, nó đổi một cục tiền bây giờ lấy một chuỗi
nghĩa vụ về sau; cộng vào doanh thu thì đồ thị dòng tiền sẽ báo một tháng cực lãi, và đó đúng là
cái bẫy mà mục 1 muốn người chơi nhìn thấy.

## Mục 2 — Nông học và mùa vụ

`Core/Agronomy.cs`, bảng HUD **Nông học và mùa vụ**.

Chè vào game thành **bốn loại cây riêng trên cùng một gốc**, mỗi loại là một tiêu chuẩn thu hái,
mở dần bằng bốn bậc tay nghề:

| Phân hạng | Cây | Búp một ô | Giá gói 8 búp |
|---|---|---|---|
| 1 tôm 3 lá / búp xô | `crop_tea_xo` | 64 | 120 xu |
| 1 tôm 2 lá | `crop_tea_moccau` | 28 | 300 xu |
| 1 tôm 1 lá | `crop_tea_nontom` | 11 | 800 xu |
| 1 tôm (đinh trà) | `crop_tea_dinh` | 4 | 2.200 xu |

Nhân lại thì **doanh thu một vụ của bốn phân hạng gần bằng nhau** — đó là kết quả thật của bảng
trong bản mô phỏng, không phải một sự làm cho dễ. Cái khác nhau là **số lượng**: búp xô cho gấp mười
lăm lần số hàng phải chạy qua dây chuyền để kiếm cùng số tiền. Nên khi dây chuyền là giới hạn thì
hái non hơn là thắng, còn khi dây chuyền còn rộng thì hái xô lại nhanh hơn. Có một bài test giữ
khoảng lệch đó dưới 30% (`BonPhanHangThuHaiChoDoanhThuGanBangNhau`).

Bảng sinh hoá bốn mùa thành hai hệ số nhân: `YieldPercent` vào số búp lúc gieo, `PricePercent` vào
tiền của mẻ trà ở quầy. Hè nhiều búp giá thấp (130% / 80%), xuân ít búp giá cao (90% / 130%), đông
gần như không có gì (25%). Trà thảo mộc — bạc hà, cúc, dâu, sả, nhài — **không** chịu hai hệ số
này: chúng không có bảng sinh hoá nào trong bản mô phỏng.

Cơ chế rầy xanh: ô chè bị *Jacobiasca formosana* chích hút nhẹ vào cuối xuân đầu hè (22% số vụ) thu
về **lá rầy xanh** thay vì búp chè thường, và vụ đó **không** bị sâu bệnh — hai cái đó là cùng một
con vật, chỉ khác mức độ, nên một vụ vừa được món quà vừa mất trắng là một kết quả không ai đọc ra
được. Lá rầy xanh chỉ giữ được giá nếu chế biến đúng đường (xem mục 3).

## Mục 3 — Động học chế biến

`Core/Crafting.cs`, bảng HUD **Kỹ thuật căn lửa**.

Ba đường chế biến, mỗi đường có khoảng chuẩn riêng cho bốn thông số:

| Đường | Nhiệt T₁ | Vò | Độ ẩm | Oxy hoá |
|---|---|---|---|---|
| Trà xanh | 250–260°C | 15–20 phút | ≤ 45‰ | 0–5% |
| Hồng trà | 100–130°C | 25–40 phút | ≤ 45‰ | 85–95% |
| Đông Phương Mỹ Nhân | 110–140°C | 18–28 phút | ≤ 45‰ | 60–75% |

Khoảng chuẩn **hẹp** còn khoảng chấp nhận được **rộng**: một người chơi không đọc sổ tay vẫn làm ra
trà bán được (100%), chỉ là không bao giờ đạt giá cao nhất (130%). Ra ngoài cả khoảng rộng là mẻ
lỗi (55%) kèm một câu nói rõ lỗi gì — "nhiệt thấp, enzyme PPO chưa chết hẳn: nước đỏ bã, vị chát
gắt".

Đông Phương Mỹ Nhân cần **cả hai**: lá của ô bị rầy xanh, **và** mức oxy hoá 60–75%. Sao lá đó theo
đường trà xanh, hay ủ quá 75%, thì linalool và geraniol không chuyển hoá và mẻ ấy chỉ còn 20% giá
(`OrientalBeautyFallbackPercent`). Lá rầy xanh là thứ đắt nhất trong vườn và cũng là thứ dễ mất giá
nhất.

## Mục 4 — Pháp lý, ATTP và xưởng một chiều

`Core/Compliance.cs`, bảng HUD **Pháp lý và ATTP**.

Hai hình thức kinh doanh, và đổi hình thức **không phải một bước nâng cấp một chiều**:

| | HKD | TNHH một thành viên |
|---|---|---|
| Đăng ký | UBND cấp huyện, 3–5 ngày | Sở KH&ĐT, 5–10 ngày |
| Thuế | khoán 4,5% **doanh thu** | 20% **lợi nhuận ròng** |
| Kế toán | sổ đơn giản, miễn phí | 20 xu mỗi kỳ |
| Hoá đơn GTGT | không | có |

Biên lợi nhuận mỏng thì thuế khoán trên doanh thu ăn nặng hơn; biên dày thì 20% lợi nhuận đắt hơn.
Có một bài test giữ cả hai chiều đó (`HoKinhDoanhDongThueTrenDoanhThuConCongTyDongTrenLoiNhuan`).

**GTGT 8% không tính vào tiền phải đóng.** Bản mô phỏng ghi "Thuế TNDN 20% + Thuế GTGT 8-10%", và
đem cộng thẳng cả hai vào chi phí là sai — GTGT thu của người mua và khấu trừ đầu vào, mà đúng cái
khả năng "xuất hoá đơn GTGT khấu trừ" mới là lợi thế của TNHH trong chính bản mô phỏng đó. Cộng vào
thì TNHH không bao giờ rẻ hơn HKD (8% GTGT một mình đã gần gấp đôi cả 4,5% thuế khoán), và cả cái
đánh đổi biến mất. Con số 8% vẫn hiện ra bảng so sánh, chỉ không tính vào tiền
(`EntityOption.VatPassedThrough`).

Bốn lỗi bị xử phạt theo Nghị định 115/2018, kiểm tra hai kỳ một lần:

| Lỗi | Phạt | Trong game là gì |
|---|---|---|
| Chưa có giấy ATTP | 20–30tr + đình chỉ | `FoodSafetyCertified == false` |
| Không theo nguyên tắc một chiều | 5–7tr | Có **lỗ hổng giữa** dây chuyền |
| Côn trùng xâm nhập | 1–3tr | Có ô đang bị sâu bệnh |
| Thiếu đồ bảo hộ | 500k–1tr | Có thợ mà chưa mua `upgrade_protective_gear` |

Nguyên tắc một chiều thành một luật đọc được từ trạng thái: vi phạm **không** phải "mua thiếu máy" —
thiếu thì chỉ là chưa xây xong. Vi phạm là **có lỗ hổng ở giữa**: một cái máy đứng sau một chặng
chưa có máy thì nguyên liệu phải vòng ngược lại qua khu đã xử lý.

**Chỗ làm khác bản mô phỏng.** Lần kiểm tra đầu tiên chỉ **nhắc** về chuyện chưa có giấy ATTP, kỳ
sau mới phạt. Một người vừa mua cái máy đầu tiên mà bị phạt 200 xu kèm đình chỉ thì không học được
gì — họ chỉ thấy xưởng mình đóng cửa mà không biết mình đã làm gì. Nhắc trước rồi phạt sau vẫn giữ
đủ răng của cái luật, và khác hẳn ở chỗ người chơi có một kỳ để đi xin giấy — mà thẩm định mất
15–45 ngày in-game nên họ cần đúng khoảng thời gian đó.

Đình chỉ đóng **xưởng**, vườn vẫn lớn bình thường. Niêm phong của ngân hàng đóng **cả hai**. Hai
việc khác nhau nên hai câu trả lời khác nhau (`FarmSimulation.FactoryAllowed` và `Loan.Sealed`).

## Mục 5 — Ba phân nhánh kinh doanh

`Core/Branches.cs`, bảng HUD **Mô hình kinh doanh**.

| Nhánh | Biên | CAPEX | Vòng quay | Trong game |
|---|---|---|---|---|
| Gia công thô B2B | 10–15% | 75–95tr | 3–7 ngày | giá 65%, **bán được trà mộc** (bỏ qua máy đóng gói) |
| Bán lẻ thủ công DTC | 50–70% | 90–130tr | 30–60 ngày | giá 165%, **tiền về sau 45 ngày** |
| Du lịch trải nghiệm | 70–85% | 180–250tr | ngay | thu mỗi kỳ theo **số món cảnh quan** |

Ba nhánh **không loại trừ nhau** — đúng như bản mô phỏng nói. Cái phải chọn là kênh bán chính cho
mỗi mẻ trà, và đổi được bất cứ lúc nào; du lịch không phải kênh bán trà mà là một nguồn thu riêng.

Vòng quay vốn dài của DTC là một cái giá **thật**: tiền thành `GameState.Receivables` và chỉ về túi
ở kỳ chốt sau 45 ngày in-game, nên kỳ này vẫn có thể không đủ tiền trả nợ dù hàng đã bán xong. Cộng
ngay vào túi thì biên 165% trở thành một món quà không kèm điều kiện nào.

Lợi thế của B2B là **ít máy nhất**: bán trà mộc thì không cần mua máy đóng gói (2.600 xu) và không
cần một thợ chạy nó. Đó cũng là lý do ai đã mua máy đóng gói thì nên đổi sang kênh khác.

Lộ trình ba giai đoạn suy ra từ nhánh đã mở, không lưu trong save: sinh tồn → nâng biên → hệ sinh
thái. Du lịch cần 6 món cảnh quan trong vườn, nên "tái đầu tư cảnh quan nương chè" của giai đoạn 3
là một việc làm tăng tiền thật, không phải một câu khẩu hiệu.

## Mục 6 — Đoạn mở màn

`Core/IntroStoryboard.cs` (nội dung) và `Views/IntroCinematic.cs` (hiển thị).

Bốn phân cảnh, đúng thứ tự, đúng câu phụ đề và đúng độ dài của kịch bản — tổng 1 phút 5 giây. Chạy
một lần cho mỗi ván (`GameState.IntroSeen`), bỏ qua được bằng chuột phải, Space hay Escape.

Hình vẽ 2D hand-drawn và âm thanh ASMR chưa có, nên bản chạy được hiện tại là một **bản đọc kịch
bản**: chữ trắng trên nền tối, hiện cả dòng "hình ảnh" và "âm thanh" để người làm art đọc được mình
phải vẽ gì vào đây. Nhịp nằm ở `IntroScene.DurationMs` trong Core, nên khi thay khối chữ bằng khối
ảnh thì nhịp không đổi.

Chụp chính đoạn mở màn cần `-vuonnho-show-intro`; không có nó thì mọi ảnh QA sẽ là một khối chữ trên
nền đen.

## Những gì chưa làm

- **pH đất 4,5–5,5** trong bảng phân bổ vốn: game chỉ có một chỉ số độ phì. Thêm pH thành một con
  số thứ hai cho mỗi ô mà cả hai đều bảo "bón phân đi" là hai con số cho một quyết định.
- **Mòn máy theo mẻ** (bom sao chè "mòn theo mẻ"): chưa có: cần một trạng thái mới cho mỗi máy và
  một đường sửa chữa, mà chưa rõ nó thêm được quyết định gì ngoài một khoản chi định kỳ.
- **MOQ bao bì 1.000–3.000 đơn vị**: chiếm dụng vốn lưu động ban đầu — chưa có, vì kênh DTC đã
  mang đúng cái sức ép đó dưới dạng tiền về chậm.
- **Xuất khẩu**: bản mô phỏng nhắc TNHH "đủ chuẩn xuất khẩu" nhưng không có số nào cho nó.

## Chạy lại phần kiểm tra

```bash
"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\PC\Desktop\Game\VuonNho" -runTests -testPlatform EditMode -testResults results.xml -logFile test.log
```

204 test EditMode, trong đó 55 bài của `StartupTests` phủ sáu mục trên. Phần giao diện chạy trong
chính bản build:

```bash
Build\HudReview\VuonNho-HudReview.exe -screen-fullscreen 0 -screen-width 1366 -screen-height 768 -vuonnho-qa "Docs\QA-startup.md"
```

196 mục kiểm, đạt hết ở cả 1366 × 768 và 1920 × 1080 (chạy ba lần liên tiếp để chắc là không có mục nào phụ thuộc hạt sâu bệnh của lần chạy). Ảnh của năm bảng ở
`Docs/screenshots/17-*.png`, đoạn mở màn ở `18-intro.png`.
