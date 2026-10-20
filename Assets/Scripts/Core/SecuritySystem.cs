using System;
using System.Linq;

namespace MinhaVila.Core
{
    public static class SecuritySystem
    {
        public const double FirstRaidHours = 72;
        public const double WarningHours = 2;
        public const double BattleHours = 1.5;

        public static int Guards(VillageState state)
        {
            return state.Residents.Count(resident => resident.IsGuard && !resident.IsOnMission);
        }

        public static int RecommendedGuards(VillageState state)
        {
            return (int)Math.Ceiling(state.Population / 25.0);
        }

        public static double Defense(VillageState state)
        {
            int total = 0;
            foreach (ResidentRecord resident in state.Residents)
            {
                if (!resident.IsGuard || resident.IsOnMission) continue;
                BuildingRecord post = state.FindBuilding(resident.WorkplaceId);
                BuildingDefinition definition = post == null ? null : BuildingCatalog.Get(post.Type, post.Level);
                total += definition == null ? 5 : definition.GuardPower;
            }

            return total * (1 - Math.Max(0, Math.Min(100, state.Security.Disorder)) * 0.005)
                + state.ReinforcedWallsLevel * 10;
        }

        public static void BeginManualRaid(VillageState state, ThreatType type, VillageSimulation simulation)
        {
            if (state.Population <= 0 || state.Security.HasActiveThreat)
            {
                simulation.AddNotification("Não é possível iniciar outro ataque agora.", NotificationKind.Security);
                return;
            }

            BeginRaid(state, type);
            simulation.AddNotification("Alerta: " + ThreatName(type) + " avistados perto da vila.", NotificationKind.Security);
        }

        public static void AdvanceMinute(VillageState state, VillageSimulation simulation)
        {
            SecurityRecord security = state.Security;
            double step = 1.0 / 60.0;
            if (state.Population == 0)
            {
                security.Disorder = Math.Max(0, security.Disorder - step);
                if (security.HasActiveThreat)
                {
                    security.LastThreat = security.ActiveThreat;
                    security.LastOutcome = "dispersed";
                    security.LastLoot = 0;
                    security.HasActiveThreat = false;
                    security.IsBattle = false;
                    security.PeaceRemainingHours = FirstRaidHours;
                    simulation.AddNotification("A ameaça se dispersou porque a vila está vazia.", NotificationKind.Security);
                }
                return;
            }

            security.ActiveHours += step;
            double happiness = simulation.Happiness / 100.0;
            double patrol = Math.Min(3, Guards(state) / Math.Max(1, state.Population / 25.0) * 1.2);
            double disorderPerHour = (1 - happiness) * 2.4 - happiness * 0.35 - patrol;
            security.Disorder = Math.Max(0, Math.Min(100, security.Disorder + disorderPerHour * step));

            if (!security.HasActiveThreat)
            {
                double raidFrequency = security.Sequence == 0
                    ? 1.0
                    : MissionSystem.RaidFrequencyMultiplier(state);
                security.PeaceRemainingHours -= step * raidFrequency;
                if (security.PeaceRemainingHours <= 1e-8)
                {
                    ThreatType threat = ChooseThreat(state, NextRandom(security));
                    BeginRaid(state, threat);
                    simulation.AddNotification("Alerta: " + ThreatName(threat) + " estão se aproximando.", NotificationKind.Security);
                }
                return;
            }

            security.ThreatPhaseRemainingHours -= step;
            if (security.ThreatPhaseRemainingHours > 1e-8)
            {
                return;
            }

            if (!security.IsBattle)
            {
                security.IsBattle = true;
                security.ThreatPhaseRemainingHours = BattleHours;
                simulation.AddNotification(
                    "A batalha contra " + ThreatName(security.ActiveThreat) + " começou.", NotificationKind.Security);
                return;
            }

            ResolveRaid(state, simulation);
        }

        public static string ThreatName(ThreatType type)
        {
            switch (type)
            {
                case ThreatType.Bandits: return "bandidos";
                case ThreatType.Dragon: return "o dragão";
                default: return "goblins";
            }
        }

        private static ThreatType ChooseThreat(VillageState state, double roll)
        {
            if (state.Population >= 300 && state.Security.ActiveHours >= 240 && roll < 0.05)
                return ThreatType.Dragon;
            if (state.Population >= 50 && roll < 0.35)
                return ThreatType.Bandits;
            return ThreatType.Goblins;
        }

        private static void BeginRaid(VillageState state, ThreatType type)
        {
            SecurityRecord security = state.Security;
            security.Sequence++;
            security.ActiveThreat = type;
            security.HasActiveThreat = true;
            security.IsBattle = false;
            security.ThreatPhaseRemainingHours = WarningHours;
            security.ThreatSide = (int)(NextRandom(security) * 4);
            double baseStrength;
            int populationStep;
            switch (type)
            {
                case ThreatType.Bandits: baseStrength = 6; populationStep = 40; break;
                case ThreatType.Dragon: baseStrength = 28; populationStep = 30; break;
                default: baseStrength = 3; populationStep = 50; break;
            }
            security.ThreatStrength = (int)Math.Ceiling(
                baseStrength + state.Population / (double)populationStep + security.Disorder * 0.08);
        }

        private static void ResolveRaid(VillageState state, VillageSimulation simulation)
        {
            SecurityRecord security = state.Security;
            ThreatType type = security.ActiveThreat;
            double defense = Defense(state);
            double breach = Math.Max(0, Math.Min(1, 1 - defense / Math.Max(1, security.ThreatStrength)));
            int loot = 0;

            if (breach <= 1e-10)
            {
                security.Repelled++;
                security.Disorder = Math.Max(0, security.Disorder - 5);
                security.LastOutcome = "repelled";
                simulation.AddNotification("A guarda repeliu " + ThreatName(type) + ".", NotificationKind.Security);
            }
            else
            {
                int baseLoot;
                int perResident;
                double goldFraction;
                int unrest;
                switch (type)
                {
                    case ThreatType.Bandits: baseLoot = 120; perResident = 3; goldFraction = 0.05; unrest = 15; break;
                    case ThreatType.Dragon: baseLoot = 600; perResident = 6; goldFraction = 0.10; unrest = 25; break;
                    default: baseLoot = 50; perResident = 2; goldFraction = 0.02; unrest = 10; break;
                }

                double loss = Math.Ceiling((baseLoot + state.Population * perResident
                    + Math.Min(10000, Math.Floor(state.Gold) * goldFraction)) * breach);
                loot = (int)Math.Min(Math.Floor(state.Gold), loss);
                state.Gold -= loot;
                state.LootedGold += loot;
                security.Breached++;
                security.Disorder = Math.Min(100, security.Disorder + unrest * breach);
                security.LastOutcome = "breached";
                simulation.AddNotification(
                    "O ataque saqueou " + loot + " ouros. Não houve perda de moradores ou prédios.",
                    NotificationKind.Security);
            }

            security.LastThreat = type;
            security.LastLoot = loot;
            security.HasActiveThreat = false;
            security.IsBattle = false;
            security.ThreatPhaseRemainingHours = 0;
            double disorderFactor = 1 - security.Disorder * 0.004;
            security.PeaceRemainingHours = (48 + Math.Floor(NextRandom(security) * 49)) * disorderFactor;
        }

        private static double NextRandom(SecurityRecord security)
        {
            unchecked { security.RandomState = security.RandomState * 1664525u + 1013904223u; }
            return security.RandomState / 4294967296.0;
        }
    }
}
