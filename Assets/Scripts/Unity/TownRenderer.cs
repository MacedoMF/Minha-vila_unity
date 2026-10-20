using MinhaVila.Core;
using UnityEngine;

namespace MinhaVila.Unity
{
    public sealed class TownRenderer : MonoBehaviour
    {
        private static readonly Color Plaster = new Color(0.82f, 0.72f, 0.55f);
        private static readonly Color PalePlaster = new Color(0.91f, 0.84f, 0.69f);
        private static readonly Color DarkTimber = new Color(0.20f, 0.12f, 0.08f);
        private static readonly Color Timber = new Color(0.40f, 0.24f, 0.12f);
        private static readonly Color Terracotta = new Color(0.56f, 0.20f, 0.11f);
        private static readonly Color Slate = new Color(0.24f, 0.30f, 0.33f);
        private static readonly Color Stone = new Color(0.46f, 0.46f, 0.40f);
        private static readonly Color DarkStone = new Color(0.29f, 0.31f, 0.29f);
        private static readonly Color Glass = new Color(0.21f, 0.40f, 0.43f);
        private static readonly Color Gold = new Color(0.76f, 0.57f, 0.19f);

        private MeshFilter _terrainFilter;
        private MeshFilter _townFilter;
        private MeshFilter _gridFilter;
        private MeshFilter _highlightFilter;
        private MeshRenderer _terrainRenderer;
        private MeshRenderer _townRenderer;
        private MeshRenderer _gridRenderer;
        private MeshRenderer _highlightRenderer;
        private Material _vertexMaterial;
        private VillageState _state;
        private bool _showGrid;
        private GridPoint? _highlightPoint;
        private bool _highlightIsValid;

        public void Initialize()
        {
            Shader shader = Shader.Find("MinhaVila/VertexColorLit");
            if (shader == null) shader = Shader.Find("Standard");
            _vertexMaterial = new Material(shader) { name = "MinhaVila Vertex Color Material" };

            _terrainFilter = CreateMeshObject("Terreno", out _terrainRenderer);
            _townFilter = CreateMeshObject("Vila", out _townRenderer);
            _gridFilter = CreateMeshObject("Grade de construção", out _gridRenderer);
            _highlightFilter = CreateMeshObject("Lote selecionado", out _highlightRenderer);
            _highlightRenderer.enabled = false;
            _terrainRenderer.receiveShadows = true;
            _townRenderer.receiveShadows = true;
            _townRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _terrainRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void SetState(VillageState state)
        {
            _state = state;
            Rebuild();
        }

        public void SetBuildMode(bool enabled)
        {
            _showGrid = enabled;
            RebuildGrid();
        }

        public void SetHighlight(GridPoint? point, bool isValid)
        {
            _highlightPoint = point;
            _highlightIsValid = isValid;
            RebuildHighlight();
        }

        public static Vector3 TileCenter(GridPoint point)
        {
            return new Vector3(point.X - 23.5f, 0, point.Z - 23.5f);
        }

        public static GridPoint WorldToTile(Vector3 position)
        {
            return new GridPoint(Mathf.FloorToInt(position.x + 24), Mathf.FloorToInt(position.z + 24));
        }

        private void Rebuild()
        {
            if (_state == null) return;
            ReplaceMesh(_terrainFilter, _terrainRenderer, BuildTerrain());
            ReplaceMesh(_townFilter, _townRenderer, BuildTown());
            RebuildGrid();
            RebuildHighlight();
        }

        private Mesh BuildTerrain()
        {
            MeshAccumulator mesh = new MeshAccumulator();
            Color baseColor = new Color(0.24f, 0.34f, 0.19f);
            mesh.AddQuad(new Vector3(-32, -0.12f, -32), new Vector3(-32, -0.12f, 32),
                new Vector3(32, -0.12f, 32), new Vector3(32, -0.12f, -32), baseColor);

            for (int x = 0; x < 48; x++)
            {
                for (int z = 0; z < 48; z++)
                {
                    bool owned = LandSystem.IsOwned(_state, x, z);
                    int variation = (x * 13 + z * 7) % 9;
                    Color grass = owned
                        ? Color.Lerp(new Color(0.37f, 0.47f, 0.25f), new Color(0.47f, 0.53f, 0.30f), variation / 8f)
                        : Color.Lerp(new Color(0.23f, 0.32f, 0.18f), new Color(0.31f, 0.38f, 0.21f), variation / 8f);
                    float left = x - 24;
                    float right = left + 1;
                    float bottom = z - 24;
                    float top = bottom + 1;
                    mesh.AddQuad(new Vector3(left, -0.04f, bottom), new Vector3(left, -0.04f, top),
                        new Vector3(right, -0.04f, top), new Vector3(right, -0.04f, bottom), grass);
                }
            }

            AddRiverAndBridge(mesh);
            AddCastle(mesh);
            AddBorderTrees(mesh);
            return mesh.CreateMesh("Minha Vila Terrain");
        }

        private Mesh BuildTown()
        {
            MeshAccumulator mesh = new MeshAccumulator();
            foreach (BuildingRecord building in _state.Buildings)
            {
                if (building.Type == BuildingType.Road) AddRoad(mesh, building);
                else AddBuilding(mesh, building);
            }

            return mesh.CreateMesh("Minha Vila Buildings");
        }

        private void RebuildGrid()
        {
            if (_gridFilter == null) return;
            _gridRenderer.enabled = _showGrid && _state != null;
            if (!_showGrid || _state == null)
            {
                ReplaceMesh(_gridFilter, _gridRenderer, null);
                return;
            }

            int minimum = (48 - _state.TerritorySize) / 2;
            int maximum = minimum + _state.TerritorySize;
            MeshAccumulator mesh = new MeshAccumulator();
            Color lineColor = new Color(0.82f, 0.75f, 0.52f, 1);
            float y = 0.025f;
            for (int i = minimum; i <= maximum; i++)
            {
                float position = i - 24;
                mesh.AddBox(new Vector3(position, y, (minimum + maximum) * 0.5f - 24),
                    new Vector3(0.012f, 0.006f, _state.TerritorySize), lineColor);
                mesh.AddBox(new Vector3((minimum + maximum) * 0.5f - 24, y, position),
                    new Vector3(_state.TerritorySize, 0.006f, 0.012f), lineColor);
            }

            ReplaceMesh(_gridFilter, _gridRenderer, mesh.CreateMesh("Build Grid"));
        }

        private void RebuildHighlight()
        {
            if (_highlightFilter == null) return;
            if (!_highlightPoint.HasValue || _state == null)
            {
                _highlightRenderer.enabled = false;
                ReplaceMesh(_highlightFilter, _highlightRenderer, null);
                return;
            }

            Vector3 center = TileCenter(_highlightPoint.Value);
            Color color = _highlightIsValid ? new Color(0.30f, 0.82f, 0.43f) : new Color(0.87f, 0.39f, 0.24f);
            MeshAccumulator mesh = new MeshAccumulator();
            float y = 0.04f;
            float size = 0.88f;
            mesh.AddQuad(new Vector3(center.x - size * 0.5f, y, center.z - size * 0.5f),
                new Vector3(center.x - size * 0.5f, y, center.z + size * 0.5f),
                new Vector3(center.x + size * 0.5f, y, center.z + size * 0.5f),
                new Vector3(center.x + size * 0.5f, y, center.z - size * 0.5f), color);
            ReplaceMesh(_highlightFilter, _highlightRenderer, mesh.CreateMesh("Selected Tile"));
            _highlightRenderer.enabled = true;
        }

        private void AddRoad(MeshAccumulator mesh, BuildingRecord building)
        {
            Vector3 center = TileCenter(new GridPoint(building.X, building.Z));
            center.y = 0.025f;
            mesh.AddBox(center, new Vector3(0.98f, 0.07f, 0.98f), DarkStone);
            for (int i = 0; i < 6; i++)
            {
                int seed = (building.X * 37 + building.Z * 19 + i * 13) % 17;
                float x = ((seed % 5) - 2) * 0.16f;
                float z = (((seed / 3) % 5) - 2) * 0.16f;
                Color stone = Color.Lerp(new Color(0.47f, 0.46f, 0.40f), new Color(0.63f, 0.59f, 0.49f), (seed % 8) / 7f);
                mesh.AddBox(center + new Vector3(x, 0.045f, z), new Vector3(0.18f, 0.025f, 0.16f), stone);
            }
        }

        private void AddBuilding(MeshAccumulator mesh, BuildingRecord building)
        {
            Vector3 center = TileCenter(new GridPoint(building.X, building.Z));
            BuildingDefinition definition = BuildingCatalog.Get(building.Type, building.Level);
            if (definition == null) return;

            float width = building.Type == BuildingType.RoyalTavern || building.Type == BuildingType.Bank ? 0.90f : 0.78f;
            float depth = building.Type == BuildingType.RoyalTavern ? 0.90f : 0.76f;
            float bodyHeight = building.Type == BuildingType.GuardPost || building.Type == BuildingType.ArcaneTower
                ? 1.48f : building.Level == 2 && building.Type == BuildingType.House ? 1.42f : 1.05f;
            Color wall = building.Type == BuildingType.Bank || building.Type == BuildingType.GuardPost ? Stone : Plaster;
            Color roof = building.Type == BuildingType.ArcaneTower || building.Type == BuildingType.GrainMill ? Slate : Terracotta;

            mesh.AddBox(center + new Vector3(0, 0.11f, 0), new Vector3(width + 0.12f, 0.20f, depth + 0.12f), DarkStone);
            mesh.AddBox(center + new Vector3(0, 0.17f + bodyHeight * 0.5f, 0),
                new Vector3(width, bodyHeight, depth), wall);

            float eaves = 0.17f + bodyHeight;
            float ridge = eaves + (building.Type == BuildingType.ArcaneTower ? 0.58f : 0.48f);
            mesh.AddGableRoof(center, width + 0.14f, depth + 0.18f, eaves, ridge, roof, wall);

            AddTimberFrame(mesh, center, width, depth, eaves);
            AddDoorAndWindows(mesh, center, width, depth, bodyHeight, building.Type == BuildingType.Bank);

            if (building.Type == BuildingType.Tavern || building.Type == BuildingType.RoyalTavern)
                AddTavernDetails(mesh, center, building.Type == BuildingType.RoyalTavern);
            if (building.Type == BuildingType.Forge || building.Type == BuildingType.Sawmill || building.Type == BuildingType.Quarry)
                AddChimney(mesh, center, width);
            if (building.Type == BuildingType.Stable) AddHorse(mesh, center);
            if (building.Type == BuildingType.Farm) AddCropRows(mesh, center);
            if (building.Type == BuildingType.Orchard) AddOrchard(mesh, center);
            if (building.Type == BuildingType.GuardPost) AddGuardTower(mesh, center);
            if (building.Type == BuildingType.ResearchCenter || building.Type == BuildingType.ArcaneTower)
                AddResearchDetails(mesh, center, ridge);
            if (building.Type == BuildingType.BardSquare) AddGarden(mesh, center);
            if (building.Type == BuildingType.GrainMill) AddMillWheel(mesh, center);
            if (building.Type == BuildingType.Bank) AddBankDetails(mesh, center);
        }

        private void AddTimberFrame(MeshAccumulator mesh, Vector3 center, float width, float depth, float eaves)
        {
            float beamWidth = 0.055f;
            float beamHeight = Mathf.Min(0.16f, eaves * 0.13f);
            mesh.AddBox(center + new Vector3(-width * 0.5f + beamWidth * 0.5f, eaves * 0.52f, -depth * 0.5f - 0.012f),
                new Vector3(beamWidth, eaves * 0.92f, beamWidth), DarkTimber);
            mesh.AddBox(center + new Vector3(width * 0.5f - beamWidth * 0.5f, eaves * 0.52f, -depth * 0.5f - 0.012f),
                new Vector3(beamWidth, eaves * 0.92f, beamWidth), DarkTimber);
            mesh.AddBox(center + new Vector3(0, eaves * 0.52f, -depth * 0.5f - 0.025f),
                new Vector3(width, beamHeight, beamWidth), DarkTimber);
            mesh.AddBox(center + new Vector3(0, eaves * 0.22f, -depth * 0.5f - 0.025f),
                new Vector3(width * 0.93f, beamWidth, beamWidth), DarkTimber);
            mesh.AddBox(center + new Vector3(0, eaves * 0.52f, depth * 0.5f + 0.012f),
                new Vector3(width, beamHeight, beamWidth), DarkTimber);
        }

        private void AddDoorAndWindows(MeshAccumulator mesh, Vector3 center, float width, float depth, float bodyHeight, bool stoneFrame)
        {
            Color frame = stoneFrame ? DarkStone : DarkTimber;
            float front = center.z - depth * 0.5f - 0.025f;
            mesh.AddBox(center + new Vector3(0, 0.43f, front), new Vector3(0.22f, 0.68f, 0.045f), Timber);
            mesh.AddBox(center + new Vector3(-0.11f, 0.77f, front - 0.005f), new Vector3(0.29f, 0.06f, 0.07f), frame);
            foreach (float side in new[] { -1f, 1f })
            {
                mesh.AddBox(center + new Vector3(side * width * 0.29f, 0.69f, front - 0.018f),
                    new Vector3(0.16f, 0.23f, 0.035f), frame);
                mesh.AddBox(center + new Vector3(side * width * 0.29f, 0.69f, front - 0.041f),
                    new Vector3(0.10f, 0.16f, 0.02f), Glass);
                mesh.AddBox(center + new Vector3(side * width * 0.29f, 0.69f, front - 0.053f),
                    new Vector3(0.02f, 0.18f, 0.018f), Gold);
            }

            if (bodyHeight > 1.2f)
            {
                mesh.AddBox(center + new Vector3(-width * 0.30f, 1.27f, front - 0.02f),
                    new Vector3(0.16f, 0.18f, 0.035f), frame);
                mesh.AddBox(center + new Vector3(-width * 0.30f, 1.27f, front - 0.045f),
                    new Vector3(0.10f, 0.12f, 0.02f), Glass);
            }
        }

        private void AddTavernDetails(MeshAccumulator mesh, Vector3 center, bool royal)
        {
            Color sign = royal ? new Color(0.14f, 0.26f, 0.37f) : new Color(0.42f, 0.20f, 0.10f);
            mesh.AddBox(center + new Vector3(0.25f, 0.94f, -0.43f), new Vector3(0.30f, 0.18f, 0.06f), sign);
            mesh.AddBox(center + new Vector3(0.25f, 1.06f, -0.43f), new Vector3(0.045f, 0.30f, 0.05f), DarkTimber);
            if (royal)
            {
                mesh.AddBox(center + new Vector3(0.31f, 1.44f, -0.22f), new Vector3(0.05f, 0.72f, 0.05f), DarkTimber);
                mesh.AddTriangle(center + new Vector3(0.33f, 1.67f, -0.20f), center + new Vector3(0.70f, 1.59f, -0.20f),
                    center + new Vector3(0.33f, 1.42f, -0.20f), new Color(0.15f, 0.29f, 0.44f));
                mesh.AddBox(center + new Vector3(0, 1.72f, 0), new Vector3(0.23f, 0.08f, 0.23f), Gold);
            }
            else
            {
                mesh.AddBox(center + new Vector3(-0.28f, 1.13f, -0.30f), new Vector3(0.24f, 0.09f, 0.16f), Timber);
            }
        }

        private void AddChimney(MeshAccumulator mesh, Vector3 center, float width)
        {
            Vector3 chimney = center + new Vector3(width * 0.30f, 1.45f, width * 0.24f);
            mesh.AddBox(chimney, new Vector3(0.16f, 0.66f, 0.16f), DarkStone);
            mesh.AddBox(chimney + Vector3.up * 0.34f, new Vector3(0.22f, 0.08f, 0.22f), Stone);
        }

        private void AddHorse(MeshAccumulator mesh, Vector3 center)
        {
            Vector3 horse = center + new Vector3(0.13f, 0.42f, 0.31f);
            Color coat = new Color(0.43f, 0.25f, 0.15f);
            mesh.AddSphere(horse, new Vector3(0.38f, 0.34f, 0.22f), 8, coat);
            mesh.AddSphere(horse + new Vector3(0.17f, 0.20f, -0.04f), new Vector3(0.16f, 0.28f, 0.16f), 7, coat);
            mesh.AddSphere(horse + new Vector3(0.22f, 0.34f, -0.03f), new Vector3(0.18f, 0.15f, 0.16f), 7, new Color(0.58f, 0.39f, 0.23f));
            for (int i = 0; i < 4; i++)
            {
                float x = (i < 2 ? -0.10f : 0.13f);
                float z = (i % 2 == 0 ? -0.06f : 0.09f);
                mesh.AddBox(horse + new Vector3(x, -0.18f, z), new Vector3(0.055f, 0.32f, 0.055f), DarkTimber);
            }
        }

        private void AddCropRows(MeshAccumulator mesh, Vector3 center)
        {
            Color soil = new Color(0.31f, 0.24f, 0.14f);
            mesh.AddBox(center + new Vector3(0, 0.035f, 0.39f), new Vector3(0.70f, 0.04f, 0.27f), soil);
            for (int row = 0; row < 3; row++)
            {
                for (int plant = 0; plant < 3; plant++)
                {
                    float x = -0.23f + plant * 0.23f;
                    float z = 0.30f + row * 0.08f;
                    mesh.AddBox(center + new Vector3(x, 0.13f, z), new Vector3(0.035f, 0.19f, 0.035f),
                        row % 2 == 0 ? new Color(0.57f, 0.59f, 0.24f) : new Color(0.37f, 0.51f, 0.22f));
                }
            }
        }

        private void AddOrchard(MeshAccumulator mesh, Vector3 center)
        {
            AddTree(mesh, center + new Vector3(-0.27f, 0, 0.25f), 0.65f, new Color(0.35f, 0.44f, 0.20f));
            AddTree(mesh, center + new Vector3(0.28f, 0, 0.23f), 0.62f, new Color(0.48f, 0.47f, 0.20f));
            AddTree(mesh, center + new Vector3(0.30f, 0, -0.28f), 0.58f, new Color(0.35f, 0.47f, 0.23f));
        }

        private void AddGuardTower(MeshAccumulator mesh, Vector3 center)
        {
            mesh.AddBox(center + new Vector3(0, 1.67f, 0), new Vector3(0.60f, 0.12f, 0.60f), DarkTimber);
            mesh.AddBox(center + new Vector3(0, 1.82f, 0), new Vector3(0.72f, 0.20f, 0.72f), Timber);
            mesh.AddGableRoof(center + new Vector3(0, 1.7f, 0), 0.88f, 0.88f, 1.95f, 2.36f, Slate, DarkTimber);
        }

        private void AddResearchDetails(MeshAccumulator mesh, Vector3 center, float ridge)
        {
            Color accent = new Color(0.24f, 0.48f, 0.55f);
            mesh.AddSphere(center + new Vector3(0.22f, ridge + 0.10f, 0.14f), new Vector3(0.18f, 0.20f, 0.18f), 7, accent);
            mesh.AddBox(center + new Vector3(-0.12f, 1.08f, -0.42f), new Vector3(0.12f, 0.24f, 0.04f), accent);
        }

        private void AddGarden(MeshAccumulator mesh, Vector3 center)
        {
            mesh.AddBox(center + new Vector3(0, 0.08f, 0), new Vector3(0.86f, 0.12f, 0.86f), new Color(0.40f, 0.49f, 0.27f));
            mesh.AddCylinder(center + new Vector3(0, 0.19f, 0), 0.13f, 0.14f, 8, Stone);
            mesh.AddCylinder(center + new Vector3(0, 0.29f, 0), 0.06f, 0.12f, 7, Glass);
            AddTree(mesh, center + new Vector3(-0.27f, 0.02f, -0.25f), 0.52f, new Color(0.40f, 0.50f, 0.26f));
            AddTree(mesh, center + new Vector3(0.29f, 0.02f, 0.26f), 0.48f, new Color(0.48f, 0.53f, 0.29f));
        }

        private void AddMillWheel(MeshAccumulator mesh, Vector3 center)
        {
            Vector3 wheel = center + new Vector3(0.45f, 0.62f, 0.04f);
            mesh.AddCylinder(wheel, 0.31f, 0.09f, 10, DarkTimber);
            mesh.AddCylinder(wheel + Vector3.right * 0.06f, 0.10f, 0.12f, 8, Timber);
        }

        private void AddBankDetails(MeshAccumulator mesh, Vector3 center)
        {
            mesh.AddBox(center + new Vector3(0, 1.27f, -0.42f), new Vector3(0.54f, 0.12f, 0.06f), Gold);
            mesh.AddBox(center + new Vector3(0, 1.38f, -0.42f), new Vector3(0.34f, 0.07f, 0.06f), DarkTimber);
        }

        private void AddRiverAndBridge(MeshAccumulator mesh)
        {
            for (int i = 0; i < 16; i++)
            {
                float x0 = -32 + i * 4;
                float x1 = x0 + 4;
                float bend = Mathf.Sin(i * 0.75f) * 0.55f;
                Color water = Color.Lerp(new Color(0.20f, 0.43f, 0.52f), new Color(0.33f, 0.58f, 0.62f), (i % 3) / 2f);
                mesh.AddQuad(new Vector3(x0, -0.02f, 25 + bend), new Vector3(x0, -0.02f, 27.5f + bend),
                    new Vector3(x1, -0.02f, 27.5f + bend), new Vector3(x1, -0.02f, 25 + bend), water);
            }

            for (int i = 0; i < 8; i++)
            {
                float x = -3.5f + i;
                mesh.AddBox(new Vector3(x, 0.22f, 26.25f), new Vector3(0.92f, 0.22f, 1.55f), Stone);
                mesh.AddBox(new Vector3(x, 0.36f, 26.25f), new Vector3(0.92f, 0.08f, 1.65f), new Color(0.59f, 0.55f, 0.46f));
            }

            foreach (float x in new[] { -3.4f, 3.4f })
            {
                mesh.AddBox(new Vector3(x, 0.78f, 26.25f), new Vector3(0.34f, 1.1f, 1.6f), Stone);
                mesh.AddBox(new Vector3(x, 1.36f, 26.25f), new Vector3(0.48f, 0.14f, 1.75f), PalePlaster);
            }
        }

        private void AddCastle(MeshAccumulator mesh)
        {
            Vector3 hill = new Vector3(0, -0.04f, 34.8f);
            mesh.AddSphere(hill, new Vector3(14, 2.3f, 8), 16, new Color(0.37f, 0.43f, 0.26f));
            Color stone = new Color(0.53f, 0.53f, 0.47f);
            mesh.AddBox(new Vector3(0, 1.03f, 34.4f), new Vector3(8.2f, 2.4f, 3.0f), stone);
            mesh.AddBox(new Vector3(0, 2.32f, 34.4f), new Vector3(9.0f, 0.20f, 3.35f), DarkStone);
            foreach (float x in new[] { -3.5f, 3.5f })
            {
                mesh.AddCylinder(new Vector3(x, 2.55f, 34.4f), 0.82f, 4.5f, 10, stone);
                mesh.AddBox(new Vector3(x, 4.78f, 34.4f), new Vector3(1.85f, 0.28f, 1.85f), DarkStone);
                mesh.AddCone(new Vector3(x, 5.5f, 34.4f), 0.92f, 1.35f, 10, Slate);
            }
            for (int i = -4; i <= 4; i++)
            {
                mesh.AddBox(new Vector3(i * 0.82f, 2.57f, 32.83f), new Vector3(0.44f, 0.55f, 0.42f), PalePlaster);
            }
            mesh.AddBox(new Vector3(0.3f, 3.2f, 32.77f), new Vector3(0.08f, 2.0f, 0.08f), DarkTimber);
            mesh.AddTriangle(new Vector3(0.34f, 4.1f, 32.78f), new Vector3(1.5f, 3.85f, 32.78f),
                new Vector3(0.34f, 3.48f, 32.78f), new Color(0.14f, 0.29f, 0.45f));
        }

        private void AddBorderTrees(MeshAccumulator mesh)
        {
            for (int x = -29; x <= 29; x += 4)
            {
                AddTreeIfOutside(mesh, x, -27 + (x % 3) * 0.3f);
                AddTreeIfOutside(mesh, x + 1.3f, 28 + (x % 4) * 0.2f);
            }
            for (int z = -24; z <= 25; z += 5)
            {
                AddTreeIfOutside(mesh, -27 + (z % 4) * 0.25f, z);
                AddTreeIfOutside(mesh, 28 + (z % 3) * 0.25f, z + 1.1f);
            }
        }

        private void AddTreeIfOutside(MeshAccumulator mesh, float x, float z)
        {
            GridPoint tile = WorldToTile(new Vector3(x, 0, z));
            if (LandSystem.IsOwned(_state, tile.X, tile.Z)) return;
            float height = 2.0f + Mathf.Abs(Mathf.Sin(x * 0.7f + z)) * 1.2f;
            Color leaves = new Color(0.23f, 0.35f, 0.18f);
            AddTree(mesh, new Vector3(x, 0, z), height, leaves);
        }

        private void AddTree(MeshAccumulator mesh, Vector3 position, float height, Color leaves)
        {
            mesh.AddCylinder(position + Vector3.up * (height * 0.24f), 0.12f, height * 0.50f, 6, new Color(0.30f, 0.20f, 0.11f));
            mesh.AddCone(position + Vector3.up * (height * 0.65f), height * 0.30f, height * 0.85f, 7, leaves);
            mesh.AddCone(position + Vector3.up * (height * 0.88f), height * 0.22f, height * 0.62f, 7,
                Color.Lerp(leaves, new Color(0.52f, 0.57f, 0.30f), 0.18f));
        }

        private MeshFilter CreateMeshObject(string objectName, out MeshRenderer meshRenderer)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            MeshFilter filter = child.AddComponent<MeshFilter>();
            meshRenderer = child.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _vertexMaterial;
            return filter;
        }

        private void ReplaceMesh(MeshFilter filter, MeshRenderer meshRenderer, Mesh mesh)
        {
            if (filter == null) return;
            Mesh previous = filter.sharedMesh;
            filter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = _vertexMaterial;
            if (previous != null) Destroy(previous);
        }
    }
}
