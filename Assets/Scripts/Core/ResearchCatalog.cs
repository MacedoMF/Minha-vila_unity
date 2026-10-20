using System.Collections.Generic;

namespace MinhaVila.Core
{
    public sealed class ResearchDefinition
    {
        public ResearchId Id { get; private set; }
        public string Name { get; private set; }
        public int GoldCost { get; private set; }
        public ResourceBundle ResourceCost { get; private set; }
        public double RequiredHours { get; private set; }
        public ResearchId? Prerequisite { get; private set; }
        public string Result { get; private set; }

        public ResearchDefinition(ResearchId id, string name, int goldCost, ResourceBundle resourceCost,
            double requiredHours, ResearchId? prerequisite, string result)
        {
            Id = id; Name = name; GoldCost = goldCost; ResourceCost = resourceCost;
            RequiredHours = requiredHours; Prerequisite = prerequisite; Result = result;
        }
    }

    public static class ResearchCatalog
    {
        private static readonly Dictionary<ResearchId, ResearchDefinition> Definitions =
            new Dictionary<ResearchId, ResearchDefinition>
            {
                {
                    ResearchId.Irrigation,
                    new ResearchDefinition(ResearchId.Irrigation, "Irrigação", 150,
                        new ResourceBundle(4, 0, 0, 6), 4, null,
                        "+50% de produção de comida nas fazendas.")
                },
                {
                    ResearchId.Orchards,
                    new ResearchDefinition(ResearchId.Orchards, "Pomares", 300,
                        new ResourceBundle(6, 0, 2, 12), 6, ResearchId.Irrigation,
                        "Libera o Pomar, com 3 vagas e 2 comidas por trabalhador/hora.")
                },
                {
                    ResearchId.GrainMilling,
                    new ResearchDefinition(ResearchId.GrainMilling, "Engenharia dos moinhos", 450,
                        new ResourceBundle(0, 10, 4, 20), 8, ResearchId.Orchards,
                        "Libera o Moinho de Cereais.")
                },
                {
                    ResearchId.Masonry,
                    new ResearchDefinition(ResearchId.Masonry, "Alvenaria", 200,
                        new ResourceBundle(6, 8, 0, 8), 4, null,
                        "Libera Casa de Alvenaria, Via Real e Jardim dos Bardos.")
                },
                {
                    ResearchId.AdvancedCarpentry,
                    new ResearchDefinition(ResearchId.AdvancedCarpentry, "Carpintaria avançada", 350,
                        new ResourceBundle(8, 0, 4, 12), 6, ResearchId.Masonry,
                        "Libera Madeireira II, Mineradora II e Estábulo II.")
                },
                {
                    ResearchId.CommerceAndHospitality,
                    new ResearchDefinition(ResearchId.CommerceAndHospitality, "Comércio e hospitalidade", 400,
                        new ResourceBundle(8, 6, 0, 12), 6, ResearchId.Masonry,
                        "Libera Grande Taverna, Grande Guilda e Tesouro do Reino.")
                },
                {
                    ResearchId.Metallurgy,
                    new ResearchDefinition(ResearchId.Metallurgy, "Metalurgia", 500,
                        new ResourceBundle(0, 8, 6, 16), 8, ResearchId.AdvancedCarpentry,
                        "Libera Forja de Aço e Quartel da Guarda.")
                },
                {
                    ResearchId.AdvancedStudies,
                    new ResearchDefinition(ResearchId.AdvancedStudies, "Estudos avançados", 600,
                        new ResourceBundle(8, 10, 4, 20), 8, ResearchId.CommerceAndHospitality,
                        "Libera Academia da Vila e Torre dos Arquimagos.")
                }
            };

        public static IReadOnlyCollection<ResearchDefinition> All { get { return Definitions.Values; } }
        public static ResearchDefinition Get(ResearchId id)
        {
            return Definitions.TryGetValue(id, out ResearchDefinition definition) ? definition : null;
        }

        public static bool IsCompleted(VillageState state, ResearchId id)
        {
            return state != null && state.CompletedResearch.Contains(id);
        }

        public static bool IsAvailable(VillageState state, ResearchDefinition definition)
        {
            return state != null && definition != null && !IsCompleted(state, definition.Id)
                && (!definition.Prerequisite.HasValue || IsCompleted(state, definition.Prerequisite.Value));
        }
    }
}
