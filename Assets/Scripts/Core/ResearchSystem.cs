using System;
using System.Linq;

namespace MinhaVila.Core
{
    public static class ResearchSystem
    {
        public static bool TryStart(VillageState state, ResearchId id, out string message)
        {
            message = string.Empty;
            ResearchDefinition definition = ResearchCatalog.Get(id);
            if (definition == null)
            {
                message = "Estudo desconhecido.";
                return false;
            }

            if (ResearchCatalog.IsCompleted(state, id))
            {
                message = "Esse estudo já foi concluído.";
                return false;
            }

            if (state.HasActiveResearch)
            {
                message = "Conclua ou retome o estudo atual antes de iniciar outro.";
                return false;
            }

            if (!ResearchCatalog.IsAvailable(state, definition))
            {
                message = "Pesquise o pré-requisito antes deste estudo.";
                return false;
            }

            if (!state.Buildings.Any(building => building.Type == BuildingType.ResearchCenter))
            {
                message = "Construa um Centro de Pesquisa.";
                return false;
            }

            if (state.Gold < definition.GoldCost || !definition.ResourceCost.IsAffordableWith(state.Stock))
            {
                message = "Faltam ouro ou materiais para iniciar a pesquisa.";
                return false;
            }

            state.Gold -= definition.GoldCost;
            state.SpentGold += definition.GoldCost;
            definition.ResourceCost.TrySpendFrom(state.Stock);
            state.ActiveResearch = id;
            state.HasActiveResearch = true;
            state.ResearchProgressHours = 0;
            message = "Pesquisa iniciada: " + definition.Name + ".";
            return true;
        }

        public static void AdvanceWork(VillageState state, double researcherHours, VillageSimulation simulation)
        {
            if (!state.HasActiveResearch || researcherHours <= 0)
            {
                return;
            }

            ResearchDefinition definition = ResearchCatalog.Get(state.ActiveResearch);
            if (definition == null)
            {
                state.HasActiveResearch = false;
                state.ResearchProgressHours = 0;
                return;
            }

            state.ResearchProgressHours = Math.Min(definition.RequiredHours,
                state.ResearchProgressHours + researcherHours);
            if (state.ResearchProgressHours + 1e-8 < definition.RequiredHours)
            {
                return;
            }

            if (!state.CompletedResearch.Contains(definition.Id))
            {
                state.CompletedResearch.Add(definition.Id);
            }
            state.HasActiveResearch = false;
            state.ResearchProgressHours = 0;
            simulation.AddNotification("Pesquisa concluída: " + definition.Name + ".", NotificationKind.Research);
            simulation.RebuildWorkforce();
        }

        public static double FarmMultiplier(VillageState state)
        {
            return ResearchCatalog.IsCompleted(state, ResearchId.Irrigation) ? 1.5 : 1;
        }
    }
}
