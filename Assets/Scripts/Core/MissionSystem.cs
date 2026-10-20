using System;
using System.Collections.Generic;
using System.Linq;

namespace MinhaVila.Core
{
    public sealed class MissionDefinition
    {
        public MissionType Type;
        public string Name;
        public int Crew;
        public double Hours;
        public int GoldReward;
        public ResourceBundle Reward;

        public MissionDefinition(MissionType type, string name, int crew, double hours, int goldReward, ResourceBundle reward)
        {
            Type = type; Name = name; Crew = crew; Hours = hours; GoldReward = goldReward; Reward = reward;
        }
    }

    public static class MissionSystem
    {
        private static readonly Dictionary<MissionType, MissionDefinition> Definitions =
            new Dictionary<MissionType, MissionDefinition>
            {
                {
                    MissionType.Caravan,
                    new MissionDefinition(MissionType.Caravan, "Escoltar a caravana real",
                        2, 6, 40, new ResourceBundle(3, 2, 1))
                },
                {
                    MissionType.RecoverMine,
                    new MissionDefinition(MissionType.RecoverMine, "Recuperar a mina",
                        3, 8, 70, new ResourceBundle(1, 4, 3))
                },
                {
                    MissionType.ExploreRuins,
                    new MissionDefinition(MissionType.ExploreRuins, "Explorar as ruínas",
                        4, 12, 120, new ResourceBundle(0, 2, 2, relics: 1))
                },
                {
                    MissionType.BorderExpedition,
                    new MissionDefinition(MissionType.BorderExpedition, "Expedição às fronteiras",
                        4, 12, 60, new ResourceBundle(1, 1, 1))
                }
            };

        public static IReadOnlyCollection<MissionDefinition> All { get { return Definitions.Values; } }
        public static MissionDefinition Get(MissionType type)
        {
            return Definitions.TryGetValue(type, out MissionDefinition definition) ? definition : null;
        }

        public static bool TryStart(VillageState state, int tavernId, MissionType type, out string message)
        {
            message = string.Empty;
            if (Get(type) == null)
            {
                message = "Missão desconhecida.";
                return false;
            }

            BuildingRecord tavern = state.FindBuilding(tavernId);
            if (tavern == null || tavern.Type != BuildingType.RoyalTavern)
            {
                message = "Construa uma Taverna Real para enviar mercenários.";
                return false;
            }

            if (!new RoadNetwork(state).BuildingHasRoadAccess(tavern))
            {
                message = "Ligue a Taverna Real à rede de ruas.";
                return false;
            }

            MissionDefinition definition = Get(type);
            HashSet<int> alreadyAway = new HashSet<int>();
            foreach (ActiveMissionRecord active in state.ActiveMissions)
            {
                foreach (int id in active.MemberIds) alreadyAway.Add(id);
            }

            List<ResidentRecord> available = state.Residents
                .Where(resident => resident.WorkplaceId == tavernId && !alreadyAway.Contains(resident.Id))
                .OrderBy(resident => resident.Id)
                .Take(definition.Crew)
                .ToList();

            if (available.Count < definition.Crew)
            {
                message = "A Taverna Real precisa de " + definition.Crew + " mercenários com acesso por ruas.";
                return false;
            }

            ActiveMissionRecord record = new ActiveMissionRecord
            {
                Id = state.NextMissionId++,
                Type = type,
                TavernId = tavernId,
                RemainingHours = definition.Hours,
                MemberIds = available.Select(resident => resident.Id).ToList()
            };
            state.ActiveMissions.Add(record);
            foreach (ResidentRecord resident in available) resident.IsOnMission = true;
            message = definition.Name + " iniciada. Retorno previsto em " + definition.Hours + " horas da vila.";
            return true;
        }

        public static bool TryUpgrade(VillageState state, bool improveTools, out string message)
        {
            message = string.Empty;
            if (!state.Buildings.Any(building => building.Type == BuildingType.RoyalTavern))
            {
                message = "Construa uma Taverna Real antes de comprar melhorias.";
                return false;
            }

            int currentLevel = improveTools ? state.CityToolsLevel : state.ReinforcedWallsLevel;
            if (currentLevel >= 3)
            {
                message = "Essa melhoria já atingiu o nível máximo.";
                return false;
            }

            int nextLevel = currentLevel + 1;
            ResourceBundle cost = improveTools
                ? new ResourceBundle(3 * nextLevel, 0, 2 * nextLevel, relics: nextLevel - 1)
                : new ResourceBundle(4 * nextLevel, 0, 3 * nextLevel, relics: nextLevel - 1);

            if (!cost.IsAffordableWith(state.Stock))
            {
                message = "Faltam materiais ou relíquias para essa melhoria.";
                return false;
            }

            cost.TrySpendFrom(state.Stock);
            if (improveTools) state.CityToolsLevel++;
            else state.ReinforcedWallsLevel++;
            message = improveTools
                ? "Ferramentas da cidade melhoradas para o nível " + nextLevel + "."
                : "Defesas reforçadas para o nível " + nextLevel + ".";
            return true;
        }

        public static void AdvanceMinute(VillageState state, VillageSimulation simulation)
        {
            if (state.ActiveMissions == null || state.ActiveMissions.Count == 0)
            {
                return;
            }

            List<ActiveMissionRecord> completed = new List<ActiveMissionRecord>();
            foreach (ActiveMissionRecord mission in state.ActiveMissions)
            {
                mission.RemainingHours -= 1.0 / 60.0;
                if (mission.RemainingHours <= 1e-8) completed.Add(mission);
            }

            foreach (ActiveMissionRecord mission in completed)
            {
                MissionDefinition definition = Get(mission.Type);
                if (definition != null)
                {
                    definition.Reward.AddTo(state.Stock);
                    state.Gold += definition.GoldReward;
                    state.EarnedGold += definition.GoldReward;
                    state.CompletedMissions++;
                    if (mission.Type == MissionType.BorderExpedition) state.CompletedExpeditions++;
                    simulation.AddNotification("Missão concluída: " + definition.Name + ".", NotificationKind.Mission);
                }

                foreach (int memberId in mission.MemberIds)
                {
                    ResidentRecord resident = state.FindResident(memberId);
                    if (resident != null) resident.IsOnMission = false;
                }
                state.ActiveMissions.Remove(mission);
            }
        }

        public static int AvailableCrew(VillageState state, int tavernId)
        {
            HashSet<int> away = new HashSet<int>();
            foreach (ActiveMissionRecord mission in state.ActiveMissions)
                foreach (int id in mission.MemberIds) away.Add(id);
            return state.Residents.Count(resident => resident.WorkplaceId == tavernId && !away.Contains(resident.Id));
        }

        public static double RaidFrequencyMultiplier(VillageState state)
        {
            return 1.0 - Math.Min(5, state.CompletedExpeditions) * 0.1;
        }
    }
}
