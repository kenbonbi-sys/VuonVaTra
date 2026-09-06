# Vườn và Trà

Tên trong [kế hoạch MVP](../Ke-hoach-MVP-Vuon-Nho.md) là *Vườn Nhỏ: Quán Trà*; hướng v1 đổi thành
**Vườn và Trà** và đó là tên đang nằm trong `PlayerSettings`, nên cũng là tên thư mục save.
Lý do đổi: [Docs/Huong-di-v1.md](Docs/Huong-di-v1.md).

Vòng chơi **chọn cây → gieo → cây lớn → thu → chế biến → quầy trà → mua nâng cấp** chạy được từ
đầu đến cuối, có save/offline/lifecycle và bộ test logic. Art đã thay xong bằng model Blender tự sinh
(mốc L01/B02); phần còn thiếu nằm ở mục "Còn lại" cuối trang.

- Unity **6000.6.0f1**, URP 17.6.0, uGUI 2.6.0, Test Framework 1.8.0 (đều là bản đi kèm editor,
  không cần tải thêm).
- Windows x64, chơi bằng chuột, 1366 × 768 trở lên.

## Điều khiển

| Thao tác | Kết quả |
|---|---|
| Chuột trái vào ô đất, vào máy, vào nút | Thao tác như cũ |
| Giữ chuột trái rồi kéo | Kéo màn hình đi xem chỗ khác |
| Lăn chuột | Phóng to / thu nhỏ, bám vào điểm dưới con trỏ |
| Chuột phải xuống đất | Nhân vật đi tới đó, có vòng tròn báo lại chỗ vừa bấm |
| Chuột phải qua chỗ có đồ đã đặt | Nhân vật đi vòng qua — đồ trang trí là vật cứng |
| Rê chuột lên một cái máy | Máy sáng lên, hiện bảng nguyên liệu, tiến độ và tình trạng thợ |
| Chọn một món trang trí | Bóng ma của món đó bám theo con trỏ |
| `R` khi đang cầm món | Xoay 180° |
| Giữ chuột phải kéo ngang khi đang cầm món | Xoay tự do |
| Chuột phải khi đang đặt / gỡ đồ trang trí | Thoát chế độ đó (không đi) |

Hai điều đáng biết vì chúng ràng buộc nhau:

- **Thao tác chuột trái chốt lúc thả, không phải lúc nhấn.** Lúc nhấn thì chưa biết người chơi
  định bấm hay định kéo màn hình. `CameraRig` là nơi quyết định (`ClickWasDrag`, ngưỡng 6 px) và
  `GameBootstrap.HandleClick` chỉ làm việc khi lần nhấn đó không phải một cú kéo.
- **Chuột phải vẫn là đường thoát khỏi chế độ đặt/gỡ đồ.** Đó là lối ra duy nhất người chơi đã
  quen; cho nhân vật đi lúc đó sẽ cướp mất nó. Chỉ khi không ở trong chế độ nào thì chuột phải
  mới là lệnh đi.

**Vật cứng** — hàng rào, thân cây, quầy trà và đồ trang trí đã đặt: nhân vật đi vòng qua chứ
không xuyên qua. Đánh dấu bằng thành phần `WalkBlocker` đặt cạnh collider, **không phải "mọi
collider đều chặn"**: ô đất, nền vườn và quầy trà đều có collider gameplay để nhận click, mà đi
lên ô đất thì phải được.

Hai ngoại lệ có lý do:

- **Phiến đá** không chặn. Món đó sinh ra để người ta bước lên; chặn nó lại là tự mâu thuẫn với
  chính mô tả của nó. Cờ nằm ở `DecorationDefinition.BlocksWalking` trong Core, không phải ở phía
  Unity, vì đó là chuyện của món đồ chứ không phải của cách dựng scene.
- **Cây chỉ chặn phần thân**, không chặn tán lá. Tán vươn ra hơn một mét mà đi dưới tán cây thì
  phải được — chặn cả tán sẽ thành một bức tường tròn vô hình giữa bãi cỏ.

Bụi và tảng đá **không chặn**: chúng thấp và nằm rải rác ở mép, chặn lại chỉ làm đường đi vướng
mà không được gì.

Cách tránh là **trượt dọc một trục**, không phải tìm đường: đâm vào thì bỏ một thành phần và đi
nốt thành phần kia. Đồ trang trí đều là hộp vuông góc với trục nên thế là đủ để đi vòng. Cái giá
phải trả: đứng giữa hai món kê sát nhau thành một góc lõm thì nhân vật dừng lại chứ không lùi ra
để vòng — bấm một cú nữa là đi tiếp được.

Vòng tròn báo lại (`ClickMarker`) **vẽ đè lên mọi thứ trong vườn**, không kiểm tra độ sâu. Nó
nằm sát mặt đất mà luống cây cao 17 cm và chiếm gần hết khu giữa — chỗ người chơi bấm nhiều nhất
— nên nếu để kiểm tra độ sâu thì bấm vào luống sẽ không được báo lại gì. Vòng tròn hiện ở **đích**
chứ không ở chỗ con trỏ: bấm ra ngoài vườn thì nhân vật dừng ở mép, và vòng tròn phải nằm đúng
chỗ nó sẽ dừng.

Chọn một món trang trí thì **bóng ma của chính món đó** bám theo con trỏ (`PlacementPreview`) —
dùng đúng prefab chứ không phải một hình thay thế, vì câu hỏi người chơi đang hỏi là "đặt xuống
đây thì nó trông thế nào". Chỗ không đặt được thì bóng ma **đỏ lên chứ không biến mất**: biến mất
là câu trả lời mơ hồ, người chơi không biết là mình đưa chuột ra ngoài vườn hay chỗ đó vướng.

### Robot thu hoạch

Robot **đi tới từng ô để thu**, không thu sạch mọi ô chín trong cùng một khoảnh khắc. Cây chín thì
**nằm chờ** cho tới lượt nó — đó là ý nghĩa của việc robot có mặt trong vườn. Nó luôn nhắm ô chín
gần chỗ nó đang đứng nhất, bằng nhau thì lấy ô có `plotId` nhỏ hơn, nên chạy lại cùng một lịch cho
ra cùng một kết quả.

Cả quãng đi lẫn lúc dừng lại thu đều là **deadline**, y như cây lớn và mẻ pha, nên bù offline không
cần luật riêng: nó chỉ là nhiều bước `AdvanceTo` liên tiếp. Quãng đường tính bằng **căn bậc hai số
nguyên** chứ không phải `Math.Sqrt`, để đường đi của robot không phụ thuộc vào cách từng máy làm
tròn số thực.

Cái giá: **thu nhập giảm khoảng 12%** ở bốn ô bạc hà — đo được 792/900 xu trong mười phút mô phỏng.
Đó là quãng cây chín nằm chờ, và nó là thứ người chơi nhìn thấy chứ không phải một khoản mất vô
hình. Công thức cân bằng cũ giờ là **trần**, không còn là mức đạt tới được; bài test giữ trần đó
làm mục kiểm thật — vượt trần nghĩa là robot đã quay về lối thu sạch tức thì.

Hình robot suy ra hoàn toàn từ state: chỗ xuất phát, ô đang nhắm và mốc tới nơi là đủ. Không lưu
thêm trường nào cho quãng đang đi, nên nạp lại giữa một chuyến, robot vẫn đứng đúng chỗ nó phải đứng.

Nhân vật đi thẳng, không tìm đường: vườn là một mảnh đất phẳng không có vật cản. Không có xương
và không có animation clip — hai chân là hai nhóm mesh với gốc xoay ở hông, nhịp chân tính theo
quãng đường đã đi nên chân luôn chạm đất đúng nhịp dù tốc độ có đổi.

## Canh tác

Một ô đất không chỉ có "trống" và "đang lớn". Nó còn bốn con số, và bốn con số đó là chỗ người
chơi thật sự ra quyết định:

| Hệ | Con số | Nó bóp cái gì |
|---|---|---|
| Đất | độ phì 0–100 | **thu được bao nhiêu** |
| Cỏ dại | cỏ 0–100 | **lớn nhanh hay chậm** |
| Sâu bệnh | có / không | **mất trắng vụ hay không** |
| Thời vụ | xuân · hạ · thu · đông | **thu được bao nhiêu** (trái vụ còn 50%) |

Bốn hệ **cố ý đổ vào bốn chỗ khác nhau** thay vì cùng bóp một chỗ. Nhìn một ô đang kém là đọc ra
ngay nó thiếu cái gì; ba hệ cùng làm giảm sản lượng thì không ai biết vì sao.

**Cả hai con số của một vụ đều được chốt ngay lúc gieo**, không tính lại lúc thu. Bón phân giữa vụ
không cứu được vụ đang chạy, nên phải lo đất **trước** khi gieo — và nút bón phân từ chối thẳng khi
đất còn tốt, để người chơi biết điều đó mà không phải mất tiền học. Popup ô vì vậy hiện độ phì, cỏ
và mùa **ở trên** danh sách cây, còn mỗi nút cây nói sẵn vụ này sẽ mất bao lâu và cho mấy đơn vị.

- **Độ phì** tụt 4 điểm mỗi lần gieo và tự hồi 1 điểm mỗi 2 giây, nhưng **chỉ tự hồi tới 45**. Muốn
  cao hơn phải bón — đó là chỗ tiền đi ra. Tốc độ hồi phải nhanh hơn nhịp gieo, nếu không độ phì
  dính đáy và cái trần kia không còn ý nghĩa gì: nó thành cái bẫy chứ không phải một lựa chọn.
- **Cỏ dại** mọc 1 điểm mỗi 40 giây, ô đầy cỏ làm cây lớn chậm hơn 60%. Làm cỏ **không tốn xu** —
  cái giá của nó là một vòng người chơi đi qua từng ô.
- **Sâu bệnh** đánh 20% số vụ, hiện ra ở quãng 25–75% của vụ và báo bằng toast kèm số ô. Không trị
  thì vụ đó về không. Nâng cấp "Phòng trừ sinh học" hạ tỉ lệ xuống 45% mức cũ.
- **Thời vụ** đổi mỗi 4 phút mô phỏng. Bạc hà trồng được quanh năm (cây mở đầu, không nên dạy luật
  mới ngay phút thứ nhất); cúc xuân–thu, dâu xuân–hạ, sả hạ–thu, nhài chỉ mùa hạ.

Cả bốn đều là **hàm thuần tuý trên số nguyên** trong `Cultivation`, bám vào `SimulationTimeMs` chứ
không phải đồng hồ thật, và sâu bệnh bốc từ một hash viết tay chứ không phải `Random`. Nhờ vậy bù
offline không cần một dòng luật riêng nào: chạy bù chỉ là `AdvanceTo` một bước dài, và cây **chết
được trong lúc người chơi vắng mặt** đúng như khi họ đang ngồi xem. Phần cỏ và độ phì cộng dồn theo
**bước nguyên** nên vắng một đêm cũng tốn đúng bằng vắng một giây.

Đo được: **vườn bỏ bê còn 42% thu nhập của vườn chăm kỹ** (324 so với 780 xu trong mười phút mô
phỏng). Có một bài test giữ tỉ lệ đó trong khoảng 25–60% — thấp hơn thì bỏ một buổi là về tay
trắng, cao hơn thì chăm hay không cũng thế và cả bốn hệ chỉ là trang trí.

Trong vườn: **đất bạc màu nhạt đi**, ô nhiều cỏ mọc bụi cỏ vàng ở bốn góc, ô có sâu đeo một dấu đỏ
cạnh dấu chín. Ba thứ này đọc được từ xa nên không phải mở popup từng ô mới biết vườn đang thế nào.

## Dây chuyền chế biến trà

Giữa **thu hoạch** và **quầy trà** có sáu công đoạn, đúng thứ tự nghề làm trà thật:

| # | Công đoạn | Máy | Vào → ra |
|---|---|---|---|
| 1 | Thu hoạch | ô đất + robot | — → lá tươi |
| 2 | Làm héo | Máng làm héo | 8 lá tươi → 8 lá héo |
| 3 | Diệt men | Máy sao diệt men | 8 lá héo → 8 lá đã diệt men |
| 4 | Vò và tạo hình | Máy vò trà | 8 lá đã diệt men → 8 lá đã vò |
| 5 | Lên men | Phòng lên men | 8 lá đã vò → 8 lá đã lên men |
| 6 | Sấy khô | Máy sấy băng tải | 8 lá đã lên men → 8 trà khô |
| 7 | Phân loại và đóng gói | Máy sàng và đóng gói | 8 trà khô → 8 trà đóng gói |
| 8 | Phân phối | quầy trà | trà đóng gói → xu |

**Dây chuyền là đường nâng thu nhập, không phải cái cổng chặn đường.** Quầy trà ưu tiên trà đã
đóng gói và trả **gấp năm**; không có thì nó quay về pha từ lá tươi như cũ. Nếu bắt phải có đủ sáu
cái máy mới kiếm được đồng xu đầu tiên từ trà thì đoạn mở đầu sẽ dài và chậm mà không ai xin.

### Thợ

Thợ **không gắn vào một cái máy nào**: họ là trần cho số máy chạy cùng lúc, và máy nào chạy trước
thì theo thứ tự dây chuyền — chặng đầu trước, vì dây chuyền dừng ở đầu thì cả dây phải đợi.

- Thuê một lần bằng xu, rồi **ăn lương mỗi kỳ 60 giây** thời gian mô phỏng.
- Không đủ xu trả lương thì **không ai bị mất**, chỉ là kỳ đó ít người chạy máy hơn. Trả đủ là họ
  làm lại ngay. Cho xu âm hay sa thải tự động đều là cách làm người chơi mất thứ mà họ không bấm
  vào đâu cả.
- Ít thợ hơn số máy đang có thì máy cuối dây nằm không. Đọc được bằng mắt: **thợ đứng ngay cạnh
  máy đang chạy**, máy nào không có ai đứng là máy đang nằm không.

**Thợ đứng máy là thường trực, không hiện theo từng mẻ.** Thuê xong là họ ở lại sân: hết việc thì
đứng nghỉ và hạ tay xuống, có việc thì hai tay đưa lên làm. Ít thợ hơn máy thì họ chuyển sang máy
đang cần người chứ không biến mất rồi hiện lại — thợ nhấp nháy theo từng mẻ đọc ra như lỗi hiển
thị, không đọc ra như "máy này đang nằm không".

Máy nào chế biến cây nào là tự chọn: cây đang dồn nhiều nhất ở đầu vào. Bằng nhau thì lấy cây
đứng trước trong catalog, nên chạy lại cùng một lịch cho cùng một kết quả. Sáu cái máy mà mỗi cái
một ô chọn cây là sáu lần bấm mỗi khi đổi cây, cho một quyết định gần như luôn là "cái nào đang
nhiều nhất".

Số liệu nằm ở `DefaultStages` và `BalanceConfig` trong Core, không ở phía Unity.

## Mở và chạy

```bash
"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -projectPath "C:\Users\PC\Desktop\Game\VuonNho"
```

Mở scene `Assets/Scenes/Garden.unity` rồi bấm Play. Menu **Vườn Nhỏ** trong editor có:

| Mục | Việc |
|---|---|
| 0. Tạo hoặc mở GardenSkin | Mở asset chứa toàn bộ prefab hình ảnh và kích thước |
| 1. Thiết lập project | Gán URP, tên sản phẩm, color space, độ phân giải mặc định |
| 2. Dựng lại scene Garden | Dựng lại toàn bộ scene primitive từ code |
| 3. Build Windows (bản playtest) | Không có công cụ tua thời gian |
| 4. Build Windows (bản dev) | Có tua 1 phút / 60 phút trong Cài đặt |
| 5. Chụp ảnh scene | Ảnh editor — xem cảnh báo bên dưới |

Chạy từ dòng lệnh:

```bash
"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\PC\Desktop\Game\VuonNho" -executeMethod VuonNho.EditorTools.BuildTool.BuildWindowsPlaytest -logFile build.log
```

Bản build nằm ở `Build/VuonNho-playtest/` và `Build/VuonNho-dev/`.

## Chạy test

```bash
"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\PC\Desktop\Game\VuonNho" -runTests -testPlatform EditMode -testResults results.xml -logFile test.log
```

83 test EditMode, phủ bảng "Kiểm thử logic bắt buộc" ở mục 10 của kế hoạch: tính nhất quán thời
gian, biên timer, trước/sau robot, cap offline 4 giờ, đồng thời, đổi cây/công thức, nâng tốc độ
giữa chu kỳ, kho, giao dịch, resume lặp, save/backup/schema, chỉnh đồng hồ, balance khác và múi giờ.

Phần "kiểm thử build và giao diện" của mục 10 chạy bằng bộ QA nằm trong chính bản build:

```bash
Build\VuonNho-dev\VuonNho.exe -screen-fullscreen 0 -screen-width 1366 -screen-height 768 -vuonnho-qa "Docs\QA-1366x768.md"
```

Nó chơi từ vườn mới tới mở 12 ô, nạp lại save giữa chu kỳ, quét chữ tràn và click xuyên UI trên mọi
panel, đo frame time và thời gian bù 4 giờ, rồi ghi báo cáo Markdown và thoát với mã 0/1.
Kết quả gần nhất và những gì nó **không** chứng minh được: [Docs/QA-moc-A.md](Docs/QA-moc-A.md).

## Kiến trúc

Tách mô phỏng khỏi hiển thị đúng như mục 8 của kế hoạch. `Assets/Scripts/`:

| Assembly | Nội dung | Phụ thuộc |
|---|---|---|
| `VuonNho.Core` | `GameState`, `FarmSimulation`, `GameSession`, `ContentCatalog`, `SaveSerializer`, `Processing`, `TutorialGuide`, JSON | **C# thuần, không tham chiếu UnityEngine** |
| `VuonNho.Infrastructure` | `FileSaveRepository`, `SystemClock`, `FileTestLogger` | Core + UnityEngine |
| `VuonNho.Views` | `GameBootstrap`, `PlotView`, `MachineView`, `HelperView`, `CharacterView`, `CameraRig`, `ClickMarker`, `PlacementPreview`, `WalkBlocker`, `StationView`, `GameHud`, `GardenSkin`, `SfxPlayer`, `QaScreenshot` | Core + Infrastructure |
| `VuonNho.Editor` | `SceneFactory`, `ProjectSetup`, `BuildTool`, `SceneCapture` | Editor-only |

`GameState` là nguồn sự thật duy nhất cho tiền, kho, timer và mở khóa. View dựng lại được mà
tiến trình không đổi. Không có coroutine hay animation nào làm nguồn timer kinh tế.

`FarmSimulation.AdvanceTo` là **một thuật toán dùng cho cả online và offline**: nhảy theo deadline
chính xác, thứ tự cố định tại mỗi timestamp (cây/mẻ đến hạn → robot thu và gieo lại → máy bắt đầu
mẻ mới). Chia nhiều tick nhỏ hay gọi một lần cho cùng kết quả — có test khẳng định điều này.

### Chống cộng lại tiền khi quay lại

`GameSession.ApplyOfflineProgress` mô phỏng trên **bản sao** state, ghi state mới + checkpoint mới
+ báo cáo trong cùng một snapshot, và **chỉ đưa ra UI sau khi lưu thành công**. Checkpoint được đặt
về hiện tại nên phần dư trên cap 4 giờ không được nhận ở lần mở tiếp theo. Callback focus/pause
lặp lại chỉ tạo một lần Suspended → Resuming.

Lệnh mua trừ tiền và thay tiến độ trong cùng một transaction; lỗi ghi cho thử lại mà không trừ
tiền thêm lần nào.

### Save

`%USERPROFILE%\AppData\LocalLow\Vuon va Tra\Vuon va Tra\`

Thư mục lấy theo `companyName`/`productName` trong `ProjectSettings`, đặt bởi menu
"1. Thiết lập project". Đổi hai tên đó là **đổi chỗ save của người đang chơi** — save cũ nằm
lại thư mục cũ và game sẽ mở ra một vườn mới.

- `vuon-nho-save.json` — file chính, `vuon-nho-save.backup.json` — bản tốt trước đó.
- Ghi file tạm → đọc lại kiểm tra → `File.Replace` để thay file chính và giữ backup.
- File chính hỏng thì tự dùng backup. **Cả hai hỏng thì game báo lỗi và dừng, không âm thầm reset.**
- Save có schema mới hơn ứng dụng cũng bị chặn có chủ đích.
- `vuon-nho-playtest.log` — log sự kiện cục bộ, không gửi đi đâu; nút "Xuất dữ liệu test" trong
  Cài đặt sao chép ra file riêng.

## Cân bằng

Toàn bộ số của balance v0 (mục 5 kế hoạch) nằm trong `Assets/Scripts/Core/DefaultContent.cs` và
`BalanceConfig`. Đổi số ở đó rồi chạy lại test là đủ; validator từ chối id trùng/không tồn tại,
thời gian không dương, giá âm, yield dưới 4.

**Yield tối thiểu là 4, không phải 1.** Độ phì nhân vào sản lượng, nên một cây chỉ cho 1 đơn vị thì
cả thang độ phì 0–100 chỉ còn hai bậc: 0 hoặc 1. Yield của mọi cây đã nhân bốn và số nguyên liệu
một mẻ cũng nhân bốn, nên kinh tế ở độ phì đầy **không đổi một xu nào** — chỉ có độ phân giải là
đủ để bón phân thấy được kết quả.

Bài test cân bằng chạy **hai kịch bản** chứ không một: vườn chăm kỹ giữ trần (0.85–1.02 lần công
thức), vườn bỏ bê giữ sàn (25–60% của vườn chăm kỹ). Một kịch bản không phân biệt được "cân bằng
đúng" với "cả bốn hệ canh tác không nối vào đâu cả".

## Thay asset — GardenSkin

Scene được sinh ra bằng code nên **đừng sửa art trực tiếp trong scene**: chạy lại menu "2. Dựng lại
scene Garden" là mất hết. Thay vào đó thả prefab vào `Assets/Settings/GardenSkin.asset`
(menu **Vườn Nhỏ → 0. Tạo hoặc mở GardenSkin**), rồi dựng lại scene.

Chỗ nào chưa có prefab thì tự rơi về primitive của mốc A, nên thay được từng phần một —
làm xong cây bạc hà là thả vào ngay, không phải chờ đủ bộ. Log lúc dựng scene báo còn thiếu bao
nhiêu chỗ.

| Ô trong GardenSkin | Nhận model gì | Ghi chú |
|---|---|---|
| `SoilPrefab` | Mặt đất một ô | Không gán `LockedOverlayPrefab` thì ô khóa được tô màu tối bằng renderer đầu tiên tìm thấy |
| `LockedOverlayPrefab` | Hình riêng cho ô chưa mở | Hiện đè lên ô khóa |
| `SeedlingPrefab` | Mầm chung cho cả 5 cây | |
| `Crops[].MaturePrefab` | Cây trưởng thành, mỗi loại một model | Tài liệu asset viết "1 mầm chung + 3 cây"; v1 thêm sả và nhài nên thành 5 |
| `ReadyBadgePrefab` | Dấu hiệu cây chín | Nổi ở độ cao 1,15 m trên ô |
| `StationPrefab` | Cụm quán trà kèm máy | Đặt con tên `StatusAnchor` và `SteamAnchor` để có đèn trạng thái và hơi nước |
| `RobotPrefab` | Robot nổi | Đặt con tên `Face` để mặt đổi màu khi robot thức |
| `GroundPrefab`, `TreePrefabs`, `BushPrefabs`, `RockPrefabs`, `FencePostPrefab` | Nền và props | Mảng props được chọn theo chỉ số cố định nên scene dựng lại luôn giống nhau |

Skin cũng giữ kích thước và ánh sáng: `PlotSpacing` (mặc định **1,6 m**), `PlotSize` (**1,4 m**),
`GroundSize`, `CameraOrthographicSize`, màu trời và ambient. Đổi ở đây rồi dựng lại scene là cả bố
cục theo — vị trí quán, robot, hàng rào đều tính từ `PlotSpacing`.

Yêu cầu với prefab: 1 unit = 1 m, pivot ở giữa chân, +Y lên, scale 1, không rig. Collider trong art
được tự động tắt lúc dựng scene để raycast chỉ trúng collider gameplay trên `PlotRoot`. Prefab được
đặt vào scene bằng `PrefabUtility.InstantiatePrefab` nên sửa prefab là scene đang mở cập nhật theo.

Model trưởng thành bị script scale từ 0,55 lên 1,0 theo tiến độ, nên **đừng scale sẵn ở prefab
root** — bọc mesh trong một child rồi scale ở child.

## HUD — quy ước dựng giao diện

Toàn bộ HUD được dựng bằng code trong `GameHud.BuildUi`, không có prefab UI nào trong scene.
Sửa `GameHud.cs`, `UiFactory.cs` hoặc `GardenPalette.cs` rồi build lại là xong — **không cần chạy
lại "2. Dựng lại scene Garden"**, vì scene chỉ giữ `HudCanvas` (Canvas + CanvasScaler 1366 × 768 +
GraphicRaycaster + `GameHud`).

Ba file, ba việc:

| File | Giữ gì |
|---|---|
| `GardenPalette` | **Mọi màu HUD.** Không tự chế màu trong `GameHud`, thêm hằng ở đây |
| `UiFactory` | Panel, nhãn, nút, thanh tiến độ, layout, bậc cỡ chữ và bán kính bo góc |
| `GameHud` | Bố cục và nội dung từng bề mặt |

Vài quy ước đã áp dụng, giữ nguyên khi thêm màn hình mới:

- **Góc bo sinh bằng code.** `UiFactory.Rounded(radius)` dựng texture trắng + alpha trong bộ nhớ
  rồi cắt 9-slice, cache theo bán kính. Không cần file ảnh, và vì ảnh chỉ có màu trắng nên màu thật
  vẫn do `Image.color` quyết định — không dính gì đến color space Linear. Bán kính chuẩn:
  `RadiusPanel` 14 cho panel/popup, `RadiusControl` 10 cho nút và thẻ dòng, `RadiusTrack` 5 cho
  thanh tiến độ, `RadiusDot` 6 cho đèn trạng thái.
- **Một rail duy nhất:** `EdgeMargin` 16 px cho lề ngoài, `Gutter` 12 px giữa hai bề mặt.
- **Bậc chữ:** `FontSizeMeta` 16 (chú thích), `FontSizeBody` 18 (chữ chính, cũng là sàn), 
  `FontSizeRowTitle` 20 (tên dòng), `FontSizeTitle` 24 (tiêu đề). `lineSpacing` 1,15 để dấu tiếng
  Việt chồng hai tầng không chạm dòng trên.
- **Nút có ba trạng thái** qua `UiFactory.SetButtonState`: `Normal`, `Selected` (đang chọn — đổi
  màu *và* ghi ra bằng chữ, đừng chỉ đổi màu), `Disabled` (mờ cả nền lẫn chữ, kèm lý do trong nhãn).
  `ButtonStyle.Primary` cho hành động, `ButtonStyle.Quiet` cho điều hướng và huỷ.
- **Đừng đặt cứng chiều cao dòng có chữ tiếng Việt.** Dùng layout group cho uGUI tự tính; đặt cứng
  thì chữ xuống dòng sẽ đè lên nút bên dưới. Chỉ đặt `LayoutElement.minHeight` làm sàn, không đặt
  `preferredHeight`.
- **Popup bắt buộc trả lời** (`BuildModal`) luôn có lớp scrim phủ cả màn hình, vừa để tách khỏi
  vườn vừa để chặn click xuống đất. Toast thì ngược lại: `raycastTarget = false` để không nuốt click.
- **Kho là lưới ô kiểu túi đồ,** không phải danh sách thẻ. Mỗi mặt hàng một ô cùng cỡ, xếp theo
  từng cây: lá tươi rồi sáu chặng chế biến của chính cây đó. Ô trống vẫn hiện (làm mờ) để vị trí
  của từng món không đổi giữa hai lần mở. Ô chi tiết neo ở đáy panel, **ngoài vùng cuộn**, và cả
  túi chỉ có một bộ nút bán — "Bán hết" không hoàn tác được nên càng ít chỗ bấm nhầm càng tốt,
  và nó nhẹ hơn "Bán 1" một bậc.
- **Nút muốn vừa bằng chữ thì phải gọi `UiFactory.HugContent`,** không đủ nếu chỉ tắt
  `childForceExpandWidth` của layout group cha. Nhãn bên trong nút được đặt `flexibleWidth = 1`
  để chữ căn được giữa khi nút bị kéo rộng; layout group của nút lại báo `flexibleWidth` của
  chính nó bằng tổng của các con, nên con số đó nổi lên thành `flexibleWidth` của cả cái nút — và
  Unity chia chỗ trống cho mọi thứ có `flexibleWidth` dương, không quan tâm `childForceExpandWidth`
  đã tắt hay chưa. `HugContent` ép về 0.

### Chữ "xu" là một glyph trong chính font chữ

Một `UI.Text` của uGUI chỉ vẽ được bằng **một** font. Icon tiền là glyph của Material Symbols, nên
mọi chỗ muốn đặt icon cạnh một con số đều phải tự dựng một hàng ngang `[ô glyph][ô chữ]`. Cách đó
làm được cho thẻ và nút, nhưng không làm được **giữa một câu** — "Mục tiêu: gom 240 xu để kích
hoạt robot" thì icon không có ô nào để nhét vào.

Nên thay vì lách quanh giới hạn của font, bỏ giới hạn đi: `SourceArt/Fonts/merge_coin_glyph.py`
chép đúng một glyph (`local_atm`, U+E53E) từ Material Symbols sang cả sáu file National Park, giữ
nguyên mã. Sau bước đó, chữ "xu" ở bất kỳ đâu chỉ là ký tự `DefaultContent.CoinGlyph` trong chuỗi
— kể cả giữa văn xuôi, trong toast, trong lý do từ chối của một lệnh ở Core.

Hằng số nằm ở Core vì cả Core lẫn Views đều dùng nó, và `UiFactory.Symbols.LocalAtm` trỏ về đúng
hằng số đó thay vì viết lại mã ký tự lần thứ hai.

Chạy lại script khi đổi font chữ hoặc đổi icon tiền. Nó bỏ qua file nào đã có glyph, nên chạy hai
lần không hỏng gì.

Ảnh chụp QA: `-vuonnho-open-panel` nhận `inventory`, `upgrade`, `settings`, `decorate` và `plot`
(mở popup ô 1).
Ảnh tham chiếu của HUD hiện tại nằm ở `Docs/screenshots/`, chụp từ bản build chứ không phải editor.

## Khác với kế hoạch — đã cân nhắc

| Kế hoạch | Bản này | Lý do |
|---|---|---|
| Unity Input System | `Input` cũ + `StandaloneInputModule` | Input System không đi kèm editor, phải tải về; input đi qua đúng một chỗ (`GameBootstrap.HandleClick`) nên đổi sau là một file |
| TextMeshPro | `UnityEngine.UI.Text` + font hệ thống | TMP cần import Essentials thủ công; dấu tiếng Việt đã kiểm bằng chuỗi đủ dấu và hiển thị đúng trong build |
| ScriptableObject cho nội dung tĩnh | Class C# thuần trong Core | Giữ Core không phụ thuộc UnityEngine để test chạy độc lập; chuyển sang SO chỉ cần một lớp adapter |
| Nội dung mốc A gọn hơn | Đã có sẵn 5 cây, 12 ô, 11 nâng cấp, bán thô | Là dữ liệu, không tốn công code thêm. Số cây và nâng cấp là của thang v1, không phải v0 |

Âm thanh: 5 cue được **tổng hợp bằng sóng ngay trong game** (`SfxPlayer`), không phụ thuộc file.
Đây là bản tạm để có phản hồi; mốc B thay bằng bản tự ghi nếu cần.

## Cảnh báo: ảnh chụp từ editor batchmode không đáng tin

`SceneCapture` chạy trong `-batchmode` cho ra ảnh **một màu duy nhất** dù material trong scene đã
đúng màu. Đây là lỗi của đường render offscreen trong batchmode, không phải của game. Muốn kiểm
tra hình ảnh thật, chụp từ chính bản build:

```bash
Build\VuonNho-dev\VuonNho.exe -screen-fullscreen 0 -screen-width 1366 -screen-height 768 -vuonnho-screenshot "C:\anh.png" -vuonnho-screenshot-delay 4 -vuonnho-open-panel upgrade -vuonnho-screenshot-seed
```

`-vuonnho-screenshot-place "<id>[,góc]"` cầm sẵn một món trang trí và xoay sẵn, để ảnh có bóng ma
xem trước. Bóng ma bám theo **con trỏ thật**, nên trước khi chụp phải đưa chuột vào giữa cửa sổ —
xem cách làm trong lịch sử lệnh, hoặc dùng `[System.Windows.Forms.Cursor]::Position`.

`-vuonnho-screenshot-walk "x,z"` gửi một lệnh đi tới điểm (x, z) rồi chụp sau 0,15 giây, để ảnh
bắt được cả vòng tròn báo lại lẫn nhân vật đang dở bước. Nó đi qua đúng đường mà chuột thật đi
(`GameBootstrap.TryWalkCommand`) chứ không gọi thẳng `WalkTo`: ảnh chụp phải là ảnh của thứ người
chơi sẽ thấy.

`-vuonnho-screenshot-seed` tua vườn tới lúc đã mở đủ 12 ô và mua hết nâng cấp trước khi chụp, nên
ảnh tài liệu cho thấy một khu vườn có thứ để nhìn thay vì bốn ô đất trống. Nó mua theo đúng thứ
tự catalog — thứ tự đó đã thỏa điều kiện mở khóa của từng mục — nên không chép lại lộ trình của
bản cân bằng.

Hai điều đã làm hỏng một lượt chụp và sẽ làm hỏng lượt sau nếu quên:

- **Chạy tuần tự.** `& $exe ...` trong PowerShell không chờ tiến trình game. Năm bản chạy song
  song sẽ giẫm lên cùng một file save và cho ra năm khu vườn khác nhau. Dùng `Start-Process -Wait`.
- **Xóa save thì xóa cả `vuon-nho-save.backup.json`.** Xóa mỗi file chính thì game đọc bản dự
  phòng lên, và lượt chụp không bắt đầu từ vườn mới như mình tưởng.

## Đã xong

- **Mốc A** — vòng chơi, save/offline/lifecycle, HUD, 83 test, hai bản build.
- **L01 + B02** — 13 model Blender (gồm sả và nhài), 9 material, 5 cue âm thanh, prefab và
  GardenSkin đã điền đủ. Xem [Docs/Art/L01-B02.md](Docs/Art/L01-B02.md).
- **Checklist mục 10** — 149 test logic + 149 mục kiểm trong bản build, hai độ phân giải.
  Xem [Docs/QA-moc-A.md](Docs/QA-moc-A.md).
- **Hướng v1** — đổi tên, siết nhịp, thêm sả/nhài, pha 2 trang trí.
  Xem [Docs/Huong-di-v1.md](Docs/Huong-di-v1.md).
- **Font** — National Park, sáu weight, kèm giấy phép OFL.
- **Điều khiển camera và nhân vật** — lăn chuột để zoom, giữ trái để kéo màn hình, chuột phải để
  ra lệnh cho nhân vật đi, một vòng tròn dưới đất báo lại chỗ vừa bấm, hàng rào / cây / quầy trà /
  đồ đã đặt là vật cứng, và bóng ma xem trước khi đặt đồ. Xem mục [Điều khiển](#điều-khiển).
  22 mục kiểm chạy trong bản build vì tất cả đều cần camera thật, va chạm thật và nhiều frame
  thật, không kiểm được bằng test EditMode.
- **Icon cây** — bạc hà, cúc, dâu, sả, nhài. Hiện ở kho, bảng chọn cây và thẻ máy pha.
- **Icon tiền thay chữ "xu"** — glyph đồng tiền được ghép thẳng vào font chữ của game, nên nó
  đứng được cả giữa một câu chứ không chỉ trong một ô icon riêng. Xem mục
  [Chữ "xu" là một glyph trong chính font chữ](#chữ-xu-là-một-glyph-trong-chính-font-chữ).
- **Dây chuyền chế biến trà** — sáu công đoạn giữa thu hoạch và quầy trà, sáu máy, thợ ăn lương.
  Xem mục [Dây chuyền chế biến trà](#dây-chuyền-chế-biến-trà). Save lên schema 3; save cũ mở
  được và vào với dây chuyền trống.
- **Robot đi thu từng ô** — cây chín nằm chờ tới lượt thay vì bốc hơi tức thì; mất khoảng 12%
  thu nhập và đó là quãng người chơi nhìn thấy. Xem mục [Robot thu hoạch](#robot-thu-hoạch).
- **Canh tác** — độ phì, cỏ dại, sâu bệnh và thời vụ, kèm ba việc chăm sóc trong popup ô và ba
  dấu hiệu đọc được từ xa trong vườn. Vườn bỏ bê còn 42% thu nhập của vườn chăm kỹ. Xem mục
  [Canh tác](#canh-tác). Save lên schema 5; save cũ mở được với đất tốt và không cỏ.

## Còn lại

Việc cần người, không tự động được:

- **A05 / B04 — playtest.** Chưa có ai ngoài chơi thử. Toàn bộ nhịp v1 (trà đầu 18 giây,
  robot 1,6 phút) mới là **giả thuyết đo bằng máy**, chưa ai xác nhận là vui.
- **Máy chuẩn để đo hiệu năng.** Số hiện tại đo trên RTX 3060 nên chưa đủ nghiệm thu; kế hoạch
  yêu cầu chọn một máy cấu hình thấp trong nhóm tester.

Việc còn làm được bằng code:

- **Icon cho 5 món trang trí** — năm cây đã có icon vẽ tay, nút HUD đã có icon Material Symbols,
  nhưng bảng trang trí thì vẫn toàn chữ.
- **Model cho 5 món trang trí** — đang là primitive; ô `GardenSkin.Decorations` còn trống.
- **TextMeshPro** thay `UI.Text`, và **Input System** nếu làm Android.
- **Bold 700 của font** — file trong bản tải về hỏng, cần tải lại nếu muốn bậc chữ này.
