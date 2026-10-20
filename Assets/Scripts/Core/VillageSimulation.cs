using System;
using System.Collections.Generic;
using System.Linq;

namespace MinhaVila.Core
{
    public sealed class VillageSimulation
    {
        private sealed class UndoEntry
        {
            public string Action;
            public BuildingRecord Building;
            public ResourceBundle RefundResources;
            public int RefundGold;
            public List<ResidentRecord> Residents;
        }

        private readonly List<UndoEntry> _undoHistory = new List<UndoEntry>();
        private double _realSecondsRemainder;

        public VillageState State { get; private set; }
        public event Action Changed;

        public VillageSimulation(VillageState state)
        {
            State = state ?? VillageState.CreateNew("Minha Vila");
            NormalizeState();
            RebuildWorkforce();
        }

        public bool IsPaused { get { return State.Paused; } }
        public int Population { get { return State.Population; } }
        public int EmployedCount { get { return State.Residents.Count(resident => resident.WorkplaceId >= 0); } }
        public int GuardCount { get { return SecuritySystem.Guards(State); } }

        public int ServedCount
        {
            get { return GetServedResidentIds().Count; }
        }

        public int Happiness
        {
            get
            {
                if (Population == 0) return 0;
                return (int)Math.Round(50.0 * (EmployedCount + ServedCount) / Population);
            }
        }

        public double IncomePerHour
        {
            get
            {
                double income = 0;
                foreach (ResidentRecord resident in State.Residents)
                {
                    if (resident.WorkplaceId < 0 || resident.IsGuard || resident.IsOnMission) continue;
                    BuildingRecord workplace = State.FindBuilding(resident.WorkplaceId);
                    if (workplace == null) continue;
                    income += BuildingCatalog.Get(workplace.Type, workplace.Level).GoldPerWorkerHour;
                }

                return income * IncomeMultiplier();
            }
        }

        public double ResearchProgressPercent
        {
            get
            {
                if (!State.HasActiveResearch) return 0;
                ResearchDefinition definition = ResearchCatalog.Get(State.ActiveResearch);
                return definition == null ? 0 : Math.Min(1, State.ResearchProgressHours / definition.RequiredHours);
            }
        }

        public void SetPaused(bool paused)
        {
            if (State.Paused == paused) return;
            State.Paused = paused;
            Changed?.Invoke();
        }

        public bool SetSpeed(int speed)
        {
            if (speed != 1 && speed != 2 && speed != 5 && speed != 10) return false;
            State.Speed = speed;
            Changed?.Invoke();
            return true;
        }

        public void AdvanceRealSeconds(double realSeconds)
        {
            if (State.Paused || realSeconds <= 0 || double.IsNaN(realSeconds) || double.IsInfinity(realSeconds))
            {
                return;
            }

            _realSecondsRemainder += realSeconds * State.Speed;
            int wholeMinutes = (int)Math.Floor(_realSecondsRemainder);
            if (wholeMinutes <= 0) return;
            _realSecondsRemainder -= wholeMinutes;

            for (int i = 0; i < wholeMinutes; i++)
            {
                AdvanceOneMinute();
            }

            Changed?.Invoke();
        }

        public void AdvanceGameMinutes(int minutes)
        {
            if (State.Paused || minutes <= 0) return;
            for (int i = 0; i < minutes; i++) AdvanceOneMinute();
            Changed?.Invoke();
        }

        public bool TryBuild(BuildingType type, GridPoint point, int rotation, out string message)
        {
            message = string.Empty;
            BuildingDefinition definition = BuildingCatalog.Get(type);
            if (definition == null)
            {
                message = "Construção inválida.";
                return false;
            }

            if (!LandSystem.IsOwned(State, point.X, point.Z))
            {
                message = "Esse lote ainda não pertence à vila.";
                return false;
            }

            if (State.FindBuildingAt(point.X, point.Z) != null)
            {
                message = "O lote já está ocupado.";
                return false;
            }

            if (definition.RequiredResearch.HasValue && !ResearchCatalog.IsCompleted(State, definition.RequiredResearch.Value))
            {
                message = "Pesquise " + ResearchCatalog.Get(definition.RequiredResearch.Value).Name + " para liberar essa construção.";
                return false;
            }

            if (type == BuildingType.Bank)
            {
                int limit = State.Population / 1000;
                int built = State.Buildings.Count(building => building.Type == BuildingType.Bank);
                if (built >= limit)
                {
                    message = "O limite de bancos ativos é de um para cada 1.000 moradores.";
                    return false;
                }
            }

            if (State.Gold < definition.GoldCost || !definition.ResourceCost.IsAffordableWith(State.Stock))
            {
                message = "Faltam ouro ou materiais para construir.";
                return false;
            }

            State.Gold -= definition.GoldCost;
            State.SpentGold += definition.GoldCost;
            definition.ResourceCost.TrySpendFrom(State.Stock);
            BuildingRecord building = new BuildingRecord
            {
                Id = State.NextBuildingId++,
                Type = type,
                Level = 1,
                X = point.X,
                Z = point.Z,
                Rotation = ((rotation % 4) + 4) % 4,
                HouseCapacity = type == BuildingType.House ? RollHouseCapacity() : 0
            };
            State.Buildings.Add(building);
            PushUndo(new UndoEntry
            {
                Action = "build",
                Building = CopyBuilding(building),
                RefundGold = definition.GoldCost,
                RefundResources = definition.ResourceCost.Copy()
            });

            RebuildWorkforce();
            message = definition.Name + " construída.";
            AddNotification(message, NotificationKind.Construction);
            Changed?.Invoke();
            return true;
        }

        public bool TryUpgrade(int buildingId, out string message)
        {
            message = string.Empty;
            BuildingRecord building = State.FindBuilding(buildingId);
            if (building == null || building.Level != 1)
            {
                message = "Selecione uma construção básica que aceite melhoria.";
                return false;
            }

            BuildingDefinition improved = BuildingCatalog.Get(building.Type, 2);
            ResourceBundle cost = BuildingCatalog.GetImprovementCost(building.Type, out int goldCost);
            if (improved == null || cost == null)
            {
                message = "Essa construção não possui uma versão melhorada.";
                return false;
            }

            if (improved.RequiredResearch.HasValue && !ResearchCatalog.IsCompleted(State, improved.RequiredResearch.Value))
            {
                message = "Pesquise " + ResearchCatalog.Get(improved.RequiredResearch.Value).Name + " para liberar a melhoria.";
                return false;
            }

            if (State.Gold < goldCost || !cost.IsAffordableWith(State.Stock))
            {
                message = "Faltam ouro ou materiais para a melhoria.";
                return false;
            }

            State.Gold -= goldCost;
            State.SpentGold += goldCost;
            cost.TrySpendFrom(State.Stock);
            building.Level = 2;
            PushUndo(new UndoEntry
            {
                Action = "upgrade",
                Building = CopyBuilding(building),
                RefundGold = goldCost,
                RefundResources = cost.Copy()
            });
            RebuildWorkforce();
            message = improved.Name + " concluída. Moradores e localização foram preservados.";
            AddNotification(message, NotificationKind.Construction);
            Changed?.Invoke();
            return true;
        }

        public bool TryDemolish(int buildingId, out string message)
        {
            message = string.Empty;
            BuildingRecord building = State.FindBuilding(buildingId);
            if (building == null)
            {
                message = "Selecione uma construção.";
                return false;
            }

            List<ResidentRecord> residents = State.Residents
                .Where(resident => resident.HomeId == buildingId)
                .Select(CopyResident)
                .ToList();

            State.Buildings.Remove(building);
            State.Residents.RemoveAll(resident => resident.HomeId == buildingId);
            foreach (ActiveMissionRecord mission in State.ActiveMissions.Where(item => item.TavernId == buildingId).ToList())
            {
                CancelMission(mission);
            }

            PushUndo(new UndoEntry
            {
                Action = "demolish",
                Building = CopyBuilding(building),
                Residents = residents
            });
            RebuildWorkforce();
            message = BuildingCatalog.Get(building.Type, building.Level).Name + " demolida. Demolir não devolve recursos.";
            AddNotification(message, NotificationKind.Construction);
            Changed?.Invoke();
            return true;
        }

        public bool UndoLastAction(out string message)
        {
            message = string.Empty;
            if (_undoHistory.Count == 0)
            {
                message = "Não há ações para desfazer.";
                return false;
            }

            UndoEntry entry = _undoHistory[_undoHistory.Count - 1];
            _undoHistory.RemoveAt(_undoHistory.Count - 1);
            if (entry.Action == "build")
            {
                BuildingRecord building = State.FindBuilding(entry.Building.Id);
                if (building == null || State.Residents.Any(resident => resident.HomeId == building.Id))
                {
                    _undoHistory.Add(entry);
                    message = "A construção não pode ser desfeita enquanto estiver ocupada.";
                    return false;
                }

                State.Buildings.Remove(building);
                State.Gold += entry.RefundGold;
                State.SpentGold -= entry.RefundGold;
                entry.RefundResources.AddTo(State.Stock);
                message = "Construção desfeita e investimento devolvido.";
            }
            else if (entry.Action == "upgrade")
            {
                BuildingRecord building = State.FindBuilding(entry.Building.Id);
                if (building == null)
                {
                    message = "A construção não está mais disponível.";
                    return false;
                }

                building.Level = 1;
                State.Gold += entry.RefundGold;
                State.SpentGold -= entry.RefundGold;
                entry.RefundResources.AddTo(State.Stock);
                message = "Melhoria desfeita.";
            }
            else
            {
                State.Buildings.Add(CopyBuilding(entry.Building));
                foreach (ResidentRecord resident in entry.Residents)
                    State.Residents.Add(CopyResident(resident));
                message = "Demolição desfeita.";
            }

            RebuildWorkforce();
            AddNotification(message, NotificationKind.Construction);
            Changed?.Invoke();
            return true;
        }

        public bool TryStartResearch(ResearchId id, out string message)
        {
            bool started = ResearchSystem.TryStart(State, id, out message);
            if (started)
            {
                AddNotification(message, NotificationKind.Research);
                Changed?.Invoke();
            }
            return started;
        }

        public bool TryExpandTerritory(out string message)
        {
            bool expanded = LandSystem.TryExpand(State, out message);
            if (expanded)
            {
                AddNotification(message, NotificationKind.General);
                Changed?.Invoke();
            }
            return expanded;
        }

        public bool TryBuyResource(ResourceType type, int quantity, out string message)
        {
            bool bought = FinanceSystem.TryBuy(State, type, quantity, out message);
            if (bought)
            {
                AddNotification(message, NotificationKind.Economy);
                Changed?.Invoke();
            }
            return bought;
        }

        public bool TryBorrow(int amount, out string message)
        {
            bool borrowed = FinanceSystem.TryBorrow(State, amount, out message);
            if (borrowed)
            {
                AddNotification(message, NotificationKind.Economy);
                Changed?.Invoke();
            }
            return borrowed;
        }

        public bool TryRepay(int amount, out string message)
        {
            bool repaid = FinanceSystem.TryRepay(State, amount, out message);
            if (repaid)
            {
                AddNotification(message, NotificationKind.Economy);
                Changed?.Invoke();
            }
            return repaid;
        }

        public bool TryAdjustGold(int amount, out string message)
        {
            bool adjusted = FinanceSystem.TryAdjustGold(State, amount, out message);
            if (adjusted)
            {
                AddNotification(message, NotificationKind.Economy);
                Changed?.Invoke();
            }
            return adjusted;
        }

        public bool TryStartMission(int tavernId, MissionType type, out string message)
        {
            bool started = MissionSystem.TryStart(State, tavernId, type, out message);
            if (started)
            {
                AddNotification(message, NotificationKind.Mission);
                Changed?.Invoke();
            }
            return started;
        }

        public bool TryUpgradeRoyalItem(bool improveTools, out string message)
        {
            bool upgraded = MissionSystem.TryUpgrade(State, improveTools, out message);
            if (upgraded)
            {
                AddNotification(message, NotificationKind.Mission);
                Changed?.Invoke();
            }
            return upgraded;
        }

        public void SetClock(int hour, int minute)
        {
            State.Hour = Math.Max(0, Math.Min(23, hour));
            State.Minute = Math.Max(0, Math.Min(59, minute));
            _realSecondsRemainder = 0;
            Changed?.Invoke();
        }

        public void RebuildWorkforce()
        {
            RoadNetwork network = new RoadNetwork(State);
            List<BuildingRecord> workBuildings = State.Buildings
                .Where(building => BuildingCatalog.Get(building.Type, building.Level).WorkSlots > 0)
                .OrderBy(building => building.Id)
                .ToList();

            foreach (ResidentRecord resident in State.Residents)
            {
                resident.WorkplaceId = -1;
                resident.IsGuard = false;
                resident.IsOnMission = false;
            }

            int requiredGuards = Math.Min(State.Population, SecuritySystem.RecommendedGuards(State));
            int assignedGuards = 0;
            foreach (BuildingRecord post in workBuildings.Where(item => item.Type == BuildingType.GuardPost))
            {
                BuildingDefinition definition = BuildingCatalog.Get(post.Type, post.Level);
                for (int slot = 0; slot < definition.WorkSlots && assignedGuards < requiredGuards; slot++)
                {
                    ResidentRecord resident = State.Residents
                        .Where(item => item.WorkplaceId < 0)
                        .OrderBy(item => item.Id)
                        .FirstOrDefault(item => network.BuildingsConnected(State.FindBuilding(item.HomeId), post));
                    if (resident == null) break;
                    resident.WorkplaceId = post.Id;
                    resident.IsGuard = true;
                    assignedGuards++;
                }
            }

            foreach (BuildingRecord workplace in workBuildings.Where(item => item.Type != BuildingType.GuardPost))
            {
                BuildingDefinition definition = BuildingCatalog.Get(workplace.Type, workplace.Level);
                for (int slot = 0; slot < definition.WorkSlots; slot++)
                {
                    ResidentRecord resident = State.Residents
                        .Where(item => item.WorkplaceId < 0)
                        .OrderBy(item => item.Id)
                        .FirstOrDefault(item => network.BuildingsConnected(State.FindBuilding(item.HomeId), workplace));
                    if (resident == null) break;
                    resident.WorkplaceId = workplace.Id;
                    resident.TravelRemainingMinutes = 2 + (resident.Id % 5);
                }
            }

            HashSet<int> away = new HashSet<int>();
            foreach (ActiveMissionRecord mission in State.ActiveMissions)
            {
                foreach (int id in mission.MemberIds) away.Add(id);
            }

            foreach (ResidentRecord resident in State.Residents)
            {
                resident.IsOnMission = away.Contains(resident.Id);
            }
        }

        public void AddNotification(string message, NotificationKind kind)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (State.Notifications == null) State.Notifications = new List<VillageNotification>();
            State.Notifications.Add(new VillageNotification
            {
                Id = State.NextNotificationId++,
                Message = message,
                Kind = kind,
                Day = State.Day,
                Hour = State.Hour,
                Minute = State.Minute,
                Read = false
            });
            while (State.Notifications.Count > 100) State.Notifications.RemoveAt(0);
            Changed?.Invoke();
        }

        public void MarkNotificationsRead()
        {
            foreach (VillageNotification notification in State.Notifications) notification.Read = true;
            Changed?.Invoke();
        }

        public List<GridPoint> GetRoute(int fromBuildingId, int toBuildingId)
        {
            return new RoadNetwork(State).FindPath(State.FindBuilding(fromBuildingId), State.FindBuilding(toBuildingId));
        }

        private void AdvanceOneMinute()
        {
            bool duringWork = State.Hour >= 8 && State.Hour < 18;
            if (State.Hour == 8 && State.Minute == 0)
            {
                foreach (ResidentRecord resident in State.Residents)
                {
                    if (resident.WorkplaceId >= 0 && !resident.IsGuard && !resident.IsOnMission)
                        resident.TravelRemainingMinutes = 2 + (resident.Id % 5);
                }
            }

            if (duringWork)
            {
                AdvanceWorkMinute();
            }

            FinanceSystem.AdvanceMinute(State);
            MissionSystem.AdvanceMinute(State, this);
            SecuritySystem.AdvanceMinute(State, this);
            AdvanceSettlementMinute();
            AdvanceClockMinute();
        }

        private void AdvanceWorkMinute()
        {
            double income = 0;
            double researchHours = 0;
            Dictionary<int, int> workersByBuilding = new Dictionary<int, int>();

            foreach (ResidentRecord resident in State.Residents)
            {
                if (resident.WorkplaceId < 0 || resident.IsOnMission) continue;
                if (resident.TravelRemainingMinutes > 0)
                {
                    resident.TravelRemainingMinutes = Math.Max(0, resident.TravelRemainingMinutes - 1);
                    continue;
                }

                BuildingRecord workplace = State.FindBuilding(resident.WorkplaceId);
                if (workplace == null) continue;
                workersByBuilding.TryGetValue(workplace.Id, out int count);
                workersByBuilding[workplace.Id] = count + 1;
                BuildingDefinition definition = BuildingCatalog.Get(workplace.Type, workplace.Level);
                if (!resident.IsGuard) income += definition.GoldPerWorkerHour / 60.0;
                if (workplace.Type == BuildingType.ResearchCenter && State.HasActiveResearch)
                    researchHours += definition.ResearchRate / 60.0;
            }

            income *= IncomeMultiplier();
            if (income > 0)
            {
                State.Gold += income;
                State.EarnedGold += income;
            }

            foreach (KeyValuePair<int, int> item in workersByBuilding)
            {
                BuildingRecord building = State.FindBuilding(item.Key);
                if (building == null) continue;
                Produce(building, item.Value);
            }

            ResearchSystem.AdvanceWork(State, researchHours, this);
        }

        private void Produce(BuildingRecord building, int workers)
        {
            if (workers <= 0) return;
            BuildingDefinition definition = BuildingCatalog.Get(building.Type, building.Level);
            if (definition.TimberPerWorkerHour > 0)
                AddProduction(ref building.TimberProgress, workers * definition.TimberPerWorkerHour / 60.0, ref State.Stock.Timber);
            if (definition.StonePerWorkerHour > 0)
                AddProduction(ref building.StoneProgress, workers * definition.StonePerWorkerHour / 60.0, ref State.Stock.Stone);
            if (definition.IronPerWorkerHour > 0)
                AddProduction(ref building.IronProgress, workers * definition.IronPerWorkerHour / 60.0, ref State.Stock.Iron);
            double foodMultiplier = building.Type == BuildingType.Farm ? ResearchSystem.FarmMultiplier(State) : 1;
            if (definition.FoodPerWorkerHour > 0)
                AddProduction(ref building.FoodProgress,
                    workers * definition.FoodPerWorkerHour * foodMultiplier / 60.0, ref State.Stock.Food);
        }

        private static void AddProduction(ref double progress, double amount, ref int stock)
        {
            progress += amount;
            if (progress < 1) return;
            int whole = (int)Math.Floor(progress);
            int added = Math.Min(whole, ResourceStock.Maximum - stock);
            stock += added;
            progress = stock >= ResourceStock.Maximum ? 0 : progress - whole;
        }

        private void AdvanceSettlementMinute()
        {
            if (Population > 0)
            {
                HashSet<int> servedResidents = GetServedResidentIds();
                foreach (ResidentRecord resident in State.Residents)
                {
                    if (resident.WorkplaceId >= 0 && servedResidents.Contains(resident.Id)) resident.NeedHours = 0;
                    else resident.NeedHours += 1.0 / 60.0;
                }
            }

            State.ImmigrationProgressMinutes += 1;
            if (State.ImmigrationProgressMinutes < 30) return;
            State.ImmigrationProgressMinutes -= 30;

            ResidentRecord departure = State.Residents
                .Where(resident => resident.NeedHours >= 12)
                .OrderByDescending(resident => resident.NeedHours)
                .ThenBy(resident => resident.Id)
                .FirstOrDefault();
            if (departure != null)
            {
                State.Residents.Remove(departure);
                AddNotification("Um morador deixou a vila após 12 horas sem emprego ou lazer.", NotificationKind.Population);
                RebuildWorkforce();
                return;
            }

            if (!CanAttractResident()) return;
            BuildingRecord homeForNewResident = FindBestHomeForArrival();
            if (homeForNewResident == null) return;

            ResidentRecord arrival = new ResidentRecord
            {
                Id = State.NextResidentId++,
                HomeId = homeForNewResident.Id,
                PortraitSeed = State.NextResidentId * 17,
                NeedHours = 0
            };
            State.Residents.Add(arrival);
            State.PopulationRecord = Math.Max(State.PopulationRecord, State.Population);
            RebuildWorkforce();
            AddNotification("Um novo morador chegou à vila.", NotificationKind.Population);
        }

        private bool CanAttractResident()
        {
            if (State.Population >= State.HousingCapacity) return false;
            if (EmployedCount < Population || ServedCount < Population) return false;
            return FindBestHomeForArrival() != null;
        }

        private BuildingRecord FindBestHomeForArrival()
        {
            RoadNetwork network = new RoadNetwork(State);
            List<BuildingRecord> homes = State.Buildings
                .Where(building => building.Type == BuildingType.House)
                .OrderBy(building => HouseholdCount(building.Id) / (double)Math.Max(1, EffectiveCapacity(building)))
                .ThenBy(building => building.Id)
                .ToList();
            List<BuildingRecord> work = State.Buildings
                .Where(building => BuildingCatalog.Get(building.Type, building.Level).WorkSlots > 0
                    && building.Type != BuildingType.GuardPost)
                .ToList();
            List<BuildingRecord> leisure = State.Buildings
                .Where(building => BuildingCatalog.Get(building.Type, building.Level).LeisureCapacity > 0)
                .ToList();

            foreach (BuildingRecord home in homes)
            {
                int vacancies = EffectiveCapacity(home) - HouseholdCount(home.Id);
                if (vacancies <= 0 || !network.BuildingHasRoadAccess(home)) continue;

                bool hasOpenJob = work.Any(place =>
                    network.BuildingsConnected(home, place) && AvailableJobs(place) > 0);
                bool hasOpenLeisure = leisure.Any(place =>
                    network.BuildingsConnected(home, place) && AvailableLeisure(place) > 0);
                if (hasOpenJob && hasOpenLeisure) return home;
            }

            return null;
        }

        private int AvailableJobs(BuildingRecord building)
        {
            BuildingDefinition definition = BuildingCatalog.Get(building.Type, building.Level);
            int occupied = State.Residents.Count(resident => resident.WorkplaceId == building.Id);
            return Math.Max(0, definition.WorkSlots - occupied);
        }

        private int AvailableLeisure(BuildingRecord building)
        {
            BuildingDefinition definition = BuildingCatalog.Get(building.Type, building.Level);
            int connectedUsers = 0;
            RoadNetwork network = new RoadNetwork(State);
            foreach (ResidentRecord resident in State.Residents)
            {
                if (network.BuildingsConnected(State.FindBuilding(resident.HomeId), building)) connectedUsers++;
            }
            return Math.Max(0, definition.LeisureCapacity - connectedUsers);
        }

        private int HouseholdCount(int houseId)
        {
            return State.Residents.Count(resident => resident.HomeId == houseId);
        }

        private HashSet<int> GetServedResidentIds()
        {
            HashSet<int> servedResidents = new HashSet<int>();
            Dictionary<int, int> usedCapacity = new Dictionary<int, int>();
            RoadNetwork network = new RoadNetwork(State);
            List<BuildingRecord> leisure = State.Buildings
                .Where(building => BuildingCatalog.Get(building.Type, building.Level).LeisureCapacity > 0)
                .OrderBy(building => building.Id)
                .ToList();

            foreach (ResidentRecord resident in State.Residents.OrderBy(item => item.HomeId).ThenBy(item => item.Id))
            {
                BuildingRecord home = State.FindBuilding(resident.HomeId);
                foreach (BuildingRecord place in leisure)
                {
                    BuildingDefinition definition = BuildingCatalog.Get(place.Type, place.Level);
                    usedCapacity.TryGetValue(place.Id, out int used);
                    if (used >= definition.LeisureCapacity || !network.BuildingsConnected(home, place)) continue;
                    usedCapacity[place.Id] = used + 1;
                    servedResidents.Add(resident.Id);
                    break;
                }
            }

            return servedResidents;
        }

        private int EffectiveCapacity(BuildingRecord building)
        {
            BuildingDefinition definition = BuildingCatalog.Get(building.Type, building.Level);
            return definition.HousingCapacity > 0 ? definition.HousingCapacity : building.HouseCapacity;
        }

        private double IncomeMultiplier()
        {
            double bankBonus = 0;
            int bankLimit = Population / 1000;
            foreach (BuildingRecord bank in State.Buildings
                .Where(item => item.Type == BuildingType.Bank)
                .OrderBy(item => item.Id)
                .Take(bankLimit))
            {
                bankBonus += BuildingCatalog.Get(bank.Type, bank.Level).BankIncomeBonus > 0
                    ? BuildingCatalog.Get(bank.Type, bank.Level).BankIncomeBonus
                    : 0.05;
            }

            return 1 + bankBonus + State.CityToolsLevel * 0.05;
        }

        private void AdvanceClockMinute()
        {
            State.Minute++;
            if (State.Minute < 60) return;
            State.Minute = 0;
            State.Hour++;
            if (State.Hour < 24) return;
            State.Hour = 0;
            State.Day++;
            AddNotification("Começou o dia " + State.Day + " na vila.", NotificationKind.General);
        }

        private int RollHouseCapacity()
        {
            uint value;
            unchecked
            {
                State.Security.RandomState = State.Security.RandomState * 1664525u + 1013904223u;
                value = State.Security.RandomState;
            }

            int roll = (int)(value % 100);
            if (roll < 30) return 1;
            if (roll < 60) return 2;
            if (roll < 85) return 3;
            if (roll < 95) return 4;
            return 5;
        }

        private void CancelMission(ActiveMissionRecord mission)
        {
            foreach (int residentId in mission.MemberIds)
            {
                ResidentRecord resident = State.FindResident(residentId);
                if (resident != null) resident.IsOnMission = false;
            }
            State.ActiveMissions.Remove(mission);
            AddNotification("Missão cancelada: a Taverna Real deixou de existir.", NotificationKind.Mission);
        }

        private void PushUndo(UndoEntry entry)
        {
            _undoHistory.Add(entry);
            if (_undoHistory.Count > 20) _undoHistory.RemoveAt(0);
        }

        private static BuildingRecord CopyBuilding(BuildingRecord source)
        {
            return new BuildingRecord
            {
                Id = source.Id, Type = source.Type, Level = source.Level, X = source.X, Z = source.Z,
                Rotation = source.Rotation, HouseCapacity = source.HouseCapacity,
                TimberProgress = source.TimberProgress, StoneProgress = source.StoneProgress,
                IronProgress = source.IronProgress, FoodProgress = source.FoodProgress
            };
        }

        private static ResidentRecord CopyResident(ResidentRecord source)
        {
            return new ResidentRecord
            {
                Id = source.Id, HomeId = source.HomeId, WorkplaceId = source.WorkplaceId,
                IsGuard = source.IsGuard, IsOnMission = source.IsOnMission, PortraitSeed = source.PortraitSeed,
                NeedHours = source.NeedHours, TravelRemainingMinutes = source.TravelRemainingMinutes
            };
        }

        private void NormalizeState()
        {
            if (State.Stock == null) State.Stock = new ResourceStock();
            if (State.Loan == null) State.Loan = new LoanRecord();
            if (State.Security == null) State.Security = new SecurityRecord();
            if (State.Buildings == null) State.Buildings = new List<BuildingRecord>();
            if (State.Residents == null) State.Residents = new List<ResidentRecord>();
            if (State.CompletedResearch == null) State.CompletedResearch = new List<ResearchId>();
            if (State.ActiveMissions == null) State.ActiveMissions = new List<ActiveMissionRecord>();
            if (State.Notifications == null) State.Notifications = new List<VillageNotification>();
            State.Speed = State.Speed == 2 || State.Speed == 5 || State.Speed == 10 ? State.Speed : 1;
            State.TerritorySize = Math.Max(32, Math.Min(48, State.TerritorySize));
            State.Hour = Math.Max(0, Math.Min(23, State.Hour));
            State.Minute = Math.Max(0, Math.Min(59, State.Minute));
            if (string.IsNullOrWhiteSpace(State.Id)) State.Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(State.Name)) State.Name = "Minha Vila";
        }
    }
}
