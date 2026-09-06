# Vườn và Trà

Tên trong [kế hoạch MVP](../Ke-hoach-MVP-Vuon-Nho.md) là *Vườn Nhỏ: Quán Trà*; hướng v1 đổi thành
**Vườn và Trà** và đó là tên đang nằm trong `PlayerSettings`, nên cũng là tên thư mục save.
Lý do đổi: [Docs/Huong-di-v1.md](Docs/Huong-di-v1.md).

Vòng chơi **chọn cây → gieo → cây lớn → thu → máy pha → tự bán → mua nâng cấp** chạy được từ đầu
đến cuối, có save/offline/lifecycle và bộ test logic. Art đã thay xong bằng model Blender tự sinh
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
| Chuột phải khi đang đặt / gỡ đồ trang trí | Thoát chế độ đó (không đi) |

Hai điều đáng biết vì chúng ràng buộc nhau:

- **Thao tác chuột trái chốt lúc thả, không phải lúc nhấn.** Lúc nhấn thì chưa biết người chơi
  định bấm hay định kéo màn hình. `CameraRig` là nơi quyết định (`ClickWasDrag`, ngưỡng 6 px) và
  `GameBootstrap.HandleClick` chỉ làm việc khi lần nhấn đó không phải một cú kéo.
- **Chuột phải vẫn là đường thoát khỏi chế độ đặt/gỡ đồ.** Đó là lối ra duy nhất người chơi đã
  quen; cho nhân vật đi lúc đó sẽ cướp mất nó. Chỉ khi không ở trong chế độ nào thì chuột phải
  mới là lệnh đi.

Đồ trang trí đã đặt là **vật cứng**: nhân vật đi vòng qua chứ không xuyên qua. Ngoại lệ duy nhất
là **phiến đá** — món đó sinh ra để người ta bước lên, chặn nó lại là tự mâu thuẫn. Cờ nằm ở
`DecorationDefinition.BlocksWalking` trong Core, không phải ở phía Unity, vì đó là chuyện của
món đồ chứ không phải của cách dựng scene.

Cách tránh là **trượt dọc một trục**, không phải tìm đường: đâm vào thì bỏ một thành phần và đi
nốt thành phần kia. Đồ trang trí đều là hộp vuông góc với trục nên thế là đủ để đi vòng. Cái giá
phải trả: đứng giữa hai món kê sát nhau thành một góc lõm thì nhân vật dừng lại chứ không lùi ra
để vòng — bấm một cú nữa là đi tiếp được.

Vòng tròn báo lại (`ClickMarker`) **vẽ đè lên mọi thứ trong vườn**, không kiểm tra độ sâu. Nó
nằm sát mặt đất mà luống cây cao 17 cm và chiếm gần hết khu giữa — chỗ người chơi bấm nhiều nhất
— nên nếu để kiểm tra độ sâu thì bấm vào luống sẽ không được báo lại gì. Vòng tròn hiện ở **đích**
chứ không ở chỗ con trỏ: bấm ra ngoài vườn thì nhân vật dừng ở mép, và vòng tròn phải nằm đúng
chỗ nó sẽ dừng.

Nhân vật đi thẳng, không tìm đường: vườn là một mảnh đất phẳng không có vật cản. Không có xương
và không có animation clip — hai chân là hai nhóm mesh với gốc xoay ở hông, nhịp chân tính theo
quãng đường đã đi nên chân luôn chạm đất đúng nhịp dù tốc độ có đổi.

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
| `VuonNho.Core` | `GameState`, `FarmSimulation`, `GameSession`, `ContentCatalog`, `SaveSerializer`, `TutorialGuide`, JSON | **C# thuần, không tham chiếu UnityEngine** |
| `VuonNho.Infrastructure` | `FileSaveRepository`, `SystemClock`, `FileTestLogger` | Core + UnityEngine |
| `VuonNho.Views` | `GameBootstrap`, `PlotView`, `MachineView`, `HelperView`, `CharacterView`, `CameraRig`, `ClickMarker`, `GameHud`, `GardenSkin`, `SfxPlayer`, `QaScreenshot` | Core + Infrastructure |
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
thời gian không dương, giá âm, yield không dương.

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
- **Nút muốn vừa bằng chữ thì phải gọi `UiFactory.HugContent`,** không đủ nếu chỉ tắt
  `childForceExpandWidth` của layout group cha. Nhãn bên trong nút được đặt `flexibleWidth = 1`
  để chữ căn được giữa khi nút bị kéo rộng; layout group của nút lại báo `flexibleWidth` của
  chính nó bằng tổng của các con, nên con số đó nổi lên thành `flexibleWidth` của cả cái nút — và
  Unity chia chỗ trống cho mọi thứ có `flexibleWidth` dương, không quan tâm `childForceExpandWidth`
  đã tắt hay chưa. `HugContent` ép về 0.

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
- **Checklist mục 10** — 97 test logic + 88 mục kiểm trong bản build, hai độ phân giải.
  Xem [Docs/QA-moc-A.md](Docs/QA-moc-A.md).
- **Hướng v1** — đổi tên, siết nhịp, thêm sả/nhài, pha 2 trang trí.
  Xem [Docs/Huong-di-v1.md](Docs/Huong-di-v1.md).
- **Font** — National Park, sáu weight, kèm giấy phép OFL.
- **Điều khiển camera và nhân vật** — lăn chuột để zoom, giữ trái để kéo màn hình, chuột phải để
  ra lệnh cho nhân vật đi, một vòng tròn dưới đất báo lại chỗ vừa bấm, và đồ đã đặt là vật cứng.
  Xem mục [Điều khiển](#điều-khiển). 16 mục kiểm chạy trong bản build vì tất cả đều cần camera
  thật, va chạm thật và nhiều frame thật, không kiểm được bằng test EditMode.
- **Icon cây** — bạc hà, cúc, dâu, sả, nhài. Hiện ở kho, bảng chọn cây và thẻ máy pha.

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
