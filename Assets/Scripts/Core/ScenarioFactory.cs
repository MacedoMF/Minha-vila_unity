using System;
using System.Collections.Generic;
using System.Linq;

namespace MinhaVila.Core
{
    public sealed class ScenarioDefinition
    {
        public string Id;
        public string Name;
        public string Description;

        public ScenarioDefinition(string id, string name, string description)
        {
            Id = id; Name = name; Description = description;
        }
    }

    public static class ScenarioFactory
    {
        private static readonly ScenarioDefinition[] Definitions =
        {
            new ScenarioDefinition("dragon", "Dragão sobre a cidade", "Uma cidade grande enfrenta a ameaça mais perigosa."),
            new ScenarioDefinition("bandits", "Invasão de bandidos", "Proteja os moradores e o tesouro da vila."),
            new ScenarioDefinition("goblins", "Goblins contra a guarda", "Uma pequena cidade se prepara para o combate."),
            new ScenarioDefinition("prosperous", "Cidade próspera", "Uma comunidade estabelecida com economia e serviços ativos."),
            new ScenarioDefinition("disorder", "Desordem nas ruas", "Restaure os serviços e recupere a segurança."),
            new ScenarioDefinition("housing", "Falta de moradia", "A população cresceu e precisa de novas casas."),
            new ScenarioDefinition("lands", "Novas terras", "A vila alcançou população suficiente para expandir."),
            new ScenarioDefinition("debt", "Vila endividada", "Administre os custos e quite o empréstimo.")
        };

        public static IReadOnlyList<ScenarioDefinition> All { get { return Definitions; } }

        public static VillageState CreateStarter()
        {
            VillageState state = VillageState.CreateNew("Minha Vila");
            state.Scenario = "starter";

            for (int x = 18; x <= 30; x++) AddRoad(state, x, 24);
            for (int z = 20; z <= 28; z++) AddRoad(state, 24, z);
            AddRoad(state, 21, 23);
            AddRoad(state, 23, 23);
            AddRoad(state, 27, 23);
            AddRoad(state, 21, 25);
            AddRoad(state, 27, 25);
            AddRoad(state, 22, 25);

            BuildingRecord[] homes =
            {
                AddBuilding(state, BuildingType.House, new GridPoint(21, 22)),
                AddBuilding(state, BuildingType.House, new GridPoint(27, 22)),
                AddBuilding(state, BuildingType.House, new GridPoint(21, 26)),
                AddBuilding(state, BuildingType.House, new GridPoint(27, 26))
            };

            AddBuilding(state, BuildingType.Tavern, new GridPoint(20, 23));
            AddBuilding(state, BuildingType.Farm, new GridPoint(25, 23));
            AddBuilding(state, BuildingType.Sawmill, new GridPoint(22, 26));
            AddBuilding(state, BuildingType.Quarry, new GridPoint(26, 25));
            AddBuilding(state, BuildingType.ResearchCenter, new GridPoint(22, 23));
            AddBuilding(state, BuildingType.GuardPost, new GridPoint(26, 23));

            for (int i = 0; i < 8; i++)
            {
                state.Residents.Add(MakeResident(state, homes[i % homes.Length].Id));
            }

            state.PopulationRecord = state.Population;
            return state;
        }

        public static VillageState Create(string id)
        {
            ScenarioDefinition definition = Definitions.FirstOrDefault(item => item.Id == id);
            if (definition == null) return VillageState.CreateNew("Minha Vila");
            VillageState state = VillageState.CreateNew(definition.Name);
            state.Scenario = definition.Id;
            state.Gold = 12000;
            state.Stock = new ResourceStock { Timber = 600, Stone = 800, Iron = 300, Food = 150, Relics = 8 };

            if (id == "dragon")
            {
                state.Name = "Vila do Dragão";
                state.TerritorySize = 48;
                state.Gold = 80000;
                state.Stock = new ResourceStock { Timber = 10000, Stone = 12000, Iron = 4000, Food = 1000, Relics = 20 };
                BuildTown(state, 300, 38);
                state.Security.ActiveHours = 300;
                StartThreat(state, ThreatType.Dragon);
            }
            else if (id == "bandits")
            {
                BuildTown(state, 58, 12);
                state.Gold = 16000;
                state.Security.Disorder = 25;
                state.Security.ActiveHours = 100;
                StartThreat(state, ThreatType.Bandits);
            }
            else if (id == "goblins")
            {
                BuildTown(state, 24, 8);
                state.Security.ActiveHours = 72;
                StartThreat(state, ThreatType.Goblins);
            }
            else if (id == "prosperous")
            {
                BuildTown(state, 48, 16);
                state.Gold = 95000;
                state.CompletedResearch.AddRange(new[]
                {
                    ResearchId.Irrigation, ResearchId.Orchards, ResearchId.GrainMilling,
                    ResearchId.Masonry, ResearchId.AdvancedCarpentry, ResearchId.CommerceAndHospitality
                });
                state.CityToolsLevel = 2;
                state.CompletedExpeditions = 3;
            }
            else if (id == "disorder")
            {
                BuildTown(state, 36, 9);
                state.Security.Disorder = 78;
                state.Gold = 4000;
            }
            else if (id == "housing")
            {
                BuildTown(state, 28, 7);
                foreach (BuildingRecord home in state.Buildings.Where(building => building.Type == BuildingType.House))
                {
                    BuildingDefinition definition = BuildingCatalog.Get(home.Type, home.Level);
                    int capacity = definition.HousingCapacity > 0 ? definition.HousingCapacity : home.HouseCapacity;
                    while (state.Residents.Count(resident => resident.HomeId == home.Id) < capacity)
                        state.Residents.Add(MakeResident(state, home.Id));
                }
                state.PopulationRecord = state.Population;
                state.Gold = 6000;
            }
            else if (id == "lands")
            {
                BuildTown(state, 106, 26);
                state.PopulationRecord = 300;
                state.Gold = 700000;
                state.TerritorySize = 36;
            }
            else if (id == "debt")
            {
                BuildTown(state, 32, 10);
                state.Gold = 700;
                state.Loan = new LoanRecord
                {
                    Principal = 2000,
                    Fee = 200,
                    Interest = 80,
                    Debt = 2280,
                    ElapsedHours = 120,
                    InterestDays = 2
                };
                state.Gold = 700;
            }

            state.PopulationRecord = Math.Max(state.PopulationRecord, state.Population);
            return state;
        }

        private static void BuildTown(VillageState state, int population, int houseCount)
        {
            AddRoad(state, 24, 24);
            AddRoad(state, 23, 24);
            AddRoad(state, 25, 24);
            AddRoad(state, 24, 23);
            AddRoad(state, 24, 25);

            List<BuildingRecord> homes = new List<BuildingRecord>();
            for (int i = 0; i < houseCount; i++)
            {
                GridPoint point = FindOpenBuildingPoint(state, 11 + (i % 8) * 2, 10 + (i / 8) * 3);
                int level = population > houseCount * 5 || i % 3 == 0 ? 2 : 1;
                BuildingRecord home = AddBuilding(state, BuildingType.House, point, level);
                home.HouseCapacity = home.Level == 2 ? 8 : 5;
                homes.Add(home);
                ConnectToHub(state, home);
            }

            BuildingType[] services =
            {
                BuildingType.Tavern, BuildingType.RoyalTavern, BuildingType.Guild, BuildingType.Forge,
                BuildingType.Sawmill, BuildingType.Quarry, BuildingType.Stable, BuildingType.Farm,
                BuildingType.ResearchCenter, BuildingType.BardSquare, BuildingType.GuardPost,
                BuildingType.ArcaneTower, BuildingType.Orchard, BuildingType.GrainMill
            };

            int serviceCount = Math.Max(12, (population / 5) + 4);
            for (int i = 0; i < serviceCount; i++)
            {
                BuildingType type = services[i % services.Length];
                if ((type == BuildingType.Orchard || type == BuildingType.GrainMill)
                    && !state.CompletedResearch.Contains(type == BuildingType.Orchard ? ResearchId.Orchards : ResearchId.GrainMilling))
                {
                    state.CompletedResearch.Add(ResearchId.Irrigation);
                    state.CompletedResearch.Add(ResearchId.Orchards);
                    state.CompletedResearch.Add(ResearchId.GrainMilling);
                }

                GridPoint point = FindOpenBuildingPoint(state, 27 + (i % 6) * 2, 10 + (i / 6) * 3);
                BuildingRecord service = AddBuilding(state, type, point, type == BuildingType.Tavern && i % 2 == 0 ? 2 : 1);
                ConnectToHub(state, service);
            }

            for (int i = 0; i < population; i++)
            {
                BuildingRecord home = homes[i % homes.Count];
                if (state.Residents.Count(resident => resident.HomeId == home.Id) >= home.HouseCapacity)
                {
                    home = homes.First(candidate =>
                        state.Residents.Count(resident => resident.HomeId == candidate.Id) < candidate.HouseCapacity);
                }

                state.Residents.Add(MakeResident(state, home.Id));
            }

            state.NextResidentId = state.Residents.Count + 1;
        }

        private static ResidentRecord MakeResident(VillageState state, int homeId)
        {
            return new ResidentRecord
            {
                Id = state.NextResidentId++,
                HomeId = homeId,
                WorkplaceId = -1,
                PortraitSeed = state.NextResidentId * 23,
                NeedHours = 0
            };
        }

        private static GridPoint FindOpenBuildingPoint(VillageState state, int preferredX, int preferredZ)
        {
            for (int radius = 0; radius < 20; radius++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int x = Math.Max(8, Math.Min(39, preferredX + dx));
                        int z = Math.Max(8, Math.Min(39, preferredZ + dz));
                        if (state.FindBuildingAt(x, z) == null && LandSystem.IsOwned(state, x, z))
                            return new GridPoint(x, z);
                    }
                }
            }

            return new GridPoint(preferredX, preferredZ);
        }

        private static BuildingRecord AddBuilding(VillageState state, BuildingType type, GridPoint point, int level = 1)
        {
            BuildingRecord building = new BuildingRecord
            {
                Id = state.NextBuildingId++,
                Type = type,
                Level = level,
                X = point.X,
                Z = point.Z,
                HouseCapacity = type == BuildingType.House ? (level == 2 ? 8 : 5) : 0
            };
            state.Buildings.Add(building);
            return building;
        }

        private static void AddRoad(VillageState state, int x, int z)
        {
            if (x < 0 || z < 0 || x >= 48 || z >= 48 || state.FindBuildingAt(x, z) != null) return;
            state.Buildings.Add(new BuildingRecord
            {
                Id = state.NextBuildingId++,
                Type = BuildingType.Road,
                Level = 1,
                X = x,
                Z = z
            });
        }

        private static void ConnectToHub(VillageState state, BuildingRecord building)
        {
            GridPoint[] starts =
            {
                new GridPoint(building.X + 1, building.Z),
                new GridPoint(building.X - 1, building.Z),
                new GridPoint(building.X, building.Z + 1),
                new GridPoint(building.X, building.Z - 1)
            };
            Queue<GridPoint> queue = new Queue<GridPoint>();
            Dictionary<GridPoint, GridPoint> previous = new Dictionary<GridPoint, GridPoint>();
            foreach (GridPoint start in starts)
            {
                if (start.X < 0 || start.Z < 0 || start.X >= 48 || start.Z >= 48) continue;
                if (BlockedByBuilding(state, start)) continue;
                queue.Enqueue(start);
                previous[start] = start;
            }

            GridPoint hub = new GridPoint(24, 24);
            GridPoint? end = null;
            GridPoint[] directions =
            {
                new GridPoint(1, 0), new GridPoint(-1, 0), new GridPoint(0, 1), new GridPoint(0, -1)
            };
            while (queue.Count > 0)
            {
                GridPoint current = queue.Dequeue();
                if (current == hub || IsAdjacentToRoad(state, current))
                {
                    end = current;
                    break;
                }

                foreach (GridPoint direction in directions)
                {
                    GridPoint next = new GridPoint(current.X + direction.X, current.Z + direction.Z);
                    if (next.X < 8 || next.Z < 8 || next.X > 39 || next.Z > 39 || previous.ContainsKey(next))
                        continue;
                    if (BlockedByBuilding(state, next)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (!end.HasValue) return;
            GridPoint cursor = end.Value;
            while (true)
            {
                AddRoad(state, cursor.X, cursor.Z);
                if (previous[cursor] == cursor) break;
                cursor = previous[cursor];
            }
        }

        private static bool IsAdjacentToRoad(VillageState state, GridPoint point)
        {
            return IsRoad(state, point.X + 1, point.Z) || IsRoad(state, point.X - 1, point.Z)
                || IsRoad(state, point.X, point.Z + 1) || IsRoad(state, point.X, point.Z - 1);
        }

        private static bool IsRoad(VillageState state, int x, int z)
        {
            BuildingRecord building = state.FindBuildingAt(x, z);
            return building != null && building.Type == BuildingType.Road;
        }

        private static bool BlockedByBuilding(VillageState state, GridPoint point)
        {
            BuildingRecord building = state.FindBuildingAt(point.X, point.Z);
            return building != null && building.Type != BuildingType.Road;
        }

        private static void StartThreat(VillageState state, ThreatType type)
        {
            state.Security.Sequence = 1;
            state.Security.ActiveThreat = type;
            state.Security.HasActiveThreat = true;
            state.Security.IsBattle = true;
            state.Security.ThreatPhaseRemainingHours = 1.5;
            state.Security.ThreatStrength = type == ThreatType.Dragon
                ? (int)Math.Ceiling(28 + state.Population / 30.0 + state.Security.Disorder * 0.08)
                : type == ThreatType.Bandits
                    ? (int)Math.Ceiling(6 + state.Population / 40.0 + state.Security.Disorder * 0.08)
                    : (int)Math.Ceiling(3 + state.Population / 50.0 + state.Security.Disorder * 0.08);
        }
    }
}
