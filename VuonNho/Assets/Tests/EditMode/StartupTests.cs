using NUnit.Framework;
using VuonNho.Core;

namespace VuonNho.Tests
{
    /// <summary>
    /// Bon he cua ban mo phong khoi nghiep tra: von va no, nong hoc va thoi vu, can lua, phap ly
    /// va phan nhanh kinh doanh.
    ///
    /// Trong tam la nhung thu de sai ma khong bao gi ca: mot khoan vay khong bao gio dong duoc vi
    /// lam tron chia deu, mot vong su kien quay tai cho vi hai dieu kien lech nhau, mot lan kiem
    /// tra siet chet mot nguoi choi chua kip biet luat, va tien ban tra cham lang le bien thanh
    /// tien mat.
    /// </summary>
    public sealed class StartupTests
    {
        ContentCatalog _catalog;
        FarmSimulation _simulation;
        long Cycle { get { return Finance.CycleMs(_catalog.Balance); } }

        [SetUp]
        public void SetUp()
        {
            _catalog = TestKit.Catalog();
            _simulation = new FarmSimulation(_catalog);
        }

        // ---- muc 1: von moi va vay ngan hang -----------------------------------------

        [Test]
        public void MotXuLaMotTramNghinDong()
        {
            // Ca bang tai chinh cua ban mo phong doc theo ti le nay. Doi no la doi het.
            Assert.AreEqual(10, _catalog.Balance.CoinsPerMillionVnd);
            Assert.AreEqual(200, _catalog.Balance.SeedCapitalMillionVnd);
            Assert.AreEqual(2000, _catalog.Balance.UnsecuredLoanCapCoins,
                            "Han muc tin chap phai bang 200 trieu.");
            Assert.AreEqual(3500, _catalog.Balance.SecuredLoanCapCoins,
                            "Han muc the chap phai bang 350 trieu.");
        }

        [Test]
        public void NghiaVuThangDauKhopBangMoPhong()
        {
            // Bang mo phong: L0 = 200 trieu, r = 8,5%/nam, n = 36 thang.
            // Goc thang 5.555.556 dong, lai thang dau 1.416.667 dong.
            long principal = Finance.PrincipalDue(2000, 36, 0);
            long interest = Finance.InterestDue(2000, 850);
            Assert.AreEqual(55, principal, "2000 xu chia 36 ky.");
            Assert.AreEqual(15, interest, "2000 xu, 8,5%/nam, mot thang — lam tron len.");
        }

        [Test]
        public void TongGocCuaCaKyHanDungBangGocBanDau()
        {
            // Chia deu roi lam tron xuong o moi ky se de lai vai xu khong bao gio duoc tra, va
            // khoan vay se khong bao gio dong duoc. Ky cuoi phai ganh phan le.
            foreach (int term in new[] { 12, 17, 23, 36 })
                foreach (long amount in new[] { 1000L, 1777L, 2000L, 3500L })
                {
                    long total = 0;
                    for (int month = 0; month < term; month++)
                        total += Finance.PrincipalDue(amount, term, month);
                    Assert.AreEqual(amount, total, "Goc bi that lac o " + amount + "/" + term);
                }
        }

        [Test]
        public void VayDaiHonThiLaiSuatCaoHon()
        {
            var offer = Finance.Offer(_catalog.Balance, LoanKind.Unsecured);
            Assert.AreEqual(650, Finance.RateBpsFor(offer, offer.MinTermMonths));
            Assert.AreEqual(850, Finance.RateBpsFor(offer, offer.MaxTermMonths));
            Assert.Less(Finance.RateBpsFor(offer, 12), Finance.RateBpsFor(offer, 36));
        }

        [Test]
        public void TienVayKhongPhaiDoanhThu()
        {
            var state = TestKit.NewState(_catalog);
            Finance.Borrow(_catalog.Balance, state, LoanKind.Unsecured, 2000, 36, 0);

            Assert.AreEqual(2000, state.Coins);
            Assert.AreEqual(0, state.CycleRevenueCoins,
                            "Vay khong lam vuon giau hon, no chi doi cuc tien lay chuoi nghia vu.");
        }

        [Test]
        public void VayTheChapCanGiayAnToanThucPham()
        {
            var state = TestKit.NewState(_catalog);
            string reason;
            Assert.IsFalse(Finance.CanBorrow(_catalog.Balance, state, LoanKind.Secured, 3000, 24, out reason));
            Assert.IsNotNull(reason);

            state.Compliance.FoodSafetyCertified = true;
            Assert.IsTrue(Finance.CanBorrow(_catalog.Balance, state, LoanKind.Secured, 3000, 24, out reason),
                          reason);
        }

        [Test]
        public void KhongVayHaiKhoanCungLuc()
        {
            var state = TestKit.NewState(_catalog);
            Finance.Borrow(_catalog.Balance, state, LoanKind.Unsecured, 1000, 12, 0);
            string reason;
            Assert.IsFalse(Finance.CanBorrow(_catalog.Balance, state, LoanKind.Unsecured, 500, 12, out reason));
        }

        [Test]
        public void TraDuMotKyThiDuNoGiamVaLichTienMotBuoc()
        {
            var state = TestKit.NewState(_catalog);
            Finance.Borrow(_catalog.Balance, state, LoanKind.Unsecured, 1200, 12, 0);
            state.Coins = 5000;

            _simulation.AdvanceTo(state, Cycle);

            Assert.AreEqual(1, state.Loan.MonthsPaid);
            Assert.AreEqual(1100, state.Loan.RemainingPrincipalCoins, "1200 chia 12 ky la 100 mot ky.");
            Assert.AreEqual(0, state.Loan.ConsecutiveShortfalls);
            Assert.Greater(state.Loan.InterestPaidCoins, 0);
        }

        [Test]
        public void BaKyLienKhongTraDuThiNganHangNiemPhong()
        {
            var state = TestKit.NewState(_catalog);
            Finance.Borrow(_catalog.Balance, state, LoanKind.Unsecured, 1200, 12, 0);
            state.Coins = 0;   // vay xong tieu het, khong con gi tra no

            _simulation.AdvanceTo(state, Cycle * 2 + Cycle / 2);
            Assert.IsFalse(state.Loan.Sealed, "Hai ky thieu thi chi la canh bao.");
            Assert.AreEqual(2, state.Loan.ConsecutiveShortfalls);

            _simulation.AdvanceTo(state, Cycle * 3 + Cycle / 2);
            Assert.IsTrue(state.Loan.Sealed, "Du ba ky lien la siet no.");
        }

        [Test]
        public void NiemPhongDongXuongVaQuayTraChuKhongDongVuon()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            session.State.Loan.Sealed = true;

            Assert.IsFalse(_simulation.CanStartBatchNow(session.State), "Quầy trà phải dừng.");
            Assert.IsFalse(_simulation.FactoryAllowed(session.State, session.State.SimulationTimeMs),
                           "Xưởng phải dừng.");
            Assert.IsTrue(session.Plant(0, DefaultContent.CropMint).Success,
                          "Vẫn phải gieo được bằng tay, nếu không thì không còn đường trả nợ.");
        }

        [Test]
        public void BiNiemPhongVoiKhoRongVanConDuongTraNo()
        {
            // Bai test quan trong nhat cua ca he: mot cai bay khong loi ra thi khong day duoc
            // nguoi choi lam gi ca. Het sach xu, kho rong, dang bi niem phong — van phai co
            // duong ve, du la duong cham nhat.
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            var loan = session.State.Loan;
            loan.Kind = LoanKind.Unsecured;
            loan.PrincipalCoins = 200;
            loan.RemainingPrincipalCoins = 200;
            loan.TermMonths = 12;
            loan.Sealed = true;
            session.State.Coins = 0;
            foreach (var itemId in _catalog.Items) session.State.Inventory[itemId] = 0;

            long guard = 0;
            while (session.State.Coins < Finance.PayoffAmount(session.State.Loan) && guard++ < 400)
            {
                for (int i = 0; i < session.State.Plots.Count; i++)
                {
                    var plot = session.State.Plot(i);
                    if (!plot.Unlocked) continue;
                    if (plot.Phase == PlotPhase.Empty) session.Plant(i, DefaultContent.CropMint);
                    else if (plot.Phase == PlotPhase.Ready) session.HarvestAndReplant(i);
                }
                session.SellAllRaw(DefaultContent.CropMint);
                session.DebugAdvance(10000);
            }

            Assert.GreaterOrEqual(session.State.Coins, Finance.PayoffAmount(session.State.Loan),
                                  "Hái tay và bán lá tươi phải gom đủ tiền trả nợ.");
            Assert.IsTrue(session.RepayLoan(Finance.PayoffAmount(session.State.Loan)).Success);
            Assert.IsFalse(session.State.Loan.Sealed, "Trả hết nợ là thảo niêm phong.");
        }

        [Test]
        public void TraHetNoThiThaoNiemPhong()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            var loan = session.State.Loan;
            loan.Kind = LoanKind.Unsecured;
            loan.PrincipalCoins = 500;
            loan.RemainingPrincipalCoins = 500;
            loan.TermMonths = 12;
            loan.OverdueCoins = 20;
            loan.Sealed = true;
            session.State.Coins = 1000;

            var result = session.RepayLoan(520);

            Assert.IsTrue(result.Success, result.FailureReason);
            Assert.IsFalse(session.State.Loan.Sealed);
            Assert.AreEqual(LoanKind.None, session.State.Loan.Kind);
            Assert.IsTrue(session.Plant(0, DefaultContent.CropMint).Success,
                          "Thao niem phong roi thi gieo duoc ngay.");
        }

        [Test]
        public void LichSuDongTienChiGiuMuoiHaiKyGanNhat()
        {
            var state = TestKit.NewState(_catalog);
            _simulation.AdvanceTo(state, Cycle * 20);

            Assert.AreEqual(_catalog.Balance.CashHistoryCycles, state.CashHistory.Count);
            Assert.AreEqual(20, state.CashHistory[state.CashHistory.Count - 1].Month,
                            "Ban ghi cuoi phai la ky vua chot, khong phai ky dau.");
        }

        [Test]
        public void BanDuBaoMuoiHaiKyKhopVoiCachTraNoThat()
        {
            // Do thi trong bang mo phong phai la cung mot phep tinh voi so tien that bi tru khi
            // den han. Lech nhau thi nguoi choi quyet dinh vay dua tren mot con so khong that.
            var state = TestKit.NewState(_catalog);
            Finance.Borrow(_catalog.Balance, state, LoanKind.Unsecured, 1200, 12, 0);
            state.Coins = 1000000;
            // Lai suat lay tu chinh khoan vay: vay 12 thang duoc muc thap nhat cua khoang, khong
            // phai muc cao nhat. Ghi cung 850 vao day la du bao mot khoan vay khac.
            var rows = Finance.Project(1200, state.Loan.AnnualRateBps, 12, 400, 200, 12);

            for (int month = 0; month < 12; month++)
            {
                long before = state.Coins;
                _simulation.AdvanceTo(state, Cycle * (month + 1));
                Assert.AreEqual(rows[month].DebtServiceCoins, before - state.Coins,
                                "Nghia vu ky " + (month + 1) + " lech giua du bao va thuc te.");
                Assert.AreEqual(rows[month].DebtRemainingCoins, state.Loan.RemainingPrincipalCoins,
                                "Du no ky " + (month + 1) + " lech.");
            }
            Assert.AreEqual(0, state.Loan.RemainingPrincipalCoins, "Het ky han la phai dong khoan vay.");
        }

        // ---- muc 2: nong hoc, thoi vu va ray xanh ------------------------------------

        [Test]
        public void BonPhanHangThuHaiChoDoanhThuGanBangNhau()
        {
            // Day la ket qua that cua bang trong ban mo phong, khong phai mot su lam cho de: cai
            // khac nhau giua bon phan hang la **so luong hang phai chay qua day chuyen**, khong
            // phai so tien kiem duoc.
            long lowest = long.MaxValue, highest = 0;
            foreach (var grade in Agronomy.Grades())
            {
                var crop = _catalog.Crop(grade.CropId);
                var recipe = _catalog.RecipeForCrop(grade.CropId);
                long perHarvest = (long)crop.Yield * recipe.PackedOutputCoins / recipe.PackedInputCount;
                if (perHarvest < lowest) lowest = perHarvest;
                if (perHarvest > highest) highest = perHarvest;
            }
            Assert.Less(highest * 100 / lowest, 130,
                        "Doanh thu mot vu cua bon phan hang khong duoc lech qua 30%.");
        }

        [Test]
        public void HaiNonHonThiItHangHonHan()
        {
            long xo = _catalog.Crop(DefaultContent.CropTeaXo).Yield;
            long dinh = _catalog.Crop(DefaultContent.CropTeaDinh).Yield;
            Assert.Greater(xo / dinh, 10, "Bup xo phai cho gap nhieu lan so hang cua dinh tra.");
        }

        [Test]
        public void BangSinhHoaDoiHuongTheoNhietDo()
        {
            var spring = Agronomy.Profile(Season.Xuan);
            var summer = Agronomy.Profile(Season.Ha);
            Assert.Greater(spring.TheaninePermille, summer.TheaninePermille,
                           "Mat thi cay tich theanine — vi ngot em.");
            Assert.Greater(summer.PolyphenolPermille, spring.PolyphenolPermille,
                           "Nong thi cay tich polyphenol — vi chat gat.");
            Assert.Greater(summer.YieldPercent, spring.YieldPercent, "He nhieu bup hon xuan.");
            Assert.Greater(spring.PricePercent, summer.PricePercent, "Tra xuan dat hon tra he.");
            Assert.IsTrue(Agronomy.Profile(Season.Dong).Dormant, "Vu dong la vu che ngu dong.");
        }

        [Test]
        public void MuaDoiCaNangSuatVaGiaCuaChe()
        {
            var state = TestKit.NewState(_catalog);
            state.UnlockedCropIds.Add(DefaultContent.CropTeaXo);
            var plot = state.Plot(0);
            plot.Fertility = 100;

            long springStart = _catalog.Balance.SeasonLengthMs * 4;      // dau mot vu xuan
            long summerStart = springStart + _catalog.Balance.SeasonLengthMs;
            state.SimulationTimeMs = springStart;
            _simulation.Plant(state, plot, DefaultContent.CropTeaXo, springStart);
            int springYield = plot.PendingYield;

            plot.Phase = PlotPhase.Empty;
            plot.Fertility = 100;
            state.SimulationTimeMs = summerStart;
            _simulation.Plant(state, plot, DefaultContent.CropTeaXo, summerStart);
            int summerYield = plot.PendingYield;

            Assert.Greater(summerYield, springYield, "He phai nhieu bup hon xuan.");

            var recipe = _catalog.Recipe(DefaultContent.RecipeTeaXo);
            Assert.Greater(_simulation.SalePricePercent(state, recipe, springStart),
                           _simulation.SalePricePercent(state, recipe, summerStart),
                           "Tra xuan phai duoc gia hon tra he.");
        }

        [Test]
        public void TraThaoMocKhongChiuBangSinhHoaCuaChe()
        {
            var state = TestKit.NewState(_catalog);
            var recipe = _catalog.Recipe(DefaultContent.RecipeMint);
            long spring = _catalog.Balance.SeasonLengthMs * 4;
            long summer = spring + _catalog.Balance.SeasonLengthMs;
            Assert.AreEqual(100, _simulation.SalePricePercent(state, recipe, spring));
            Assert.AreEqual(100, _simulation.SalePricePercent(state, recipe, summer),
                            "Khong ai sao diet men mot bong bac ha.");
        }

        [Test]
        public void RayXanhChiChichHutVaoMuaRay()
        {
            int rayHits = 0, winterHits = 0;
            for (int plotId = 0; plotId < 200; plotId++)
            {
                if (Agronomy.LeafhopperStrikes(1234, plotId, 1, Season.Xuan, 22)) rayHits++;
                if (Agronomy.LeafhopperStrikes(1234, plotId, 1, Season.Dong, 22)) winterHits++;
            }
            Assert.Greater(rayHits, 0, "Cuoi xuan dau he phai co ray xanh.");
            Assert.AreEqual(0, winterHits, "Vu dong va vu thu khong co ray xanh.");
        }

        [Test]
        public void OBiRayXanhThiThuVeLaRayXanhChuKhongPhaiBupChe()
        {
            var state = TestKit.NewState(_catalog);
            state.UnlockedCropIds.Add(DefaultContent.CropTeaMocCau);
            var plot = state.Plot(0);
            plot.Fertility = 100;
            _simulation.Plant(state, plot, DefaultContent.CropTeaMocCau, 0);
            plot.Leafhopper = true;
            plot.Phase = PlotPhase.Ready;

            _simulation.HarvestAndReplant(state, plot, 0, false);

            Assert.Greater(state.InventoryOf(DefaultContent.CropOrientalBeauty), 0,
                           "Kho phai co dong la ray xanh — do la thu duy nhat noi cho nguoi choi biet.");
            Assert.AreEqual(0, state.InventoryOf(DefaultContent.CropTeaMocCau));
        }

        [Test]
        public void DuocRayXanhThiVuDoKhongBiSauBenh()
        {
            // Hai cai do la cung mot con vat, chi khac muc do. Mot vu vua duoc mon qua vua mat
            // trang la mot ket qua khong ai doc ra duoc.
            var state = TestKit.NewState(_catalog);
            state.UnlockedCropIds.Add(DefaultContent.CropTeaXo);
            state.PestSeed = 77;
            int leafhopperCycles = 0;

            var plot = state.Plot(0);
            for (int cycle = 0; cycle < 60; cycle++)
            {
                plot.Phase = PlotPhase.Empty;
                plot.Fertility = 100;
                _simulation.Plant(state, plot, DefaultContent.CropTeaXo, 0);
                if (!plot.Leafhopper) continue;
                leafhopperCycles++;
                Assert.IsFalse(plot.PestPending, "Vu duoc ray xanh khong duoc dinh sau benh.");
            }
            Assert.Greater(leafhopperCycles, 0, "Phai co it nhat mot vu bi ray xanh trong 60 vu.");
        }

        [Test]
        public void KhongGieoDuocDongPhuongMyNhan()
        {
            var state = TestKit.NewState(_catalog);
            Assert.IsFalse(state.UnlockedCropIds.Contains(DefaultContent.CropOrientalBeauty),
                           "La ray xanh chi sinh ra tu su kien, khong ai gieo ra no duoc.");
        }

        // ---- muc 3: dong hoc che bien ------------------------------------------------

        [Test]
        public void DungChuanThiDuocMeThuongHang()
        {
            var verdict = Crafting.Evaluate(Crafting.DefaultsFor(TeaRoute.Green), false, 130, 55);
            Assert.AreEqual(BatchQuality.Premium, verdict.Quality);
            Assert.AreEqual(130, verdict.PricePercent);
            Assert.AreEqual("Xanh trong", verdict.Liquor);
        }

        [Test]
        public void NhietThapThiPpoConSongVaMeBiLoi()
        {
            var settings = Crafting.DefaultsFor(TeaRoute.Green);
            settings.FixTempC = 180;   // duoi ca khoang cham nhan duoc
            var verdict = Crafting.Evaluate(settings, false, 130, 55);

            Assert.AreEqual(BatchQuality.Flawed, verdict.Quality);
            Assert.AreEqual(55, verdict.PricePercent);
            StringAssert.Contains("PPO", verdict.Note, "Loi phai noi ro la loi gi.");
        }

        [Test]
        public void DoAmTrenNamPhanTramLaKhongDeDuoc()
        {
            var settings = Crafting.DefaultsFor(TeaRoute.Green);
            settings.MoisturePermille = 70;
            var verdict = Crafting.Evaluate(settings, false, 130, 55);
            Assert.AreEqual(BatchQuality.Flawed, verdict.Quality);
        }

        [Test]
        public void LechKhoiChuanNhungConTrongKhoangThiVanBanDuoc()
        {
            var settings = Crafting.DefaultsFor(TeaRoute.Green);
            settings.RollMinutes = 13;   // ngoai 15–20, trong 11–26
            var verdict = Crafting.Evaluate(settings, false, 130, 55);
            Assert.AreEqual(BatchQuality.Standard, verdict.Quality);
            Assert.AreEqual(100, verdict.PricePercent);
        }

        [Test]
        public void DongPhuongMyNhanCanCaLaRayXanhVaOxyHoaDungKhoang()
        {
            var settings = Crafting.DefaultsFor(TeaRoute.OrientalBeauty);
            Assert.IsTrue(settings.OxidationPercent >= 60 && settings.OxidationPercent <= 75,
                          "Mac dinh cua duong nay phai nam trong 60–75%.");

            var withLeaves = Crafting.Evaluate(settings, true, 130, 55);
            Assert.IsTrue(withLeaves.OrientalBeauty);
            Assert.AreEqual(130, Crafting.BatchPricePercent(withLeaves, true, 20));

            var withoutLeaves = Crafting.Evaluate(settings, false, 130, 55);
            Assert.IsFalse(withoutLeaves.OrientalBeauty,
                           "Cai lam nen no la con ray, khong phai cai nut oxy hoa.");
        }

        [Test]
        public void LaRayXanhSaoTheoDuongTraXanhThiMatGanHetGia()
        {
            var green = Crafting.DefaultsFor(TeaRoute.Green);
            var verdict = Crafting.Evaluate(green, true, 130, 55);
            Assert.IsFalse(verdict.OrientalBeauty);
            Assert.AreEqual(130 * 20 / 100, Crafting.BatchPricePercent(verdict, true, 20));
        }

        [Test]
        public void DoiDuongCheBienThiBonNutVeChuanCuaDuongMoi()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);

            Assert.IsTrue(session.SetCraftRoute(TeaRoute.Black).Success);
            var verdict = Crafting.Evaluate(session.State.Craft, false, 130, 55);
            Assert.AreEqual(BatchQuality.Premium, verdict.Quality,
                            "Doi duong xong phai dung chuan ngay, khong giu lai so cua duong cu.");
            Assert.AreEqual(TeaRoute.Black, session.State.Craft.Route);
        }

        [Test]
        public void MeLoiLamGiamTienThatCuaMotMeTra()
        {
            var state = TestKit.NewState(_catalog);
            state.UnlockedCropIds.Add(DefaultContent.CropTeaXo);
            state.UnlockedRecipeIds.Add(DefaultContent.RecipeTeaXo);
            var recipe = _catalog.Recipe(DefaultContent.RecipeTeaXo);

            int good = _simulation.SalePricePercent(state, recipe, 0);
            state.Craft.FixTempC = 180;
            int bad = _simulation.SalePricePercent(state, recipe, 0);
            Assert.Less(bad, good, "Can lua sai phai lam giam tien that, khong chi doi mot cau chu.");
        }

        // ---- muc 4: phap ly, an toan thuc pham va xuong mot chieu ---------------------

        [Test]
        public void HoHongGiuaDayChuyenLaViPhamMotChieu()
        {
            var state = TestKit.NewState(_catalog);
            // Mua chang 1 va chang 3, bo trong chang 2: nguyen lieu phai vong nguoc lai.
            state.Station(_catalog.Stages[0].Id).Owned = true;
            state.Station(_catalog.Stages[2].Id).Owned = true;
            Assert.IsFalse(Compliance.OneWayRespected(_catalog, state));

            state.Station(_catalog.Stages[1].Id).Owned = true;
            Assert.IsTrue(Compliance.OneWayRespected(_catalog, state),
                          "Mua thieu may cuoi day chuyen chi la chua xay xong, khong phai vi pham.");
        }

        [Test]
        public void LanKiemTraDauChiNhacRoiLanSauMoiPhat()
        {
            var state = TestKit.NewState(_catalog);
            for (int i = 0; i < state.Stations.Count; i++) state.Stations[i].Owned = true;
            state.Coins = 5000;
            long period = Cycle * _catalog.Balance.InspectionEveryCycles;

            _simulation.AdvanceTo(state, period + 1);
            Assert.IsTrue(state.Compliance.LicenceWarned, "Lan dau phai la mot loi nhac.");
            Assert.AreEqual(0, state.Compliance.TotalFinesCoins);
            Assert.IsFalse(state.Compliance.Suspended(state.SimulationTimeMs));

            _simulation.AdvanceTo(state, period * 2 + 1);
            Assert.Greater(state.Compliance.TotalFinesCoins, 0, "Ky sau la phat that.");
            Assert.IsTrue(state.Compliance.Suspended(state.SimulationTimeMs),
                          "Hoat dong khi chua co giay an toan thuc pham thi bi dinh chi.");
        }

        [Test]
        public void DinhChiThiDongXuongChuKhongDongVuon()
        {
            var state = TestKit.NewState(_catalog);
            TestKit.OpenWholeChain(state, _catalog, 6);
            state.Compliance.SuspendedUntilMs = 999999;
            state.AddInventory(DefaultContent.CropMint, 100);

            Assert.IsFalse(_simulation.FactoryAllowed(state, 0));
            Assert.IsFalse(_simulation.CanStartAnyStation(state), "Xuong phai dung.");

            state.UnlockedCropIds.Add(DefaultContent.CropMint);
            var plot = state.Plot(0);
            _simulation.Plant(state, plot, DefaultContent.CropMint, 0);
            Assert.AreEqual(PlotPhase.Growing, plot.Phase, "Vuon van lon binh thuong.");
        }

        [Test]
        public void KhongCoXuongThiKhongAiDenKiemTra()
        {
            var state = TestKit.NewState(_catalog);
            _simulation.AdvanceTo(state, Cycle * 12);
            Assert.AreEqual(0, state.Compliance.InspectionCount,
                            "Bon o dat va chua xay gi thi khong co xuong nao de kiem.");
            Assert.AreEqual(0, state.Compliance.TotalFinesCoins);
        }

        [Test]
        public void ThieuDoBaoHoThiKyNaoCungBiPhat()
        {
            var state = TestKit.NewState(_catalog);
            for (int i = 0; i < state.Stations.Count; i++) state.Stations[i].Owned = true;
            state.Compliance.FoodSafetyCertified = true;
            state.HiredWorkers = 2;

            var result = Compliance.Inspect(_catalog, state, 0);
            Assert.Contains(Compliance.ViolationGear, result.ViolationIds);

            state.Compliance.ProtectiveGear = true;
            Assert.IsTrue(Compliance.Inspect(_catalog, state, 0).Clean);
        }

        [Test]
        public void HoKinhDoanhDongThueTrenDoanhThuConCongTyDongTrenLoiNhuan()
        {
            // Bien loi nhuan mong thi thue khoan an nang hon; bien day thi TNHH dat hon. Do la ly
            // do doi hinh thuc phap ly khong phai mot buoc nang cap mot chieu.
            long thinRevenue = 1000, thinExpense = 950;
            Assert.Greater(Compliance.TaxFor(BusinessEntity.Hkd, thinRevenue, thinExpense),
                           Compliance.TaxFor(BusinessEntity.Tnhh, thinRevenue, thinExpense),
                           "Bien mong: thue khoan tren doanh thu nang hon.");

            long fatRevenue = 1000, fatExpense = 200;
            Assert.Greater(Compliance.TaxFor(BusinessEntity.Tnhh, fatRevenue, fatExpense),
                           Compliance.TaxFor(BusinessEntity.Hkd, fatRevenue, fatExpense),
                           "Bien day: 20% loi nhuan cong GTGT nang hon.");
        }

        [Test]
        public void KhongDangKyThiKhongPhaiDongThue()
        {
            Assert.AreEqual(0, Compliance.TaxFor(BusinessEntity.None, 5000, 100));
        }

        [Test]
        public void ThueDuocTruKhiChotKy()
        {
            var state = TestKit.NewState(_catalog);
            state.Compliance.Entity = BusinessEntity.Hkd;
            state.EarnCoins(1000);

            _simulation.AdvanceTo(state, Cycle);

            Assert.AreEqual(45, state.Compliance.TotalTaxCoins, "Thue khoan 4,5% cua 1000 xu.");
            Assert.AreEqual(955, state.Coins);
        }

        [Test]
        public void HoSoPhapLyMatMaySoNgayMoiCoHieuLuc()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            session.State.Coins = 5000;

            Assert.IsTrue(session.RegisterEntity(BusinessEntity.Hkd).Success);
            Assert.AreEqual(BusinessEntity.None, session.State.Compliance.Entity,
                            "Nop ho so xong chua co giay ngay.");
            Assert.AreEqual(BusinessEntity.Hkd, session.State.Compliance.PendingEntity);

            long ready = session.State.Compliance.EntityReadyAtMs;
            long minMs = 3 * Compliance.DayMs(_catalog.Balance);
            long maxMs = 5 * Compliance.DayMs(_catalog.Balance);
            Assert.IsTrue(ready >= minMs && ready <= maxMs, "Tham dinh phai nam trong 3–5 ngay.");
        }

        [Test]
        public void KhongXinGiayAnToanTruocKhiDangKyKinhDoanh()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            session.State.Coins = 5000;
            string reason;
            Assert.IsFalse(session.CanApplyFoodSafety(out reason));
            Assert.IsNotNull(reason);
        }

        [Test]
        public void NamKhuXuongMotChieuTheoDungThuTu()
        {
            var zones = Compliance.Zones();
            Assert.AreEqual(5, zones.Count);
            for (int i = 0; i < zones.Count; i++)
                Assert.AreEqual(i + 1, zones[i].Order, "Thu tu khu phai lien tuc tu 1.");
            Assert.IsNull(zones[zones.Count - 1].StageId, "Kho thanh pham khong co may.");
        }

        // ---- muc 5: phan nhanh kinh doanh --------------------------------------------

        [Test]
        public void BaNhanhDanhDoiNguocNhau()
        {
            var bulk = Branches.Definition(BusinessBranch.BulkB2B);
            var dtc = Branches.Definition(BusinessBranch.ArtisanalDtc);
            var stay = Branches.Definition(BusinessBranch.Farmstay);

            Assert.Less(bulk.PricePercent, dtc.PricePercent, "B2B bien mong hon DTC.");
            Assert.Less(bulk.PayoutDelayDays, dtc.PayoutDelayDays, "B2B thu tien nhanh hon DTC.");
            Assert.Greater(stay.UnlockCostCoins, bulk.UnlockCostCoins, "Du lich CAPEX cao nhat.");
            Assert.IsTrue(stay.WinterIncome, "Du lich la loi ra cho mua che ngu dong.");
            Assert.IsTrue(bulk.SellsUnpacked, "B2B ban tra moc, khong can may dong goi.");
        }

        [Test]
        public void BanLeThuCongTraTienChamChuKhongVaoTuiNgay()
        {
            var state = TestKit.NewState(_catalog);
            TestKit.OpenWholeChain(state, _catalog, 6);
            state.UnlockedBranches.Add(BusinessBranch.ArtisanalDtc);
            state.SalesChannel = BusinessBranch.ArtisanalDtc;
            state.Machine.SelectedRecipeId = DefaultContent.RecipeMint;
            state.AddInventory(ProcessChain.ItemId(DefaultContent.CropMint, ProcessChain.SuffixPacked),
                               _catalog.Recipe(DefaultContent.RecipeMint).PackedInputCount);

            long brew = _simulation.BrewMsFor(state, DefaultContent.RecipeMint);
            _simulation.AdvanceTo(state, brew + 10);

            Assert.AreEqual(0, state.Coins, "Hang ban roi ma tien chua ve tay.");
            Assert.AreEqual(1, state.Receivables.Count);
            Assert.Greater(state.Receivables[0].AmountCoins, 0);

            _simulation.AdvanceTo(state, state.Receivables[0].DueAtMs + Cycle);
            Assert.Greater(state.Coins, 0, "Den han thi tien phai ve.");
            Assert.AreEqual(0, state.Receivables.Count);
        }

        [Test]
        public void GiaCongThoBanDuocTraMocKhongCanMayDongGoi()
        {
            var state = TestKit.NewState(_catalog);
            // Mua het tru may dong goi: dung tinh huong cua nhanh B2B.
            for (int i = 0; i < _catalog.Stages.Count - 1; i++)
                state.Station(_catalog.Stages[i].Id).Owned = true;
            state.HiredWorkers = 6;
            state.StaffedWorkers = 6;
            state.Compliance.FoodSafetyCertified = true;
            state.Compliance.ProtectiveGear = true;
            state.Machine.SelectedRecipeId = DefaultContent.RecipeMint;

            var recipe = _catalog.Recipe(DefaultContent.RecipeMint);
            string bulkItem = _simulation.BulkItemFor(recipe);
            state.AddInventory(bulkItem, recipe.PackedInputCount);

            // Chua mo nhanh B2B: tra moc khong ban duoc o quay.
            Assert.IsNull(_simulation.ResolveSale(state, recipe, 0),
                          "Chua mo nhanh thi tra moc chua phai mot mat hang ban duoc.");

            state.UnlockedBranches.Add(BusinessBranch.BulkB2B);
            state.SalesChannel = BusinessBranch.BulkB2B;
            var sale = _simulation.ResolveSale(state, recipe, 0);
            Assert.IsNotNull(sale);
            Assert.AreEqual(bulkItem, sale.ItemId);
            Assert.AreEqual(recipe.PackedOutputCoins * 65 / 100, sale.Coins,
                            "Gia cua hang dong goi, chiu he so kenh 65%.");
        }

        [Test]
        public void DuLichThuTienTheoCanhQuanVaThuCaMuaDong()
        {
            var state = TestKit.NewState(_catalog);
            Assert.AreEqual(0, Branches.FarmstayIncome(_catalog.Balance, state),
                            "Chua mo nhanh thi khong co dong nao.");

            state.UnlockedBranches.Add(BusinessBranch.Farmstay);
            long bare = Branches.FarmstayIncome(_catalog.Balance, state);
            state.Decorations.Add(new PlacedDecoration());
            state.Decorations.Add(new PlacedDecoration());
            Assert.Greater(Branches.FarmstayIncome(_catalog.Balance, state), bare,
                           "Tai dau tu canh quan phai lam tang tien that.");
        }

        [Test]
        public void DuLichCanCanhQuanTruocKhiMoDuoc()
        {
            var state = TestKit.NewState(_catalog);
            state.Coins = 100000;
            string reason;
            Assert.IsFalse(Branches.CanUnlock(_catalog, state, BusinessBranch.Farmstay, out reason));
            StringAssert.Contains("cảnh quan", reason);

            for (int i = 0; i < Branches.FarmstayDecorationsRequired; i++)
                state.Decorations.Add(new PlacedDecoration());
            Assert.IsTrue(Branches.CanUnlock(_catalog, state, BusinessBranch.Farmstay, out reason), reason);
        }

        [Test]
        public void BanLeThuCongCanGiayAnToanVaMayDongGoi()
        {
            var state = TestKit.NewState(_catalog);
            state.Coins = 100000;
            string reason;
            Assert.IsFalse(Branches.CanUnlock(_catalog, state, BusinessBranch.ArtisanalDtc, out reason));

            state.Compliance.FoodSafetyCertified = true;
            Assert.IsFalse(Branches.CanUnlock(_catalog, state, BusinessBranch.ArtisanalDtc, out reason));

            state.Station(DefaultStages.Pack).Owned = true;
            Assert.IsTrue(Branches.CanUnlock(_catalog, state, BusinessBranch.ArtisanalDtc, out reason), reason);
        }

        [Test]
        public void LoTrinhBaGiaiDoanTienTheoNhanhDaMo()
        {
            var state = TestKit.NewState(_catalog);
            Assert.AreEqual(1, Branches.PhaseOf(state));
            state.UnlockedBranches.Add(BusinessBranch.ArtisanalDtc);
            Assert.AreEqual(2, Branches.PhaseOf(state));
            state.UnlockedBranches.Add(BusinessBranch.Farmstay);
            Assert.AreEqual(3, Branches.PhaseOf(state));
        }

        [Test]
        public void DuLichKhongPhaiMotKenhBanTra()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            session.State.UnlockedBranches.Add(BusinessBranch.Farmstay);
            Assert.IsFalse(session.SetSalesChannel(BusinessBranch.Farmstay).Success);
        }

        // ---- muc 6: doan mo man -------------------------------------------------------

        [Test]
        public void KichBanMoManCoBonPhanCanhDungNhip()
        {
            var scenes = IntroStoryboard.Scenes();
            Assert.AreEqual(4, scenes.Count);
            for (int i = 0; i < scenes.Count; i++)
            {
                Assert.AreEqual(i + 1, scenes[i].Index);
                Assert.Greater(scenes[i].DurationMs, 0);
                Assert.IsNotEmpty(scenes[i].Subtitle);
                Assert.IsNotEmpty(scenes[i].Visual);
                Assert.IsNotEmpty(scenes[i].Audio);
            }
            Assert.AreEqual(65000, IntroStoryboard.TotalDurationMs(),
                            "Kich ban dai 1 phut 5 giay — nhip cua ban dung phim.");
        }

        // ---- tinh nhat quan cua mo phong ---------------------------------------------

        [Test]
        public void MotBuocDaiBangNhieuBuocNgan_CoTaiChinhVaPhapLy()
        {
            // Bai test quan trong nhat cua ca nhom: bon he moi deu them deadline vao vong su
            // kien, va mot deadline bi bo sot chi hien ra khi chay bu offline — dung luc nguoi
            // choi vang mat va khong ai nhin thay no sai.
            var oneStep = BusyState();
            var reference = BusyState();

            _simulation.AdvanceTo(oneStep, Cycle * 9);
            for (int i = 1; i <= 9 * 20; i++)
                _simulation.AdvanceTo(reference, Cycle * i / 20);

            Assert.AreEqual(reference.Coins, oneStep.Coins, "So xu lech giua hai duong chay.");
            Assert.AreEqual(reference.Loan.RemainingPrincipalCoins, oneStep.Loan.RemainingPrincipalCoins);
            Assert.AreEqual(reference.Loan.MonthsPaid, oneStep.Loan.MonthsPaid);
            Assert.AreEqual(reference.Loan.OverdueCoins, oneStep.Loan.OverdueCoins);
            Assert.AreEqual(reference.Compliance.TotalFinesCoins, oneStep.Compliance.TotalFinesCoins);
            Assert.AreEqual(reference.Compliance.TotalTaxCoins, oneStep.Compliance.TotalTaxCoins);
            Assert.AreEqual(reference.CashHistory.Count, oneStep.CashHistory.Count);
            Assert.AreEqual(reference.Receivables.Count, oneStep.Receivables.Count);
        }

        /// <summary>Mot van dang chay het cong suat: co vay, co xuong, co thue, co kenh tra cham.</summary>
        GameState BusyState()
        {
            var state = TestKit.NewState(_catalog);
            TestKit.OpenWholeChain(state, _catalog, 6);
            state.UnlockedCropIds.Add(DefaultContent.CropTeaXo);
            state.UnlockedRecipeIds.Add(DefaultContent.RecipeTeaXo);
            state.Compliance.Entity = BusinessEntity.Hkd;
            state.UnlockedBranches.Add(BusinessBranch.ArtisanalDtc);
            state.UnlockedBranches.Add(BusinessBranch.Farmstay);
            state.SalesChannel = BusinessBranch.ArtisanalDtc;
            state.Machine.SelectedRecipeId = DefaultContent.RecipeTeaXo;
            state.Coins = 400;
            Finance.Borrow(_catalog.Balance, state, LoanKind.Unsecured, 1200, 12, 0);
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                if (!plot.Unlocked) continue;
                plot.Fertility = 100;
                plot.NextCropId = DefaultContent.CropTeaXo;
                _simulation.Plant(state, plot, DefaultContent.CropTeaXo, 0);
            }
            return state;
        }

        // ---- save --------------------------------------------------------------------

        [Test]
        public void SaveGiuDuBonHeMoi()
        {
            var state = BusyState();
            _simulation.AdvanceTo(state, Cycle * 5);
            state.Craft = Crafting.DefaultsFor(TeaRoute.OrientalBeauty);
            state.Compliance.FoodSafetyCertified = true;
            state.Compliance.LicenceWarned = true;
            state.IntroSeen = true;

            string json = SaveSerializer.Write(
                new SaveSnapshot { BalanceVersion = _catalog.Balance.Version, BuildId = "test", State = state },
                _catalog, false);
            var loaded = SaveSerializer.Read(json, _catalog).State;

            Assert.AreEqual(state.Loan.Kind, loaded.Loan.Kind);
            Assert.AreEqual(state.Loan.RemainingPrincipalCoins, loaded.Loan.RemainingPrincipalCoins);
            Assert.AreEqual(state.Loan.AnnualRateBps, loaded.Loan.AnnualRateBps);
            Assert.AreEqual(state.Loan.MonthsPaid, loaded.Loan.MonthsPaid);
            Assert.AreEqual(state.Compliance.Entity, loaded.Compliance.Entity);
            Assert.AreEqual(state.Compliance.FoodSafetyCertified, loaded.Compliance.FoodSafetyCertified);
            Assert.AreEqual(state.Compliance.LicenceWarned, loaded.Compliance.LicenceWarned);
            Assert.AreEqual(state.Compliance.TotalTaxCoins, loaded.Compliance.TotalTaxCoins);
            Assert.AreEqual(TeaRoute.OrientalBeauty, loaded.Craft.Route);
            Assert.AreEqual(state.Craft.OxidationPercent, loaded.Craft.OxidationPercent);
            Assert.AreEqual(state.SalesChannel, loaded.SalesChannel);
            Assert.AreEqual(state.UnlockedBranches.Count, loaded.UnlockedBranches.Count);
            Assert.AreEqual(state.Receivables.Count, loaded.Receivables.Count);
            Assert.AreEqual(state.CashHistory.Count, loaded.CashHistory.Count);
            Assert.AreEqual(state.NextCycleCloseAtMs, loaded.NextCycleCloseAtMs);
            Assert.IsTrue(loaded.IntroSeen);
        }

        [Test]
        public void HaiLanGhiCungMotStateChoCungMotChuoiByte()
        {
            var state = BusyState();
            _simulation.AdvanceTo(state, Cycle * 4);
            var snapshot = new SaveSnapshot
            {
                BalanceVersion = _catalog.Balance.Version, BuildId = "test", State = state
            };
            Assert.AreEqual(SaveSerializer.Write(snapshot, _catalog, false),
                            SaveSerializer.Write(snapshot, _catalog, false));
        }

        [Test]
        public void SaveCuaBanTruocDocLenLaMotVanHopLe()
        {
            // Schema 5 khong co truong nao cua bon he moi. Mac dinh phai la trang thai ban dau:
            // khong vay, chua dang ky, chua mo nhanh nao — chu khong phai mot van bi khoa.
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            session.State.Coins = 300;
            session.SaveNow();

            string legacy = repository.Main
                .Replace("\"schemaVersion\": 6", "\"schemaVersion\": 5");
            var loaded = SaveSerializer.Read(legacy, _catalog).State;

            Assert.AreEqual(LoanKind.None, loaded.Loan.Kind);
            Assert.IsFalse(loaded.Loan.Sealed);
            Assert.AreEqual(BusinessEntity.None, loaded.Compliance.Entity);
            Assert.AreEqual(BusinessBranch.None, loaded.SalesChannel);
            Assert.Greater(loaded.NextCycleCloseAtMs, 0, "Ky dau phai co mot moc, khong thi he tai chinh nam im.");
        }

        [Test]
        public void SaveBiSuaTayVoiThongSoVoLyKhongLamMeTraKhongDanhGiaDuoc()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            session.SaveNow();
            string tampered = repository.Main.Replace("\"fixTempC\": 255", "\"fixTempC\": 99999");

            var loaded = SaveSerializer.Read(tampered, _catalog).State;
            var windows = Crafting.Windows(loaded.Craft.Route);
            Assert.LessOrEqual(loaded.Craft.FixTempC, windows[0].Maximum);
        }
    }
}
