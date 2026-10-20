using System.Collections.Generic;
using System.Linq;
using MinhaVila.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace MinhaVila.Unity
{
    public sealed class CitizenVisualSystem : MonoBehaviour
    {
        private sealed class CitizenAgent
        {
            public int ResidentId;
            public int Outfit;
            public int CurrentLocationId;
            public int TargetLocationId;
            public bool IsGuard;
            public bool HideAtDestination;
            public bool Visible;
            public int PathIndex;
            public Vector3 Position;
            public Vector3 Facing = Vector3.forward;
            public List<Vector3> Path = new List<Vector3>();
        }

        private readonly List<CitizenAgent> _agents = new List<CitizenAgent>();
        private readonly List<Matrix4x4>[] _matrices = new List<Matrix4x4>[6];
        private readonly Matrix4x4[][] _matrixBuffers = new Matrix4x4[6][];
        private readonly Mesh[] _citizenMeshes = new Mesh[6];
        private readonly Mesh[] _enemyMeshes = new Mesh[2];
        private GameController _game;
        private VillageState _state;
        private Material _material;
        private Mesh _dragonMesh;
        private int _lastSchedule = -1;
        private int _lastHour = -1;
        private int _mobileLimit = 64;
        private int _lastPopulation = -1;
        private int _lastMissionCount = -1;

        public void Initialize(GameController game)
        {
            _game = game;
            Shader shader = Shader.Find("MinhaVila/VertexColorLit");
            if (shader == null) shader = Shader.Find("Standard");
            _material = new Material(shader) { name = "MinhaVila Character Material", enableInstancing = true };
            _mobileLimit = SystemInfo.deviceType == DeviceType.Handheld ? 48 : 96;

            Color[] clothes =
            {
                new Color(0.42f, 0.25f, 0.16f),
                new Color(0.28f, 0.36f, 0.48f),
                new Color(0.49f, 0.34f, 0.20f),
                new Color(0.35f, 0.39f, 0.25f),
                new Color(0.47f, 0.25f, 0.22f),
                new Color(0.47f, 0.43f, 0.32f)
            };

            for (int i = 0; i < clothes.Length; i++)
            {
                _citizenMeshes[i] = CreateCharacterMesh(clothes[i], new Color(0.70f, 0.48f, 0.33f), DarkCloth(clothes[i]));
                _matrixBuffers[i] = new Matrix4x4[1023];
                _matrices[i] = new List<Matrix4x4>(128);
            }

            _enemyMeshes[0] = CreateCharacterMesh(
                new Color(0.32f, 0.48f, 0.22f), new Color(0.40f, 0.31f, 0.23f), new Color(0.20f, 0.28f, 0.17f));
            _enemyMeshes[1] = CreateCharacterMesh(
                new Color(0.24f, 0.20f, 0.18f), new Color(0.53f, 0.37f, 0.27f), new Color(0.16f, 0.14f, 0.12f));
            _dragonMesh = CreateDragonMesh();
        }

        public void SetState(VillageState state)
        {
            _state = state;
            _lastSchedule = -1;
            _lastHour = -1;
            RebuildAgents();
        }

        private void Update()
        {
            if (_state == null || _game == null || _game.IsInMenu) return;

            if (_state.Population != _lastPopulation || _state.ActiveMissions.Count != _lastMissionCount)
            {
                RebuildAgents();
            }

            if (_state.Hour != _lastHour)
            {
                _lastHour = _state.Hour;
                int schedule = ScheduleForHour(_state.Hour);
                if (schedule != _lastSchedule)
                {
                    _lastSchedule = schedule;
                    ReplanAgents(schedule);
                }
            }

            float travel = _state.Paused ? 0 : Time.unscaledDeltaTime * _state.Speed * 0.72f;
            if (travel > 0)
            {
                foreach (CitizenAgent agent in _agents)
                {
                    if (agent.Visible) AdvanceAgent(agent, travel);
                }
            }
        }

        private void LateUpdate()
        {
            if (_state == null || _game == null || _game.IsInMenu || _material == null) return;
            for (int i = 0; i < _matrices.Length; i++) _matrices[i].Clear();

            foreach (CitizenAgent agent in _agents)
            {
                if (!agent.Visible) continue;
                List<Matrix4x4> group = _matrices[agent.Outfit];
                if (group.Count >= _matrixBuffers[agent.Outfit].Length) continue;
                Quaternion rotation = agent.Facing.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(agent.Facing, Vector3.up)
                    : Quaternion.identity;
                group.Add(Matrix4x4.TRS(agent.Position, rotation, Vector3.one));
            }

            for (int i = 0; i < _matrices.Length; i++)
            {
                List<Matrix4x4> group = _matrices[i];
                for (int start = 0; start < group.Count; start += 1023)
                {
                    int count = Mathf.Min(1023, group.Count - start);
                    for (int index = 0; index < count; index++) _matrixBuffers[i][index] = group[start + index];
                    Graphics.DrawMeshInstanced(_citizenMeshes[i], 0, _material, _matrixBuffers[i], count,
                        null, ShadowCastingMode.Off, false, 0);
                }
            }

            DrawThreats();
        }

        private void RebuildAgents()
        {
            _agents.Clear();
            if (_state == null)
            {
                _lastPopulation = -1;
                _lastMissionCount = -1;
                return;
            }
            _lastPopulation = _state.Population;
            _lastMissionCount = _state.ActiveMissions.Count;
            foreach (ResidentRecord resident in _state.Residents.OrderBy(item => item.Id).Take(_mobileLimit))
            {
                _agents.Add(new CitizenAgent
                {
                    ResidentId = resident.Id,
                    Outfit = Mathf.Abs(resident.PortraitSeed) % _citizenMeshes.Length,
                    CurrentLocationId = resident.HomeId,
                    Position = DoorPosition(_state.FindBuilding(resident.HomeId)),
                    IsGuard = resident.IsGuard
                });
            }

            _lastSchedule = -1;
            ReplanAgents(ScheduleForHour(_state.Hour));
        }

        private void ReplanAgents(int schedule)
        {
            if (_state == null) return;
            RoadNetwork network = new RoadNetwork(_state);
            foreach (CitizenAgent agent in _agents)
            {
                ResidentRecord resident = _state.FindResident(agent.ResidentId);
                if (resident == null)
                {
                    agent.Visible = false;
                    continue;
                }

                agent.IsGuard = resident.IsGuard;
                BuildingRecord home = _state.FindBuilding(resident.HomeId);
                BuildingRecord target = null;
                int sourceId = agent.CurrentLocationId;
                bool show = false;
                bool hideAtDestination = true;

                if (resident.IsOnMission)
                {
                    agent.Visible = false;
                    continue;
                }

                if (schedule == 2 && resident.WorkplaceId >= 0)
                {
                    target = _state.FindBuilding(resident.WorkplaceId);
                    show = resident.IsGuard || resident.Id % 2 == 0;
                }
                else if (schedule == 3)
                {
                    target = FindLeisurePlace(home, network);
                    if (target == null && resident.WorkplaceId >= 0 && resident.IsGuard)
                        target = _state.FindBuilding(resident.WorkplaceId);
                    show = resident.Id % 3 != 1;
                    hideAtDestination = false;
                }
                else if (schedule == 0)
                {
                    target = home;
                    show = resident.Id % 8 == 0;
                }
                else if (schedule == 1)
                {
                    target = home;
                    show = resident.Id % 5 == 0;
                }
                else
                {
                    target = home;
                    show = resident.Id % 4 == 0;
                }

                if (target == null || home == null)
                {
                    agent.Visible = false;
                    continue;
                }

                if (sourceId <= 0 || _state.FindBuilding(sourceId) == null) sourceId = home.Id;
                BuildingRecord source = _state.FindBuilding(sourceId);
                if (source == null) source = home;
                if (source.Id == target.Id)
                {
                    agent.CurrentLocationId = target.Id;
                    agent.TargetLocationId = target.Id;
                    agent.Path.Clear();
                    agent.Position = DoorPosition(target);
                    agent.Visible = schedule == 3 && show;
                    continue;
                }

                if (!show)
                {
                    agent.CurrentLocationId = target.Id;
                    agent.TargetLocationId = target.Id;
                    agent.Visible = false;
                    agent.Path.Clear();
                    continue;
                }

                SetRoute(agent, source, target, network, hideAtDestination);
            }
        }

        private void SetRoute(CitizenAgent agent, BuildingRecord from, BuildingRecord to, RoadNetwork network, bool hideAtDestination)
        {
            List<GridPoint> roads = network.FindPath(from, to);
            if (roads.Count == 0)
            {
                agent.Visible = false;
                agent.Path.Clear();
                return;
            }

            List<Vector3> points = new List<Vector3>(roads.Count + 2);
            points.Add(DoorPosition(from));
            float lane = (agent.ResidentId % 3 - 1) * 0.08f;
            foreach (GridPoint road in roads)
            {
                Vector3 position = TownRenderer.TileCenter(road);
                position.y = 0.03f;
                position.x += lane;
                points.Add(position);
            }
            points.Add(DoorPosition(to));

            agent.Path = points;
            agent.PathIndex = 1;
            agent.Position = points[0];
            agent.Facing = points.Count > 1 ? points[1] - points[0] : Vector3.forward;
            agent.CurrentLocationId = from.Id;
            agent.TargetLocationId = to.Id;
            agent.HideAtDestination = hideAtDestination;
            agent.Visible = true;
        }

        private void AdvanceAgent(CitizenAgent agent, float distance)
        {
            while (distance > 0 && agent.PathIndex < agent.Path.Count)
            {
                Vector3 target = agent.Path[agent.PathIndex];
                Vector3 delta = target - agent.Position;
                delta.y = 0;
                float length = delta.magnitude;
                if (length <= distance)
                {
                    agent.Position = new Vector3(target.x, 0.03f, target.z);
                    distance -= length;
                    agent.PathIndex++;
                    if (length > 0.001f) agent.Facing = delta / length;
                }
                else
                {
                    Vector3 step = delta.normalized * distance;
                    agent.Position += step;
                    agent.Facing = delta.normalized;
                    distance = 0;
                }
            }

            if (agent.PathIndex >= agent.Path.Count && agent.Path.Count > 0)
            {
                agent.CurrentLocationId = agent.TargetLocationId;
                if (agent.HideAtDestination) agent.Visible = false;
            }
        }

        private BuildingRecord FindLeisurePlace(BuildingRecord home, RoadNetwork network)
        {
            if (home == null) return null;
            foreach (BuildingRecord building in _state.Buildings.OrderBy(item => item.Id))
            {
                BuildingDefinition definition = BuildingCatalog.Get(building.Type, building.Level);
                if (definition.LeisureCapacity > 0 && network.BuildingsConnected(home, building))
                    return building;
            }
            return null;
        }

        private void DrawThreats()
        {
            SecurityRecord security = _state.Security;
            if (!security.HasActiveThreat) return;
            int side = security.ThreatSide % 4;
            Vector3 start = side == 0 ? new Vector3(-19, 0.03f, 0)
                : side == 1 ? new Vector3(19, 0.03f, 0)
                : side == 2 ? new Vector3(0, 0.03f, -19)
                : new Vector3(0, 0.03f, 19);
            Vector3 inward = -start.normalized;

            if (security.ActiveThreat == ThreatType.Dragon)
            {
                float progress = security.IsBattle ? 0.58f : 0.16f;
                Vector3 dragonPosition = Vector3.Lerp(start, Vector3.zero, progress) + Vector3.up * 6.2f;
                Quaternion rotation = Quaternion.LookRotation(inward, Vector3.up);
                Graphics.DrawMesh(_dragonMesh, Matrix4x4.TRS(dragonPosition, rotation, Vector3.one), _material,
                    0, null, 0, null, ShadowCastingMode.Off, false);
                return;
            }

            Mesh enemy = _enemyMeshes[security.ActiveThreat == ThreatType.Bandits ? 1 : 0];
            int count = security.ActiveThreat == ThreatType.Bandits ? 4 : 6;
            float progressAmount = security.IsBattle ? 0.48f : 0.12f;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3((i - (count - 1) * 0.5f) * 0.55f, 0, 0);
                Vector3 position = Vector3.Lerp(start + offset, Vector3.zero + offset, progressAmount);
                Graphics.DrawMesh(enemy, Matrix4x4.TRS(position, Quaternion.LookRotation(inward), Vector3.one),
                    _material, 0, null, 0, null, ShadowCastingMode.Off, false);
            }
        }

        private static int ScheduleForHour(int hour)
        {
            if (hour < 5) return 0;
            if (hour < 8) return 1;
            if (hour < 18) return 2;
            if (hour < 22) return 3;
            return 4;
        }

        private static Vector3 DoorPosition(BuildingRecord building)
        {
            if (building == null) return Vector3.zero;
            Vector3 center = TownRenderer.TileCenter(new GridPoint(building.X, building.Z));
            return center + new Vector3(0, 0.03f, -0.35f);
        }

        private Mesh CreateCharacterMesh(Color clothing, Color skin, Color accessory)
        {
            MeshAccumulator mesh = new MeshAccumulator();
            mesh.AddBox(new Vector3(0, 0.83f, 0), new Vector3(0.33f, 0.48f, 0.22f), clothing);
            mesh.AddBox(new Vector3(0, 0.56f, 0), new Vector3(0.30f, 0.12f, 0.23f), accessory);
            mesh.AddBox(new Vector3(0, 1.08f, 0), new Vector3(0.25f, 0.14f, 0.23f), clothing);
            mesh.AddSphere(new Vector3(0, 1.29f, 0), new Vector3(0.20f, 0.22f, 0.20f), 8, skin);
            mesh.AddBox(new Vector3(0, 1.43f, 0), new Vector3(0.23f, 0.07f, 0.23f), DarkTimber);
            mesh.AddBox(new Vector3(-0.22f, 0.83f, 0), new Vector3(0.11f, 0.42f, 0.12f), clothing);
            mesh.AddBox(new Vector3(0.22f, 0.83f, 0), new Vector3(0.11f, 0.42f, 0.12f), clothing);
            mesh.AddSphere(new Vector3(-0.22f, 0.60f, 0), new Vector3(0.09f, 0.09f, 0.09f), 6, skin);
            mesh.AddSphere(new Vector3(0.22f, 0.60f, 0), new Vector3(0.09f, 0.09f, 0.09f), 6, skin);
            mesh.AddBox(new Vector3(-0.09f, 0.30f, 0), new Vector3(0.11f, 0.42f, 0.13f), DarkTimber);
            mesh.AddBox(new Vector3(0.09f, 0.30f, 0), new Vector3(0.11f, 0.42f, 0.13f), DarkTimber);
            mesh.AddBox(new Vector3(-0.09f, 0.07f, 0.04f), new Vector3(0.15f, 0.12f, 0.24f), accessory);
            mesh.AddBox(new Vector3(0.09f, 0.07f, 0.04f), new Vector3(0.15f, 0.12f, 0.24f), accessory);
            return mesh.CreateMesh("Low Poly Villager");
        }

        private Mesh CreateDragonMesh()
        {
            MeshAccumulator mesh = new MeshAccumulator();
            Color scale = new Color(0.24f, 0.35f, 0.20f);
            Color wing = new Color(0.32f, 0.38f, 0.21f);
            mesh.AddSphere(new Vector3(0, 0, 0), new Vector3(1.65f, 0.76f, 2.2f), 10, scale);
            mesh.AddSphere(new Vector3(0, 0.18f, 1.45f), new Vector3(0.55f, 0.55f, 0.62f), 8, scale);
            mesh.AddSphere(new Vector3(0, 0.32f, 1.92f), new Vector3(0.64f, 0.38f, 0.55f), 8, new Color(0.44f, 0.42f, 0.26f));
            mesh.AddBox(new Vector3(-0.25f, 0.60f, 2.02f), new Vector3(0.10f, 0.22f, 0.08f), Color.white);
            mesh.AddBox(new Vector3(0.25f, 0.60f, 2.02f), new Vector3(0.10f, 0.22f, 0.08f), Color.white);
            mesh.AddBox(new Vector3(0, 0.72f, 0), new Vector3(0.10f, 0.08f, 2.0f), scale);
            mesh.AddTriangle(new Vector3(0, 0.18f, -0.8f), new Vector3(-2.1f, 0.42f, -1.15f), new Vector3(-0.35f, 0.18f, 0.52f), wing);
            mesh.AddTriangle(new Vector3(0, 0.18f, -0.8f), new Vector3(2.1f, 0.42f, -1.15f), new Vector3(0.35f, 0.18f, 0.52f), wing);
            mesh.AddBox(new Vector3(-0.52f, -0.36f, -0.25f), new Vector3(0.20f, 0.72f, 0.22f), scale);
            mesh.AddBox(new Vector3(0.52f, -0.36f, -0.25f), new Vector3(0.20f, 0.72f, 0.22f), scale);
            mesh.AddBox(new Vector3(-0.52f, -0.36f, 0.82f), new Vector3(0.20f, 0.72f, 0.22f), scale);
            mesh.AddBox(new Vector3(0.52f, -0.36f, 0.82f), new Vector3(0.20f, 0.72f, 0.22f), scale);
            return mesh.CreateMesh("Dragon");
        }

        private static Color DarkCloth(Color color)
        {
            return Color.Lerp(color, Color.black, 0.35f);
        }
    }
}
