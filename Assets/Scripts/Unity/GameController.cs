using System.Collections.Generic;
using MinhaVila.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MinhaVila.Unity
{
    public sealed class GameController : MonoBehaviour
    {
        private SaveStore _saveStore;
        private TownRenderer _townRenderer;
        private DynamicLighting _lighting;
        private CitizenVisualSystem _citizens;
        private MobileUiController _ui;
        private MusicPlayer _music;
        private CameraController _cameraController;
        private int _lastAutoSaveHour;
        private bool _resumeAfterModal;
        private bool _modalPauseActive;
        private bool _wasRunningBeforeAppPause;

        public VillageSimulation Simulation { get; private set; }
        public VillageState State { get { return Simulation == null ? null : Simulation.State; } }
        public bool IsInMenu { get; private set; } = true;
        public BuildingType? SelectedBuildType { get; private set; }
        public int? SelectedBuildingId { get; private set; }
        public bool IsBuildMode { get { return SelectedBuildType.HasValue; } }
        public CameraController CameraController { get { return _cameraController; } }
        public MusicPlayer Music { get { return _music; } }

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (Simulation == null || IsInMenu) return;

            Simulation.AdvanceRealSeconds(Time.unscaledDeltaTime);
            _lighting.UpdateTime(State.Hour);

            int elapsedHours = (State.Day - 1) * 24 + State.Hour;
            if (elapsedHours - _lastAutoSaveHour >= 6)
            {
                SaveCurrent(false);
                _lastAutoSaveHour = elapsedHours;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (Simulation == null || IsInMenu) return;
            if (paused)
            {
                _wasRunningBeforeAppPause = !Simulation.IsPaused;
                if (_wasRunningBeforeAppPause) Simulation.SetPaused(true);
                SaveCurrent(false);
                return;
            }

            if (_wasRunningBeforeAppPause && !IsInMenu)
                Simulation.SetPaused(false);
            _wasRunningBeforeAppPause = false;
        }

        public void Initialize()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            _saveStore = new SaveStore();

            GameObject cameraObject = new GameObject("Câmera isométrica");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.tag = "MainCamera";
            _cameraController = cameraObject.AddComponent<CameraController>();
            _cameraController.Initialize(this);

            GameObject world = new GameObject("Mundo da vila");
            world.transform.SetParent(transform, false);
            _townRenderer = world.AddComponent<TownRenderer>();
            _townRenderer.Initialize();
            _lighting = world.AddComponent<DynamicLighting>();
            _lighting.Initialize(cameraObject.GetComponent<Camera>());
            _citizens = world.AddComponent<CitizenVisualSystem>();
            _citizens.Initialize(this);

            EnsureEventSystem();
            _music = gameObject.AddComponent<MusicPlayer>();
            _music.Initialize();

            Simulation = new VillageSimulation(VillageState.CreateNew("Minha Vila"));
            Simulation.SetPaused(true);
            Simulation.Changed += OnSimulationChanged;
            RefreshScene();
            _ui = gameObject.AddComponent<MobileUiController>();
            _ui.Initialize(this);
            _music.SetInVillage(false);
        }

        public void StartNewGame()
        {
            StartGame(ScenarioFactory.CreateStarter());
        }

        public void StartScenario(string scenarioId)
        {
            StartGame(ScenarioFactory.Create(scenarioId));
        }

        public void StartGame(VillageState state)
        {
            if (state == null) return;
            if (Simulation != null) Simulation.Changed -= OnSimulationChanged;
            Simulation = new VillageSimulation(state);
            Simulation.SetPaused(false);
            Simulation.Changed += OnSimulationChanged;
            IsInMenu = false;
            SelectedBuildType = null;
            SelectedBuildingId = null;
            _resumeAfterModal = false;
            _modalPauseActive = false;
            _lastAutoSaveHour = (State.Day - 1) * 24 + State.Hour;
            _cameraController.SetCamera(State.CameraX, State.CameraZ, State.CameraZoom);
            _music.SetInVillage(true);
            RefreshScene();
            _ui.ShowGame();
            _ui.ShowToast("A vila começou. Construa e conecte seus edifícios por ruas.");
        }

        public void ReturnToMainMenu()
        {
            if (Simulation != null) SaveCurrent(false);
            IsInMenu = true;
            SelectedBuildType = null;
            SelectedBuildingId = null;
            if (Simulation != null) Simulation.SetPaused(true);
            _music.SetInVillage(false);
            _townRenderer.SetBuildMode(false);
            _townRenderer.SetHighlight(null, false);
            _ui.ShowMainMenu();
        }

        public List<VillageSummary> ListSaves()
        {
            return _saveStore.ListSaves();
        }

        public void LoadSave(string id)
        {
            if (!_saveStore.TryLoad(id, out VillageState state, out string message))
            {
                _ui.ShowToast(message);
                return;
            }

            StartGame(state);
            _ui.ShowToast(message);
        }

        public void SaveCurrent(bool showMessage = true)
        {
            if (State == null) return;
            State.CameraX = _cameraController.Target.x;
            State.CameraZ = _cameraController.Target.z;
            State.CameraZoom = _cameraController.Zoom;
            if (_saveStore.Save(State, out string message) && showMessage)
                _ui.ShowToast(message);
            else if (!string.IsNullOrEmpty(message) && showMessage)
                _ui.ShowToast(message);
        }

        public void SetPaused(bool paused)
        {
            if (Simulation == null) return;
            Simulation.SetPaused(paused);
            _ui.RefreshHud();
        }

        public void TogglePaused()
        {
            if (Simulation != null) SetPaused(!Simulation.IsPaused);
        }

        public void SetSpeed(int speed)
        {
            if (Simulation != null) Simulation.SetSpeed(speed);
        }

        public void BeginModal()
        {
            if (Simulation == null || IsInMenu) return;
            if (_modalPauseActive) return;
            _modalPauseActive = true;
            _resumeAfterModal = !Simulation.IsPaused;
            if (_resumeAfterModal) Simulation.SetPaused(true);
        }

        public void EndModal()
        {
            if (!_modalPauseActive) return;
            if (Simulation != null && !IsInMenu && _resumeAfterModal)
                Simulation.SetPaused(false);
            _resumeAfterModal = false;
            _modalPauseActive = false;
        }

        public void SelectBuildType(BuildingType type)
        {
            if (IsInMenu) return;
            SelectedBuildType = type;
            SelectedBuildingId = null;
            _townRenderer.SetBuildMode(true);
            _townRenderer.SetHighlight(null, false);
            _ui.ShowToast("Toque em um lote livre para construir " + BuildingCatalog.Get(type).Name + ".");
            _ui.RefreshHud();
        }

        public void CancelBuildMode()
        {
            SelectedBuildType = null;
            _townRenderer.SetBuildMode(false);
            _townRenderer.SetHighlight(null, false);
            _ui.RefreshHud();
        }

        public void HandleWorldTileClicked(GridPoint point)
        {
            if (IsInMenu || State == null) return;
            BuildingRecord building = State.FindBuildingAt(point.X, point.Z);
            if (SelectedBuildType.HasValue)
            {
                if (building != null)
                {
                    ShowBuildHighlight(point, false);
                    _ui.ShowToast("Esse lote já está ocupado.");
                    return;
                }

                bool built = Simulation.TryBuild(SelectedBuildType.Value, point, 0, out string message);
                ShowBuildHighlight(point, built);
                if (built) RefreshScene();
                _ui.ShowToast(message);
                return;
            }

            SelectedBuildingId = building == null ? (int?)null : building.Id;
            _townRenderer.SetHighlight(building == null ? (GridPoint?)null : point, true);
            if (building != null) _ui.ShowBuildingDetails(building);
            else _ui.HideBuildingDetails();
        }

        public bool UpgradeBuilding(int id, out string message)
        {
            bool result = Simulation.TryUpgrade(id, out message);
            if (result) RefreshScene();
            return result;
        }

        public bool DemolishBuilding(int id, out string message)
        {
            bool result = Simulation.TryDemolish(id, out message);
            if (result)
            {
                SelectedBuildingId = null;
                RefreshScene();
                _ui.HideBuildingDetails();
            }
            return result;
        }

        public bool Build(BuildingType type, GridPoint point, out string message)
        {
            bool result = Simulation.TryBuild(type, point, 0, out message);
            if (result) RefreshScene();
            return result;
        }

        public bool Undo(out string message)
        {
            bool result = Simulation.UndoLastAction(out message);
            if (result) RefreshScene();
            return result;
        }

        public bool StartResearch(ResearchId id, out string message)
        {
            return Simulation.TryStartResearch(id, out message);
        }

        public bool ExpandLand(out string message)
        {
            bool result = Simulation.TryExpandTerritory(out message);
            if (result) _townRenderer.SetState(State);
            return result;
        }

        public bool BuyResource(ResourceType type, int quantity, out string message)
        {
            return Simulation.TryBuyResource(type, quantity, out message);
        }

        public bool Borrow(int amount, out string message)
        {
            return Simulation.TryBorrow(amount, out message);
        }

        public bool Repay(int amount, out string message)
        {
            return Simulation.TryRepay(amount, out message);
        }

        public bool StartMission(int tavernId, MissionType type, out string message)
        {
            return Simulation.TryStartMission(tavernId, type, out message);
        }

        public bool UpgradeRoyalItem(bool tools, out string message)
        {
            return Simulation.TryUpgradeRoyalItem(tools, out message);
        }

        public void BeginRaid(ThreatType type)
        {
            SecuritySystem.BeginManualRaid(State, type, Simulation);
        }

        public void MarkNotificationsRead()
        {
            Simulation.MarkNotificationsRead();
        }

        public void ShowBuildHighlight(GridPoint point, bool isValid)
        {
            _townRenderer.SetHighlight(point, isValid);
        }

        public void RefreshScene()
        {
            if (State == null) return;
            _townRenderer.SetState(State);
            _citizens.SetState(State);
            _lighting.UpdateTime(State.Hour);
            _ui?.RefreshHud();
        }

        private void OnSimulationChanged()
        {
            _ui?.RefreshHud();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetAsLastSibling();
        }
    }
}
