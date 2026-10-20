using System.Linq;
using MinhaVila.Core;
using NUnit.Framework;

namespace MinhaVila.Tests
{
    public sealed class VillageSimulationTests
    {
        [Test]
        public void StarterVillageHasResidentsAndRoadConnectedBuildings()
        {
            VillageState state = ScenarioFactory.CreateStarter();
            VillageSimulation simulation = new VillageSimulation(state);
            RoadNetwork roads = new RoadNetwork(state);

            Assert.AreEqual(8, state.Population);
            Assert.Greater(simulation.EmployedCount, 0);
            Assert.IsTrue(state.Buildings
                .Where(building => building.Type != BuildingType.Road)
                .All(roads.BuildingHasRoadAccess));
        }

        [Test]
        public void BuildAndUndoRestoresGoldAndMaterials()
        {
            VillageState state = ScenarioFactory.CreateStarter();
            VillageSimulation simulation = new VillageSimulation(state);
            double goldBefore = state.Gold;
            int timberBefore = state.Stock.Timber;
            int stoneBefore = state.Stock.Stone;

            Assert.IsTrue(simulation.TryBuild(BuildingType.House, new GridPoint(30, 30), 0, out _));
            Assert.IsTrue(simulation.UndoLastAction(out _));

            Assert.AreEqual(goldBefore, state.Gold);
            Assert.AreEqual(timberBefore, state.Stock.Timber);
            Assert.AreEqual(stoneBefore, state.Stock.Stone);
            Assert.IsNull(state.FindBuildingAt(30, 30));
        }

        [Test]
        public void PausedVillageDoesNotAdvance()
        {
            VillageState state = VillageState.CreateNew("Pausa");
            VillageSimulation simulation = new VillageSimulation(state);
            simulation.SetPaused(true);
            simulation.AdvanceGameMinutes(90);

            Assert.AreEqual(1, state.Day);
            Assert.AreEqual(8, state.Hour);
            Assert.AreEqual(0, state.Minute);
        }

        [Test]
        public void OneGameDayAdvancesClockAndDayCounter()
        {
            VillageState state = VillageState.CreateNew("Relógio");
            VillageSimulation simulation = new VillageSimulation(state);
            simulation.SetPaused(false);
            simulation.AdvanceGameMinutes(24 * 60);

            Assert.AreEqual(2, state.Day);
            Assert.AreEqual(8, state.Hour);
            Assert.AreEqual(0, state.Minute);
        }
    }
}
