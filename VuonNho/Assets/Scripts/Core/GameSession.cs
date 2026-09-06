using System;
using System.Collections.Generic;

namespace VuonNho.Core
{
    public interface IClock
    {
        /// <summary>Thoi gian UTC dung cho quang nghi offline.</summary>
        long UtcNowMs { get; }
        /// <summary>Dong ho khong lui, dung trong phien de tranh anh huong cua chinh gio he thong.</summary>
        long MonotonicMs { get; }
    }

    public interface ITestLogger
    {
        /// <summary>fields la cac cap key, value lien tiep.</summary>
        void Log(string eventName, params string[] fields);
    }

    public sealed class SaveCandidate
    {
        public string Name;
        public string Json;
        public string ReadError;
    }

    public interface ISaveRepository
    {
        bool HasSave { get; }
        /// <summary>Theo thu tu uu tien: file chinh roi den backup.</summary>
        IList<SaveCandidate> LoadCandidates();
        /// <summary>Ghi file tam, doc lai de kiem tra, roi thay the file chinh va giu ban truoc lam backup.</summary>
        void Save(string json);
        void DeleteAll();
    }

    public enum LifecyclePhase
    {
        Running,
        Suspended,
        Resuming
    }

    public enum LoadOutcome
    {
        NewGame,
        LoadedPrimary,
        LoadedBackup,
        /// <summary>Save moi hon ung dung. Khong reset, cho nguoi choi biet va dung lai.</summary>
        BlockedIncompatible,
        /// <summary>Ca file chinh va backup deu khong dung duoc. Khong am tham reset.</summary>
        BlockedCorrupt
    }

    public sealed class CommandResult
    {
        public bool Success;
        public string FailureReason;

        public static CommandResult Ok()
        {
            return new CommandResult { Success = true };
        }

        public static CommandResult Fail(string reason)
        {
            return new CommandResult { Success = false, FailureReason = reason };
        }
    }

    /// <summary>
    /// Tuan tu hoa lenh, dua simulation den hien tai truoc khi validate, goi luu va bao view cap nhat.
    /// Khong so huu quy tac gia hay animation.
    /// </summary>
    public sealed class GameSession : ISimulationListener
    {
        /// <summary>Duoi nguong nay thi khong hien bao cao quay lai, nhung tien do van duoc ap dung.</summary>
        public const long OfflineReportMinMs = 60000;

        readonly ContentCatalog _catalog;
        readonly FarmSimulation _simulation;
        readonly IClock _clock;
        readonly ISaveRepository _repository;
        readonly ITestLogger _logger;
        readonly string _buildId;

        GameState _state;
        LifecyclePhase _phase = LifecyclePhase.Suspended;
        long _lastTickMonotonicMs;
        long _lastSaveMonotonicMs;
        bool _dirty;
        bool _loggedFirstPlant;
        bool _loggedFirstBrew;
        int _offlineSummaryCounter;

        public event Action StateChanged;
        /// <summary>Chan chinh gio lui, sai lech dong ho: ghi lai de doc khi xem log test.</summary>
        public event Action<string> Diagnostic;

        public ContentCatalog Catalog { get { return _catalog; } }
        public FarmSimulation Simulation { get { return _simulation; } }
        public GameState State { get { return _state; } }
        public LifecyclePhase Phase { get { return _phase; } }
        public LoadOutcome LastLoadOutcome { get; private set; }
        public string LastLoadError { get; private set; }

        /// <summary>
        /// balanceVersion cua save vua nap. Khac voi cau hinh dang chay thi khong tu chuyen doi
        /// du lieu; cac gia tri da chot cua vu/me dang chay van giu nguyen so cu.
        /// </summary>
        public string LoadedBalanceVersion { get; private set; }
        public bool LoadedWithDifferentBalance { get; private set; }

        public GameSession(ContentCatalog catalog, IClock clock, ISaveRepository repository,
                           ITestLogger logger, string buildId)
        {
            if (catalog == null) throw new ArgumentNullException("catalog");
            if (clock == null) throw new ArgumentNullException("clock");
            if (repository == null) throw new ArgumentNullException("repository");
            _catalog = catalog;
            _clock = clock;
            _repository = repository;
            _logger = logger;
            _buildId = buildId ?? "dev";
            _simulation = new FarmSimulation(catalog);
            _simulation.AddListener(this);
        }

        // ---------------------------------------------------------------- nap game

        public LoadOutcome Initialize()
        {
            LastLoadError = null;
            var candidates = _repository.LoadCandidates();

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.Json == null)
                {
                    if (candidate.ReadError != null) LastLoadError = candidate.ReadError;
                    continue;
                }

                try
                {
                    var snapshot = SaveSerializer.Read(candidate.Json, _catalog);
                    _state = snapshot.State;
                    LastLoadOutcome = i == 0 ? LoadOutcome.LoadedPrimary : LoadOutcome.LoadedBackup;

                    LoadedBalanceVersion = snapshot.BalanceVersion;
                    LoadedWithDifferentBalance =
                        LoadedBalanceVersion != null &&
                        !string.Equals(LoadedBalanceVersion, _catalog.Balance.Version, StringComparison.Ordinal);
                    if (LoadedWithDifferentBalance)
                        RaiseDiagnostic("Save duoc tao voi balance " + LoadedBalanceVersion +
                                        ", cau hinh dang chay la " + _catalog.Balance.Version +
                                        ". Vu va me dang chay giu nguyen gia tri da chot.");

                    BeginSession();
                    ApplyOfflineProgress();
                    return LastLoadOutcome;
                }
                catch (SaveIncompatibleException error)
                {
                    LastLoadError = error.Message;
                    LastLoadOutcome = LoadOutcome.BlockedIncompatible;
                    return LastLoadOutcome;
                }
                catch (SaveCorruptException error)
                {
                    LastLoadError = error.Message;
                }
            }

            if (_repository.HasSave)
            {
                LastLoadOutcome = LoadOutcome.BlockedCorrupt;
                return LastLoadOutcome;
            }

            StartNewGame();
            LastLoadOutcome = LoadOutcome.NewGame;
            return LastLoadOutcome;
        }

        /// <summary>Dung cho lan choi dau va cho lenh reset da duoc xac nhan.</summary>
        public void StartNewGame()
        {
            _state = GameState.CreateNew(_catalog, _clock.UtcNowMs);
            _loggedFirstPlant = false;
            _loggedFirstBrew = false;
            BeginSession();
            SaveNow();
            RaiseChanged();
        }

        void BeginSession()
        {
            _phase = LifecyclePhase.Running;
            _lastTickMonotonicMs = _clock.MonotonicMs;
            _lastSaveMonotonicMs = _clock.MonotonicMs;
            _dirty = false;
            Log("session_start", "buildId", _buildId, "balanceVersion", _catalog.Balance.Version,
                "coins", _state.Coins.ToString());
        }

        // ---------------------------------------------------------------- vong doi

        /// <summary>
        /// Gop callback focus/pause thanh mot lan chuyen Running -> Suspended.
        /// Goi lai khi da Suspended khong lam gi.
        /// </summary>
        public void Suspend()
        {
            if (_phase != LifecyclePhase.Running) return;
            EnsureCurrent();
            _state.CheckpointUtcMs = _clock.UtcNowMs;
            _phase = LifecyclePhase.Suspended;
            SaveNow();
            Log("app_suspended", "simulationTimeMs", _state.SimulationTimeMs.ToString());
        }

        /// <summary>Chi mot lan Resuming cho moi lan quay lai, du he thong goi callback nhieu lan.</summary>
        public void ResumeFromBackground()
        {
            if (_phase != LifecyclePhase.Suspended) return;
            _phase = LifecyclePhase.Resuming;
            ApplyOfflineProgress();
        }

        /// <summary>Nhip online. View noi suy thanh tien do moi frame, khong dung tick nay lam nguon timer.</summary>
        public void Tick()
        {
            if (_phase != LifecyclePhase.Running) return;
            EnsureCurrent();
            MaybeAutosave();
        }

        void EnsureCurrent()
        {
            long monotonic = _clock.MonotonicMs;
            long delta = monotonic - _lastTickMonotonicMs;
            if (delta < 0) delta = 0;
            _lastTickMonotonicMs = monotonic;
            if (delta == 0) return;

            long before = _state.Coins;
            _simulation.AdvanceTo(_state, _state.SimulationTimeMs + delta);
            if (before != _state.Coins) _dirty = true;
        }

        // ---------------------------------------------------------------- offline

        /// <summary>
        /// elapsedMs = clamp(now - checkpoint, 0, cap). Mo phong tren ban sao, luu thanh cong roi
        /// moi dua state moi ra UI. Checkpoint duoc dat ve hien tai nen phan du khong duoc nhan lai.
        /// </summary>
        public bool ApplyOfflineProgress()
        {
            long nowUtcMs = _clock.UtcNowMs;
            long rawElapsedMs = nowUtcMs - _state.CheckpointUtcMs;
            if (rawElapsedMs < 0)
            {
                RaiseDiagnostic("Dong ho he thong lui " + (-rawElapsedMs) + " ms; bo qua quang nay.");
                rawElapsedMs = 0;
            }

            long cap = _catalog.Balance.OfflineCapMs;
            long elapsedMs = rawElapsedMs > cap ? cap : rawElapsedMs;

            var working = _state.Clone();
            long coinsBefore = working.Coins;
            var inventoryBefore = new Dictionary<string, long>(working.Inventory, StringComparer.Ordinal);

            _simulation.ListenersMuted = true;   // khong phat lai hang nghin hieu ung cua quang nghi
            try
            {
                if (elapsedMs > 0)
                    _simulation.AdvanceTo(working, working.SimulationTimeMs + elapsedMs);
            }
            finally
            {
                _simulation.ListenersMuted = false;
            }

            working.CheckpointUtcMs = nowUtcMs;
            working.SaveRevision = _state.SaveRevision + 1;

            if (elapsedMs >= OfflineReportMinMs)
            {
                _offlineSummaryCounter++;
                var summary = new OfflineSummary
                {
                    Id = "offline-" + working.SaveRevision + "-" + _offlineSummaryCounter,
                    ElapsedMs = elapsedMs,
                    CoinsGained = working.Coins - coinsBefore,
                    Seen = false
                };
                foreach (var crop in _catalog.Crops)
                {
                    long before;
                    inventoryBefore.TryGetValue(crop.Id, out before);
                    long delta = working.InventoryOf(crop.Id) - before;
                    if (delta != 0)
                        summary.ItemsGained.Add(new InventoryDelta { CropId = crop.Id, Amount = delta });
                }
                string missingCropId;
                long missingAmount;
                summary.MachineWaiting = _simulation.TryGetMissingInput(working, out missingCropId, out missingAmount);
                working.PendingOfflineSummary = summary;
            }

            // Ghi state moi, checkpoint moi va bao cao trong cung mot snapshot.
            string json;
            try
            {
                json = Serialize(working);
                _repository.Save(json);
            }
            catch (Exception error)
            {
                // Giu checkpoint cu de lan sau thu lai; khong vua hien tien moi vua giu save cu.
                RaiseDiagnostic("Khong luu duoc sau khi quay lai: " + error.Message);
                _phase = LifecyclePhase.Suspended;
                return false;
            }

            _state = working;
            _phase = LifecyclePhase.Running;
            _lastTickMonotonicMs = _clock.MonotonicMs;   // quang suspend khong duoc tick tinh lai
            _lastSaveMonotonicMs = _clock.MonotonicMs;
            _dirty = false;

            Log("offline_applied", "elapsedMs", elapsedMs.ToString(),
                "rawElapsedMs", rawElapsedMs.ToString(),
                "coinsGained", (working.Coins - coinsBefore).ToString());
            RaiseChanged();
            return true;
        }

        /// <summary>"Tiep tuc" chi danh dau bao cao da xem, khong cong them gi.</summary>
        public void AcknowledgeOfflineSummary()
        {
            if (_state.PendingOfflineSummary == null) return;
            _state.PendingOfflineSummary.Seen = true;
            _state.PendingOfflineSummary = null;
            _dirty = true;
            RaiseChanged();
        }

        // ---------------------------------------------------------------- lenh nguoi choi

        public CommandResult Plant(int plotId, string cropId)
        {
            EnsureCurrent();
            var plot = _state.Plot(plotId);
            if (plot == null) return CommandResult.Fail("Không có ô này.");
            if (!plot.Unlocked) return CommandResult.Fail("Ô đất chưa mở.");
            if (plot.Phase != PlotPhase.Empty) return CommandResult.Fail("Ô đất đang có cây.");
            CropDefinition crop;
            if (!_catalog.TryGetCrop(cropId, out crop)) return CommandResult.Fail("Không có loại cây này.");
            if (!_state.UnlockedCropIds.Contains(cropId)) return CommandResult.Fail("Cây chưa được mở khóa.");

            _simulation.Plant(_state, plot, cropId, _state.SimulationTimeMs);
            _dirty = true;
            if (!_loggedFirstPlant)
            {
                _loggedFirstPlant = true;
                Log("first_plant", "cropId", cropId);
            }
            RaiseChanged();
            return CommandResult.Ok();
        }

        /// <summary>
        /// Doi cay khi dang co cay chi dat lua chon cho lan gieo tiep theo. O trong thi gieo ngay.
        /// Khong huy cay hien tai, khong hoan tien.
        /// </summary>
        public CommandResult SetNextCrop(int plotId, string cropId)
        {
            EnsureCurrent();
            var plot = _state.Plot(plotId);
            if (plot == null) return CommandResult.Fail("Không có ô này.");
            if (!plot.Unlocked) return CommandResult.Fail("Ô đất chưa mở.");
            CropDefinition crop;
            if (!_catalog.TryGetCrop(cropId, out crop)) return CommandResult.Fail("Không có loại cây này.");
            if (!_state.UnlockedCropIds.Contains(cropId)) return CommandResult.Fail("Cây chưa được mở khóa.");

            plot.NextCropId = cropId;
            if (plot.Phase == PlotPhase.Empty)
                _simulation.Plant(_state, plot, cropId, _state.SimulationTimeMs);

            _dirty = true;
            RaiseChanged();
            return CommandResult.Ok();
        }

        /// <summary>Mot click tren o chin: thu 1 nguyen lieu va gieo lai trong cung mot hanh dong.</summary>
        public CommandResult HarvestAndReplant(int plotId)
        {
            EnsureCurrent();
            var plot = _state.Plot(plotId);
            if (plot == null) return CommandResult.Fail("Không có ô này.");
            if (plot.Phase != PlotPhase.Ready) return CommandResult.Fail("Cây chưa chín.");

            _simulation.HarvestAndReplant(_state, plot, _state.SimulationTimeMs, false);
            _simulation.ResolveImmediate(_state);
            _dirty = true;
            Log("crop_harvested", "plotId", plotId.ToString());
            RaiseChanged();
            return CommandResult.Ok();
        }

        /// <summary>
        /// Doi cong thuc khi dang pha: me hien tai hoan thanh truoc. Khi may dang cho thi co hieu luc ngay.
        /// </summary>
        public CommandResult SelectRecipe(string recipeId)
        {
            EnsureCurrent();
            RecipeDefinition recipe;
            if (!_catalog.TryGetRecipe(recipeId, out recipe)) return CommandResult.Fail("Không có công thức này.");
            if (!_state.UnlockedRecipeIds.Contains(recipeId)) return CommandResult.Fail("Công thức chưa mở khóa.");

            _state.Machine.SelectedRecipeId = recipeId;
            _simulation.ResolveImmediate(_state);
            _dirty = true;
            Log("recipe_selected", "recipeId", recipeId);
            RaiseChanged();
            return CommandResult.Ok();
        }

        public CommandResult SellRaw(string cropId, long amount)
        {
            EnsureCurrent();
            CropDefinition crop;
            if (!_catalog.TryGetCrop(cropId, out crop)) return CommandResult.Fail("Không có loại cây này.");
            if (amount <= 0) return CommandResult.Fail("Số lượng bán phải lớn hơn 0.");
            long have = _state.InventoryOf(cropId);
            if (have < amount) return CommandResult.Fail("Kho không đủ " + crop.DisplayName + ".");

            _state.AddInventory(cropId, -amount);
            _state.Coins += crop.RawSellPrice * amount;
            _simulation.ResolveImmediate(_state);
            _dirty = true;
            Log("raw_sold", "cropId", cropId, "amount", amount.ToString());
            RaiseChanged();
            return CommandResult.Ok();
        }

        public CommandResult SellAllRaw(string cropId)
        {
            long have = _state.InventoryOf(cropId);
            if (have <= 0) return CommandResult.Fail("Kho trống.");
            return SellRaw(cropId, have);
        }

        /// <summary>Truoc khi mua robot, cua hang chi cho mua robot.</summary>
        public bool IsPurchasable(string upgradeId, out string reason)
        {
            reason = null;
            UpgradeDefinition upgrade;
            if (!_catalog.TryGetUpgrade(upgradeId, out upgrade))
            {
                reason = "Không có nâng cấp này.";
                return false;
            }
            if (_state.UpgradeLevel(upgradeId) >= 1)
            {
                reason = "Đã mua.";
                return false;
            }
            if (!_state.RobotUnlocked && upgrade.Kind != UpgradeKind.UnlockRobot)
            {
                reason = "Mở robot trước đã.";
                return false;
            }
            if (upgrade.RequiresUpgradeId != null && _state.UpgradeLevel(upgrade.RequiresUpgradeId) < 1)
            {
                reason = "Cần mua " + _catalog.Upgrade(upgrade.RequiresUpgradeId).DisplayName + " trước.";
                return false;
            }
            if (_state.Coins < upgrade.Cost)
            {
                reason = "Thiếu " + (upgrade.Cost - _state.Coins) + " xu.";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Tru tien va thay tien do trong cung mot transaction, chi xac nhan thanh cong sau khi luu xong.
        /// Loi ghi cho thu lai ma khong tru tien them lan nao.
        /// </summary>
        public CommandResult Purchase(string upgradeId)
        {
            EnsureCurrent();
            string reason;
            if (!IsPurchasable(upgradeId, out reason)) return CommandResult.Fail(reason);

            var upgrade = _catalog.Upgrade(upgradeId);
            var working = _state.Clone();
            working.Coins -= upgrade.Cost;
            working.UpgradeLevels[upgrade.Id] = working.UpgradeLevel(upgrade.Id) + 1;

            switch (upgrade.Kind)
            {
                case UpgradeKind.UnlockRobot:
                    working.RobotUnlocked = true;
                    break;
                case UpgradeKind.UnlockCrop:
                    working.UnlockedCropIds.Add(upgrade.TargetId);
                    break;
                case UpgradeKind.ExpandPlots:
                    for (int i = 0; i < working.Plots.Count; i++)
                    {
                        var plot = working.Plots[i];
                        if (!plot.Unlocked && plot.PlotId < upgrade.IntValue)
                        {
                            plot.Unlocked = true;
                            plot.Phase = PlotPhase.Empty;
                        }
                    }
                    break;
                case UpgradeKind.GrowthSpeed:
                case UpgradeKind.BrewSpeed:
                    // Deadline dang chay giu nguyen; chu ky bat dau sau khi mua moi dung so moi.
                    break;
            }

            // Cong thuc gan voi cung nang cap duoc mo cung luc.
            for (int i = 0; i < _catalog.Recipes.Count; i++)
                if (_catalog.Recipes[i].UnlockUpgradeId == upgrade.Id)
                    working.UnlockedRecipeIds.Add(_catalog.Recipes[i].Id);

            _simulation.ListenersMuted = true;
            try
            {
                _simulation.ResolveImmediate(working);
            }
            finally
            {
                _simulation.ListenersMuted = false;
            }

            working.SaveRevision = _state.SaveRevision + 1;
            try
            {
                _repository.Save(Serialize(working));
            }
            catch (Exception error)
            {
                RaiseDiagnostic("Không lưu được sau khi mua: " + error.Message);
                return CommandResult.Fail("Chưa lưu được tiến độ, hãy thử lại.");
            }

            _state = working;
            _lastSaveMonotonicMs = _clock.MonotonicMs;
            _dirty = false;

            Log("upgrade_purchased", "upgradeId", upgrade.Id, "cost", upgrade.Cost.ToString(),
                "coinsAfter", _state.Coins.ToString());
            if (upgrade.Kind == UpgradeKind.UnlockRobot) Log("robot_unlocked");
            if (upgrade.Kind == UpgradeKind.UnlockCrop) Log("crop_unlocked", "cropId", upgrade.TargetId);

            RaiseChanged();
            return CommandResult.Ok();
        }

        // ---------------------------------------------------------------- trang tri (pha 2)

        /// <summary>Pha 2 chi mo sau khi nguoi choi da dung xong farm o pha 1.</summary>
        public bool DecoratingUnlocked
        {
            get { return _state != null && GardenPhase.IsSetupComplete(_state, _catalog); }
        }

        /// <summary>
        /// Kiem tra mot cho dat co hop le khong, dung chung cho ca lenh dat va cho preview trong UI
        /// de nguoi choi thay truoc khi bam. ignoreIndex de bo qua chinh mon dang duoc di chuyen.
        /// </summary>
        public bool CanPlaceDecoration(string definitionId, int xMm, int zMm, int ignoreIndex, out string reason)
        {
            reason = null;
            if (!DecoratingUnlocked)
            {
                string missing = GardenPhase.SetupRemainingHint(_state, _catalog);
                reason = missing != null ? "Cần " + missing + " trước khi trang trí." : "Chưa mở trang trí.";
                return false;
            }

            DecorationDefinition definition;
            if (!_catalog.TryGetDecoration(definitionId, out definition))
            {
                reason = "Không có món này.";
                return false;
            }

            int halfExtent = _catalog.Balance.GardenHalfExtentMm;
            if (Math.Abs(xMm) > halfExtent || Math.Abs(zMm) > halfExtent)
            {
                reason = "Ngoài khu vườn.";
                return false;
            }

            for (int i = 0; i < _state.Decorations.Count; i++)
            {
                if (i == ignoreIndex) continue;
                var other = _state.Decorations[i];
                DecorationDefinition otherDefinition;
                if (!_catalog.TryGetDecoration(other.DefinitionId, out otherDefinition)) continue;

                long dx = xMm - other.XMm;
                long dz = zMm - other.ZMm;
                long minimum = definition.FootprintMm + otherDefinition.FootprintMm;
                if (dx * dx + dz * dz < minimum * minimum)
                {
                    reason = "Sát món khác quá.";
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Mua va dat mot mon trang tri. Tru tien va ghi save trong cung mot transaction giong
        /// lenh mua nang cap, vi day cung la mot lan tieu tien.
        /// </summary>
        public CommandResult PlaceDecoration(string definitionId, int xMm, int zMm, int rotationDeg)
        {
            EnsureCurrent();

            string reason;
            if (!CanPlaceDecoration(definitionId, xMm, zMm, -1, out reason))
                return CommandResult.Fail(reason);

            var definition = _catalog.Decoration(definitionId);
            if (_state.Coins < definition.Cost)
                return CommandResult.Fail("Thiếu " + (definition.Cost - _state.Coins) + " xu.");

            var working = _state.Clone();
            working.Coins -= definition.Cost;
            working.Decorations.Add(new PlacedDecoration
            {
                DefinitionId = definitionId,
                XMm = xMm,
                ZMm = zMm,
                RotationDeg = NormalizeRotation(rotationDeg)
            });

            var commit = CommitWithSave(working, "Chưa lưu được món vừa đặt, hãy thử lại.");
            if (!commit.Success) return commit;

            Log("decoration_placed", "definitionId", definitionId, "cost", definition.Cost.ToString());
            return commit;
        }

        /// <summary>Di chuyen mon da dat. Khong ton tien nen khong can transaction rieng.</summary>
        public CommandResult MoveDecoration(int index, int xMm, int zMm, int rotationDeg)
        {
            EnsureCurrent();
            if (index < 0 || index >= _state.Decorations.Count)
                return CommandResult.Fail("Không có món này trong vườn.");

            var placed = _state.Decorations[index];
            string reason;
            if (!CanPlaceDecoration(placed.DefinitionId, xMm, zMm, index, out reason))
                return CommandResult.Fail(reason);

            placed.XMm = xMm;
            placed.ZMm = zMm;
            placed.RotationDeg = NormalizeRotation(rotationDeg);
            _dirty = true;
            RaiseChanged();
            return CommandResult.Ok();
        }

        /// <summary>
        /// Go mon da dat va hoan lai du tien. Trang tri thuan tham my nen hoan du khong tao ke ho
        /// kinh te nao, va nguoi choi thu bo cuc thoai mai ma khong so mat tien.
        /// </summary>
        public CommandResult RemoveDecoration(int index)
        {
            EnsureCurrent();
            if (index < 0 || index >= _state.Decorations.Count)
                return CommandResult.Fail("Không có món này trong vườn.");

            var placed = _state.Decorations[index];
            DecorationDefinition definition;
            long refund = _catalog.TryGetDecoration(placed.DefinitionId, out definition) ? definition.Cost : 0;

            var working = _state.Clone();
            working.Decorations.RemoveAt(index);
            working.Coins += refund;

            var commit = CommitWithSave(working, "Chưa lưu được thay đổi, hãy thử lại.");
            if (!commit.Success) return commit;

            Log("decoration_removed", "definitionId", placed.DefinitionId, "refund", refund.ToString());
            return commit;
        }

        static int NormalizeRotation(int degrees)
        {
            int value = degrees % 360;
            return value < 0 ? value + 360 : value;
        }

        /// <summary>
        /// Thay state bang ban da sua sau khi ghi thanh cong. Loi ghi giu nguyen state cu nen
        /// nguoi choi khong bi tru tien ma khong duoc gi.
        /// </summary>
        CommandResult CommitWithSave(GameState working, string failureMessage)
        {
            working.SaveRevision = _state.SaveRevision + 1;
            try
            {
                _repository.Save(Serialize(working));
            }
            catch (Exception error)
            {
                RaiseDiagnostic(failureMessage + " " + error.Message);
                return CommandResult.Fail(failureMessage);
            }

            _state = working;
            _lastSaveMonotonicMs = _clock.MonotonicMs;
            _dirty = false;
            RaiseChanged();
            return CommandResult.Ok();
        }

        // ---------------------------------------------------------------- luu

        public string Serialize(GameState state)
        {
            var snapshot = new SaveSnapshot
            {
                SchemaVersion = SaveSnapshot.CurrentSchemaVersion,
                BalanceVersion = _catalog.Balance.Version,
                BuildId = _buildId,
                State = state
            };
            return SaveSerializer.Write(snapshot, _catalog, true);
        }

        void MaybeAutosave()
        {
            if (!_dirty) return;
            if (_clock.MonotonicMs - _lastSaveMonotonicMs < _catalog.Balance.AutosaveIntervalMs) return;
            SaveNow();
        }

        public bool SaveNow()
        {
            try
            {
                _state.SaveRevision++;
                _repository.Save(Serialize(_state));
                _lastSaveMonotonicMs = _clock.MonotonicMs;
                _dirty = false;
                return true;
            }
            catch (Exception error)
            {
                _state.SaveRevision--;
                RaiseDiagnostic("Không ghi được save: " + error.Message);
                return false;
            }
        }

        public void EndSession()
        {
            Log("session_end", "simulationTimeMs", _state.SimulationTimeMs.ToString(),
                "coins", _state.Coins.ToString());
        }

        // ---------------------------------------------------------------- log su kien mo phong

        void ISimulationListener.OnCropReady(int plotId, string cropId, long atMs) { }

        void ISimulationListener.OnHarvested(int plotId, string cropId, int amount, bool byRobot, long atMs)
        {
            _dirty = true;
            if (byRobot) Log("crop_harvested", "plotId", plotId.ToString(), "byRobot", "true");
        }

        void ISimulationListener.OnPlanted(int plotId, string cropId, long atMs) { }

        void ISimulationListener.OnBatchStarted(string recipeId, long atMs)
        {
            _dirty = true;
            if (!_loggedFirstBrew)
            {
                _loggedFirstBrew = true;
                Log("first_brew", "recipeId", recipeId);
            }
        }

        void ISimulationListener.OnBatchCompleted(string recipeId, long coins, long atMs)
        {
            _dirty = true;
        }

        // ---------------------------------------------------------------- tien ich

        void Log(string eventName, params string[] fields)
        {
            if (_logger == null) return;
            _logger.Log(eventName, fields);
        }

        void RaiseChanged()
        {
            var handler = StateChanged;
            if (handler != null) handler();
        }

        void RaiseDiagnostic(string message)
        {
            var handler = Diagnostic;
            if (handler != null) handler(message);
        }

        /// <summary>Chi dung cho test va cong cu dev: tua thoi gian mo phong.</summary>
        public void DebugAdvance(long milliseconds)
        {
            if (milliseconds <= 0) return;
            _simulation.AdvanceTo(_state, _state.SimulationTimeMs + milliseconds);
            _dirty = true;
            RaiseChanged();
        }
    }
}
