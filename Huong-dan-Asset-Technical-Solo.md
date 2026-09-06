# Tự tạo asset và tích hợp Unity — Vườn Nhỏ

Phiên bản 0.2 · 06/09/2026 · Đi kèm [kế hoạch MVP solo](C:/Users/PC/Documents/Codex/2026-09-05/baay/outputs/Ke-hoach-MVP-Vuon-Nho.md).

Điều kiện đã xác nhận: một người làm toàn bộ, 25–30 giờ/tuần, mới bắt đầu Blender. Đây là hướng dẫn sản xuất và tiêu chí kỹ thuật; chưa có model, icon, audio hoặc project game được tạo trong lần sửa kế hoạch này.

## 1. Cách làm phù hợp với một người

**Mốc A:** dựng bằng cube, sphere, cylinder và prefab Unity để kiểm tra gameplay, automation và save. Tạo thủ công một ô rồi nhân bản. Nếu một thao tác lặp đã tốn thời gian, có thể viết script Editor sinh các ô hoặc gán material; không xây cả bộ công cụ tạo asset trước khi biết mình cần gì.

**Mốc B:** học đúng các thao tác Blender cần cho bộ model nhỏ; làm một ô bạc hà hoàn chỉnh, đưa vào Unity và nhìn ở camera thật. Sau khi pipeline này chạy ổn mới làm robot, quầy, hai cây còn lại và props.

Phong cách chốt: mô hình đồ chơi, khối bo nhẹ, màu phẳng, camera cố định. Không đưa sculpt, retopology, texture vẽ tay, baking normal, armature, weight painting, tóc hoặc vải vào đường bắt buộc. Phần dễ thương đến từ tỷ lệ, silhouette, màu, ánh sáng và chuyển động nhỏ.

AI hoặc script có thể hỗ trợ dựng hình, tạo biến thể hoặc chuẩn hóa file; mỗi kết quả vẫn phải qua gate import và kiểm tra trong game. Kế hoạch không phụ thuộc dịch vụ tạo 3D trả phí hay việc ảnh AI tự biến thành model dùng được. Không tính công cụ hỗ trợ như một nhân sự thứ hai trong lịch.

## 2. Danh mục phải sản xuất

“Model gốc” ở đây là một thiết kế đối tượng để tái sử dụng. Một model có thể có nhiều mesh con. Instance đặt trên map và giai đoạn tăng trưởng không tính thành model mới.

| ID | Tên dự kiến | Model gốc | Đầu ra trong Unity |
|---|---|---:|---|
| A01 | SM_Plot | 1 | Ô đất dùng lại 12 lần |
| A02 | SM_Seedling | 1 | Mầm chung cho 3 cây |
| A03 | SM_Mint | 1 | Bạc hà: thân cao, các cặp lá tầng |
| A04 | SM_Chamomile | 1 | Cúc: tán gọn, hoa sáng/nhụy vàng |
| A05 | SM_Strawberry | 1 | Dâu: tán thấp, quả đỏ lớn |
| A06 | SM_Helper | 1 | Robot nổi, các phần cứng tách được |
| A07 | SM_TeaStation | 1 | Quầy và máy pha cùng một cụm |
| A08 | SM_BackgroundTree | 1 | Cây nền ở rìa sau |
| A09 | SM_Bush | 1 | Bụi thấp, có thể lặp |
| A10 | SM_Fence | 1 | Một đoạn hàng rào modular |
| A11 | SM_Rock | 1 | Một khối đá để xoay/scale |
| **Tổng** | | **11** | Nền map dùng cube/plane Unity, không thêm thiết kế riêng |

Đầu ra ngoài model: 9 material màu chung; 7 icon PNG; 4 mẫu motion/VFX; 5 cue âm thanh; các prefab UI cơ bản. Nhạc nền là tùy chọn sau test, cần ngân sách riêng nếu làm.

7 icon chính xác: bạc hà, cúc, dâu, mầm, robot, quầy và ô đất. Nút công thức tái sử dụng icon cây kèm tên trà. Tốc độ cây dùng icon mầm, tốc độ máy dùng icon quầy, diện tích dùng icon ô. Kho/cài đặt và tiền có thể dùng nhãn chữ để không phát sinh một bộ icon thứ hai.

## 3. Quy chuẩn trước khi dựng model

| Thuộc tính | Quy ước khởi điểm |
|---|---|
| Đơn vị | 1 đơn vị Unity = 1 mét trong quy ước dự án; kiểm bằng cube chuẩn |
| Ô đất | Khoảng 1,4 × 1,4 m; tâm các ô cách 1,6 m |
| Cụm 3 × 4 ô | Footprint khoảng 4,6 × 6,2 m, chưa tính viền map |
| Cây | Cao khoảng 0,45–0,95 m, tán nằm trong khoảng 1,1 m |
| Robot | Thân khoảng 0,55–0,7 m; đặt vị trí nghỉ cạnh vườn |
| Quầy | Khoảng 2,8–3,2 m rộng, 1,5–2 m cao; kiểm lại framing |
| Hướng Unity | +Y lên; +Z là hướng trước của wrapper |
| Pivot | Ô/cây/công trình ở giữa chân; visual robot ở tâm thân; wrapper robot ở vị trí nghỉ |
| Root | Scale 1,1,1; collider và script gameplay không nằm trên mesh trang trí |
| Camera | Orthographic; bắt đầu yaw 45°, pitch khoảng 35°, chỉnh size theo map |
| Kiểm tra nhỏ | Game view 1366 × 768; xem thêm 1920 × 1080 |

Đây là số thiết kế ban đầu, không phải số đo từ ảnh. Chốt sau một scene mẫu; nếu đổi kích thước ô phải cập nhật quy chuẩn rồi mới tiếp tục sản xuất.

Các mục tiêu hình học để tự giới hạn chi tiết: mỗi cây trưởng thành khoảng 500–1.800 triangle; robot khoảng 1.000–3.000; quầy khoảng 2.000–5.000; mỗi prop khoảng 100–1.500. Đây là ngân sách mềm, không bảo đảm FPS. Ưu tiên giảm số bộ phận/renderers nhỏ không cần thiết và đo toàn scene.

Không mirror bằng scale âm trong prefab cuối. Lá dùng mesh có độ dày để không phải xử lý vật liệu hai mặt ngay từ đầu. Chi tiết không thấy ở camera chơi không được ưu tiên thêm.

## 4. Bảng màu và material

| Material dùng chung | Màu khởi điểm | Dùng cho |
|---|---|---|
| M_Grass | #A8BC78 | Nền cỏ, khối nền |
| M_Leaf | #4F7B50 | Lá, bụi, tán cây |
| M_Soil | #916447 | Ô đất |
| M_Wood | #B88858 | Quầy, thân cây, hàng rào |
| M_Cream | #F2E3BE | Quầy, thân robot, cánh cúc |
| M_Dark | #303C32 | Mặt robot, điểm nhấn tối |
| M_Yellow | #E9BF5C | Nhụy cúc, đèn, phản hồi tiến độ |
| M_Red | #D97672 | Dâu, điểm nhấn màu |
| M_Accent | #81B8AB | Máy/robot |

Dùng URP/Lit màu phẳng, metallic 0, smoothness thấp vừa phải. Không cần texture cho các model này. Điều chỉnh màu một lượt dưới ánh sáng thật rồi dùng chung toàn bộ; không tạo một material cho mỗi ô hoặc mỗi chiếc lá.

Material trong Blender giúp phân chia bề mặt và xem màu khi dựng; tạo/remap material Unity theo tên ổn định. Không kỳ vọng một node graph Blender chuyển nguyên vẹn thành shader URP. Các phần tĩnh có thể gộp sau khi chốt hình để giảm số object, nhưng giữ các phần cần bật/tắt hoặc chuyển động độc lập.

Ánh sáng đầu tiên: một Directional Light, ambient nhẹ, bóng vừa đủ nhìn tiếp xúc đất. Chưa thêm nhiều đèn real-time, custom shader, SSAO hoặc hậu kỳ mạnh. Chỉ bổ sung khi một vấn đề thị giác cụ thể cần giải quyết và đã đo hiệu năng.

## 5. Học Blender có đầu ra, tổng 16–28 giờ

| Bài học | Giờ | Đầu ra để chứng minh đã biết |
|---|---:|---|
| Viewport, chọn đối tượng, Object/Edit Mode, G/R/S, duplicate | 3–5 | Một cụm 3 khối đúng kích thước và tên |
| Mesh cơ bản, chỉnh vertex, cylinder ít cạnh, bevel, normals | 5–8 | Ô đất bo góc và một chiếc lá có độ dày |
| Material, origin, collection, tên file và lưu nguồn | 3–5 | Một model nhiều phần có màu và pivot đúng |
| Export FBX, import Unity, kiểm scale/axis, sửa và reimport | 5–10 | Cube chuẩn 1 m và một cây thử thay được VisualRoot |
| **Tổng** | **16–28** | Không bao gồm học C#/Unity từ đầu |

Mỗi bài dừng khi làm được đầu ra; không cần học hết mọi chức năng Blender. Những giờ này tính riêng với giờ sản xuất asset sau khi biết thao tác. Nếu 28 giờ vẫn chưa qua import gate, giữ visual primitive, ghi vấn đề cụ thể và ước tính lại trước khi làm cả bộ.

Kiểm tra cài đặt hiện tại chưa tìm thấy Blender ở các thư mục/registry phổ biến đã kiểm tra. Khi triển khai cần xác nhận hoặc cài bản stable, ghi chính xác phiên bản dùng; tài liệu này chưa cài phần mềm. Các tên mục export có thể khác giữa các phiên bản Blender, nên kết quả cube chuẩn là điều kiện quyết định preset đúng.

## 6. Cách dựng từng nhóm asset

### 6.1 Ô đất và mầm

1. Tạo cube, chỉnh kích thước ô khoảng 1,4 × 1,4 × 0,12 m; apply scale trước khi chỉnh bevel.
2. Bo cạnh nhỏ, ít segment; dùng material đất. Đặt origin giữa mặt đáy, tránh làm hình dạng nhấp nhô ở ranh giới click.
3. Nền xung quanh là một khối lớn dùng M_Grass. Collider chọn ô nằm trên wrapper Unity; collider trang trí không nhận input.
4. Mầm gồm một thân ngắn và hai lá. Lá tạo từ UV sphere ít segment ép dẹt nhưng vẫn có độ dày, hoặc mesh lá đơn giản đã học.
5. Dùng material lá; giữ mầm thành một thiết kế dùng chung. Đưa ô/mầm vào scene game và kiểm tra tương phản trước khi làm cây trưởng thành.

Không thêm từng vết cày hoặc hạt đất nhỏ ở bước này; chúng chỉ cần làm nếu thấy rõ và giúp đọc trạng thái.

### 6.2 Bộ thân và lá tái sử dụng

Tạo thân từ cylinder ít cạnh. Tạo một lá dày có đầu hơi nhọn; duplicate, xoay và scale để lắp các cây. Các bản duplicate là bộ phận của model, không phải asset phải thiết kế từ đầu. Giữ một collection bộ phận nguồn để sửa thuận tiện.

Sau khi bố cục lá đã ổn, gộp các phần cùng chuyển động và cùng mục đích hiển thị. Mỗi cây giữ FoliageRoot và phần ReadyAccents cần bật khi chín. Không đặt một MonoBehaviour trên mỗi lá.

### 6.3 Bạc hà

1. Một thân chính cao khoảng 0,7 m, 3–4 tầng lá đối nhau.
2. Dùng khoảng 6–8 lá lớn; thay góc và kích thước nhẹ giữa tầng.
3. Tán theo chiều đứng để khác dâu thấp và cúc có hoa.
4. Khi chín, tán đầy và có badge sẵn sàng dùng chung. Không cần model “quả bạc hà”.

### 6.4 Cúc

1. Dùng bộ thân/lá đã có, giữ tán gọn và cao vừa.
2. Một hoa mẫu gồm nhụy sphere màu vàng và khoảng 6–8 cánh màu kem nhân quanh tâm.
3. Nhân thành 2–3 hoa với độ cao khác nhẹ; hoa nằm trong ReadyAccents.
4. Kiểm tra ở camera nhỏ: hoa sáng phải đọc được ngay, không biến thành nhiều chấm nhiễu.

### 6.5 Dâu

1. Tạo tán lá thấp và rộng, dùng lại leaf primitive với góc hướng ra ngoài.
2. Quả từ sphere ít segment, chỉnh phần dưới hơi thuôn; thêm đài bằng vài lá nhỏ.
3. Dùng 2–3 quả đủ lớn, màu đỏ; không tạo từng hạt dâu ở MVP.
4. Quả nằm trong ReadyAccents. Từ xa phải nhận ra tán thấp và quả đỏ, kể cả khi không đọc tên.

### 6.6 Ba giai đoạn từ bốn model cây

| Trạng thái logic / tiến độ | Hiển thị |
|---|---|
| Growing, dưới 25% | Mầm dùng chung |
| Growing, từ 25% đến trước Ready | FoliageRoot của cây tương ứng, scale từ khoảng 0,65 lên 1; ReadyAccents ẩn |
| Ready | Cây đầy đủ, bật hoa/quả nếu có và badge sẵn sàng |

Không tạo ba FBX riêng cho mỗi cây. CropView lấy tiến độ từ startAt/finishAt và state, không có timer kinh tế riêng. Bạc hà cần badge cùng phản hồi hình dáng để trạng thái chín không chỉ phụ thuộc màu.

Khi automation thu ngay lúc chín, không giữ Ready lâu để chờ animation. Có thể phát một hiệu ứng thu ngắn trên vị trí ô trong khi model đã bắt đầu vụ mới.

### 6.7 Robot nổi

1. Thân từ cube bo cạnh hoặc sphere dẹt, màu kem/điểm nhấn xanh.
2. Mặt là một phần tối gắn phía trước; hai mắt dùng sphere hoặc khối nhỏ. Không cần texture mặt.
3. Anten là cylinder và sphere nhỏ. Không thêm tay/chân cần khớp.
4. Giữ Body, Face và Antenna là các phần có tên rõ; origin visual ở tâm thân, wrapper giữ vị trí nghỉ.
5. Robot nằm bên cạnh vườn, nhấp nhô vài centimet và nghiêng khi có thu hoạch; có thể xoay mặt về ô vừa thu. Không cần bay đến từng ô.

Khi nhiều ô thu cùng lúc, gom phản hồi robot thành một chuyển động ngắn. Không xếp hàng hàng trăm animation và không để sản xuất chờ robot.

### 6.8 Quầy và máy pha tích hợp

1. Quầy dùng một khối mặt bàn, hai trụ và mái đơn giản; không làm nội thất.
2. Máy từ hộp bo nhẹ, bình cylinder và vòi ngắn. Đặt thành một cụm quầy cố định.
3. Tạo một điểm SteamAnchor, một StatusAnchor và vùng nhấn máy ở wrapper.
4. Chỉ dùng mặt trước/bên nhìn thấy làm điểm nhấn. Có thể giữ mặt sau đơn giản nhưng mesh vẫn kín, không để lộ lỗ ở góc camera hiện tại.
5. Khi nâng cấp, đổi nhịp motion/hiệu ứng hoặc bật một đèn; không dựng model cấp 2/cấp 3 riêng.

### 6.9 Props

- Cây nền: thân cylinder và 2–3 khối tán bo; đặt chủ yếu phía sau.
- Bụi: 2–3 khối tán thấp, chung material lá.
- Hàng rào: một đoạn gồm 2 cọc và 2 thanh; kích thước theo module để đặt lặp.
- Đá: icosphere ít subdivision, chỉnh một số vertex để bớt tròn; dùng một material sẵn có.

Chỉ dùng bốn thiết kế này để lấp viền. Biến thể bằng xoay và scale khoảng 0,85–1,15. Chốt vị trí có seed hoặc lưu trong scene; không random lại bố cục mỗi lần load. Mọi prop không được che ô, badge hoặc collider click.

## 7. Pipeline Blender → Unity

### Source và export

Lưu nguồn Blender trong SourceArt/Blender ở ngoài thư mục Assets của project. Export FBX vào thư mục model trong Assets. Unity khuyến nghị dùng định dạng xuất như FBX cho production; nhập file nguồn trực tiếp có thể phụ thuộc ứng dụng dựng hình được cài. [Tài liệu Unity về file nguồn 3D](https://docs.unity3d.com/6000.0/Documentation/Manual/HOWTO-ImportObjectsFrom3DApps.html).

Preset khởi điểm cho asset tĩnh: chọn đúng object cần xuất; chỉ mesh và empty cần giữ; scale export 1; hướng thử -Z Forward / Y Up; áp dụng modifier cần thiết; tắt bake animation. Đây là điểm xuất phát phải xác nhận với phiên bản thực tế, không phải cấu hình bảo đảm đúng cho mọi exporter. Không xuất camera/light dùng để dựng mẫu vào scene gameplay.

Trước khi xuất: kiểm dimensions, apply rotation/scale phù hợp trên các phần tĩnh, đặt origin đúng, kiểm normals mặt ngoài và lưu nguồn. Với robot, giữ hierarchy các phần cần chuyển động. Không gộp mất điểm pivot của chúng.

### Gate cube chuẩn bắt buộc

1. Dựng cube 1 m, thêm một dấu chỉ hướng trước rõ ràng, xuất bằng preset dự kiến.
2. Import vào Unity, so với cube Unity 1 × 1 × 1 ở scale 1.
3. Kiểm cao/rộng/sâu, hướng trước, hướng đứng và pivot; chụp lại kết quả.
4. Nếu sai 100 lần hoặc xoay sai, sửa units/export/import nhất quán. Không bù riêng mỗi prefab bằng scale 0,01 hoặc rotation bất kỳ rồi tiếp tục cả bộ.
5. Đưa một cây thử qua pipeline, thay đổi lá trong Blender, export lại và xác nhận prefab Unity cập nhật mà không mất script/material.
6. Lưu preset và ghi phiên bản Blender/Unity khi cả hai thử nghiệm đạt.

Unity có các lựa chọn Scale Factor, Convert Units, Bake Axis Conversion, import camera/light và normals. Chỉ đổi một biến mỗi lần kiểm chuẩn; không đồng thời bù trục ở exporter, importer và wrapper mà không ghi lại. [Model Import Settings của Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/FBXImporter-Model.html).

### Import vào project

- Static model: không import animation/rig không dùng; camera/light tắt; không tự tạo collider từ mesh trang trí.
- Normals: thử import normals khi nguồn đã đúng; nếu bóng/cạnh lỗi, kiểm lại normals và cách smooth trước khi đổi cả preset.
- Material: remap các slot sang bộ 9 material Unity; tránh sinh bản trùng mỗi lần export.
- Tạo prefab wrapper riêng, đặt FBX dưới VisualRoot. ID, script và collider thuộc wrapper.
- Giữ nguyên đường dẫn file và file .meta khi cập nhật. Không xóa rồi tạo lại prefab đang được config/scene tham chiếu.
- Nguồn .blend, bản export, prefab và icon phải truy được về cùng asset ID.

## 8. Hợp đồng prefab và mã hiển thị

```text
PlotRoot — plotId, collider click, PlotView; scale 1
  VisualRoot
    SoilMesh
    CropAnchor
      SeedlingVisual
      MatureVisual
        FoliageRoot
        ReadyAccents
  ReadyBadgeAnchor

HelperRoot — vị trí nghỉ cố định, HelperView
  VisualRoot — gốc tại tâm thân
    Body
    Face
    Antenna

TeaStationRoot — collider click, MachineView
  VisualRoot
    StationMesh
  SteamAnchor
  StatusAnchor
```

Các tên trên là hợp đồng thiết kế, không phải file/code đã triển khai. Có thể thêm component vào cùng GameObject thay vì tạo nhiều tầng hơn nếu không cần anchor riêng.

PlotView/CropView nhận state và thời gian simulation để dựng đúng cây. MachineView nhận Idle/Running/Waiting và tiến độ batch. HelperView nhận sự kiện thu đã hoàn tất hoặc trạng thái automation; không trừ nguyên liệu/cộng xu. UI gọi GameSession, không sửa inventory trực tiếp.

Có thể giữ sẵn 12 PlotView vì số lượng nhỏ; không cần streaming/Addressables. Chỉ tạo hệ thống pooling cho hiệu ứng nếu profiling thấy cần. Khi load offline, dựng thẳng state cuối và bỏ các hiệu ứng lịch sử.

Nếu dùng script Editor hỗ trợ: chỉ tạo/cập nhật đường dẫn asset được chỉ định, chạy bằng menu chủ động, giữ seed và thông số. Không chạy generator mỗi lần Play hoặc ghi đè visual đã chỉnh tay. Timebox đầu tiên cho công cụ phụ tối đa 2–3 giờ; nếu làm thủ công một mẫu rồi duplicate nhanh hơn thì dùng cách đó. Thời gian này nằm trong A04 hoặc B02, không thêm một hạng mục vô hạn.

## 9. Icon và UI tự làm

Tạo một scene chụp icon nhỏ trong Unity để dùng đúng shader/màu của game. Một camera orthographic, một nền trong suốt nếu pipeline hỗ trợ sạch, ánh sáng và vị trí vật thể cố định. Xuất 256 × 256, giữ khoảng đệm khoảng 15–20%; chuẩn hóa kích thước vật thể trong khung thay vì crop mỗi icon theo cách khác nhau.

Nếu alpha qua camera/URP tốn quá timebox, dùng cùng một nền màu kem cho cả bộ icon và cùng màu panel. Khi đó không cần giả vờ đây là ảnh trong suốt; kiểm rõ viền ở UI.

Chụp 7 đối tượng trong BOM. Đọc thử ở 48 và 64 px trước khi dùng. Không thêm chữ vào ảnh; tên, số lượng và giá là TextMeshPro để chỉnh balance và giữ nét.

Import PNG dạng Sprite (2D and UI); dùng alpha nếu có, tắt mipmap cho icon UI cố định, thử bilinear và compression để không bẩn viền. Xác nhận các lựa chọn trên Inspector của phiên bản đang dùng. [Texture Import Settings của Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/class-TextureImporter.html).

UI chỉ cần một kiểu panel, một kiểu nút với các trạng thái, thanh tiến độ, badge và popup. Tạo hình cơ bản bằng UI hoặc một sprite bo góc 9-slice dùng lại; không thiết kế riêng từng cửa sổ.

Font dùng một family có glyph tiếng Việt và quyền phân phối phù hợp, không tự làm font. Kiểm dấu với chuỗi: “Vườn nhỏ — Bạc hà — Đã mở khóa — Vụ tiếp theo — 1.840 xu”. Font/licence là dependency ghi nguồn trong dự án, không tính như thuê người làm asset.

Ở 1366 × 768, bắt đầu với chữ chính khoảng 18 px trở lên và nút chính cao khoảng 44 px trở lên, rồi kiểm bằng mắt/thao tác thật. Đây là mục tiêu nội bộ cho PC; mobile phải hiệu chỉnh theo thiết bị.

## 10. Motion và VFX không cần rig

| Mẫu | Thời lượng/thông số khởi điểm | Quy tắc |
|---|---|---|
| PlantPop | Khoảng 0,15–0,25 giây, scale bật nhẹ | Chỉ scale VisualRoot, không scale collider |
| HarvestFeedback | Khoảng 0,2–0,35 giây, vài hình lá/hạt và nhãn +1 | Cộng vật phẩm đã xảy ra trong simulation |
| HelperIdle/React | Nhấp nhô 0,03–0,06 m, chu kỳ 2–3 giây; nghiêng ngắn khi thu | Không di chuyển wrapper gameplay, không xếp hàng phản ứng dài |
| BrewSteam | Vài particle đơn giản đang pha | Dừng emission khi máy chờ; không spawn particle cho từng mẻ offline |

Giới hạn số hiệu ứng đồng thời và âm thanh khi 12 ô thu cùng lúc. Một event kinh tế có thể được gộp thành một phản hồi thị giác; việc gộp không làm mất vật phẩm.

Thêm nút tắt/giảm motion nếu chuyển động gây khó chịu trong test. Cấu hình animation dừng đúng khi object bị disable; vòng crop mới không kế thừa scale đang dở của hiệu ứng trước.

## 11. Âm thanh DIY

Chỉ làm 5 cue: click, gieo, thu, trà hoàn thành, mua nâng cấp. Có thể ghi đồ vật gần gũi như gõ gỗ/chạm cốc hoặc tổng hợp tone/noise ngắn. Cắt khoảng im lặng, làm fade đầu/cuối, cân âm lượng khi nghe cùng nhau. Bản ghi tránh lẫn tiếng nói và nhạc đang phát trong phòng.

Giữ WAV nguồn, ví dụ 48 kHz/16-bit mono cho cue ngắn. Đưa vào Unity và chọn import phù hợp sau khi nghe; không mã hóa mất dữ liệu nhiều lần. Cue ngắn có thể thử Decompress On Load; nhạc dài nếu làm sau sẽ có cấu hình load riêng. Unity mô tả riêng load type, compression và Force To Mono. [Tài liệu Audio Clip](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioClip.html).

Giới hạn polyphony và gom tiếng thu cùng lúc, ví dụ tối đa một cue thu nổi bật trong khoảng 0,1 giây. Có volume SFX và mute; nhạc chưa có thì không để một thanh nhạc giả hoạt động. Nếu muốn tự làm nhạc ở bản sau, timebox riêng và kiểm cả độ lặp; không tính trong 5 cue bắt buộc.

## 12. Tổ chức dữ liệu và theo dõi nguồn

Cấu trúc dự kiến khi tạo repository game:

```text
VuonNhoMVP/
  SourceArt/
    Blender/
    Audio/
    Notes/
  Assets/Game/
    Art/Models/
    Art/Materials/
    Art/Prefabs/
    Art/Icons/
    Art/Generated/
    Audio/
    Content/
    Scenes/
    Scripts/Core/
    Scripts/Views/
    Scripts/Infrastructure/
    Editor/
    Tests/
  Packages/
  ProjectSettings/
```

Đây là cấu trúc dự kiến, chưa được tạo. SourceArt giữ file có thể chỉnh sửa; Assets giữ bản dùng để build. Commit các file .meta và cấu hình; không commit Library/Temp/build cache. Dùng LFS cho .blend/FBX/audio lớn khi cần.

Asset register là một bảng đơn giản có assetId, file nguồn, file export, prefab, icon liên quan, người/tác vụ tạo, phiên bản tool, trạng thái và ghi chú nguồn/licence. Với custom asset tự làm, ghi “tự tạo”; với font/thư viện ghi nguồn thực tế. Các trạng thái: Planned → Blockout → Imported → InGameChecked → FinalForMVP.

Mỗi asset cần ít nhất một ảnh kiểm tra trong camera gameplay. Icon tạo từ model nào phải ghi rõ để biết khi nào cần render lại. Khi thay source, chỉ export lại các file liên quan và kiểm không mất prefab reference.

## 13. Ngân sách sản xuất asset — B02

Giả định đã hoàn thành 16–28 giờ học ở mục 5. Mỗi nhóm có một lượt tự sửa; đây là ước lượng cho người mới, chưa phải tốc độ đã đo. Nếu vượt timebox, giảm chi tiết nhìn không rõ rồi kiểm lại; không tự động thêm nhiều ngày polish.

| Nhóm việc | Giờ | Sản phẩm nghiệm thu |
|---|---:|---|
| Bảng màu, kiểm chuẩn, một góc lookdev | 3–4 | Palette, ánh sáng, camera và mẫu đã import |
| Ô đất và mầm chung | 3–5 | 2 model gốc dùng được |
| Ba cây trưởng thành | 14–20 | 3 silhouette khác nhau, foliage/ready tách được |
| Robot nổi | 4–6 | 1 model, pivot và phần cứng đúng |
| Quầy có máy pha | 5–8 | 1 cụm, đủ anchor |
| Bốn props | 4–6 | Tree/bush/fence/rock dùng lặp |
| 7 icon và hình UI dùng lại | 3–5 | Bộ ảnh đọc được ở 48–64 px |
| 4 mẫu motion/VFX | 3–5 | Không ảnh hưởng logic hoặc collider |
| 5 cue âm thanh | 3–4 | Không clipping, không chồng âm quá mức |
| QA asset toàn scene và sửa nguồn | 6–8 | Qua checklist mục 14 |
| **B02 tổng** | **48–71** | Đã tính cả sửa art toàn scene |

48–71 giờ này không bao gồm học Blender, code UI/gameplay, tuyển người test hoặc sửa lỗi logic. Chúng đã có dòng riêng trong kế hoạch chính. Giờ QA ở đây chỉ là màu/model/pivot/icon/audio; regression gameplay và đo build nằm ở B04. Các con số vì vậy không được cộng thêm lần nữa như một “artist hỗ trợ”.

Nếu phải tự học Unity/C# từ đầu, cần ước lượng lại toàn kế hoạch; ngân sách asset không bù cho thời gian học lập trình.

## 14. Checklist nghiệm thu trong game

- Model đứng đúng nền, không nổi/lún hoặc sai kích thước sau reimport; root scale đúng quy ước.
- Nhìn rõ ba cây qua hình dáng ở 1366 × 768; Ready có badge hoặc đặc điểm rõ ngoài màu.
- Thấy đủ 12 ô; cây nền, robot, quầy và UI không che mục tiêu click.
- Các bước phát triển hiển thị đúng sau load/offline, không phụ thuộc animation đã chạy hay chưa.
- Model không mất mặt, mặt tối bất thường hoặc xuất hiện vật liệu hồng do shader không phù hợp.
- Click lên UI không xuyên xuống ô. Click vùng cây nhận đúng ô đất dù mesh cây thay đổi kích thước.
- Reimport một model không mất script, ID, material remap, collider hoặc config reference.
- Icon sắc ở kích thước dùng thật; không cắt mất vật thể, viền alpha lỗi hoặc lẫn font trong ảnh.
- Âm thanh không quá to khi nhiều ô thu; tắt SFX hoạt động; không có khoảng im lặng thừa gây trễ cảm giác.
- Animation không làm collider co giãn, không chặn tiến độ, không phát hàng dài phản hồi sau offline.
- Build Windows được đo khi vườn đầy. Chỉ thêm tối ưu hoặc hiệu ứng mới theo kết quả đo.
- Mỗi custom asset có source chỉnh được; font/thư viện có nguồn ghi lại; toàn bộ file cần thiết để build nằm trong repo.

## 15. Nếu công việc art vượt khả năng hiện tại

Giữ nguyên giới hạn logic và giảm lần lượt: số props đặt trên map; mức bevel/chi tiết quầy; chuyển động anten/mắt; particle hơi nước; độ phức tạp icon. Có thể giữ robot primitive nếu silhouette và màu đã tốt. Nếu một cây mới làm tốn quá timebox, dùng bộ thân/lá đã có với hoa/quả đơn giản trước.

Không dùng việc học rig, sculpt hoặc mua thêm nhiều pack như điều kiện để tiếp tục MVP. Mục tiêu nghiệm thu là bộ hình ảnh nhất quán, dễ đọc và dễ sửa trong game. Việc đạt độ phong phú như ảnh tham khảo cần một giai đoạn art sau MVP, có ngân sách riêng.
