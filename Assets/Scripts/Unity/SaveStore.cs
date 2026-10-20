using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MinhaVila.Core;
using UnityEngine;

namespace MinhaVila.Unity
{
    public sealed class VillageSummary
    {
        public string Id;
        public string Name;
        public int Population;
        public double Gold;
        public int Day;
        public string LastSavedUtc;
    }

    public sealed class SaveStore
    {
        private readonly string _directory;

        public SaveStore()
        {
            _directory = Path.Combine(Application.persistentDataPath, "villages");
        }

        public bool Save(VillageState state, out string message)
        {
            message = string.Empty;
            if (state == null || string.IsNullOrWhiteSpace(state.Id))
            {
                message = "Não foi possível salvar uma vila sem identificação.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(_directory);
                state.LastSavedUtc = DateTime.UtcNow.ToString("O");
                string path = PathFor(state.Id);
                string temporaryPath = path + ".tmp";
                string backupPath = path + ".bak";
                string json = JsonUtility.ToJson(state, true);
                File.WriteAllText(temporaryPath, json);

                if (File.Exists(path))
                {
                    File.Copy(path, backupPath, true);
                    File.Delete(path);
                }

                File.Move(temporaryPath, path);
                message = "Vila salva.";
                return true;
            }
            catch (Exception exception)
            {
                message = "Falha ao salvar. A partida continua aberta. " + exception.Message;
                return false;
            }
        }

        public bool TryLoad(string id, out VillageState state, out string message)
        {
            state = null;
            message = string.Empty;
            string path = PathFor(id);
            if (!File.Exists(path))
            {
                message = "O arquivo da vila não foi encontrado.";
                return false;
            }

            if (TryReadValid(path, out state))
            {
                message = "Vila carregada.";
                return true;
            }

            string backup = path + ".bak";
            if (File.Exists(backup) && TryReadValid(backup, out state))
            {
                message = "A gravação principal estava inválida; a cópia de segurança foi carregada.";
                return true;
            }

            message = "A gravação é inválida e não substituiu a partida atual.";
            return false;
        }

        public List<VillageSummary> ListSaves()
        {
            List<VillageSummary> result = new List<VillageSummary>();
            if (!Directory.Exists(_directory)) return result;

            string[] files;
            try
            {
                files = Directory.GetFiles(_directory, "*.json");
            }
            catch (IOException)
            {
                return result;
            }
            catch (UnauthorizedAccessException)
            {
                return result;
            }

            foreach (string path in files)
            {
                if (!TryReadValid(path, out VillageState state)) continue;
                result.Add(new VillageSummary
                {
                    Id = state.Id,
                    Name = state.Name,
                    Population = state.Population,
                    Gold = state.Gold,
                    Day = state.Day,
                    LastSavedUtc = state.LastSavedUtc
                });
            }

            return result.OrderByDescending(item => item.LastSavedUtc).ToList();
        }

        public bool Delete(string id)
        {
            try
            {
                string path = PathFor(id);
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private string PathFor(string id)
        {
            string safeId = new string((id ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .Take(64)
                .ToArray());
            return Path.Combine(_directory, safeId + ".json");
        }

        private static bool TryReadValid(string path, out VillageState state)
        {
            state = null;
            try
            {
                string json = File.ReadAllText(path);
                VillageState candidate = JsonUtility.FromJson<VillageState>(json);
                if (!IsValid(candidate)) return false;
                state = candidate;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is FormatException)
            {
                return false;
            }
        }

        private static bool IsValid(VillageState state)
        {
            if (state == null || state.SaveVersion != VillageState.CurrentSaveVersion
                || string.IsNullOrWhiteSpace(state.Id) || state.Id.Length > 64
                || string.IsNullOrWhiteSpace(state.Name) || state.Name.Length > 80
                || state.Day < 1 || state.Hour < 0 || state.Hour > 23 || state.Minute < 0 || state.Minute > 59
                || (state.Speed != 1 && state.Speed != 2 && state.Speed != 5 && state.Speed != 10)
                || state.TerritorySize < 32 || state.TerritorySize > 48 || state.TerritorySize % 4 != 0
                || double.IsNaN(state.Gold) || double.IsInfinity(state.Gold) || state.Gold < 0
                || state.Stock == null || state.Loan == null || state.Security == null
                || state.Buildings == null || state.Residents == null || state.CompletedResearch == null
                || state.ActiveMissions == null || state.Notifications == null
                || !IsValidStock(state.Stock)
                || state.CameraZoom < 8 || state.CameraZoom > 34 || double.IsNaN(state.CameraZoom) || double.IsInfinity(state.CameraZoom)
                || double.IsNaN(state.CameraX) || double.IsInfinity(state.CameraX) || Math.Abs(state.CameraX) > 22
                || double.IsNaN(state.CameraZ) || double.IsInfinity(state.CameraZ) || Math.Abs(state.CameraZ) > 22
                || state.Loan.Debt < 0 || state.Loan.Principal < 0 || state.Loan.Paid < 0 || state.Loan.Interest < 0
                || state.Loan.InterestDays < 0 || state.Loan.ElapsedHours < 0
                || double.IsNaN(state.Loan.ElapsedHours) || double.IsInfinity(state.Loan.ElapsedHours)
                || state.Security.Disorder < 0 || state.Security.Disorder > 100
                || double.IsNaN(state.Security.Disorder) || double.IsInfinity(state.Security.Disorder)
                || state.Security.ActiveHours < 0 || double.IsNaN(state.Security.ActiveHours)
                || double.IsInfinity(state.Security.ActiveHours)
                || double.IsNaN(state.Security.PeaceRemainingHours) || double.IsInfinity(state.Security.PeaceRemainingHours)
                || state.Security.ThreatPhaseRemainingHours < 0 || double.IsNaN(state.Security.ThreatPhaseRemainingHours)
                || double.IsInfinity(state.Security.ThreatPhaseRemainingHours)
                || state.Security.HasActiveThreat && !Enum.IsDefined(typeof(ThreatType), state.Security.ActiveThreat))
            {
                return false;
            }

            HashSet<int> buildingIds = new HashSet<int>();
            HashSet<GridPoint> occupied = new HashSet<GridPoint>();
            foreach (BuildingRecord building in state.Buildings)
            {
                if (building == null || building.Id < 1 || !buildingIds.Add(building.Id)
                    || !Enum.IsDefined(typeof(BuildingType), building.Type)
                    || building.Level < 1 || building.Level > 2
                    || BuildingCatalog.Get(building.Type, building.Level) == null
                    || building.Rotation < 0 || building.Rotation > 3
                    || !LandSystem.IsOwned(state, building.X, building.Z)
                    || !occupied.Add(new GridPoint(building.X, building.Z)))
                {
                    return false;
                }

                if (building.Type == BuildingType.House && building.Level == 1
                    && (building.HouseCapacity < 1 || building.HouseCapacity > 5))
                {
                    return false;
                }

                if (double.IsNaN(building.TimberProgress) || double.IsInfinity(building.TimberProgress) || building.TimberProgress < 0
                    || double.IsNaN(building.StoneProgress) || double.IsInfinity(building.StoneProgress) || building.StoneProgress < 0
                    || double.IsNaN(building.IronProgress) || double.IsInfinity(building.IronProgress) || building.IronProgress < 0
                    || double.IsNaN(building.FoodProgress) || double.IsInfinity(building.FoodProgress) || building.FoodProgress < 0)
                {
                    return false;
                }
            }

            HashSet<int> residentIds = new HashSet<int>();
            foreach (ResidentRecord resident in state.Residents)
            {
                if (resident == null) return false;
                BuildingRecord home = state.FindBuilding(resident.HomeId);
                if (resident.Id < 1 || !residentIds.Add(resident.Id)
                    || home == null || home.Type != BuildingType.House
                    || resident.NeedHours < 0 || double.IsNaN(resident.NeedHours) || double.IsInfinity(resident.NeedHours)
                    || resident.TravelRemainingMinutes < 0 || double.IsNaN(resident.TravelRemainingMinutes)
                    || double.IsInfinity(resident.TravelRemainingMinutes))
                {
                    return false;
                }

                if (resident.WorkplaceId >= 0)
                {
                    BuildingRecord workplace = state.FindBuilding(resident.WorkplaceId);
                    if (workplace == null || BuildingCatalog.Get(workplace.Type, workplace.Level).WorkSlots <= 0)
                        return false;
                }
            }

            foreach (BuildingRecord house in state.Buildings.Where(item => item.Type == BuildingType.House))
            {
                BuildingDefinition definition = BuildingCatalog.Get(house.Type, house.Level);
                int capacity = definition.HousingCapacity > 0 ? definition.HousingCapacity : house.HouseCapacity;
                if (state.Residents.Count(resident => resident.HomeId == house.Id) > capacity) return false;
            }

            if (state.CompletedResearch.Distinct().Count() != state.CompletedResearch.Count
                || state.CompletedResearch.Any(id => ResearchCatalog.Get(id) == null))
            {
                return false;
            }

            foreach (ResearchId id in state.CompletedResearch)
            {
                ResearchDefinition definition = ResearchCatalog.Get(id);
                if (definition.Prerequisite.HasValue && !state.CompletedResearch.Contains(definition.Prerequisite.Value))
                    return false;
            }

            if (state.HasActiveResearch)
            {
                ResearchDefinition active = ResearchCatalog.Get(state.ActiveResearch);
                if (active == null || ResearchCatalog.IsCompleted(state, state.ActiveResearch)
                    || state.ResearchProgressHours < 0 || state.ResearchProgressHours >= active.RequiredHours)
                    return false;
                if (active.Prerequisite.HasValue && !state.CompletedResearch.Contains(active.Prerequisite.Value))
                    return false;
            }

            HashSet<int> missionIds = new HashSet<int>();
            HashSet<int> assignedResidents = new HashSet<int>();
            foreach (ActiveMissionRecord mission in state.ActiveMissions)
            {
                MissionDefinition definition = mission == null ? null : MissionSystem.Get(mission.Type);
                if (mission == null || mission.Id < 1 || !missionIds.Add(mission.Id)
                    || !Enum.IsDefined(typeof(MissionType), mission.Type)
                    || definition == null
                    || mission.TavernId < 1 || state.FindBuilding(mission.TavernId) == null
                    || state.FindBuilding(mission.TavernId).Type != BuildingType.RoyalTavern
                    || mission.RemainingHours <= 0 || double.IsNaN(mission.RemainingHours)
                    || double.IsInfinity(mission.RemainingHours) || mission.MemberIds == null
                    || mission.MemberIds.Count != definition.Crew)
                {
                    return false;
                }

                foreach (int residentId in mission.MemberIds)
                {
                    if (state.FindResident(residentId) == null || !assignedResidents.Add(residentId)) return false;
                }
            }

            if (state.Notifications.Any(notification => notification == null || notification.Id < 1
                || string.IsNullOrWhiteSpace(notification.Message) || !Enum.IsDefined(typeof(NotificationKind), notification.Kind)))
                return false;

            return true;
        }

        private static bool IsValidStock(ResourceStock stock)
        {
            return stock.Timber >= 0 && stock.Timber <= ResourceStock.Maximum
                && stock.Stone >= 0 && stock.Stone <= ResourceStock.Maximum
                && stock.Iron >= 0 && stock.Iron <= ResourceStock.Maximum
                && stock.Food >= 0 && stock.Food <= ResourceStock.Maximum
                && stock.Relics >= 0 && stock.Relics <= ResourceStock.Maximum;
        }
    }
}
