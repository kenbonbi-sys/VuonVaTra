# Kết quả checklist mục 10 — kiểm thử build và giao diện

Chạy tự động bên trong bản build Windows, không phải trong Editor.

- Thời điểm: 2026-09-07 21:06:14
- Build: 0.2.0 (development)
- Độ phân giải: 1366 × 768
- Kết quả: **198 đạt, 0 không đạt**


## Khởi động

- ĐẠT — Nạp save và dựng phiên chơi dưới 2 giây · 0.197 s
- ĐẠT — Engine khởi động + splash Unity (chỉ ghi nhận, không phải mục tiêu của game) · 2.02 s
- ĐẠT — Tổng thời gian tới khung hình chơi được · 2.73 s (bao gồm splash)
- ĐẠT — Độ phân giải chạy thật · 1366 × 768

## Vườn mới

- ĐẠT — Bắt đầu với 0 xu · 0 xu
- ĐẠT — Mở sẵn đúng 4 ô · 4 ô
- ĐẠT — Chưa có robot

## Nút gieo và thu hoạch giữa màn hình

- ĐẠT — Bấm Gieo hạt ở vườn mới mở bảng chọn cây · HudCanvas/FarmHud/ActionDock/FarmActionButton; graphic trên cùng: HudCanvas/FarmHud/ActionDock/FarmActionButton; click handler: HudCanvas/FarmHud/ActionDock/FarmActionButton
- ĐẠT — Chọn Bạc hà từ nút gieo bắt đầu một vụ thật · PlotPopup/CropViewport/CropList/Crop_crop_mint; graphic trên cùng: PlotPopup/CropViewport/CropList/Crop_crop_mint; click handler: PlotPopup/CropViewport/CropList/Crop_crop_mint
- ĐẠT — Bấm Thu hoạch thu đủ sản lượng và gieo lại cây đang chọn · sản lượng 4/4; HudCanvas/FarmHud/ActionDock/FarmActionButton; graphic trên cùng: HudCanvas/FarmHud/ActionDock/FarmActionButton; click handler: HudCanvas/FarmHud/ActionDock/FarmActionButton

## Chơi từ vườn mới tới mở 12 ô

- ĐẠT — Có xu đầu tiên từ trà · ở phút mô phỏng 0.3
- ĐẠT — Trà đầu trong vòng 2 phút mô phỏng · 0.3 phút
- ĐẠT — Mua được robot
- ĐẠT — Đủ tiền mua robot trong 8 phút mô phỏng · 2.6 phút, 60 lần thu tay
- ĐẠT — Mua Mở cúc + công thức
- ĐẠT — Mua Mở vườn lần 1
- ĐẠT — Mua Tốc độ máy I
- ĐẠT — Mua Mở dâu + công thức
- ĐẠT — Mua Tốc độ cây I
- ĐẠT — Mua Mở vườn lần 2
- ĐẠT — Mua Tốc độ máy II
- ĐẠT — Mua Tốc độ cây II
- ĐẠT — Mua Mở sả + công thức
- ĐẠT — Mua Mở nhài + công thức
- ĐẠT — Mua Phòng trừ sinh học
- ĐẠT — Mua Nhận nương chè + trà búp
- ĐẠT — Mua Tay nghề hái 1 tôm 2 lá
- ĐẠT — Mua Tay nghề hái 1 tôm 1 lá
- ĐẠT — Mua Tay nghề hái đinh trà
- ĐẠT — Mua Đồ bảo hộ y tế cho thợ
- ĐẠT — Mở đủ 12 ô · 12 ô sau 91.4 phút mô phỏng
- ĐẠT — Mua hết toàn bộ nâng cấp MVP
- ĐẠT — Mở hết mọi loại cây gieo được · 9/9 loại
- ĐẠT — Không có xu âm · 19 xu

## Reload khi cây và máy đang chạy

- ĐẠT — Có cây đang lớn lúc lưu
- ĐẠT — Máy đang pha lúc lưu · công thức Trà đinh ngọc, kho còn 0/8 nguyên liệu
- ĐẠT — Nạp lại được file save vừa ghi · LoadedPrimary
- ĐẠT — Giữ nguyên số xu sau khi nạp lại · 19 → 19
- ĐẠT — Thời gian mô phỏng không lùi · 5590302 → 5590315
- ĐẠT — Trạng thái ô đất dựng lại đúng
- ĐẠT — Hướng dẫn tiếp tục đúng bước · ChooseNextUpgrade
- ĐẠT — Hình ảnh ô đất khớp trạng thái sau khi dựng lại · 0 ô lệch

## Giao diện ở 1366 × 768

- ĐẠT — Thẻ Profile nằm trong màn hình và không bị che
- ĐẠT — Thẻ FarmStats nằm trong màn hình và không bị che
- ĐẠT — Thẻ CoinChip nằm trong màn hình và không bị che
- ĐẠT — Thẻ WorkerChip nằm trong màn hình và không bị che
- ĐẠT — Thẻ StockChip nằm trong màn hình và không bị che
- ĐẠT — Các nút nổi nhận được click khi không mở bảng · 14 nút
- ĐẠT — Click ngoài panel vẫn xuống được vườn · click trúng PlotRoot_0
- ĐẠT — Modal OfflineModal có lối thoát bấm được · OfflineModal/Card/ActionRow/Continue
- ĐẠT — Nút Tiếp tục vừa bằng chữ và nằm giữa · nút 120 px / thẻ 560 px, lệch tâm 0.0 px
- ĐẠT — Modal BlockedModal có lối thoát bấm được · BlockedModal/Card/Actions/Settings, BlockedModal/Card/Actions/Quit
- ĐẠT — Nút điều hướng mở inventory · HudCanvas/FarmHud/InventoryButton; graphic trên cùng: HudCanvas/FarmHud/InventoryButton; click handler: HudCanvas/FarmHud/InventoryButton
- ĐẠT — Mở được bề mặt inventory
- ĐẠT — Chữ không tràn khỏi ô chứa — inventory · 106 nhãn
- ĐẠT — Click trên inventory bị UI chặn, không xuống tới vườn · UI nhận click: InventoryPanel; đối tượng nhận sự kiện pointer: không có; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — inventory không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt inventory · HudCanvas/InventoryPanel/Close; graphic trên cùng: HudCanvas/InventoryPanel/Close; click handler: HudCanvas/InventoryPanel/Close
- ĐẠT — Nút điều hướng mở upgrade · HudCanvas/FarmHud/UpgradeButton; graphic trên cùng: HudCanvas/FarmHud/UpgradeButton; click handler: HudCanvas/FarmHud/UpgradeButton
- ĐẠT — Mở được bề mặt upgrade
- ĐẠT — Chữ không tràn khỏi ô chứa — upgrade · 87 nhãn
- ĐẠT — Click trên upgrade bị UI chặn, không xuống tới vườn · UI nhận click: Row_upgrade_expand_8; đối tượng nhận sự kiện pointer: không có; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — upgrade không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt upgrade · HudCanvas/UpgradePanel/Close; graphic trên cùng: HudCanvas/UpgradePanel/Close; click handler: HudCanvas/UpgradePanel/Close
- ĐẠT — Nút điều hướng mở decorate · HudCanvas/FarmHud/DecorateButton; graphic trên cùng: HudCanvas/FarmHud/DecorateButton; click handler: HudCanvas/FarmHud/DecorateButton
- ĐẠT — Mở được bề mặt decorate
- ĐẠT — Chữ không tràn khỏi ô chứa — decorate · 30 nhãn
- ĐẠT — Click trên decorate bị UI chặn, không xuống tới vườn · UI nhận click: Row_deco_planter; đối tượng nhận sự kiện pointer: không có; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — decorate không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt decorate · HudCanvas/DecoratePanel/Close; graphic trên cùng: HudCanvas/DecoratePanel/Close; click handler: HudCanvas/DecoratePanel/Close
- ĐẠT — Nút điều hướng mở workshop · HudCanvas/FarmHud/WorkshopButton; graphic trên cùng: HudCanvas/FarmHud/WorkshopButton; click handler: HudCanvas/FarmHud/WorkshopButton
- ĐẠT — Mở được bề mặt workshop
- ĐẠT — Chữ không tràn khỏi ô chứa — workshop · 50 nhãn
- ĐẠT — Click trên workshop bị UI chặn, không xuống tới vườn · UI nhận click: Row_stage_wither; đối tượng nhận sự kiện pointer: không có; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — workshop không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt workshop · HudCanvas/WorkshopPanel/Close; graphic trên cùng: HudCanvas/WorkshopPanel/Close; click handler: HudCanvas/WorkshopPanel/Close
- ĐẠT — Nút điều hướng mở settings · HudCanvas/FarmHud/SettingsButton; graphic trên cùng: HudCanvas/FarmHud/SettingsButton; click handler: HudCanvas/FarmHud/SettingsButton
- ĐẠT — Mở được bề mặt settings
- ĐẠT — Chữ không tràn khỏi ô chứa — settings · 16 nhãn
- ĐẠT — Click trên settings bị UI chặn, không xuống tới vườn · UI nhận click: SettingsPanel; đối tượng nhận sự kiện pointer: không có; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — settings không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt settings · HudCanvas/SettingsPanel/Close; graphic trên cùng: HudCanvas/SettingsPanel/Close; click handler: HudCanvas/SettingsPanel/Close
- ĐẠT — Nút điều hướng mở journal · HudCanvas/FarmHud/JournalButton; graphic trên cùng: HudCanvas/FarmHud/JournalButton; click handler: HudCanvas/FarmHud/JournalButton
- ĐẠT — Mở được bề mặt journal
- ĐẠT — Chữ không tràn khỏi ô chứa — journal · 13 nhãn
- ĐẠT — Click trên journal bị UI chặn, không xuống tới vườn · UI nhận click: OpenAgronomy; đối tượng nhận sự kiện pointer: OpenAgronomy; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — journal không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt journal · HudCanvas/JournalPanel/Close; graphic trên cùng: HudCanvas/JournalPanel/Close; click handler: HudCanvas/JournalPanel/Close
- ĐẠT — Mở được bề mặt plot
- ĐẠT — Chữ không tràn khỏi ô chứa — plot · 16 nhãn
- ĐẠT — Click trên plot bị UI chặn, không xuống tới vườn · UI nhận click: Crop_crop_chamomile; đối tượng nhận sự kiện pointer: Crop_crop_chamomile; nếu không chặn sẽ trúng Ground
- ĐẠT — plot không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt plot · HudCanvas/PlotPopup/ClosePopup; graphic trên cùng: HudCanvas/PlotPopup/ClosePopup; click handler: HudCanvas/PlotPopup/ClosePopup
- ĐẠT — Mở được bề mặt finance
- ĐẠT — Chữ không tràn khỏi ô chứa — finance · 22 nhãn
- ĐẠT — Click trên finance bị UI chặn, không xuống tới vườn · UI nhận click: FinancePanel; đối tượng nhận sự kiện pointer: không có; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — finance không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt finance · HudCanvas/FinancePanel/Close; graphic trên cùng: HudCanvas/FinancePanel/Close; click handler: HudCanvas/FinancePanel/Close
- ĐẠT — Mở được bề mặt agronomy
- ĐẠT — Chữ không tràn khỏi ô chứa — agronomy · 11 nhãn
- ĐẠT — Click trên agronomy bị UI chặn, không xuống tới vườn · UI nhận click: AgronomyPanel; đối tượng nhận sự kiện pointer: không có; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — agronomy không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt agronomy · HudCanvas/AgronomyPanel/Close; graphic trên cùng: HudCanvas/AgronomyPanel/Close; click handler: HudCanvas/AgronomyPanel/Close
- ĐẠT — Mở được bề mặt craft
- ĐẠT — Chữ không tràn khỏi ô chứa — craft · 32 nhãn
- ĐẠT — Click trên craft bị UI chặn, không xuống tới vườn · UI nhận click: Dials; đối tượng nhận sự kiện pointer: không có; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — craft không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt craft · HudCanvas/CraftPanel/Close; graphic trên cùng: HudCanvas/CraftPanel/Close; click handler: HudCanvas/CraftPanel/Close
- ĐẠT — Mở được bề mặt legal
- ĐẠT — Chữ không tràn khỏi ô chứa — legal · 19 nhãn
- ĐẠT — Click trên legal bị UI chặn, không xuống tới vườn · UI nhận click: Entity_Hkd; đối tượng nhận sự kiện pointer: không có; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — legal không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt legal · HudCanvas/LegalPanel/Close; graphic trên cùng: HudCanvas/LegalPanel/Close; click handler: HudCanvas/LegalPanel/Close
- ĐẠT — Mở được bề mặt model
- ĐẠT — Chữ không tràn khỏi ô chứa — model · 22 nhãn
- ĐẠT — Click trên model bị UI chặn, không xuống tới vườn · UI nhận click: Branch_BulkB2B; đối tượng nhận sự kiện pointer: không có; nếu không chặn sẽ trúng WorkshopFence
- ĐẠT — model không che hồ sơ, xu và số thợ · ba thẻ trên cùng vẫn nhìn thấy
- ĐẠT — Nút đóng nhận click thật và đóng bề mặt model · HudCanvas/ModelPanel/Close; graphic trên cùng: HudCanvas/ModelPanel/Close; click handler: HudCanvas/ModelPanel/Close
- ĐẠT — Chữ không tràn khỏi ô chứa — HUD · 24 nhãn

## Offline và hiệu năng

- ĐẠT — Mô phỏng bù 4 giờ dưới 250 ms · lần chậm nhất 8 ms trên 5 lần, vườn 12 ô
- ĐẠT — 5 vòng thoát/quay lại không đóng băng khung hình · frame dài nhất 11 ms
- ĐẠT — p95 frame time dưới 16,7 ms khi vườn đầy · trung bình 180 FPS, p95 5.6 ms, 719 khung hình
- ĐẠT — Trung bình đạt 60 FPS · trung bình 180 FPS, p95 5.6 ms, 719 khung hình

## Điều khiển camera và nhân vật

- ĐẠT — Bấm vào ô đất vẫn mở được bảng ô · ô 0
- ĐẠT — Lăn chuột tới thì nhìn gần lại · 6.60 → 4.50
- ĐẠT — Thu nhỏ hết cỡ vẫn nằm trong giới hạn · 15.73 (tối đa 15.73)
- ĐẠT — Phóng to hết cỡ vẫn nằm trong giới hạn · 3.20 (tối thiểu 3.20)
- ĐẠT — Zoom bám điểm dưới con trỏ · lệch 0.0 cm
- ĐẠT — Kéo màn hình không ra khỏi vườn · dừng ở (14.0, 14.0), giới hạn 14.0
- ĐẠT — Về lại góc nhìn ban đầu · 6.60
- ĐẠT — Nhân vật có hai chân để bước
- ĐẠT — Chuột phải xuống đất thành lệnh đi
- ĐẠT — Chỗ vừa bấm có vòng tròn báo lại
- ĐẠT — Vòng tròn tự tắt chứ không nằm lại · sau 0.50 s
- ĐẠT — Nhận lệnh đi tới điểm được chỉ · đích (-0.8, -1.4)
- ĐẠT — Đi tới nơi rồi dừng · cách đích 6 cm
- ĐẠT — Không đi xuyên qua đồ đã đặt · dừng cách tâm ghế 66 cm
- ĐẠT — Lối đi lát đá vẫn bước lên được · dừng cách tâm phiến đá 6 cm
- ĐẠT — Không đi xuyên qua quầy trà · dừng cách tâm quầy 110 cm
- ĐẠT — Chọn món thì hiện bóng ma xem trước · món deco_bench, góc 0
- ĐẠT — Bóng ma dựng được model của món đang cầm
- ĐẠT — R xoay 180° và kéo chuột phải xoay tiếp · sau R rồi kéo 45°: 225°
- ĐẠT — Món đặt xuống giữ đúng góc đã xoay · góc 225°
- ĐẠT — Thoát chế độ thì bóng ma biến mất
- ĐẠT — Túi đồ có một ô cho mỗi mặt hàng · 70 ô / 70 mặt hàng
- ĐẠT — Ô chi tiết nằm dưới đáy và không trống · Bạc hà tươi: 11
- ĐẠT — Bấm ra ngoài vườn thì dừng ở mép · đích (14.5, -14.5), giới hạn 14.5

## Dây chuyền chế biến trà

- ĐẠT — Có đủ sáu công đoạn giữa thu hoạch và quầy trà · 6 công đoạn
- ĐẠT — Gom đủ xu để xây cả dây chuyền · 12881 / 12780 xu
- ĐẠT — Mua được cả sáu máy · 6 máy
- ĐẠT — Không mua được một cái máy hai lần · Đã mua.
- ĐẠT — Thuê đủ thợ cho tất cả các máy · 6 thợ
- ĐẠT — Lá tươi đi hết chuỗi thành trà đóng gói
- ĐẠT — Thợ ăn lương và xu vẫn không âm · 4029 xu, 6/6 thợ đang làm
- ĐẠT — Xây dây chuyền xong vẫn kiếm được xu · trước 4101, sau 4029
- ĐẠT — Bảng Xưởng mở được và có đủ hàng
- ĐẠT — Chữ không tràn khỏi ô chứa — workshop · 26 nhãn

## Thợ thường trực và hover máy

- ĐẠT — Có quản lý thợ thường trực và hover máy
- ĐẠT — Thợ vẫn hiện qua nhiều mẻ làm việc và chờ nguyên liệu · ít nhất 6/6 thợ luôn có mặt
- ĐẠT — Model có đủ hai tay xoay tại vai · 6 thợ có ArmLeft và ArmRight
- ĐẠT — Tay thợ chuyển động trong player · lệch nhiều nhất 73.2°
- ĐẠT — Chữ không tràn khỏi ô chứa — hover stage_wither · 8 nhãn
- ĐẠT — Chữ không tràn khỏi ô chứa — hover stage_fix · 8 nhãn
- ĐẠT — Chữ không tràn khỏi ô chứa — hover stage_roll · 8 nhãn
- ĐẠT — Chữ không tràn khỏi ô chứa — hover stage_oxidise · 8 nhãn
- ĐẠT — Chữ không tràn khỏi ô chứa — hover stage_dry · 8 nhãn
- ĐẠT — Chữ không tràn khỏi ô chứa — hover stage_pack · 8 nhãn
- ĐẠT — Rê chuột nhận đủ sáu máy và hiện thông tin · 6/6 máy
- ĐẠT — Bảng hover nằm trong màn hình · sát mép nhất còn trong màn hình
- ĐẠT — Hover ẩn khi chuột vào HUD
- ĐẠT — Hover ẩn khi chuột rời màn hình

## Robot đi thu từng ô

- ĐẠT — Robot mất thời gian đi tới ô · còn 567 ms nữa mới tới ô 5
- ĐẠT — Cây chín nằm chờ chứ không bị thu sạch tức thì · 1 ô đang chín chờ robot
- ĐẠT — Hình robot chạy theo mô phỏng · đi được 319 cm
- ĐẠT — Thu xong thì robot đứng lại ngay ô đó · ô 5

## Canh tác: độ phì, cỏ, sâu bệnh, thời vụ

- ĐẠT — Gieo một vụ thì trừ độ phì của ô · 100 → 96 (trừ 4)
- ĐẠT — Đất bỏ không tự hồi nhưng dừng ở trần tự nhiên · 45/100, trần 45
- ĐẠT — Bón phân đưa đất về mức tốt nhất và trừ đúng tiền · độ phì 100, còn 6128 xu
- ĐẠT — Đất đang tốt thì không cho bón thêm
- ĐẠT — Bỏ bê một lúc thì cỏ mọc kín ô · 100/100
- ĐẠT — Cỏ dại làm cây lớn chậm hơn hẳn · 5120 ms → 8192 ms
- ĐẠT — Làm cỏ xoá sạch cỏ của ô · 0/100
- ĐẠT — Trị được sâu bệnh và trừ đúng tiền · còn 123051 xu
- ĐẠT — Ô không có sâu thì không cho trị
- ĐẠT — Trồng trái vụ thu ít hơn trồng đúng vụ · 4 → 2 đơn vị
- ĐẠT — Mùa suy ra được từ đồng hồ mô phỏng · đang là mùa đông
- ĐẠT — Popup ô hiện độ phì, cỏ và mùa · Độ phì 100/100 · cỏ 0/100 · mùa đông
- ĐẠT — Popup ô có đủ ba nút chăm sóc
- ĐẠT — Ô sạch sâu bệnh thì không có nút trị sâu · activeSelf = False
- ĐẠT — Ô có sâu bệnh thì nút trị sâu hiện ra
- ĐẠT — Chữ không tràn khỏi ô chứa — popup ô · 16 nhãn
- ĐẠT — Thẻ hồ sơ hiện đúng mùa hiện tại và đồng hồ mùa · Mùa đông  ·  3:35
- ĐẠT — Ô đầy cỏ và có sâu thì thấy được ngay trong vườn · cỏ True, sâu True
- ĐẠT — Làm cỏ trị sâu xong thì dấu hiệu biến mất

## Âm lượng, reset và thoát

- ĐẠT — Bật tắt âm thanh và ghi nhớ được lựa chọn
- ĐẠT — Xuất được dữ liệu test · Đã xuất dữ liệu test: C:/Users/PC/AppData/LocalLow/Vuon va Tra/VuonNho-HudReview\vuon-nho-export-20260907-210614.log
- ĐẠT — Reset trả về vườn mới · 0 xu, 4 ô
- ĐẠT — Sau reset vẫn có file save hợp lệ · C:/Users/PC/AppData/LocalLow/Vuon va Tra/VuonNho-HudReview\vuon-nho-save.json