using System.Collections.Generic;

namespace MinhaVila.Core
{
    public sealed class BuildingDefinition
    {
        public BuildingType Type { get; private set; }
        public string Name { get; private set; }
        public BuildingCategory Category { get; private set; }
        public int GoldCost { get; private set; }
        public ResourceBundle ResourceCost { get; private set; }
        public int WorkSlots { get; private set; }
        public int HousingCapacity { get; private set; }
        public int LeisureCapacity { get; private set; }
        public double GoldPerWorkerHour { get; private set; }
        public double TimberPerWorkerHour { get; private set; }
        public double StonePerWorkerHour { get; private set; }
        public double IronPerWorkerHour { get; private set; }
        public double FoodPerWorkerHour { get; private set; }
        public int GuardPower { get; private set; }
        public double ResearchRate { get; private set; }
        public double BankIncomeBonus { get; private set; }
        public double RoadSpeedBonus { get; private set; }
        public ResearchId? RequiredResearch { get; private set; }
        public bool HasUpgrade { get; private set; }

        public BuildingDefinition(BuildingType type, string name, BuildingCategory category, int goldCost,
            ResourceBundle resourceCost, int workSlots = 0, int housingCapacity = 0, int leisureCapacity = 0,
            double goldPerWorkerHour = 0, double timberPerWorkerHour = 0, double stonePerWorkerHour = 0,
            double ironPerWorkerHour = 0, double foodPerWorkerHour = 0, int guardPower = 0,
            double researchRate = 0, double bankIncomeBonus = 0, double roadSpeedBonus = 0,
            ResearchId? requiredResearch = null, bool hasUpgrade = false)
        {
            Type = type; Name = name; Category = category; GoldCost = goldCost; ResourceCost = resourceCost;
            WorkSlots = workSlots; HousingCapacity = housingCapacity; LeisureCapacity = leisureCapacity;
            GoldPerWorkerHour = goldPerWorkerHour; TimberPerWorkerHour = timberPerWorkerHour;
            StonePerWorkerHour = stonePerWorkerHour; IronPerWorkerHour = ironPerWorkerHour;
            FoodPerWorkerHour = foodPerWorkerHour; GuardPower = guardPower; ResearchRate = researchRate;
            BankIncomeBonus = bankIncomeBonus; RoadSpeedBonus = roadSpeedBonus;
            RequiredResearch = requiredResearch; HasUpgrade = hasUpgrade;
        }
    }

    public static class BuildingCatalog
    {
        private static readonly Dictionary<BuildingType, BuildingDefinition> BaseDefinitions =
            new Dictionary<BuildingType, BuildingDefinition>
            {
                {
                    BuildingType.House,
                    new BuildingDefinition(BuildingType.House, "Casa de aldeão", BuildingCategory.Housing,
                        100, new ResourceBundle(4, 2, 0), hasUpgrade: true)
                },
                {
                    BuildingType.Road,
                    new BuildingDefinition(BuildingType.Road, "Rua de pedra", BuildingCategory.Work,
                        15, new ResourceBundle(0, 1, 0), hasUpgrade: true)
                },
                {
                    BuildingType.Tavern,
                    new BuildingDefinition(BuildingType.Tavern, "Taverna do Dragão", BuildingCategory.Work,
                        500, new ResourceBundle(8, 4, 1), 4, leisureCapacity: 8,
                        goldPerWorkerHour: 2, hasUpgrade: true)
                },
                {
                    BuildingType.RoyalTavern,
                    new BuildingDefinition(BuildingType.RoyalTavern, "Taverna Real", BuildingCategory.Work,
                        2000, new ResourceBundle(12, 8, 3), 6, leisureCapacity: 12)
                },
                {
                    BuildingType.Guild,
                    new BuildingDefinition(BuildingType.Guild, "Guilda de Aventureiros", BuildingCategory.Work,
                        500, new ResourceBundle(10, 8, 2), 6, goldPerWorkerHour: 3, hasUpgrade: true)
                },
                {
                    BuildingType.Forge,
                    new BuildingDefinition(BuildingType.Forge, "Forja dos Anões", BuildingCategory.Work,
                        500, new ResourceBundle(6, 10, 4), 3, goldPerWorkerHour: 4, hasUpgrade: true)
                },
                {
                    BuildingType.Sawmill,
                    new BuildingDefinition(BuildingType.Sawmill, "Madeireira do Bosque", BuildingCategory.Work,
                        500, new ResourceBundle(8, 4, 2), 3, timberPerWorkerHour: 1, hasUpgrade: true)
                },
                {
                    BuildingType.Quarry,
                    new BuildingDefinition(BuildingType.Quarry, "Mineradora da Colina", BuildingCategory.Work,
                        500, new ResourceBundle(10, 8, 3), 3, stonePerWorkerHour: 1,
                        ironPerWorkerHour: 0.25, hasUpgrade: true)
                },
                {
                    BuildingType.Stable,
                    new BuildingDefinition(BuildingType.Stable, "Estábulo da Vila", BuildingCategory.Work,
                        500, new ResourceBundle(10, 6, 2), 3, goldPerWorkerHour: 3, hasUpgrade: true)
                },
                {
                    BuildingType.Farm,
                    new BuildingDefinition(BuildingType.Farm, "Fazenda da Vila", BuildingCategory.Work,
                        300, new ResourceBundle(8, 2, 0), 2, foodPerWorkerHour: 1)
                },
                {
                    BuildingType.ResearchCenter,
                    new BuildingDefinition(BuildingType.ResearchCenter, "Centro de Pesquisa", BuildingCategory.Work,
                        500, new ResourceBundle(10, 8, 2), 2, researchRate: 1, hasUpgrade: true)
                },
                {
                    BuildingType.Orchard,
                    new BuildingDefinition(BuildingType.Orchard, "Pomar da Vila", BuildingCategory.Work,
                        700, new ResourceBundle(12, 6, 2), 3, foodPerWorkerHour: 2,
                        requiredResearch: ResearchId.Orchards)
                },
                {
                    BuildingType.GrainMill,
                    new BuildingDefinition(BuildingType.GrainMill, "Moinho de Cereais", BuildingCategory.Work,
                        900, new ResourceBundle(16, 12, 4), 3, goldPerWorkerHour: 3,
                        foodPerWorkerHour: 1, requiredResearch: ResearchId.GrainMilling)
                },
                {
                    BuildingType.ArcaneTower,
                    new BuildingDefinition(BuildingType.ArcaneTower, "Torre Arcana", BuildingCategory.Work,
                        500, new ResourceBundle(8, 12, 4), 4, goldPerWorkerHour: 5, hasUpgrade: true)
                },
                {
                    BuildingType.BardSquare,
                    new BuildingDefinition(BuildingType.BardSquare, "Praça dos Bardos", BuildingCategory.Leisure,
                        100, new ResourceBundle(4, 4, 0), leisureCapacity: 8, hasUpgrade: true)
                },
                {
                    BuildingType.Bank,
                    new BuildingDefinition(BuildingType.Bank, "Banco do Reino", BuildingCategory.Work,
                        100000, new ResourceBundle(20, 30, 12))
                },
                {
                    BuildingType.GuardPost,
                    new BuildingDefinition(BuildingType.GuardPost, "Posto da Guarda", BuildingCategory.Work,
                        500, new ResourceBundle(8, 10, 3), 4, guardPower: 5, hasUpgrade: true)
                }
            };

        private static readonly Dictionary<BuildingType, BuildingDefinition> ImprovedDefinitions =
            new Dictionary<BuildingType, BuildingDefinition>
            {
                {
                    BuildingType.House,
                    new BuildingDefinition(BuildingType.House, "Casa de Alvenaria", BuildingCategory.Housing,
                        250, new ResourceBundle(10, 8, 1), housingCapacity: 8,
                        requiredResearch: ResearchId.Masonry)
                },
                {
                    BuildingType.Road,
                    new BuildingDefinition(BuildingType.Road, "Via Real", BuildingCategory.Work,
                        30, new ResourceBundle(0, 2, 0), roadSpeedBonus: 0.25,
                        requiredResearch: ResearchId.Masonry)
                },
                {
                    BuildingType.BardSquare,
                    new BuildingDefinition(BuildingType.BardSquare, "Jardim dos Bardos", BuildingCategory.Leisure,
                        300, new ResourceBundle(8, 10, 1), leisureCapacity: 16,
                        requiredResearch: ResearchId.Masonry)
                },
                {
                    BuildingType.Sawmill,
                    new BuildingDefinition(BuildingType.Sawmill, "Serraria Avançada", BuildingCategory.Work,
                        1000, new ResourceBundle(16, 10, 6), 5, timberPerWorkerHour: 1.5,
                        requiredResearch: ResearchId.AdvancedCarpentry)
                },
                {
                    BuildingType.Quarry,
                    new BuildingDefinition(BuildingType.Quarry, "Mina Reforçada", BuildingCategory.Work,
                        1200, new ResourceBundle(18, 16, 8), 5, stonePerWorkerHour: 1.5,
                        ironPerWorkerHour: 0.5, requiredResearch: ResearchId.AdvancedCarpentry)
                },
                {
                    BuildingType.Stable,
                    new BuildingDefinition(BuildingType.Stable, "Estábulo de Transporte", BuildingCategory.Work,
                        900, new ResourceBundle(18, 12, 4), 5, goldPerWorkerHour: 4,
                        requiredResearch: ResearchId.AdvancedCarpentry)
                },
                {
                    BuildingType.Tavern,
                    new BuildingDefinition(BuildingType.Tavern, "Grande Taverna", BuildingCategory.Work,
                        1000, new ResourceBundle(16, 12, 3), 6, leisureCapacity: 16,
                        goldPerWorkerHour: 3, requiredResearch: ResearchId.CommerceAndHospitality)
                },
                {
                    BuildingType.Guild,
                    new BuildingDefinition(BuildingType.Guild, "Grande Guilda", BuildingCategory.Work,
                        1000, new ResourceBundle(18, 16, 4), 8, goldPerWorkerHour: 5,
                        requiredResearch: ResearchId.CommerceAndHospitality)
                },
                {
                    BuildingType.Bank,
                    new BuildingDefinition(BuildingType.Bank, "Tesouro do Reino", BuildingCategory.Work,
                        180000, new ResourceBundle(32, 50, 20), bankIncomeBonus: 0.08,
                        requiredResearch: ResearchId.CommerceAndHospitality)
                },
                {
                    BuildingType.Forge,
                    new BuildingDefinition(BuildingType.Forge, "Forja de Aço", BuildingCategory.Work,
                        1200, new ResourceBundle(12, 20, 8), 5, goldPerWorkerHour: 6,
                        requiredResearch: ResearchId.Metallurgy)
                },
                {
                    BuildingType.GuardPost,
                    new BuildingDefinition(BuildingType.GuardPost, "Quartel da Guarda", BuildingCategory.Work,
                        1200, new ResourceBundle(14, 20, 8), 6, guardPower: 7,
                        requiredResearch: ResearchId.Metallurgy)
                },
                {
                    BuildingType.ResearchCenter,
                    new BuildingDefinition(BuildingType.ResearchCenter, "Academia da Vila", BuildingCategory.Work,
                        1000, new ResourceBundle(18, 16, 6), 4, researchRate: 1.5,
                        requiredResearch: ResearchId.AdvancedStudies)
                },
                {
                    BuildingType.ArcaneTower,
                    new BuildingDefinition(BuildingType.ArcaneTower, "Torre dos Arquimagos", BuildingCategory.Work,
                        1500, new ResourceBundle(14, 22, 8), 6, goldPerWorkerHour: 7,
                        requiredResearch: ResearchId.AdvancedStudies)
                }
            };

        public static IReadOnlyCollection<BuildingType> Types { get { return BaseDefinitions.Keys; } }

        public static BuildingDefinition Get(BuildingType type, int level = 1)
        {
            if (level == 2 && ImprovedDefinitions.TryGetValue(type, out BuildingDefinition improved)) return improved;
            return BaseDefinitions.TryGetValue(type, out BuildingDefinition definition) ? definition : null;
        }

        public static ResourceBundle GetImprovementCost(BuildingType type, out int goldCost)
        {
            BuildingDefinition current = Get(type, 1);
            BuildingDefinition improved = Get(type, 2);
            if (current == null || improved == null)
            {
                goldCost = 0;
                return null;
            }

            goldCost = improved.GoldCost - current.GoldCost;
            return new ResourceBundle(
                System.Math.Max(0, improved.ResourceCost.Timber - current.ResourceCost.Timber),
                System.Math.Max(0, improved.ResourceCost.Stone - current.ResourceCost.Stone),
                System.Math.Max(0, improved.ResourceCost.Iron - current.ResourceCost.Iron),
                System.Math.Max(0, improved.ResourceCost.Food - current.ResourceCost.Food),
                System.Math.Max(0, improved.ResourceCost.Relics - current.ResourceCost.Relics));
        }

        public static bool IsInCategory(BuildingType type, BuildingCategory category)
        {
            BuildingDefinition definition = Get(type);
            if (definition == null) return false;
            if (type == BuildingType.Road || definition.Category == category) return true;
            return (type == BuildingType.Tavern || type == BuildingType.RoyalTavern)
                && category == BuildingCategory.Leisure;
        }
    }
}
