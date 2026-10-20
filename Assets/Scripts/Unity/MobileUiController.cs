using System;
using System.Collections.Generic;
using System.Linq;
using MinhaVila.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MinhaVila.Unity
{
    public sealed class MobileUiController : MonoBehaviour
    {
        private enum PanelKind { Build, Village, Research, Missions, Security, Finance, Land, More, Notifications, Saves, Scenarios }

        private static readonly Color Ink = new Color(0.95f, 0.91f, 0.80f);
        private static readonly Color PanelColor = new Color(0.10f, 0.13f, 0.12f, 0.96f);
        private static readonly Color ButtonColor = new Color(0.29f, 0.25f, 0.17f, 0.98f);
        private static readonly Color AccentColor = new Color(0.47f, 0.34f, 0.18f, 1);
        private static readonly Color GoodColor = new Color(0.20f, 0.36f, 0.25f, 1);
        private static readonly Color DangerColor = new Color(0.46f, 0.20f, 0.17f, 1);

        private GameController _game;
        private Canvas _canvas;
        private RectTransform _menuRoot;
        private RectTransform _hudRoot;
        private RectTransform _modalRoot;
        private RectTransform _modalContent;
        private Text _modalTitle;
        private RectTransform _detailsRoot;
        private Text _resourceText;
        private Text _clockText;
        private Text _toastText;
        private Text _detailText;
        private float _toastRemaining;
        private bool _initialized;

        public void Initialize(GameController game)
        {
            if (_initialized) return;
            _initialized = true;
            _game = game;
            _canvas = UiFactory.CreateCanvas(transform);
            BuildHud();
            BuildMainMenu();
            BuildModal();
            BuildDetailsPanel();
            BuildToast();
            ShowMainMenu();
        }

        private void Update()
        {
            if (_toastRemaining <= 0 || _toastText == null) return;
            _toastRemaining -= Time.unscaledDeltaTime;
            if (_toastRemaining <= 0) _toastText.transform.parent.gameObject.SetActive(false);
        }

        public void RefreshHud()
        {
            if (_game == null || _game.State == null || _resourceText == null) return;
            VillageState state = _game.State;
            VillageSimulation simulation = _game.Simulation;
            _resourceText.text = "Moradores: " + state.Population + "/" + state.HousingCapacity
                + "    Ouro: " + Mathf.FloorToInt((float)state.Gold).ToString("N0")
                + "\nMadeira: " + state.Stock.Timber + "   Pedra: " + state.Stock.Stone
                + "   Ferro: " + state.Stock.Iron + "   Comida: " + state.Stock.Food;
            _clockText.text = "Dia " + state.Day + "   " + state.Hour.ToString("00") + ":" + state.Minute.ToString("00")
                + "   " + (simulation.IsPaused ? "Pausado" : simulation.State.Speed + "×");
        }

        public void ShowMainMenu()
        {
            _menuRoot.gameObject.SetActive(true);
            _hudRoot.gameObject.SetActive(false);
            _modalRoot.gameObject.SetActive(false);
            _detailsRoot.gameObject.SetActive(false);
        }

        public void ShowGame()
        {
            _menuRoot.gameObject.SetActive(false);
            _hudRoot.gameObject.SetActive(true);
            _modalRoot.gameObject.SetActive(false);
            HideBuildingDetails();
            RefreshHud();
        }

        public void ShowToast(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || _toastText == null) return;
            _toastText.text = message;
            _toastText.transform.parent.gameObject.SetActive(true);
            _toastRemaining = 3.5f;
        }

        public void ShowBuildingDetails(BuildingRecord building)
        {
            if (building == null || _game.State == null) return;
            BuildingDefinition definition = BuildingCatalog.Get(building.Type, building.Level);
            int workers = _game.State.Residents.Count(resident => resident.WorkplaceId == building.Id);
            int residents = _game.State.Residents.Count(resident => resident.HomeId == building.Id);
            string extra = building.Type == BuildingType.House
                ? "Moradores: " + residents + "/" + definition.HousingCapacity + "\n"
                : definition.WorkSlots > 0 ? "Vagas: " + workers + "/" + definition.WorkSlots + "\n" : string.Empty;
            _detailText.text = definition.Name + "\n" + extra + DescribeProduction(definition);
            _detailsRoot.gameObject.SetActive(true);
            foreach (Button button in _detailsRoot.GetComponentsInChildren<Button>(true)) button.onClick.RemoveAllListeners();
            Button upgrade = _detailsRoot.Find("Ações/Melhorar").GetComponent<Button>();
            Button demolish = _detailsRoot.Find("Ações/Demolir").GetComponent<Button>();
            bool hasUpgrade = BuildingCatalog.Get(building.Type, 2) != null;
            upgrade.gameObject.SetActive(hasUpgrade && building.Level == 1);
            upgrade.onClick.AddListener(() =>
            {
                bool success = _game.UpgradeBuilding(building.Id, out string message);
                ShowToast(message);
                if (success) ShowBuildingDetails(_game.State.FindBuilding(building.Id));
            });
            demolish.onClick.AddListener(() =>
            {
                bool success = _game.DemolishBuilding(building.Id, out string message);
                ShowToast(message);
                if (success) HideBuildingDetails();
            });
        }

        public void HideBuildingDetails()
        {
            if (_detailsRoot != null) _detailsRoot.gameObject.SetActive(false);
        }

        private void BuildHud()
        {
            _hudRoot = UiFactory.CreatePanel(_canvas.transform, "HUD", Color.clear);
            UiFactory.SetRect(_hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _hudRoot.GetComponent<Image>().raycastTarget = false;

            RectTransform status = UiFactory.CreatePanel(_hudRoot, "Status", PanelColor);
            UiFactory.SetRect(status, new Vector2(0.015f, 0.90f), new Vector2(0.58f, 0.985f), Vector2.zero, Vector2.zero);
            _resourceText = UiFactory.CreateText(status, "Recursos", string.Empty, 18, Ink);
            UiFactory.SetRect(_resourceText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 4), new Vector2(-18, -4));

            RectTransform clock = UiFactory.CreatePanel(_hudRoot, "Relógio", PanelColor);
            UiFactory.SetRect(clock, new Vector2(0.61f, 0.91f), new Vector2(0.78f, 0.985f), Vector2.zero, Vector2.zero);
            _clockText = UiFactory.CreateText(clock, "Hora", string.Empty, 21, Ink, TextAnchor.MiddleCenter);
            UiFactory.SetRect(_clockText.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0));

            RectTransform timeButtons = UiFactory.CreatePanel(_hudRoot, "Velocidade", PanelColor);
            UiFactory.SetRect(timeButtons, new Vector2(0.79f, 0.91f), new Vector2(0.985f, 0.985f), Vector2.zero, Vector2.zero);
            string[] labels = { "Ⅱ", "1×", "2×", "5×", "10×" };
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                RectTransform buttonRect = (RectTransform)UiFactory.CreateButton(timeButtons, labels[i], () =>
                {
                    if (index == 0) _game.TogglePaused();
                    else _game.SetSpeed(index == 1 ? 1 : index == 2 ? 2 : index == 3 ? 5 : 10);
                    RefreshHud();
                }, ButtonColor, 18).transform;
                float width = 1f / labels.Length;
                UiFactory.SetRect(buttonRect, new Vector2(i * width, 0), new Vector2((i + 1) * width, 1),
                    new Vector2(3, 4), new Vector2(-3, -4));
            }

            RectTransform toolbar = UiFactory.CreatePanel(_hudRoot, "Barra de ações", PanelColor);
            UiFactory.SetRect(toolbar, new Vector2(0.01f, 0.012f), new Vector2(0.99f, 0.105f), Vector2.zero, Vector2.zero);
            string[] tools = { "Construir", "Vila", "Pesquisa", "Missões", "Guarda", "Tesouro", "Terras", "Mais" };
            PanelKind[] panels =
            {
                PanelKind.Build, PanelKind.Village, PanelKind.Research, PanelKind.Missions,
                PanelKind.Security, PanelKind.Finance, PanelKind.Land, PanelKind.More
            };
            for (int i = 0; i < tools.Length; i++)
            {
                int index = i;
                RectTransform buttonRect = (RectTransform)UiFactory.CreateButton(toolbar, tools[i], () => OpenPanel(panels[index]),
                    i == 0 ? AccentColor : ButtonColor, 18).transform;
                float width = 1f / tools.Length;
                UiFactory.SetRect(buttonRect, new Vector2(i * width, 0.07f), new Vector2((i + 1) * width, 0.93f),
                    new Vector2(4, 0), new Vector2(-4, 0));
            }

            RefreshHud();
        }

        private void BuildMainMenu()
        {
            _menuRoot = UiFactory.CreatePanel(_canvas.transform, "Menu principal", new Color(0.07f, 0.09f, 0.09f, 0.96f));
            UiFactory.SetRect(_menuRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text title = UiFactory.CreateText(_menuRoot, "Título", "MINHA VILA", 48,
                new Color(0.91f, 0.77f, 0.49f), TextAnchor.MiddleCenter);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.15f, 0.82f), new Vector2(0.85f, 0.97f), Vector2.zero, Vector2.zero);
            Text subtitle = UiFactory.CreateText(_menuRoot, "Subtítulo", "Construa, cultive e proteja seu povo",
                22, Ink, TextAnchor.MiddleCenter);
            UiFactory.SetRect(subtitle.rectTransform, new Vector2(0.15f, 0.75f), new Vector2(0.85f, 0.84f), Vector2.zero, Vector2.zero);

            RectTransform menuCard = UiFactory.CreatePanel(_menuRoot, "Opções", PanelColor);
            UiFactory.SetRect(menuCard, new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.73f), Vector2.zero, Vector2.zero);
            RectTransform content;
            RectTransform scroll = UiFactory.CreateScrollView(menuCard, "Opções roláveis", out content);
            UiFactory.SetRect(scroll, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);
            AddSectionLabel(content, "Começar");
            AddMenuButton(content, "Fundar uma nova vila", _game.StartNewGame, GoodColor);
            AddMenuButton(content, "Escolher cenário", () => OpenPanel(PanelKind.Scenarios), ButtonColor);
            AddMenuButton(content, "Carregar vila salva", () => OpenPanel(PanelKind.Saves), ButtonColor);
            AddSectionLabel(content, "Cenários de demonstração");
            foreach (ScenarioDefinition scenario in ScenarioFactory.All)
            {
                ScenarioDefinition captured = scenario;
                AddMenuButton(content, captured.Name, () => _game.StartScenario(captured.Id), ButtonColor);
            }
            Text footer = UiFactory.CreateText(_menuRoot, "Rodapé", "A vila continua somente enquanto o jogo está aberto.", 17,
                new Color(0.73f, 0.72f, 0.64f), TextAnchor.MiddleCenter);
            UiFactory.SetRect(footer.rectTransform, new Vector2(0.1f, 0.01f), new Vector2(0.9f, 0.055f), Vector2.zero, Vector2.zero);
        }

        private void BuildModal()
        {
            _modalRoot = UiFactory.CreatePanel(_canvas.transform, "Janela", new Color(0, 0, 0, 0.48f));
            UiFactory.SetRect(_modalRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform card = UiFactory.CreatePanel(_modalRoot, "Cartão", PanelColor);
            UiFactory.SetRect(card, new Vector2(0.11f, 0.10f), new Vector2(0.89f, 0.90f), Vector2.zero, Vector2.zero);
            _modalTitle = UiFactory.CreateText(card, "Título da janela", string.Empty, 28, Ink);
            UiFactory.SetRect(_modalTitle.rectTransform, new Vector2(0.04f, 0.90f), new Vector2(0.83f, 0.99f), Vector2.zero, Vector2.zero);
            Button close = UiFactory.CreateButton(card, "Fechar", ClosePanel, DangerColor, 20);
            UiFactory.SetRect((RectTransform)close.transform, new Vector2(0.84f, 0.90f),
                new Vector2(0.97f, 0.99f), Vector2.zero, Vector2.zero);
            RectTransform scroll = UiFactory.CreateScrollView(card, "Conteúdo rolável", out _modalContent);
            UiFactory.SetRect(scroll, new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.88f), Vector2.zero, Vector2.zero);
            _modalRoot.gameObject.SetActive(false);
        }

        private void BuildDetailsPanel()
        {
            _detailsRoot = UiFactory.CreatePanel(_hudRoot, "Detalhes da construção", PanelColor);
            UiFactory.SetRect(_detailsRoot, new Vector2(0.68f, 0.69f), new Vector2(0.985f, 0.89f), Vector2.zero, Vector2.zero);
            _detailText = UiFactory.CreateText(_detailsRoot, "Descrição", string.Empty, 18, Ink);
            UiFactory.SetRect(_detailText.rectTransform, new Vector2(0.04f, 0.32f),
                new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);
            RectTransform actions = UiFactory.CreatePanel(_detailsRoot, "Ações", Color.clear);
            UiFactory.SetRect(actions, new Vector2(0.04f, 0.04f), new Vector2(0.97f, 0.30f), Vector2.zero, Vector2.zero);
            Button upgrade = UiFactory.CreateButton(actions, "Melhorar", null, GoodColor, 16);
            upgrade.name = "Melhorar";
            UiFactory.SetRect((RectTransform)upgrade.transform, new Vector2(0, 0), new Vector2(0.49f, 1), Vector2.zero, new Vector2(-3, 0));
            Button demolish = UiFactory.CreateButton(actions, "Demolir", null, DangerColor, 16);
            demolish.name = "Demolir";
            UiFactory.SetRect((RectTransform)demolish.transform, new Vector2(0.51f, 0), Vector2.one, new Vector2(3, 0), Vector2.zero);
            _detailsRoot.gameObject.SetActive(false);
        }

        private void BuildToast()
        {
            RectTransform toast = UiFactory.CreatePanel(_canvas.transform, "Aviso", new Color(0.10f, 0.12f, 0.11f, 0.94f));
            UiFactory.SetRect(toast, new Vector2(0.18f, 0.13f), new Vector2(0.82f, 0.21f), Vector2.zero, Vector2.zero);
            _toastText = UiFactory.CreateText(toast, "Mensagem", string.Empty, 19, Ink, TextAnchor.MiddleCenter);
            UiFactory.SetRect(_toastText.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 5), new Vector2(-14, -5));
            toast.gameObject.SetActive(false);
        }

        private void OpenPanel(PanelKind kind)
        {
            if (kind != PanelKind.Scenarios && kind != PanelKind.Saves && _game.IsInMenu) return;
            _modalRoot.gameObject.SetActive(true);
            _modalTitle.text = PanelTitle(kind);
            ClearModalContent();
            _modalPausesSimulation = !_game.IsInMenu && kind != PanelKind.Build;
            if (_modalPausesSimulation) _game.BeginModal();

            switch (kind)
            {
                case PanelKind.Build: PopulateBuildPanel(); break;
                case PanelKind.Village: PopulateVillagePanel(); break;
                case PanelKind.Research: PopulateResearchPanel(); break;
                case PanelKind.Missions: PopulateMissionPanel(); break;
                case PanelKind.Security: PopulateSecurityPanel(); break;
                case PanelKind.Finance: PopulateFinancePanel(); break;
                case PanelKind.Land: PopulateLandPanel(); break;
                case PanelKind.More: PopulateMorePanel(); break;
                case PanelKind.Notifications: PopulateNotificationsPanel(); break;
                case PanelKind.Saves: PopulateSavesPanel(); break;
                case PanelKind.Scenarios: PopulateScenariosPanel(); break;
            }
        }

        private void ClosePanel()
        {
            if (_modalRoot == null || !_modalRoot.gameObject.activeSelf) return;
            _modalRoot.gameObject.SetActive(false);
            ClearModalContent();
            if (_modalPausesSimulation) _game.EndModal();
            _modalPausesSimulation = false;
        }

        private bool _modalPausesSimulation;

        private void ClearModalContent()
        {
            if (_modalContent == null) return;
            for (int i = _modalContent.childCount - 1; i >= 0; i--)
                Destroy(_modalContent.GetChild(i).gameObject);
        }

        private void PopulateBuildPanel()
        {
            AddLabel(_modalContent,
                "Escolha uma construção e toque em um lote livre. Casas e serviços precisam de ruas conectadas.",
                68, 18);
            foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
            {
                BuildingDefinition definition = BuildingCatalog.Get(type, 1);
                bool locked = definition.RequiredResearch.HasValue
                    && !ResearchCatalog.IsCompleted(_game.State, definition.RequiredResearch.Value);
                string cost = definition.GoldCost.ToString("N0") + " ouro · " + ResourceCost(definition.ResourceCost);
                string label = definition.Name + "  |  " + cost + (locked ? "  ·  Requer pesquisa" : string.Empty);
                Button button = AddButtonRow(_modalContent, label, () =>
                {
                    if (locked)
                    {
                        ShowToast("Essa construção ainda está bloqueada por pesquisa.");
                        return;
                    }
                    _game.SelectBuildType(type);
                    ClosePanel();
                }, locked ? new Color(0.23f, 0.23f, 0.22f) : ButtonColor, 17);
                button.interactable = !locked;
            }
            AddButtonRow(_modalContent, "Cancelar construção", () =>
            {
                _game.CancelBuildMode();
                ClosePanel();
            }, DangerColor, 18);
        }

        private void PopulateVillagePanel()
        {
            VillageState state = _game.State;
            VillageSimulation simulation = _game.Simulation;
            AddSectionLabel(_modalContent, "Resumo da vila");
            AddLabel(_modalContent, "Moradores: " + state.Population + " / " + state.HousingCapacity
                + "  ·  Empregados: " + simulation.EmployedCount + "  ·  Atendidos: " + simulation.ServedCount
                + "\nFelicidade: " + simulation.Happiness + "%  ·  Guardas: " + simulation.GuardCount
                + "  ·  Defesa: " + SecuritySystem.Defense(state).ToString("0.0")
                + "\nEdifícios: " + state.Buildings.Count + "  ·  Território: " + state.TerritorySize + " × " + state.TerritorySize
                + "\nRenda prevista: " + simulation.IncomePerHour.ToString("0.0") + " ouro/h  ·  Dívida: "
                + state.Loan.Debt.ToString("N0"), 120, 18);

            AddSectionLabel(_modalContent, "Edifícios");
            foreach (IGrouping<BuildingType, BuildingRecord> group in state.Buildings
                .Where(item => item.Type != BuildingType.Road)
                .GroupBy(item => item.Type).OrderBy(item => item.Key))
            {
                BuildingDefinition definition = BuildingCatalog.Get(group.Key, 1);
                AddLabel(_modalContent, definition.Name + " — " + group.Count(), 34, 17);
            }
            AddButtonRow(_modalContent, "Desfazer última ação", () =>
            {
                bool success = _game.Undo(out string message);
                ShowToast(message);
                if (success) PopulateVillagePanelRefresh();
            }, ButtonColor);
        }

        private void PopulateVillagePanelRefresh()
        {
            OpenPanel(PanelKind.Village);
        }

        private void PopulateResearchPanel()
        {
            VillageState state = _game.State;
            if (state.HasActiveResearch)
            {
                ResearchDefinition active = ResearchCatalog.Get(state.ActiveResearch);
                AddLabel(_modalContent, "Em andamento: " + active.Name + " — "
                    + (_game.Simulation.ResearchProgressPercent * 100).ToString("0")
                    + "% concluído.\nA pesquisa avança durante o horário de trabalho com pesquisadores conectados por ruas.",
                    86, 18);
            }
            else
            {
                AddLabel(_modalContent,
                    "A pesquisa exige um Centro de Pesquisa ou uma Academia conectada por ruas e moradores trabalhando.",
                    62, 18);
            }

            foreach (ResearchDefinition definition in ResearchCatalog.All.OrderBy(item => item.Id))
            {
                bool available = ResearchCatalog.IsAvailable(state, definition);
                bool active = state.HasActiveResearch && state.ActiveResearch == definition.Id;
                string lockText = active ? "  ·  Em andamento" : available ? string.Empty : "  ·  Bloqueada ou concluída";
                string label = definition.Name + " — " + definition.RequiredHours.ToString("0") + " h · "
                    + definition.GoldCost.ToString("N0") + " ouro · " + ResourceCost(definition.ResourceCost)
                    + "\n" + definition.Result + lockText;
                Button button = AddButtonRow(_modalContent, label, () =>
                {
                    bool success = _game.StartResearch(definition.Id, out string message);
                    ShowToast(message);
                    if (success) OpenPanel(PanelKind.Research);
                }, available ? ButtonColor : new Color(0.22f, 0.23f, 0.21f), 17, 74);
                button.interactable = available && !state.HasActiveResearch;
            }
        }

        private void PopulateMissionPanel()
        {
            VillageState state = _game.State;
            List<BuildingRecord> royalTaverns = state.Buildings.Where(item => item.Type == BuildingType.RoyalTavern).ToList();
            AddLabel(_modalContent, "Expedições partem da Taverna Real. Os mercenários ficam fora da vila durante a missão.", 60, 18);
            if (royalTaverns.Count == 0)
                AddLabel(_modalContent, "Nenhuma Taverna Real construída.", 42, 18);
            else
            {
                foreach (BuildingRecord tavern in royalTaverns)
                    AddLabel(_modalContent, "Taverna Real #" + tavern.Id + " · mercenários disponíveis: "
                        + MissionSystem.AvailableCrew(state, tavern.Id), 38, 17);
            }

            foreach (ActiveMissionRecord mission in state.ActiveMissions)
            {
                MissionDefinition definition = MissionSystem.Get(mission.Type);
                AddLabel(_modalContent, "Em andamento: " + definition.Name + " · "
                    + mission.RemainingHours.ToString("0.0") + " horas restantes", 42, 17);
            }

            AddSectionLabel(_modalContent, "Enviar expedição");
            foreach (MissionDefinition definition in MissionSystem.All.OrderBy(item => item.Type))
            {
                string label = definition.Name + " · " + definition.Crew + " mercenários · "
                    + definition.Hours.ToString("0") + " h · recompensa " + definition.GoldReward + " ouro e "
                    + ResourceCost(definition.Reward);
                AddButtonRow(_modalContent, label, () =>
                {
                    if (royalTaverns.Count == 0)
                    {
                        ShowToast("Construa uma Taverna Real primeiro.");
                        return;
                    }
                    bool success = _game.StartMission(royalTaverns[0].Id, definition.Type, out string message);
                    ShowToast(message);
                    if (success) OpenPanel(PanelKind.Missions);
                }, royalTaverns.Count > 0 ? ButtonColor : new Color(0.22f, 0.23f, 0.21f), 16, 64);
                AddButtonInteractivityFromTavern(royalTaverns.Count > 0, definition.Crew);
            }

            AddSectionLabel(_modalContent, "Melhorias reais");
            AddButtonRow(_modalContent, "Ferramentas da cidade — nível " + state.CityToolsLevel + "/3", () =>
            {
                bool success = _game.UpgradeRoyalItem(true, out string message);
                ShowToast(message);
                if (success) OpenPanel(PanelKind.Missions);
            }, ButtonColor);
            AddButtonRow(_modalContent, "Defesas reforçadas — nível " + state.ReinforcedWallsLevel + "/3", () =>
            {
                bool success = _game.UpgradeRoyalItem(false, out string message);
                ShowToast(message);
                if (success) OpenPanel(PanelKind.Missions);
            }, ButtonColor);
        }

        private void AddButtonInteractivityFromTavern(bool hasTavern, int crew)
        {
            Button button = _modalContent.GetChild(_modalContent.childCount - 1).GetComponent<Button>();
            if (button == null) return;
            bool enoughCrew = hasTavern && _game.State.Buildings
                .Where(item => item.Type == BuildingType.RoyalTavern)
                .Any(item => MissionSystem.AvailableCrew(_game.State, item.Id) >= crew);
            button.interactable = enoughCrew;
        }

        private void PopulateSecurityPanel()
        {
            VillageState state = _game.State;
            SecurityRecord security = state.Security;
            AddSectionLabel(_modalContent, "Segurança da vila");
            string threat = security.HasActiveThreat
                ? SecuritySystem.ThreatName(security.ActiveThreat) + (security.IsBattle ? " em batalha" : " se aproximando")
                    + " · força " + security.ThreatStrength + " · " + security.ThreatPhaseRemainingHours.ToString("0.0") + " h"
                : "Nenhuma ameaça ativa · próxima aproximação em " + security.PeaceRemainingHours.ToString("0.0") + " h";
            AddLabel(_modalContent, threat + "\nGuardas: " + _game.Simulation.GuardCount + " recomendados: "
                + SecuritySystem.RecommendedGuards(state) + "\nDefesa: " + SecuritySystem.Defense(state).ToString("0.0")
                + " · Desordem: " + security.Disorder.ToString("0") + "%\nAtaques repelidos: " + security.Repelled
                + " · Invasões: " + security.Breached + " · Saques: " + security.LastLoot + " ouro", 150, 18);
            AddLabel(_modalContent,
                "Construa Postos da Guarda, conecte-os por ruas e cuide dos serviços da população. "
                + "A primeira ameaça surge após 72 horas habitadas.", 86, 18);
        }

        private void PopulateFinancePanel()
        {
            VillageState state = _game.State;
            AddSectionLabel(_modalContent, "Depósito e compras");
            AddLabel(_modalContent, "Estoque: madeira " + state.Stock.Timber + " · pedra " + state.Stock.Stone
                + " · ferro " + state.Stock.Iron + " · comida " + state.Stock.Food + " · relíquias " + state.Stock.Relics, 46, 17);
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int price = FinanceSystem.GetUnitPrice(type);
                AddButtonRow(_modalContent, "Comprar 10 " + ResourceName(type) + " — " + (price * 10).ToString("N0") + " ouro", () =>
                {
                    bool success = _game.BuyResource(type, 10, out string message);
                    ShowToast(message);
                    if (success) OpenPanel(PanelKind.Finance);
                }, ButtonColor, 17);
            }

            AddSectionLabel(_modalContent, "Empréstimo");
            AddLabel(_modalContent, "Saldo da dívida: " + state.Loan.Debt.ToString("N0")
                + " ouro · prazo inicial de 72 horas; juros diários após o vencimento.", 54, 17);
            if (state.Loan.Debt > 0)
            {
                int repayment = Math.Min(500, state.Loan.Debt);
                AddButtonRow(_modalContent, "Pagar " + repayment.ToString("N0") + " ouro", () =>
                {
                    bool success = _game.Repay(repayment, out string message);
                    ShowToast(message);
                    if (success) OpenPanel(PanelKind.Finance);
                }, ButtonColor);
            }
            else
            {
                AddButtonRow(_modalContent, "Contratar empréstimo de 1.000 ouro (taxa de 10%)", () =>
                {
                    bool success = _game.Borrow(1000, out string message);
                    ShowToast(message);
                    if (success) OpenPanel(PanelKind.Finance);
                }, ButtonColor);
            }
        }

        private void PopulateLandPanel()
        {
            VillageState state = _game.State;
            if (!LandSystem.HasAnotherExpansion(state))
            {
                AddLabel(_modalContent, "A vila já possui todo o território disponível: 48 × 48 lotes.", 64, 19);
                return;
            }

            int target = LandSystem.NextSize(state);
            int population = LandSystem.NextPopulationMilestone(state);
            int price = LandSystem.NextPrice(state);
            AddLabel(_modalContent, "Território atual: " + state.TerritorySize + " × " + state.TerritorySize
                + "\nPróxima expansão: " + target + " × " + target
                + "\nRequisito: " + population + " moradores · Tesouro: " + price.ToString("N0") + " ouro", 100, 19);
            Button expand = AddButtonRow(_modalContent, "Ampliar território", () =>
            {
                bool success = _game.ExpandLand(out string message);
                ShowToast(message);
                if (success) OpenPanel(PanelKind.Land);
            }, GoodColor, 19);
            expand.interactable = state.PopulationRecord >= population && state.Gold >= price;
        }

        private void PopulateMorePanel()
        {
            AddButtonRow(_modalContent, "Salvar vila", () => _game.SaveCurrent(), ButtonColor);
            AddButtonRow(_modalContent, "Notificações", () => OpenPanel(PanelKind.Notifications), ButtonColor);
            AddButtonRow(_modalContent, "Desfazer última ação", () =>
            {
                bool success = _game.Undo(out string message);
                ShowToast(message);
                if (success) RefreshHud();
            }, ButtonColor);
            string musicLabel = !_game.Music.HasTracks
                ? "Adicionar faixas de música ao projeto"
                : _game.Music.IsEnabled ? "Desativar música" : "Ativar música";
            AddButtonRow(_modalContent, musicLabel, () =>
            {
                if (!_game.Music.HasTracks)
                {
                    ShowToast("Adicione music-menu e music-village em Assets/Resources/Audio.");
                    return;
                }
                bool enabled = _game.Music.Toggle();
                OpenPanel(PanelKind.More);
                ShowToast(enabled ? "Música ativada." : "Música desativada.");
            }, _game.Music.HasTracks ? ButtonColor : new Color(0.22f, 0.23f, 0.21f));
            AddButtonRow(_modalContent, "Voltar ao menu principal", () =>
            {
                ClosePanel();
                _game.ReturnToMainMenu();
            }, DangerColor);
        }

        private void PopulateNotificationsPanel()
        {
            List<VillageNotification> notifications = _game.State.Notifications
                .OrderByDescending(item => item.Id).ToList();
            if (notifications.Count == 0) AddLabel(_modalContent, "Ainda não há notificações.", 48, 18);
            foreach (VillageNotification notification in notifications)
            {
                string label = "Dia " + notification.Day + " · " + notification.Hour.ToString("00") + ":"
                    + notification.Minute.ToString("00") + "\n" + notification.Message;
                AddLabel(_modalContent, label, 60, 17);
            }
            AddButtonRow(_modalContent, "Marcar todas como lidas", () =>
            {
                _game.MarkNotificationsRead();
                ShowToast("Notificações marcadas como lidas.");
            }, ButtonColor);
        }

        private void PopulateSavesPanel()
        {
            List<VillageSummary> saves = _game.ListSaves();
            if (saves.Count == 0)
            {
                AddLabel(_modalContent, "Nenhuma vila salva neste dispositivo. Use Salvar no menu da vila.", 70, 18);
                return;
            }

            foreach (VillageSummary save in saves)
            {
                VillageSummary captured = save;
                AddButtonRow(_modalContent, captured.Name + " · dia " + captured.Day + " · "
                    + captured.Population + " moradores · " + Mathf.FloorToInt((float)captured.Gold).ToString("N0") + " ouro",
                    () => _game.LoadSave(captured.Id), ButtonColor, 17);
            }
        }

        private void PopulateScenariosPanel()
        {
            AddLabel(_modalContent, "Escolha uma partida preparada para conhecer uma situação específica.", 56, 18);
            foreach (ScenarioDefinition scenario in ScenarioFactory.All)
            {
                ScenarioDefinition captured = scenario;
                AddButtonRow(_modalContent, captured.Name + "\n" + captured.Description,
                    () => _game.StartScenario(captured.Id), ButtonColor, 17, 66);
            }
        }

        private void AddMenuButton(Transform parent, string label, Action action, Color color)
        {
            AddButtonRow(parent, label, action, color, 20, 54);
        }

        private Button AddButtonRow(Transform parent, string label, Action action, Color color,
            int fontSize = 19, int height = 52)
        {
            Button button = UiFactory.CreateButton(parent, label,
                action == null ? null : new UnityEngine.Events.UnityAction(() => action()), color, fontSize);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            return button;
        }

        private void AddLabel(Transform parent, string label, int height, int fontSize)
        {
            Text text = UiFactory.CreateText(parent, "Texto", label, fontSize, Ink);
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private void AddSectionLabel(Transform parent, string label)
        {
            Text text = UiFactory.CreateText(parent, "Seção", label, 21, new Color(0.91f, 0.77f, 0.49f));
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 42;
            layout.preferredHeight = 42;
        }

        private static string PanelTitle(PanelKind kind)
        {
            switch (kind)
            {
                case PanelKind.Build: return "Construir";
                case PanelKind.Village: return "Visão da vila";
                case PanelKind.Research: return "Pesquisa";
                case PanelKind.Missions: return "Missões e expedições";
                case PanelKind.Security: return "Segurança";
                case PanelKind.Finance: return "Tesouro e recursos";
                case PanelKind.Land: return "Território";
                case PanelKind.More: return "Mais opções";
                case PanelKind.Notifications: return "Notificações";
                case PanelKind.Saves: return "Carregar vila";
                case PanelKind.Scenarios: return "Cenários";
                default: return "Minha Vila";
            }
        }

        private static string ResourceCost(ResourceBundle cost)
        {
            if (cost == null) return "sem materiais";
            List<string> parts = new List<string>();
            if (cost.Timber > 0) parts.Add(cost.Timber + " madeira");
            if (cost.Stone > 0) parts.Add(cost.Stone + " pedra");
            if (cost.Iron > 0) parts.Add(cost.Iron + " ferro");
            if (cost.Food > 0) parts.Add(cost.Food + " comida");
            if (cost.Relics > 0) parts.Add(cost.Relics + " relíquias");
            return parts.Count == 0 ? "sem materiais" : string.Join(", ", parts);
        }

        private static string ResourceName(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Timber: return "madeira";
                case ResourceType.Stone: return "pedra";
                case ResourceType.Iron: return "ferro";
                case ResourceType.Food: return "comida";
                default: return "recurso";
            }
        }

        private static string DescribeProduction(BuildingDefinition definition)
        {
            List<string> effects = new List<string>();
            if (definition.GoldPerWorkerHour > 0) effects.Add(definition.GoldPerWorkerHour.ToString("0.##") + " ouro por trabalhador/h");
            if (definition.TimberPerWorkerHour > 0)
                effects.Add(definition.TimberPerWorkerHour.ToString("0.##") + " madeira por trabalhador/h");
            if (definition.StonePerWorkerHour > 0) effects.Add(definition.StonePerWorkerHour.ToString("0.##") + " pedra por trabalhador/h");
            if (definition.IronPerWorkerHour > 0) effects.Add(definition.IronPerWorkerHour.ToString("0.##") + " ferro por trabalhador/h");
            if (definition.FoodPerWorkerHour > 0) effects.Add(definition.FoodPerWorkerHour.ToString("0.##") + " comida por trabalhador/h");
            if (definition.LeisureCapacity > 0) effects.Add("lazer para " + definition.LeisureCapacity + " moradores");
            if (definition.GuardPower > 0) effects.Add("defesa " + definition.GuardPower + " por guarda");
            return effects.Count == 0 ? "Edifício residencial ou de circulação." : string.Join("\n", effects);
        }
    }
}
