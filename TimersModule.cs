using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Charr.Timers_BlishHUD.Controls;
using Charr.Timers_BlishHUD.Controls.BigWigs;
using Charr.Timers_BlishHUD.IO;
using Charr.Timers_BlishHUD.Models;
using Charr.Timers_BlishHUD.Pathing.Content;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;
using Blish_HUD.Content;
using Charr.Timers_BlishHUD.State;
using Microsoft.IdentityModel.Tokens;
using Octokit;
using SharpDX.Direct3D11;
using Label = Blish_HUD.Controls.Label;
using Charr.Timers_BlishHUD.Controls.ResetButton;

namespace Charr.Timers_BlishHUD
{
    public enum AlertType
    {
        Small,
        Medium,
        Large,
        BigWigStyle
    }

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class TimersModule : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger<Blish_HUD.Modules.Module>();

        internal static TimersModule ModuleInstance;

        #region Service Managers

        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

        #endregion

        // Resources
        public Resources Resources;

        // Controls - UI
        public AlertContainer _alertContainer;
        private StandardWindow _alertSettingsWindow;
        private List<IAlertPanel> _testAlertPanels;
        private ResetButton _resetButton;

        // Controls - Tab
        private WindowTab _timersTab;
        public Menu timerCategories;
        public FlowPanel timerPanel;
        private Panel _tabPanel;
        private List<TimerDetails> _allTimerDetails;
        private List<TimerDetails> _displayedTimerDetails;

        private Label _debugText;

        // File reading
        private List<PathableResourceManager> _pathableResourceManagers;
        private JsonSerializerSettings _jsonSettings;
        private bool _encountersLoaded;
        private bool _errorCaught;

        // Model
        private HashSet<String> _encounterIds;
        public Dictionary<String, Alert> _activeAlertIds;
        public Dictionary<String, Direction> _activeDirectionIds;
        public Dictionary<String, Marker> _activeMarkerIds;
        private HashSet<Encounter> _encounters;
        private HashSet<Encounter> _activeEncounters;
        private HashSet<Encounter> _invalidEncounters;

        // New Map Loaded Event Listener
        private EventHandler<ValueEventArgs<int>> _onNewMapLoaded;

        // Settings
        private SettingEntry<Update> _lastTimersUpdate;
        private SettingEntry<bool> _showDebugSetting;
        public SettingEntry<bool> _debugModeSetting;
        private Dictionary<String, SettingEntry<bool>> _encounterEnableSettings;
        private SettingEntry<bool> _sortCategorySetting;
        public SettingCollection _timerSettingCollection;
        public SettingEntry<KeyBinding>[] _keyBindSettings;

        public SettingEntry<bool> _showResetTimerButton;
        public SettingEntry<Point> _resetTimerButtonLocationSetting;
        public SettingEntry<Point> _resetTimerButtonSizeSetting;
        public SettingEntry<KeyBinding> _resetTimerHotKeySetting;

        private SettingCollection _alertSettingCollection;
        public SettingEntry<bool> _lockAlertContainerSetting;
        private SettingEntry<bool> _centerAlertContainerSetting;
        public SettingEntry<bool> _hideAlertsSetting;
        public SettingEntry<bool> _hideDirectionsSetting;
        public SettingEntry<bool> _hideMarkersSetting;
        public SettingEntry<bool> _hideSoundsSetting;
        public SettingEntry<AlertType> _alertSizeSetting;
        public SettingEntry<ControlFlowDirection> _alertDisplayOrientationSetting;
        private SettingEntry<Point> _alertContainerLocationSetting;
        public SettingEntry<float> _alertMoveDelaySetting;
        public SettingEntry<float> _alertFadeDelaySetting;
        public SettingEntry<bool> _alertFillDirection;
        public Update update = new Update();

        public TimerLoader timerLoader;

        private bool _timersNeedUpdate = false;

        [ImportingConstructor]
        public TimersModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        #region Settings

        protected override void DefineSettings(SettingCollection settings) {
            if (!settings.TryGetSetting("LastTimersUpdate", out _lastTimersUpdate)) {
                _lastTimersUpdate = settings.DefineSetting("LastTimersUpdate", new Update(),
                    "Last Timers Update", "Date of last timers update");
            }

            _showDebugSetting = settings.DefineSetting("ShowDebugText", false, "Show Debug Text",
                "For creating timers. Placed in top-left corner. Displays location status.");
            _debugModeSetting = settings.DefineSetting("DebugMode", false, "Enable Debug Mode",
                "All Timer triggers will ignore requireCombat setting, allowing you to test them freely.");
            _sortCategorySetting = settings.DefineSetting("SortByCategory", false, "Sort Categories",
                "When enabled, categories from loaded timer files are sorted in alphanumerical order.\nOtherwise, categories are in the order they are loaded.\nThe module needs to be restarted to take effect."); ;
            _timerSettingCollection = settings.AddSubCollection("EnabledTimers", false);

            //Reset Timer Settings
            _showResetTimerButton = settings.DefineSetting("Show Reset Button", true);
            _showResetTimerButton.SettingChanged += _showResetTimerButton_SettingChanged;
            _resetTimerHotKeySetting = settings.DefineSetting("Reset Active Timer", new KeyBinding());
            _resetTimerHotKeySetting.Value.Enabled = true;
            _resetTimerHotKeySetting.Value.Activated += ResetHotkey_Activated;

            var _internalSettings = settings.AddSubCollection("Internal Settings", false);
            _resetTimerButtonLocationSetting = _internalSettings.DefineSetting("Reset Timer Button Location", new Point(150, 150));
            _resetTimerButtonSizeSetting = _internalSettings.DefineSetting("Reset Timer Button Size", new Point(64, 64));

            _keyBindSettings = new SettingEntry<KeyBinding>[5];
            for (int i = 0; i < 5; i++) {
                _keyBindSettings[i] = settings.DefineSetting("Trigger Key " + i, new KeyBinding(), "Trigger Key " + i,
                    "For timers that require keys to trigger.");
            }

            _alertSettingCollection = settings.AddSubCollection("AlertSetting", false);
            _lockAlertContainerSetting = _alertSettingCollection.DefineSetting("LockAlertContainer", false);
            _hideAlertsSetting = _alertSettingCollection.DefineSetting("HideAlerts", false);
            _hideDirectionsSetting = _alertSettingCollection.DefineSetting("HideDirections", false);
            _hideMarkersSetting = _alertSettingCollection.DefineSetting("HideMarkers", false);
            _hideSoundsSetting = _alertSettingCollection.DefineSetting("HideSounds", false);
            _centerAlertContainerSetting = _alertSettingCollection.DefineSetting("CenterAlertContainer", true);
            _alertSizeSetting = _alertSettingCollection.DefineSetting("AlertSize", AlertType.BigWigStyle);
            _alertDisplayOrientationSetting =
                _alertSettingCollection.DefineSetting("AlertDisplayOrientation", ControlFlowDirection.SingleTopToBottom);
            _alertContainerLocationSetting = _alertSettingCollection.DefineSetting("AlertContainerLocation", new Point(GameService.Graphics.WindowWidth - GameService.Graphics.WindowWidth / 4, GameService.Graphics.WindowHeight / 2));
            _alertMoveDelaySetting = _alertSettingCollection.DefineSetting("AlertMoveSpeed", 1.0f);
            _alertFadeDelaySetting = _alertSettingCollection.DefineSetting("AlertFadeSpeed", 1.0f);
            _alertFillDirection = _alertSettingCollection.DefineSetting("FillDirection", true);
        }

        private void _showResetTimerButton_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            _resetButton?.ToggleVisibility();
        }

        private void ResetHotkey_Activated(object sender, EventArgs e)
        {
            ResetActiveEncounters();
        }

        private void ResetActiveEncounters()
        {
            foreach (Encounter enc in _activeEncounters)
            {
                enc.Reset();
            }
        }

        private void SettingsUpdateShowDebug(object sender = null, EventArgs e = null) {
            if (_showDebugSetting.Value)
                _debugText?.Show();
            else
                _debugText?.Hide();
        }

        private void SettingsUpdateLockAlertContainer(object sender = null, EventArgs e = null) {
            if (_alertContainer != null) {
                _alertContainer.Lock = _lockAlertContainerSetting.Value;
            }
        }

        private void SettingsUpdateCenterAlertContainer(object sender = null, EventArgs e = null) {
        }

        private void SettingsUpdateHideAlerts(object sender = null, EventArgs e = null) {
            _testAlertPanels?.ForEach(panel => panel.ShouldShow = !_hideAlertsSetting.Value);
            foreach (var enc in _encounters) {
                enc.ShowAlerts = !_hideAlertsSetting.Value;
            }
            if (_alertContainer != null) {
                if (_hideAlertsSetting.Value) {
                    _alertContainer?.Hide();
                }
                else {
                    _alertContainer.UpdateDisplay();
                    _alertContainer?.Show();
                }

                _alertContainer.AutoShow = !_hideAlertsSetting.Value;
            }
        }

        private void SettingsUpdateHideDirections(object sender = null, EventArgs e = null) {
            foreach (var enc in _encounters) {
                enc.ShowDirections = !_hideDirectionsSetting.Value;
            }
        }

        private void SettingsUpdateHideMarkers(object sender = null, EventArgs e = null) {
            foreach (var enc in _encounters) {
                enc.ShowMarkers = !_hideMarkersSetting.Value;
            }
        }

        private void SettingsUpdateAlertSize(object sender = null, EventArgs e = null) {
            switch (_alertSizeSetting.Value) {
                case AlertType.Small:
                    AlertPanel.DEFAULT_ALERTPANEL_WIDTH = 320;
                    AlertPanel.DEFAULT_ALERTPANEL_HEIGHT = 64;
                    break;
                case AlertType.Medium:
                    AlertPanel.DEFAULT_ALERTPANEL_WIDTH = 320;
                    AlertPanel.DEFAULT_ALERTPANEL_HEIGHT = 96;
                    break;
                case AlertType.Large:
                    AlertPanel.DEFAULT_ALERTPANEL_WIDTH = 320;
                    AlertPanel.DEFAULT_ALERTPANEL_HEIGHT = 128;
                    break;
                case AlertType.BigWigStyle:
                    AlertPanel.DEFAULT_ALERTPANEL_WIDTH = 336;
                    AlertPanel.DEFAULT_ALERTPANEL_HEIGHT = 35;
                    break;
            }
        }

        private void SettingsUpdateAlertDisplayOrientation(object sender = null, EventArgs e = null) {
            _alertContainer.FlowDirection = _alertDisplayOrientationSetting.Value;
            _alertContainer.UpdateDisplay();
        }

        private void SettingsUpdateAlertContainerLocation(object sender = null, EventArgs e = null) {
            /*switch (_alertDisplayOrientationSetting.Value) {
                case ControlFlowDirection.SingleLeftToRight:
                case ControlFlowDirection.SingleTopToBottom:
                    _alertContainer.Location = _alertContainerLocationSetting.Value;
                    break;
                case ControlFlowDirection.SingleRightToLeft:
                    _alertContainer.Location =
                        new Point(_alertContainerLocationSetting.Value.X - _alertContainer.Width, _alertContainerLocationSetting.Value.Y);
                    break;
                case ControlFlowDirection.SingleBottomToTop:
                    _alertContainer.Location =
                        new Point(_alertContainerLocationSetting.Value.X, _alertContainerLocationSetting.Value.Y - _alertContainer.Height);
                    break;
            }*/
        }

        private void SettingsUpdateAlertMoveDelay(object sender = null, EventArgs e = null) {

        }

        private void SettingsUpdateAlertFadeDelay(object sender = null, EventArgs e = null) {

        }

        #endregion

        protected override void Initialize() {
            // Instantiations
            Resources = new Resources();
            _pathableResourceManagers = new List<PathableResourceManager>();
            _encounterEnableSettings = new Dictionary<string, SettingEntry<bool>>();
            _encounterIds = new HashSet<string>();
            _activeAlertIds = new Dictionary<String, Alert>();
            _activeDirectionIds = new Dictionary<String, Direction>();
            _activeMarkerIds = new Dictionary<String, Marker>();
            _encounters = new HashSet<Encounter>();
            _activeEncounters = new HashSet<Encounter>();
            _invalidEncounters = new HashSet<Encounter>();
            _allTimerDetails = new List<TimerDetails>();
            _testAlertPanels = new List<IAlertPanel>();
            _debugText = new Label {
                Parent = GameService.Graphics.SpriteScreen,
                Location = new Point(10, 38),
                TextColor = Color.White,
                Font = Resources.Font,
                Text = "DEBUG",
                HorizontalAlignment = HorizontalAlignment.Left,
                StrokeText = true,
                ShowShadow = true,
                AutoSizeWidth = true
            };
            SettingsUpdateShowDebug();

            // Bind setting listeners
            _showDebugSetting.SettingChanged += SettingsUpdateShowDebug;
            _lockAlertContainerSetting.SettingChanged += SettingsUpdateLockAlertContainer;
            _centerAlertContainerSetting.SettingChanged += SettingsUpdateCenterAlertContainer;
            _hideAlertsSetting.SettingChanged += SettingsUpdateHideAlerts;
            _hideDirectionsSetting.SettingChanged += SettingsUpdateHideDirections;
            _hideMarkersSetting.SettingChanged += SettingsUpdateHideMarkers;
            _alertSizeSetting.SettingChanged += SettingsUpdateAlertSize;
            _alertDisplayOrientationSetting.SettingChanged += SettingsUpdateAlertDisplayOrientation;
            _alertContainerLocationSetting.SettingChanged += SettingsUpdateAlertContainerLocation;
            _alertMoveDelaySetting.SettingChanged += SettingsUpdateAlertMoveDelay;
            _alertFadeDelaySetting.SettingChanged += SettingsUpdateAlertFadeDelay;
        }

        private void ResetActivatedEncounters() {
            _activeEncounters.Clear();
            foreach (Encounter enc in _encounters) {
                if (enc.Map == GameService.Gw2Mumble.CurrentMap.Id &&
                    enc.Enabled) {
                    enc.Activate();
                    _activeEncounters.Add(enc);
                }
                else {
                    enc.Deactivate();
                }
            }
            /*
            if (_activeEncounters.Count > 0)
                _alertWindow.Show();
            else
                _alertWindow.Hide();*/
        }

        private Encounter ParseEncounter(TimerStream timerStream) {
            string jsonContent;
            using (var jsonReader = new StreamReader(timerStream.Stream)) {
                jsonContent = jsonReader.ReadToEnd();
            }

            Encounter enc = null;
            try {
                enc = JsonConvert.DeserializeObject<Encounter>(jsonContent, _jsonSettings);
                enc.Initialize(timerStream.ResourceManager);
            }
            catch (TimerReadException ex) {
                enc.Description = ex.Message;
                //_debug.Text = ex.Message;
                _errorCaught = true;
                //Logger.Error(enc.Name + " Timer parsing failure: " + ex.Message);
            }
            catch (Exception ex) {
                enc?.Dispose();
                enc = new Encounter {
                    Name = timerStream.FileName.Split('\\').Last(),
                    Description = "File Path: " + (timerStream.FileName + "\n\nInvalid JSON format: " + ex.Message)
                };
                //_debug.Text = ex.Message;
                _errorCaught = true;
                //Logger.Error("File Path: " + (timerStream.FileName + "\n\nInvalid JSON format: " + ex.Message));
            }
            finally {
                enc.IsFromZip = timerStream.IsFromZip;
                enc.ZipFile = timerStream.ZipFile;
                enc.TimerFile = timerStream.FileName;
            }

            return enc;
        }

        private void AddEncounter(TimerStream timerStream) {
            Encounter enc = ParseEncounter(timerStream);
            AddEncounter(enc);
        }

        private void AddEncounter(Encounter enc) {
            if (enc.State != Encounter.EncounterStates.Error) {
                _encounters.Add(enc);
                _encounterIds.Add(enc.Id);
            }
            else {
                _invalidEncounters.Add(enc);
            }
        }

        private void UpdateEncounter(Encounter enc) {
            if (enc.State != Encounter.EncounterStates.Error) {
                _encounters.RemoveWhere(e => e.Equals(enc));
            }
            else {
                _invalidEncounters.RemoveWhere(e => e.Equals(enc));
            }
            AddEncounter(enc);
        }

        [Conditional("DEBUG")]
        private async void ShowLatestRelease() { 
            var github = new GitHubClient(new ProductHeaderValue("BlishHUD_Timers"));
            var latestRelease = await github.Repository.Release.GetLatest("QuitarHero", "Hero-Timers");
            var allReleases = await github.Repository.Release.GetAll("QuitarHero", "Hero-Timers");
            Debug.WriteLine(latestRelease.CreatedAt);
            Debug.WriteLine(allReleases[1].CreatedAt);
        }

        protected override async Task LoadAsync() {
            string timerDirectory = DirectoriesManager.GetFullDirectoryPath("timers");

            try {
                ShowLatestRelease();
                using (WebClient wc = new WebClient()) {
                    var json = new WebClient().DownloadString("https://bhm.blishhud.com/Charr.Timers_BlishHUD/timer_update.json");
                    var updates = JsonConvert.DeserializeObject<List<Update>>(json, _jsonSettings);
                    if (updates == null || updates.Count == 0) {
                        throw new ArgumentNullException();
                    }

                    update = updates[0];
                    if (update.CreatedAt > _lastTimersUpdate.Value.CreatedAt) {
                        _timersNeedUpdate = true;
                        ScreenNotification.ShowNotification($"New timers available. Go to settings to update!", ScreenNotification.NotificationType.Warning, null, 3);
                    }
                }
            }
            catch (Exception ex) {
                _timersNeedUpdate = false;
            }

            if (!_timersNeedUpdate) {
                timerLoader = new TimerLoader(timerDirectory);
                timerLoader.LoadFiles(AddEncounter);
                _encountersLoaded = true;
            }

            _tabPanel = BuildSettingsPanel(GameService.Overlay.BlishHudWindow.ContentRegion);
            _onNewMapLoaded = delegate { ResetActivatedEncounters(); };
            _resetButton = new ResetButton()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = _resetTimerButtonSizeSetting.Value,
                Location = _resetTimerButtonLocationSetting.Value,
                Visible = _showResetTimerButton.Value,
            };
            _resetButton.ButtonClicked += _resetButton_ButtonClicked;
            _resetButton.BoundsChanged += _resetButton_BoundsChanged;

            ResetActivatedEncounters();
            SettingsUpdateHideAlerts();
            SettingsUpdateHideDirections();
            SettingsUpdateHideMarkers();
        }

        private void _resetButton_BoundsChanged(object sender, EventArgs e)
        {
            _resetTimerButtonLocationSetting.Value = _resetButton.Location;
            _resetTimerButtonSizeSetting.Value = _resetButton.Size;
        }

        private void _resetButton_ButtonClicked(object sender, EventArgs e)
        {
            ResetActiveEncounters();
        }

        private void ShowTimerEntries(Panel timerPanel) {
            foreach (Encounter enc in _encounters) {
                TimerDetails entry = new TimerDetails {
                    Parent = timerPanel,
                    Encounter = enc,
                };

                entry.Initialize();

                entry.PropertyChanged += delegate { ResetActivatedEncounters(); };

                _allTimerDetails.Add(entry);


                entry.ReloadClicked += delegate (Object sender, Encounter enc) {
                    if (enc.IsFromZip) {
                        timerLoader.ReloadFile(delegate (TimerStream timerStream) {
                            Encounter enc = ParseEncounter(timerStream);
                            UpdateEncounter(enc);
                            entry.Encounter?.Dispose();
                            entry.Encounter = enc;
                            ScreenNotification.ShowNotification($"Encounter <{enc.Name}> reloaded!", ScreenNotification.NotificationType.Info, enc.Icon, 3);
                        }, enc.ZipFile, enc.TimerFile);
                    }
                    else {
                        timerLoader.ReloadFile(delegate (TimerStream timerStream) {
                            Encounter enc = ParseEncounter(timerStream);
                            UpdateEncounter(enc);
                            entry.Encounter?.Dispose();
                            entry.Encounter = enc;
                            ScreenNotification.ShowNotification($"Encounter <{enc.Name}> reloaded!", ScreenNotification.NotificationType.Info, enc.Icon, 3);
                        }, enc.TimerFile);
                    }
                };
            }

            foreach (Encounter enc in _invalidEncounters) {
                TimerDetails entry = new TimerDetails {
                    Parent = timerPanel,
                    Encounter = enc,
                };
                entry.Initialize();

                entry.PropertyChanged += delegate { ResetActivatedEncounters(); };

                _allTimerDetails.Add(entry);


                entry.ReloadClicked += delegate (Object sender, Encounter enc) {
                    if (enc.IsFromZip) {
                        timerLoader.ReloadFile(delegate (TimerStream timerStream) {
                            Encounter enc = ParseEncounter(timerStream);
                            UpdateEncounter(enc);
                            entry.Encounter?.Dispose();
                            entry.Encounter = enc;
                            ScreenNotification.ShowNotification($"Encounter <{enc.Name}> reloaded!", ScreenNotification.NotificationType.Info, enc.Icon, 3);
                        }, enc.ZipFile, enc.TimerFile);
                    }
                    else {
                        timerLoader.ReloadFile(delegate (TimerStream timerStream) {
                            Encounter enc = ParseEncounter(timerStream);
                            UpdateEncounter(enc);
                            entry.Encounter?.Dispose();
                            entry.Encounter = enc;
                            ScreenNotification.ShowNotification($"Encounter <{enc.Name}> reloaded!", ScreenNotification.NotificationType.Info, enc.Icon, 3);
                        }, enc.TimerFile);
                    }
                };

            }
        }

        public void ShowCustomTimerCategories() {
            List<IGrouping<string, Encounter>> categories = _encounters.GroupBy(enc => enc.Category).ToList();
            if (_sortCategorySetting.Value) {
                categories.Sort((cat1, cat2) => {
                    return cat1.Key.CompareTo(cat2.Key);
                });
            }
            foreach (IGrouping<string, Encounter> category in categories) {
                MenuItem cat = timerCategories.AddMenuItem(category.Key);
                cat.Click += delegate {
                    timerPanel.FilterChildren<TimerDetails>(db =>
                        string.Equals(db.Encounter.Category, category.Key));
                    _displayedTimerDetails = _allTimerDetails.Where(db =>
                        string.Equals(db.Encounter.Category, category.Key)).ToList();
                };
            }
        }

        private Panel BuildSettingsPanel(Rectangle panelBounds) {
            // 1. Timers tab
            Panel mainPanel = new Panel {
                CanScroll = false,
                Size = panelBounds.Size
            };

            _alertContainer = new AlertContainer {
                Parent = GameService.Graphics.SpriteScreen,
                ControlPadding = new Vector2(10, 5),
                PadLeftBeforeControl = true,
                PadTopBeforeControl = true,
                BackgroundColor = new Color(Color.Black, 0.3f),
                FlowDirection = _alertDisplayOrientationSetting.Value,
                Lock = _lockAlertContainerSetting.Value,
                Location = _alertContainerLocationSetting.Value,
                Visible = !_hideAlertsSetting.Value
            };
            SettingsUpdateAlertSize();
            SettingsUpdateAlertContainerLocation();
            _alertContainer.ContainerDragged += delegate {
                switch (_alertDisplayOrientationSetting.Value) {
                    case ControlFlowDirection.SingleLeftToRight:
                    case ControlFlowDirection.SingleTopToBottom:
                        _alertContainerLocationSetting.Value = _alertContainer.Location;
                        break;
                    case ControlFlowDirection.SingleRightToLeft:
                        _alertContainerLocationSetting.Value =
                            new Point(_alertContainer.Right, _alertContainer.Location.Y);
                        break;
                    case ControlFlowDirection.SingleBottomToTop:
                        _alertContainerLocationSetting.Value = new Point(_alertContainer.Location.X, _alertContainer.Bottom);
                        break;
                }
            };

            TextBox searchBox = new TextBox {
                Parent = mainPanel,
                Location = new Point(Dropdown.Standard.ControlOffset.X, Dropdown.Standard.ControlOffset.Y),
                PlaceholderText = "Search"
            };

            Panel menuSection = new Panel {
                Parent = mainPanel,
                Location = new Point(Panel.MenuStandard.PanelOffset.X,
                    searchBox.Bottom + Panel.MenuStandard.ControlOffset.Y),
                Size = Panel.MenuStandard.Size - new Point(0, Panel.MenuStandard.ControlOffset.Y),
                Title = "Timer Categories",
                CanScroll = true,
                ShowBorder = true
            };

            timerPanel = new FlowPanel {
                Parent = mainPanel,
                Location = new Point(menuSection.Right + Panel.MenuStandard.ControlOffset.X,
                    Panel.MenuStandard.ControlOffset.Y),
                FlowDirection = Blish_HUD.Controls.ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(8, 8),
                CanScroll = true,
                ShowBorder = true
            };

            StandardButton alertSettingsButton = new StandardButton {
                Parent = mainPanel,
                Text = "Alert Settings"
            };

            StandardButton enableAllButton = new StandardButton {
                Parent = mainPanel,
                Text = "Enable All"
            };

            StandardButton disableAllButton = new StandardButton {
                Parent = mainPanel,
                Text = "Disable All"
            };

            timerPanel.Size = new Point(mainPanel.Right - menuSection.Right - Control.ControlStandard.ControlOffset.X,
                                        mainPanel.Height - enableAllButton.Height - StandardButton.ControlStandard.ControlOffset.Y * 2);

            if (!Directory.EnumerateFiles(DirectoriesManager.GetFullDirectoryPath("timers")).Any() || _timersNeedUpdate) {
                var noTimersPanel = new Panel() {
                    Parent = mainPanel,
                    Location = new Point(menuSection.Right + Panel.MenuStandard.ControlOffset.X, Panel.MenuStandard.ControlOffset.Y),
                    ShowBorder = true,
                    Size = timerPanel.Size
                };

                var notice = new Label() {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Parent = noTimersPanel,
                    Size = new Point(noTimersPanel.Width, noTimersPanel.Height / 2 - 64),
                    ClipsBounds = false,
                };

                if (_timersNeedUpdate) {
                    notice.Text = "Your timers are outdated!\nDownload some now!";
                }
                else {
                    notice.Text = "You don't have any timers!\nDownload some now!";
                }

                var downloadPanel = new FlowPanel() {
                    Parent = noTimersPanel,
                    FlowDirection = ControlFlowDirection.LeftToRight,
                };
                downloadPanel.Resized += delegate {
                    downloadPanel.Location =
                        new Point(noTimersPanel.Width / 2 - downloadPanel.Width / 2, notice.Bottom + 24);
                };
                downloadPanel.Width = 196;

                var downloadHerosPack = new StandardButton() {
                    Text = "Download Hero's Timers",
                    Parent = downloadPanel,
                    Width = 196,
                };

                var manualDownload = new StandardButton() {
                    Text = "Manual Download",
                    Parent = downloadPanel,
                    Width = 196
                };
                manualDownload.Visible = false;

                var openTimersFolder = new StandardButton() {
                    Text = "Open Timers Folder",
                    Parent = noTimersPanel,
                    Width = 196,
                    Location = new Point(noTimersPanel.Width / 2 - 200, downloadPanel.Bottom + 4),
                };

                var skipUpdate = new StandardButton() {
                    Text = "Skip for now",
                    Parent = noTimersPanel,
                    Width = 196,
                    Location = new Point(openTimersFolder.Right + 4, downloadPanel.Bottom + 4)
                };

                var restartBlishHudAfter = new Label() {
                    Text = "Once done, restart this module or Blish HUD to enable them.",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Parent = noTimersPanel,
                    AutoSizeHeight = true,
                    Width = notice.Width,
                    Top = skipUpdate.Bottom + 4
                };

                bool isDownloading = false;
                downloadHerosPack.Click += async delegate(object sender, MouseEventArgs args) {
                    try {
                        if (isDownloading) {
                            return;
                        }
                        isDownloading = true;

                        var downloadUrl = update.URL;

                        // Download with WebClient
                        using var webClient = new WebClient();
                        webClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
                        webClient.Headers.Add(HttpRequestHeader.Accept, "application/octet-stream");

                        // Download the file
                        webClient.DownloadFileAsync(downloadUrl, $"{DirectoriesManager.GetFullDirectoryPath("timers")}/{update.name}");
                        restartBlishHudAfter.Text = "Downloading latest version of timers, please wait...";
                        webClient.DownloadFileCompleted += delegate (Object sender, AsyncCompletedEventArgs eventArgs) {
                            if (File.Exists($"{DirectoriesManager.GetFullDirectoryPath("timers")}/Hero-Timers.zip")) {
                                File.Delete($"{DirectoriesManager.GetFullDirectoryPath("timers")}/Hero-Timers.zip");
                            }

                            String[] filePaths = Directory.GetFiles(DirectoriesManager.GetFullDirectoryPath("timers"), "*.bhtimer",
                                SearchOption.AllDirectories);
                            var directoryReader = new DirectoryReader(DirectoriesManager.GetFullDirectoryPath("timers"));
                            var directoryResourceManager = new PathableResourceManager(directoryReader);

                            foreach (String filePath in filePaths) {
                                DateTimeOffset lastModified = Directory.GetLastWriteTimeUtc(filePath);
                                
                                if (lastModified <= DateTimeOffset.Parse("5/2/2022 5:28:21 PM +00:00")) {
                                    var encounter = ParseEncounter(new TimerStream(directoryReader.GetFileStream(filePath),
                                        directoryResourceManager, filePath));
                                    if (encounter.Author == "QuitarHero.1645") {
                                        if (File.Exists(filePath)) {
                                            File.Delete(filePath);
                                        }
                                    }
                                    encounter.Dispose();
                                }
                            }
                            directoryReader.Dispose();
                            directoryResourceManager.Dispose();

                            if (eventArgs.Error != null) {
                                notice.Text =
                                    "Download failed: "  + eventArgs.Error.Message;
                                Logger.Error("Download failed: " + eventArgs.Error.Message);
                                ScreenNotification.ShowNotification($"Failed to download timers: " + eventArgs.Error.Message, ScreenNotification.NotificationType.Error, null, 3);
                                restartBlishHudAfter.Text =
                                    "Wait and try downloading again\nOr manually download and place them in your timers folder.";
                                downloadPanel.Width = 400;
                                manualDownload.Visible = true;
                                manualDownload.Click += delegate {
                                    Process.Start("https://github.com/QuitarHero/Hero-Timers/releases/latest/download/Hero.Timer.Pack.zip");
                                    _lastTimersUpdate.Value = update;
                                };
                                downloadPanel.RecalculateLayout();
                            }
                            else {
                                notice.Text = "Your timers have been updated!";
                                restartBlishHudAfter.Text =
                                    "Download complete, click Continue to enable them.";
                                ScreenNotification.ShowNotification($"Timers updated!", ScreenNotification.NotificationType.Info, null, 3);
                                _lastTimersUpdate.Value = update;
                                downloadPanel.Dispose();
                                skipUpdate.Text = "Continue";
                            }

                            isDownloading = false;
                        };
                    }
                    catch (Exception ex) {
                        ScreenNotification.ShowNotification($"Failed to download timers: try again later...", ScreenNotification.NotificationType.Error, null, 3);
                        isDownloading = false;
                    }
                };

                openTimersFolder.Click += delegate (object sender, MouseEventArgs args) { Process.Start("explorer.exe", $"/open, \"{DirectoriesManager.GetFullDirectoryPath("timers")}\\\""); };

                skipUpdate.Click += delegate {
                    if (_encountersLoaded) {
                        return;
                    }
                    string timerDirectory = DirectoriesManager.GetFullDirectoryPath("timers");
                    timerLoader = new TimerLoader(timerDirectory);
                    timerLoader.LoadFiles(AddEncounter);
                    _encountersLoaded = true;
                    noTimersPanel.Dispose();
                    ShowTimerEntries(timerPanel);
                    ShowCustomTimerCategories();
                };
            }

            searchBox.Width = menuSection.Width;
            searchBox.TextChanged += delegate (object sender, EventArgs args) {
                timerPanel.FilterChildren<TimerDetails>(
                    db => db.Text.ToLower().Contains(searchBox.Text.ToLower()));
            };

            alertSettingsButton.Location = new Point(
                menuSection.Right + Panel.MenuStandard.ControlOffset.X,
                timerPanel.Bottom + StandardButton.ControlStandard.ControlOffset.Y);

            enableAllButton.Location =
                new Point(
                    timerPanel.Right - enableAllButton.Width - disableAllButton.Width -
                    StandardButton.ControlStandard.ControlOffset.X * 2,
                    timerPanel.Bottom + StandardButton.ControlStandard.ControlOffset.Y);

            disableAllButton.Location =
                new Point(enableAllButton.Right + StandardButton.ControlStandard.ControlOffset.X,
                    timerPanel.Bottom + StandardButton.ControlStandard.ControlOffset.Y);

            // 2. Alert Settings Window
            _alertSettingsWindow = new StandardWindow(Resources.AlertSettingsBackground, new Rectangle(24, 17, 505, 390), new Rectangle(38, 38, 472, 350)) {
                Parent = GameService.Graphics.SpriteScreen,
                Title = "Alert Settings",
                Emblem = Resources.TextureTimerEmblem,
                SavesPosition = true,
                Id = "TimersAlertSettingsWindow",
            };

            _alertSettingsWindow.Hide();

            alertSettingsButton.Click += delegate {
                if (_alertSettingsWindow.Visible) {
                    _alertSettingsWindow.Hide();
                }
                else {
                    _alertSettingsWindow.Show();
                }
            };

            Checkbox lockAlertsWindowCB = new Checkbox {
                Parent = _alertSettingsWindow,
                Text = "Lock Alerts Container",
                BasicTooltipText = "When enabled, the alerts container will be locked and cannot be moved.",
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    0)
            };
            lockAlertsWindowCB.Checked = _lockAlertContainerSetting.Value;
            lockAlertsWindowCB.CheckedChanged += delegate {
                _lockAlertContainerSetting.Value = lockAlertsWindowCB.Checked;
            };

            Checkbox centerAlertsWindowCB = new Checkbox {
                Parent = _alertSettingsWindow,
                Text = "Center Alerts Container",
                BasicTooltipText =
                    "When enabled, the location of the alerts container will always be set to the center of the screen.",
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    lockAlertsWindowCB.Bottom + Control.ControlStandard.ControlOffset.Y)
            };
            centerAlertsWindowCB.Checked = _centerAlertContainerSetting.Value;

            centerAlertsWindowCB.CheckedChanged += delegate {
                _centerAlertContainerSetting.Value = centerAlertsWindowCB.Checked;
            };

            Checkbox hideAlertsCB = new Checkbox {
                Parent = _alertSettingsWindow,
                Text = "Hide Alerts",
                BasicTooltipText =
                    "When enabled, alerts on the screen will be hidden.",
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    centerAlertsWindowCB.Bottom + Control.ControlStandard.ControlOffset.Y)
            };
            hideAlertsCB.Checked = _hideAlertsSetting.Value;

            hideAlertsCB.CheckedChanged += delegate {
                _hideAlertsSetting.Value = hideAlertsCB.Checked;
            };

            Checkbox hideDirectionsCB = new Checkbox {
                Parent = _alertSettingsWindow,
                Text = "Hide Directions",
                BasicTooltipText =
                    "When enabled, directions on the screen will be hidden.",
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    hideAlertsCB.Bottom + Control.ControlStandard.ControlOffset.Y)
            };
            hideDirectionsCB.Checked = _hideDirectionsSetting.Value;

            hideDirectionsCB.CheckedChanged += delegate {
                _hideDirectionsSetting.Value = hideDirectionsCB.Checked;
            };

            Checkbox hideMarkersCB = new Checkbox {
                Parent = _alertSettingsWindow,
                Text = "Hide Markers",
                BasicTooltipText =
                    "When enabled, markers on the screen will be hidden.",
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    hideDirectionsCB.Bottom + Control.ControlStandard.ControlOffset.Y)
            };
            hideMarkersCB.Checked = _hideMarkersSetting.Value;

            hideMarkersCB.CheckedChanged += delegate {
                _hideMarkersSetting.Value = hideMarkersCB.Checked;
            };

            Checkbox hideSoundsCB = new Checkbox {
                Parent = _alertSettingsWindow,
                Text = "Mute Text to Speech",
                BasicTooltipText =
                    "When enabled, text to speech will be muted.",
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    hideMarkersCB.Bottom + Control.ControlStandard.ControlOffset.Y)
            };
            hideSoundsCB.Checked = _hideSoundsSetting.Value;

            hideSoundsCB.CheckedChanged += delegate {
                _hideSoundsSetting.Value = hideSoundsCB.Checked;
            };

            Checkbox fillDirection = new Checkbox {
                Parent = _alertSettingsWindow,
                Text = "Invert Alert Fill",
                BasicTooltipText = "When enabled, alerts fill up as time passes.\nWhen disabled, alerts drain as time passes.",
                Location = new Point(Control.ControlStandard.ControlOffset.X,
                                     hideSoundsCB.Bottom + Control.ControlStandard.ControlOffset.Y)
            };
            fillDirection.Checked = _alertFillDirection.Value;

            fillDirection.CheckedChanged += delegate {
                _alertFillDirection.Value = fillDirection.Checked;
            };

            Label alertSizeLabel = new Label {
                Parent = _alertSettingsWindow,
                Text = "Alert Size",
                AutoSizeWidth = true,
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    fillDirection.Bottom + Control.ControlStandard.ControlOffset.Y)
            };

            Dropdown alertSizeDropdown = new Dropdown {
                Parent = _alertSettingsWindow,
            };
            alertSizeDropdown.Items.Add("Small");
            alertSizeDropdown.Items.Add("Medium");
            alertSizeDropdown.Items.Add("Large");
            alertSizeDropdown.Items.Add("BigWig Style");
            alertSizeDropdown.SelectedItem = _alertSizeSetting.Value.ToString();

            alertSizeDropdown.ValueChanged += delegate {
                _alertSizeSetting.Value =
                    (AlertType)Enum.Parse(typeof(AlertType), alertSizeDropdown.SelectedItem.Replace(" ", ""), true);
            };

            Label alertDisplayOrientationLabel = new Label {
                Parent = _alertSettingsWindow,
                Text = "Alert Display Orientation",
                AutoSizeWidth = true,
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    alertSizeLabel.Bottom + Dropdown.ControlStandard.ControlOffset.Y)
            };

            Dropdown alertDisplayOrientationDropdown = new Dropdown {
                Parent = _alertSettingsWindow,
                Location = new Point(alertDisplayOrientationLabel.Right + Dropdown.ControlStandard.ControlOffset.X,
                    alertDisplayOrientationLabel.Top)
            };
            alertDisplayOrientationDropdown.Items.Add("Left to Right");
            alertDisplayOrientationDropdown.Items.Add("Right to Left");
            alertDisplayOrientationDropdown.Items.Add("Top to Bottom");
            alertDisplayOrientationDropdown.Items.Add("Bottom to Top");

            switch (_alertDisplayOrientationSetting.Value) {
                case ControlFlowDirection.SingleLeftToRight:
                    alertDisplayOrientationDropdown.SelectedItem = "Left to Right";
                    break;
                case ControlFlowDirection.SingleRightToLeft:
                    alertDisplayOrientationDropdown.SelectedItem = "Right to Left";
                    break;
                case ControlFlowDirection.SingleTopToBottom:
                    alertDisplayOrientationDropdown.SelectedItem = "Top to Bottom";
                    break;
                case ControlFlowDirection.SingleBottomToTop:
                    alertDisplayOrientationDropdown.SelectedItem = "Bottom to Top";
                    break;
            }

            alertDisplayOrientationDropdown.ValueChanged += delegate {
                switch (alertDisplayOrientationDropdown.SelectedItem) {
                    case "Left to Right":
                        _alertDisplayOrientationSetting.Value = ControlFlowDirection.SingleLeftToRight;
                        break;
                    case "Right to Left":
                        _alertDisplayOrientationSetting.Value = ControlFlowDirection.SingleRightToLeft;
                        break;
                    case "Top to Bottom":
                        _alertDisplayOrientationSetting.Value = ControlFlowDirection.SingleTopToBottom;
                        break;
                    case "Bottom to Top":
                        _alertDisplayOrientationSetting.Value = ControlFlowDirection.SingleBottomToTop;
                        break;
                }
            };

            alertSizeDropdown.Location = new Point(alertDisplayOrientationDropdown.Left,
                alertSizeLabel.Top);

            Label alertPreviewLabel = new Label {
                Parent = _alertSettingsWindow,
                Text = "Alert Preview",
                AutoSizeWidth = true,
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    alertDisplayOrientationDropdown.Bottom + Dropdown.ControlStandard.ControlOffset.Y)
            };

            StandardButton addTestAlertButton = new StandardButton {
                Parent = _alertSettingsWindow,
                Text = "Add Test Alert",
                Location = new Point(alertDisplayOrientationDropdown.Left,
                    alertDisplayOrientationDropdown.Bottom + Dropdown.ControlStandard.ControlOffset.Y)
            };

            addTestAlertButton.Click += delegate {
                IAlertPanel newAlert = _alertSizeSetting.Value == AlertType.BigWigStyle
                                   ? new BigWigAlert()
                                   : new AlertPanel() {
                                       ControlPadding = new Vector2(10, 10),
                                       PadLeftBeforeControl = true,
                                       PadTopBeforeControl = true,
                                   };

                newAlert.Text = "Test Alert " + (_testAlertPanels.Count + 1);
                newAlert.TextColor = Color.White;
                newAlert.Icon = Texture2DExtension.Duplicate(Resources.GetIcon("raid"));
                newAlert.MaxFill = 100f;
                newAlert.CurrentFill = RandomUtil.GetRandom(0, 100) + RandomUtil.GetRandom(0, 100) * 0.01f;
                newAlert.FillColor = Color.Red;
                newAlert.ShouldShow = !_hideAlertsSetting.Value;

                ((Control)newAlert).Parent = _alertContainer;

                _testAlertPanels.Add(newAlert);
                _alertContainer.UpdateDisplay();
            };

            StandardButton clearTestAlertsButton = new StandardButton {
                Parent = _alertSettingsWindow,
                Text = "Clear Test Alerts",
                Location = new Point(addTestAlertButton.Right + Control.ControlStandard.ControlOffset.X,
                    addTestAlertButton.Top)
            };
            clearTestAlertsButton.Width = (int)(clearTestAlertsButton.Width * 1.15);

            clearTestAlertsButton.Click += delegate {
                _testAlertPanels.ForEach(panel => panel.Dispose());
                _testAlertPanels.Clear();
                _alertContainer.UpdateDisplay();
            };

            alertSizeDropdown.Width = addTestAlertButton.Width + clearTestAlertsButton.Width +
                                      Control.ControlStandard.ControlOffset.X;
            alertDisplayOrientationDropdown.Width = alertSizeDropdown.Width;

            Label alertContainerPositionLabel = new Label {
                Parent = _alertSettingsWindow,
                Text = "Alert Container Position",
                AutoSizeWidth = true,
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    clearTestAlertsButton.Bottom + Dropdown.ControlStandard.ControlOffset.Y)
            };

            StandardButton resetAlertContainerPositionButton = new StandardButton {
                Parent = _alertSettingsWindow,
                Text = "Reset Position",
                Location = new Point(addTestAlertButton.Left,
                    alertContainerPositionLabel.Top)
            };
            resetAlertContainerPositionButton.Width = alertSizeDropdown.Width;
            resetAlertContainerPositionButton.Click += delegate {
                _alertContainerLocationSetting.Value = new Point(
                    GameService.Graphics.SpriteScreen.Width / 2 - _alertContainer.Width / 2,
                    GameService.Graphics.SpriteScreen.Height / 2 - _alertContainer.Height / 2);
            };

            Label alertMoveDelayLabel = new Label {
                Parent = _alertSettingsWindow,
                Text = "Alert Move Delay",
                BasicTooltipText = "How many seconds alerts will take to reposition itself.",
                AutoSizeWidth = true,
                Location = new Point(
                    0 + Control.ControlStandard.ControlOffset.X,
                    resetAlertContainerPositionButton.Bottom + Dropdown.ControlStandard.ControlOffset.Y)
            };

            TextBox alertMoveDelayTextBox = new TextBox {
                Parent = _alertSettingsWindow,
                BasicTooltipText = "How many seconds alerts will take to reposition itself.",
                Location = new Point(resetAlertContainerPositionButton.Left, alertMoveDelayLabel.Top),
                Width = resetAlertContainerPositionButton.Width / 5,
                Height = alertMoveDelayLabel.Height,
                Text = String.Format("{0:0.00}", _alertMoveDelaySetting.Value)
            };

            TrackBar alertMoveDelaySlider = new TrackBar {
                Parent = _alertSettingsWindow,
                BasicTooltipText = "How many seconds alerts will take to reposition itself.",
                MinValue = 0,
                MaxValue = 3,
                Value = _alertMoveDelaySetting.Value,
                SmallStep = true,
                Location = new Point(alertMoveDelayTextBox.Right + Control.ControlStandard.ControlOffset.X, alertMoveDelayLabel.Top),
                Width = resetAlertContainerPositionButton.Width - alertMoveDelayTextBox.Width - Control.ControlStandard.ControlOffset.X
            };

            alertMoveDelayTextBox.TextChanged += delegate {
                int cursorIndex = alertMoveDelayTextBox.CursorIndex;
                float value;
                if (float.TryParse(alertMoveDelayTextBox.Text, out value) && value >= alertMoveDelaySlider.MinValue && value <= alertMoveDelaySlider.MaxValue) {
                    value = (float)Math.Round((double)value, 2);
                    _alertMoveDelaySetting.Value = value;
                    alertMoveDelaySlider.Value = value;
                    alertMoveDelayTextBox.Text = String.Format("{0:0.00}", value);
                }
                else {
                    alertMoveDelayTextBox.Text = String.Format("{0:0.00}", _alertMoveDelaySetting.Value);
                }

                alertMoveDelayTextBox.CursorIndex = cursorIndex;
            };

            alertMoveDelaySlider.ValueChanged += delegate {
                float value = (float)Math.Round((double)alertMoveDelaySlider.Value, 2);
                _alertMoveDelaySetting.Value = value;
                alertMoveDelayTextBox.Text = String.Format("{0:0.00}", value);
            };

            Label alertFadeDelayLabel = new Label {
                Parent = _alertSettingsWindow,
                Text = "Alert Fade In/Out Delay",
                BasicTooltipText = "How many seconds alerts will take to appear/disappear.",
                AutoSizeWidth = true,
                Location = new Point(
                    Control.ControlStandard.ControlOffset.X,
                    alertMoveDelayLabel.Bottom + Dropdown.ControlStandard.ControlOffset.Y)
            };

            TextBox alertFadeDelayTextBox = new TextBox {
                Parent = _alertSettingsWindow,
                BasicTooltipText = "How many seconds alerts will take to appear/disappear.",
                Location = new Point(resetAlertContainerPositionButton.Left, alertFadeDelayLabel.Top),
                Width = resetAlertContainerPositionButton.Width / 5,
                Height = alertFadeDelayLabel.Height,
                Text = String.Format("{0:0.00}", _alertFadeDelaySetting.Value)
            };

            TrackBar alertFadeDelaySlider = new TrackBar {
                Parent = _alertSettingsWindow,
                BasicTooltipText = "How many seconds alerts will take to appear/disappear.",
                MinValue = 0,
                MaxValue = 3,
                Value = _alertFadeDelaySetting.Value,
                SmallStep = true,
                Location = new Point(alertFadeDelayTextBox.Right + Control.ControlStandard.ControlOffset.X, alertFadeDelayLabel.Top),
                Width = resetAlertContainerPositionButton.Width - alertFadeDelayTextBox.Width - Control.ControlStandard.ControlOffset.X
            };

            alertFadeDelayTextBox.TextChanged += delegate {
                int cursorIndex = alertFadeDelayTextBox.CursorIndex;
                float value;
                if (float.TryParse(alertFadeDelayTextBox.Text, out value) && value >= alertFadeDelaySlider.MinValue && value <= alertFadeDelaySlider.MaxValue) {
                    value = (float)Math.Round((double)value, 2);
                    _alertFadeDelaySetting.Value = value;
                    alertFadeDelaySlider.Value = value;
                    alertFadeDelayTextBox.Text = String.Format("{0:0.00}", value);
                }
                else {
                    alertFadeDelayTextBox.Text = String.Format("{0:0.00}", _alertFadeDelaySetting.Value);
                }

                alertFadeDelayTextBox.CursorIndex = cursorIndex;
            };

            alertFadeDelaySlider.ValueChanged += delegate {
                float value = (float)Math.Round((double)alertFadeDelaySlider.Value, 2);
                _alertFadeDelaySetting.Value = value;
                alertFadeDelayTextBox.Text = String.Format("{0:0.00}", value);
            };

            StandardButton closeAlertSettingsButton = new StandardButton {
                Parent = _alertSettingsWindow,
                Text = "Close",
            };

            closeAlertSettingsButton.Location =
                new Point(
                    (_alertSettingsWindow.Left + _alertSettingsWindow.Right) / 2 - closeAlertSettingsButton.Width / 2,
                    _alertSettingsWindow.Bottom - StandardButton.ControlStandard.ControlOffset.Y -
                    closeAlertSettingsButton.Height);

            closeAlertSettingsButton.Click += delegate { _alertSettingsWindow.Hide(); };

            // 2. Timer Entries
            ShowTimerEntries(timerPanel);

            // 3. Categories
            timerCategories = new Menu {
                Size = menuSection.ContentRegion.Size,
                MenuItemHeight = 40,
                Parent = menuSection,
                CanSelect = true
            };

            MenuItem allTimers = timerCategories.AddMenuItem("All Timers");
            allTimers.Select();
            _displayedTimerDetails = _allTimerDetails.Where(db => true).ToList();
            allTimers.Click += delegate {
                timerPanel.FilterChildren<TimerDetails>(db => true);
                _displayedTimerDetails = _allTimerDetails.Where(db => true).ToList();
            };

            MenuItem enabledTimers = timerCategories.AddMenuItem("Enabled Timers");
            enabledTimers.Click += delegate {
                timerPanel.FilterChildren<TimerDetails>(db => db.Encounter.Enabled);
                _displayedTimerDetails = _allTimerDetails.Where(db => db.Encounter.Enabled).ToList();
            };

            MenuItem mapTimers = timerCategories.AddMenuItem("Current Map");
            mapTimers.Click += delegate {
                timerPanel.FilterChildren<TimerDetails>(db =>
                    (db.Encounter.Map == GameService.Gw2Mumble.CurrentMap.Id));
                _displayedTimerDetails = _allTimerDetails.Where(db =>
                    (db.Encounter.Map == GameService.Gw2Mumble.CurrentMap.Id)).ToList();
            };

            MenuItem invalidTimers = timerCategories.AddMenuItem("Invalid Timers");
            invalidTimers.Click += delegate {
                timerPanel.FilterChildren<TimerDetails>(db => db.Encounter.State == Encounter.EncounterStates.Error);
                _displayedTimerDetails = _allTimerDetails.Where(db => db.Encounter.State == Encounter.EncounterStates.Error).ToList();
            };

            ShowCustomTimerCategories();

            // Enable and Disable all button event handlers
            enableAllButton.Click += delegate {
                // Currently showing only enabled timers, don't need to do anything
                if (timerCategories.SelectedMenuItem == enabledTimers) {
                    return;
                }

                _displayedTimerDetails.ForEach(db => {
                    // Ignore Encounters that have errors
                    if (db.Encounter.State != Encounter.EncounterStates.Error) {
                        db.Enabled = true;
                    }
                });
            };

            disableAllButton.Click += delegate {
                _displayedTimerDetails.ForEach(db => {
                    // Ignore Encounters that have errors
                    if (db.Encounter.State != Encounter.EncounterStates.Error) {
                        db.Enabled = false;
                    }
                });
                // If only showing enabled timers, need to update the timerPanel to hide all the disabled timers
                if (timerCategories.SelectedMenuItem == enabledTimers) {
                    timerPanel.FilterChildren<TimerDetails>(db => db.Enabled);
                }
            };

            return mainPanel;
        }

        protected override void OnModuleLoaded(EventArgs e) {
            GameService.Gw2Mumble.CurrentMap.MapChanged += _onNewMapLoaded;
            _timersTab = GameService.Overlay.BlishHudWindow.AddTab("Timers",
                ContentsManager.GetTexture(@"textures\155035small.png"), _tabPanel);
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime) {
            if (_encountersLoaded) {
                foreach (var enc in _activeEncounters) {
                    enc.Update(gameTime);
                }
            }

            if (_debugText.Visible) {
                _debugText.Text = "Debug: " +
                                  GameService.Gw2Mumble.PlayerCharacter.Position.X.ToString("0.0") + " " +
                                  GameService.Gw2Mumble.PlayerCharacter.Position.Y.ToString("0.0") + " " +
                                  GameService.Gw2Mumble.PlayerCharacter.Position.Z.ToString("0.0") + " ";
            }

            if (_centerAlertContainerSetting.Value) {
                _alertContainer.Location = new Point(
                    GameService.Graphics.SpriteScreen.Width / 2 - _alertContainer.Width / 2,
                    _alertContainer.Location.Y);
            }
        }

        /// <inheritdoc />
        protected override void Unload() {
            // Unload here
            _debugText.Dispose();

            timerLoader?.Dispose();

            // Deregister event handlers
            GameService.Gw2Mumble.CurrentMap.MapChanged -= _onNewMapLoaded;
            _showDebugSetting.SettingChanged -= SettingsUpdateShowDebug;
            _lockAlertContainerSetting.SettingChanged -= SettingsUpdateLockAlertContainer;
            _centerAlertContainerSetting.SettingChanged -= SettingsUpdateCenterAlertContainer;
            _hideAlertsSetting.SettingChanged -= SettingsUpdateHideAlerts;
            _hideDirectionsSetting.SettingChanged -= SettingsUpdateHideDirections;
            _hideMarkersSetting.SettingChanged -= SettingsUpdateHideMarkers;
            _alertSizeSetting.SettingChanged -= SettingsUpdateAlertSize;
            _alertDisplayOrientationSetting.SettingChanged -= SettingsUpdateAlertDisplayOrientation;
            _alertContainerLocationSetting.SettingChanged -= SettingsUpdateAlertContainerLocation;
            _alertMoveDelaySetting.SettingChanged -= SettingsUpdateAlertMoveDelay;
            _alertFadeDelaySetting.SettingChanged -= SettingsUpdateAlertFadeDelay;

            //Cleanup Reset Button
            _resetButton?.Dispose();

            // Cleanup tab
            GameService.Overlay.BlishHudWindow.RemoveTab(_timersTab);
            _tabPanel.Dispose();
            _allTimerDetails.ForEach(de => de.Dispose());
            _allTimerDetails.Clear();
            _alertContainer.Dispose();
            _alertSettingsWindow.Dispose();
            _testAlertPanels.ForEach(panel => panel.Dispose());

            // Cleanup model
            _encounterEnableSettings.Clear();
            _activeAlertIds.Clear();
            _activeDirectionIds.Clear();
            _activeMarkerIds.Clear();
            _encounterIds.Clear();
            foreach (var enc in _encounters) {
                enc.Dispose();
            }
            _encounters.Clear();
            _activeEncounters.Clear();
            foreach (var enc in _invalidEncounters) {
                enc.Dispose();
            }
            _invalidEncounters.Clear();

            // Cleanup readers and resource managers
            _pathableResourceManagers.ForEach(m => m.Dispose());
            _pathableResourceManagers.Clear();

            // All static members must be manually unset
            ModuleInstance = null;
        }
    }
}