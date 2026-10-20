using System;

namespace MinhaVila.Core
{
    public enum BuildingType
    {
        House, Road, Tavern, RoyalTavern, Guild, Forge, Sawmill, Quarry, Stable,
        Farm, ResearchCenter, Orchard, GrainMill, ArcaneTower, BardSquare, Bank, GuardPost
    }

    public enum BuildingCategory { Housing, Work, Leisure }
    public enum ResearchId
    {
        Irrigation,
        Orchards,
        GrainMilling,
        Masonry,
        AdvancedCarpentry,
        CommerceAndHospitality,
        Metallurgy,
        AdvancedStudies
    }
    public enum MissionType { Caravan, RecoverMine, ExploreRuins, BorderExpedition }
    public enum ThreatType { Goblins, Bandits, Dragon }
    public enum ResourceType { Timber, Stone, Iron, Food }
    public enum NotificationKind { General, Construction, Economy, Research, Population, Mission, Security }

    [Serializable]
    public struct GridPoint : IEquatable<GridPoint>
    {
        public int X;
        public int Z;

        public GridPoint(int x, int z) { X = x; Z = z; }
        public bool Equals(GridPoint other) { return X == other.X && Z == other.Z; }
        public override bool Equals(object obj) { return obj is GridPoint other && Equals(other); }

        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Z; }
        }

        public static bool operator ==(GridPoint left, GridPoint right) { return left.Equals(right); }
        public static bool operator !=(GridPoint left, GridPoint right) { return !left.Equals(right); }
        public override string ToString() { return "(" + X + ", " + Z + ")"; }
    }

    [Serializable]
    public sealed class ResourceBundle
    {
        public int Timber;
        public int Stone;
        public int Iron;
        public int Food;
        public int Relics;

        public ResourceBundle() { }
        public ResourceBundle(int timber, int stone, int iron, int food = 0, int relics = 0)
        {
            Timber = timber; Stone = stone; Iron = iron; Food = food; Relics = relics;
        }

        public ResourceBundle Copy()
        {
            return new ResourceBundle(Timber, Stone, Iron, Food, Relics);
        }

        public bool IsAffordableWith(ResourceStock stock)
        {
            return stock != null && stock.Timber >= Timber && stock.Stone >= Stone
                && stock.Iron >= Iron && stock.Food >= Food && stock.Relics >= Relics;
        }

        public bool TrySpendFrom(ResourceStock stock)
        {
            if (!IsAffordableWith(stock)) return false;
            stock.Timber -= Timber; stock.Stone -= Stone; stock.Iron -= Iron;
            stock.Food -= Food; stock.Relics -= Relics;
            return true;
        }

        public void AddTo(ResourceStock stock)
        {
            if (stock == null) return;
            stock.Timber = Math.Min(ResourceStock.Maximum, stock.Timber + Timber);
            stock.Stone = Math.Min(ResourceStock.Maximum, stock.Stone + Stone);
            stock.Iron = Math.Min(ResourceStock.Maximum, stock.Iron + Iron);
            stock.Food = Math.Min(ResourceStock.Maximum, stock.Food + Food);
            stock.Relics = Math.Min(ResourceStock.Maximum, stock.Relics + Relics);
        }
    }
}
