using System.Collections.Generic;

namespace MinhaVila.Core
{
    public sealed class RoadNetwork
    {
        private static readonly GridPoint[] Directions =
        {
            new GridPoint(1, 0), new GridPoint(-1, 0), new GridPoint(0, 1), new GridPoint(0, -1)
        };

        private readonly Dictionary<GridPoint, int> _components = new Dictionary<GridPoint, int>();

        public RoadNetwork(VillageState state) { BuildComponents(state); }

        public bool BuildingsConnected(BuildingRecord first, BuildingRecord second)
        {
            int firstComponent = ComponentFor(first);
            return firstComponent >= 0 && firstComponent == ComponentFor(second);
        }

        public bool BuildingHasRoadAccess(BuildingRecord building) { return ComponentFor(building) >= 0; }

        public List<GridPoint> FindPath(BuildingRecord from, BuildingRecord to)
        {
            List<GridPoint> starts = AdjacentRoads(from);
            List<GridPoint> goals = AdjacentRoads(to);
            if (starts.Count == 0 || goals.Count == 0) return new List<GridPoint>();

            HashSet<GridPoint> goalSet = new HashSet<GridPoint>(goals);
            Queue<GridPoint> queue = new Queue<GridPoint>();
            Dictionary<GridPoint, GridPoint> previous = new Dictionary<GridPoint, GridPoint>();
            foreach (GridPoint start in starts)
            {
                queue.Enqueue(start);
                previous[start] = start;
            }

            GridPoint? found = null;
            while (queue.Count > 0)
            {
                GridPoint current = queue.Dequeue();
                if (goalSet.Contains(current))
                {
                    found = current;
                    break;
                }

                foreach (GridPoint direction in Directions)
                {
                    GridPoint next = new GridPoint(current.X + direction.X, current.Z + direction.Z);
                    if (previous.ContainsKey(next) || !_components.ContainsKey(next)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (!found.HasValue) return new List<GridPoint>();
            List<GridPoint> path = new List<GridPoint>();
            GridPoint cursor = found.Value;
            while (true)
            {
                path.Add(cursor);
                if (previous[cursor] == cursor) break;
                cursor = previous[cursor];
            }
            path.Reverse();
            return path;
        }

        private int ComponentFor(BuildingRecord building)
        {
            if (building == null) return -1;
            foreach (GridPoint point in AdjacentRoads(building))
            {
                if (_components.TryGetValue(point, out int component)) return component;
            }
            return -1;
        }

        private List<GridPoint> AdjacentRoads(BuildingRecord building)
        {
            List<GridPoint> result = new List<GridPoint>(4);
            if (building == null) return result;
            foreach (GridPoint direction in Directions)
            {
                GridPoint point = new GridPoint(building.X + direction.X, building.Z + direction.Z);
                if (_components.ContainsKey(point)) result.Add(point);
            }
            return result;
        }

        private void BuildComponents(VillageState state)
        {
            _components.Clear();
            foreach (BuildingRecord building in state.Buildings)
            {
                if (building.Type == BuildingType.Road)
                    _components[new GridPoint(building.X, building.Z)] = -1;
            }

            int componentId = 0;
            foreach (GridPoint start in new List<GridPoint>(_components.Keys))
            {
                if (_components[start] >= 0) continue;
                Queue<GridPoint> queue = new Queue<GridPoint>();
                queue.Enqueue(start);
                _components[start] = componentId;
                while (queue.Count > 0)
                {
                    GridPoint current = queue.Dequeue();
                    foreach (GridPoint direction in Directions)
                    {
                        GridPoint next = new GridPoint(current.X + direction.X, current.Z + direction.Z);
                        if (_components.TryGetValue(next, out int currentComponent) && currentComponent < 0)
                        {
                            _components[next] = componentId;
                            queue.Enqueue(next);
                        }
                    }
                }
                componentId++;
            }
        }
    }
}
