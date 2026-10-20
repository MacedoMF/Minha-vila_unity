namespace MinhaVila.Core
{
    public static class LandSystem
    {
        private static readonly int[] Sizes = { 36, 40, 44, 48 };
        private static readonly int[] PopulationMilestones = { 100, 300, 600, 1000 };
        private static readonly int[] Prices = { 300000, 600000, 900000, 1200000 };

        public static bool HasAnotherExpansion(VillageState state)
        {
            return state != null && state.TerritorySize < 48;
        }

        public static int NextSize(VillageState state)
        {
            int index = state == null ? 0 : (state.TerritorySize - 32) / 4;
            return index >= 0 && index < Sizes.Length ? Sizes[index] : 0;
        }

        public static int NextPrice(VillageState state)
        {
            int index = state == null ? 0 : (state.TerritorySize - 32) / 4;
            return index >= 0 && index < Prices.Length ? Prices[index] : 0;
        }

        public static int NextPopulationMilestone(VillageState state)
        {
            int index = state == null ? 0 : (state.TerritorySize - 32) / 4;
            return index >= 0 && index < PopulationMilestones.Length ? PopulationMilestones[index] : 0;
        }

        public static bool IsOwned(VillageState state, int x, int z)
        {
            if (state == null || x < 0 || z < 0 || x >= 48 || z >= 48)
            {
                return false;
            }

            int minimum = (48 - state.TerritorySize) / 2;
            int maximum = minimum + state.TerritorySize;
            return x >= minimum && z >= minimum && x < maximum && z < maximum;
        }

        public static bool TryExpand(VillageState state, out string message)
        {
            message = string.Empty;
            int index = state == null ? -1 : (state.TerritorySize - 32) / 4;
            if (index < 0 || index >= Sizes.Length)
            {
                message = "Todas as terras disponíveis já pertencem à vila.";
                return false;
            }

            if (state.PopulationRecord < PopulationMilestones[index])
            {
                message = "A vila precisa alcançar " + PopulationMilestones[index] + " moradores.";
                return false;
            }

            if (state.Gold < Prices[index])
            {
                message = "O tesouro precisa de " + Prices[index] + " ouros.";
                return false;
            }

            state.Gold -= Prices[index];
            state.SpentGold += Prices[index];
            state.TerritorySize = Sizes[index];
            message = "Território ampliado para " + Sizes[index] + " × " + Sizes[index] + " lotes.";
            return true;
        }
    }
}
