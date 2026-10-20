using System;
using System.Collections.Generic;

namespace MinhaVila.Core
{
    [Serializable]
    public sealed class ResourceStock
    {
        public const int Maximum = 1000000000;
        public int Timber = 40;
        public int Stone = 60;
        public int Iron = 30;
        public int Food;
        public int Relics;

        public ResourceStock Copy()
        {
            return new ResourceStock { Timber = Timber, Stone = Stone, Iron = Iron, Food = Food, Relics = Relics };
        }
    }

    [Serializable]
    public sealed class BuildingRecord
    {
        public int Id;
        public BuildingType Type;
        public int Level = 1;
        public int X;
        public int Z;
        public int Rotation;
        public int HouseCapacity;
        public double TimberProgress;
        public double StoneProgress;
        public double IronProgress;
        public double FoodProgress;
    }

    [Serializable]
    public sealed class ResidentRecord
    {
        public int Id;
        public int HomeId;
        public int WorkplaceId = -1;
        public bool IsGuard;
        public bool IsOnMission;
        public int PortraitSeed;
        public double NeedHours;
        public double TravelRemainingMinutes;
    }

    [Serializable]
    public sealed class ActiveMissionRecord
    {
        public int Id;
        public MissionType Type;
        public int TavernId;
        public double RemainingHours;
        public List<int> MemberIds = new List<int>();
    }

    [Serializable]
    public sealed class VillageNotification
    {
        public int Id;
        public string Message;
        public NotificationKind Kind;
        public int Day;
        public int Hour;
        public int Minute;
        public bool Read;
    }

    [Serializable]
    public sealed class LoanRecord
    {
        public int Principal;
        public int Fee;
        public int Paid;
        public int Interest;
        public int InterestDays;
        public int Debt;
        public double ElapsedHours;
    }

    [Serializable]
    public sealed class SecurityRecord
    {
        public double Disorder;
        public double ActiveHours;
        public double PeaceRemainingHours = 72;
        public uint RandomState = 184467u;
        public int Sequence;
        public int Repelled;
        public int Breached;
        public ThreatType ActiveThreat;
        public bool HasActiveThreat;
        public bool IsBattle;
        public double ThreatPhaseRemainingHours;
        public int ThreatStrength;
        public int ThreatSide;
        public ThreatType LastThreat;
        public string LastOutcome = string.Empty;
        public int LastLoot;
    }

    [Serializable]
    public sealed class VillageState
    {
        public const int CurrentSaveVersion = 1;
        public int SaveVersion = CurrentSaveVersion;
        public string Id;
        public string Name;
        public string LastSavedUtc = string.Empty;
        public string Scenario = string.Empty;
        public int Day = 1;
        public int Hour = 8;
        public int Minute;
        public int Speed = 1;
        public bool Paused;
        public int TerritorySize = 32;
        public int PopulationRecord;
        public int NextBuildingId = 1;
        public int NextResidentId = 1;
        public int NextNotificationId = 1;
        public int NextMissionId = 1;
        public double Gold = 1000;
        public double EarnedGold;
        public double SpentGold;
        public double AdjustedGold;
        public double LootedGold;
        public ResourceStock Stock = new ResourceStock();
        public LoanRecord Loan = new LoanRecord();
        public SecurityRecord Security = new SecurityRecord();
        public List<BuildingRecord> Buildings = new List<BuildingRecord>();
        public List<ResidentRecord> Residents = new List<ResidentRecord>();
        public List<ResearchId> CompletedResearch = new List<ResearchId>();
        public ResearchId ActiveResearch;
        public bool HasActiveResearch;
        public double ResearchProgressHours;
        public List<ActiveMissionRecord> ActiveMissions = new List<ActiveMissionRecord>();
        public int MissionSequence;
        public int CompletedMissions;
        public int CompletedExpeditions;
        public int CityToolsLevel;
        public int ReinforcedWallsLevel;
        public double ImmigrationProgressMinutes;
        public List<VillageNotification> Notifications = new List<VillageNotification>();
        public double CameraX;
        public double CameraZ;
        public double CameraZoom = 18;
        public bool BuildMode;

        public int Population { get { return Residents == null ? 0 : Residents.Count; } }
        public int HousingCapacity
        {
            get
            {
                int total = 0;
                if (Buildings == null) return total;
                foreach (BuildingRecord building in Buildings)
                {
                    if (building.Type != BuildingType.House) continue;
                    BuildingDefinition definition = BuildingCatalog.Get(building.Type, building.Level);
                    total += definition.HousingCapacity > 0 ? definition.HousingCapacity : building.HouseCapacity;
                }
                return total;
            }
        }

        public static VillageState CreateNew(string name)
        {
            return new VillageState
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = string.IsNullOrWhiteSpace(name) ? "Minha Vila" : name.Trim(),
                Day = 1, Hour = 8, Minute = 0, Speed = 1, Paused = false, TerritorySize = 32,
                Gold = 1000, Stock = new ResourceStock(), Loan = new LoanRecord(), Security = new SecurityRecord(),
                Buildings = new List<BuildingRecord>(), Residents = new List<ResidentRecord>(),
                CompletedResearch = new List<ResearchId>(), ActiveMissions = new List<ActiveMissionRecord>(),
                Notifications = new List<VillageNotification>(), NextBuildingId = 1, NextResidentId = 1,
                NextNotificationId = 1, NextMissionId = 1
            };
        }

        public BuildingRecord FindBuilding(int id) { return Buildings.Find(building => building.Id == id); }
        public BuildingRecord FindBuildingAt(int x, int z) { return Buildings.Find(building => building.X == x && building.Z == z); }
        public ResidentRecord FindResident(int id) { return Residents.Find(resident => resident.Id == id); }
    }
}
