# Checklist mục 10 — kết quả chạy

Ngày chạy: 06/09/2026. Bản build: `0.2.0` (development), Windows x64, URP.
Lần chạy gần nhất đã có bộ art L01/B02 thật, không còn primitive.

| Phần của mục 10 | Cách chạy | Kết quả |
|---|---|---|
| Kiểm thử logic bắt buộc | 83 test EditMode | **83 đạt, 0 hỏng** |
| Kiểm thử build và giao diện | Bộ QA tự chạy bên trong bản build, 2 độ phân giải | **62 đạt, 0 hỏng** mỗi độ phân giải |
| Ngân sách hiệu năng | Đo trên bản build | Đạt mọi ngưỡng — **nhưng chưa phải máy chuẩn**, xem cảnh báo cuối |

Báo cáo chi tiết: [QA-1366x768.md](QA-1366x768.md), [QA-1920x1080.md](QA-1920x1080.md).

Chạy lại:

```bash
Build\VuonNho-dev\VuonNho.exe -screen-fullscreen 0 -screen-width 1366 -screen-height 768 -vuonnho-qa "Docs\QA-1366x768.md"
```

Tiến trình thoát với mã 0 khi mọi mục đạt, mã 1 khi có mục hỏng — dùng được trong script.

## 1. Kiểm thử logic bắt buộc

Mỗi dòng của bảng trong kế hoạch được đối chiếu với test cụ thể.

| Nhóm test theo kế hoạch | Test phủ | Đạt |
|---|---|---|
| Tính nhất quán thời gian | `MotBuoc30PhutBangNhieuBuocCongLai`, `TickLeThapPhanGiayVanCungKetQua` | ✅ |
| Biên timer | `CayChinDungThoiDiemDeadline`, `MeMayCongXuDungMotLanTaiDeadline` | ✅ |
| Trước robot | `TruocRobotKhongTuThuVaKhongGieoThem`, `TruocRobotThiOfflineChiChoCayChinKhongThuThem`, `TruocRobotNghiKhiCayDaChinThiVanChiLaChin` | ✅ |
| Sau robot (0/60/300/14400 s) | `SauRobotKhopVoiMoPhongOnline` (4 tham số), `BonOBacHaChayOnDinhChoDungMuoiXuMoiMe` | ✅ |
| Cap offline | `BonGioVaHaiMuoiBonGioChoCungPhanTienDo`, `PhanDuTrenCapKhongDuocNhanOLanMoTiepTheo`, `ChinhGioTienVanBiChanBoiCap` | ✅ |
| Đồng thời | `NhieuSuKienCungTimestampChoKetQuaOnDinh` | ✅ |
| Đổi cây/công thức | `DoiCayGiuaVuChiApDungTuVuSau`, `DoiCongThucGiuaMeThiMeHienTaiVanHoanThanhTruoc` | ✅ |
| Nâng tốc độ | `MuaTocDoGiuaChuKyKhongDoiDeadlineDangChay` | ✅ |
| Kho | `BanHetNguyenLieuKhiMayDangPhaThiMeHienTaiVanXong`, `NguyenLieuDaVaoMeThiKhongConTrongKhoDeBanTay` | ✅ |
| Giao dịch | `ThieuXuThiKhongMuaDuoc`, `MuaHaiLanCungMotNangCapChiTruTienMotLan`, `BamThuHaiLanTrenCungMotOChiThuDuocMotLan`, `KhongTrongDuocTrenODaKhoa` | ✅ |
| Resume | `GoiResumeNhieuLanChiCongTienMotLan`, `CrashKhiDangMoBaoCaoQuayLaiThiKhongCongLaiTienCuaQuangNghiCu` | ✅ |
| Save | `LuuRoiDocLaiGiuNguyenTrangThaiGiuaVuVaGiuaMe`, `FileChinhHongThiLoadTuBackup`, `CaHaiFileHongThiKhongTuTaoVuonMoi`, `LoiGhiKhiMuaThiKhongTruTienVaChoThuLai` | ✅ |
| Phiên bản | `IdCayLaLamSaveKhongHopLe`, `SchemaMoiHonUngDungThiBaoLoi_KhongAmThamReset`, `SaveTaoBoiBalanceKhacVanNapDuocVaGiuGiaTriDaChot` | ✅ |
| Đồng hồ | `ChinhGioLuiKhongTaoTienDoAmVaKhongLamHongState`, `DongHoTrongGameDungUtcNenDoiMuiGioKhongAnhHuong`, `QuangNghiChiPhuThuocKhoangUtc` | ✅ |

Bốn ô trước đây còn trống, đã bổ sung trong đợt này: **balance khác**, **chuyển múi giờ**,
**crash khi popup quay lại đang mở**, **nghỉ khi cây đã chín**. Kèm theo là một thay đổi hành vi:
`GameSession` giờ so `balanceVersion` của save với cấu hình đang chạy, báo diagnostic khi khác, và
**không** chuyển đổi dữ liệu — vụ và mẻ đang chạy giữ nguyên giá trị đã chốt.

## 2. Kiểm thử build và giao diện

Chạy trong bản build, không phải Editor.

| Yêu cầu trong kế hoạch | Kết quả |
|---|---|
| Chơi từ fresh save đến mở 12 ô trong build Windows | Đạt với balance v1. Trà đầu ở **phút 0,3**; đủ tiền robot ở **phút 1,6** sau **36 lần thu tay**; mua hết thang nâng cấp ở **phút 19,3** |
| Click UI không thu cây phía sau | Đạt cho cả 4 bề mặt — kiểm bằng raycast của EventSystem, xem cảnh báo bên dưới |
| Popup không che nút quan trọng | Đạt — cả 3 nút trên thanh HUD vẫn nằm ngoài mọi panel |
| Text không tràn ở 1366 × 768 và 1920 × 1080 | Đạt — quét toàn bộ nhãn đang hiện, so `preferredHeight`/`preferredWidth` với ô chứa |
| Reload khi cây/máy đang chạy | Đạt — xu, trạng thái ô, bước hướng dẫn và hình ảnh đều khớp sau khi nạp lại |
| Quay lại sau offline nhiều vòng không đóng băng input | Đạt — 5 vòng liên tiếp, khung hình dài nhất 39 ms |
| Âm lượng, thoát, reset | Đạt — bật tắt âm ghi nhớ được, reset trả về vườn mới và vẫn còn file save hợp lệ |
| APK | Ngoài phạm vi bản này |

Các mốc này là của **balance v1** (nhịp thắng nhanh), không so được với lộ trình mục 5 kế hoạch vốn
dựng cho balance v0. Xem [Huong-di-v1.md](Huong-di-v1.md) để biết vì sao đổi.

## 3. Ngân sách hiệu năng

Máy đo: NVIDIA GeForce RTX 3060, 12 GB VRAM, 12 luồng CPU.

| Mục tiêu | Đo được |
|---|---|
| 60 FPS ở 1080p | 178 FPS trung bình (đã có model thật) |
| p95 frame time dưới 16,7 ms khi vườn đầy | 5,6 ms |
| Mô phỏng catch-up 4 giờ dưới 250 ms | 1 ms (chậm nhất trong 5 lần, vườn 12 ô) |
| Load/quay lại dưới 2 giây | Nạp save và dựng phiên: **0,09 s** |

Về mốc 2 giây: tổng thời gian từ lúc chạy tiến trình tới khung hình chơi được là **2,6 s**, trong đó
**2,0 s là splash Unity** — không tắt được ở bản Personal và không phải phần game tối ưu được.
Phần ứng dụng tự chịu trách nhiệm là 0,09 s. Báo cáo ghi tách hai số này thay vì gộp lại.

## Bốn điều checklist này KHÔNG chứng minh được

1. **Máy đo không phải máy chuẩn.** Kế hoạch yêu cầu chọn một máy Windows cấu hình thấp trong nhóm
   tester làm máy chuẩn. RTX 3060 quá mạnh so với yêu cầu đó, nên các con số trên **chưa đủ để
   nghiệm thu** ngân sách hiệu năng — phải đo lại trên máy chuẩn khi đã chọn được. Ngoài ra scene
   hiện đã dùng bộ model thật của B02, nên con số sát thực tế hơn trước — nhưng vẫn là máy mạnh.
2. **Chưa bơm input chuột thật.** Mục "click UI không thu cây phía sau" được kiểm bằng cách bắn
   raycast của EventSystem vào giữa panel và xác nhận UI chặn được, cộng với việc `GameBootstrap`
   có chốt `IsPointerOverGameObject`. Đó là bằng chứng gián tiếp, chưa phải một cú click thật.
3. **Chưa kill ứng dụng giữa lúc đang ghi save.** Phần reload dùng một lần thoát sạch. Trường hợp
   tắt máy đúng lúc đang ghi file vẫn cần thử tay.
4. **Chơi bằng tua thời gian, không phải thời gian thật.** Các mốc phút ở trên là phút mô phỏng.
   Cảm giác chờ đợi thật — thứ quyết định người chơi có chán trước robot không — chỉ đo được bằng
   người thật ngồi chơi.

Nói cách khác: checklist này bắt được lỗi kỹ thuật, **không thay thế được vòng playtest 3 người của
gate A**. Kế hoạch cũng nói đúng như vậy ở mục 11.
