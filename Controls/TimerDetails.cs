using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Settings;
using Charr.Timers_BlishHUD.Models;

namespace Charr.Timers_BlishHUD.Controls {
    class TimerDetails : DetailsButton {
        private string _enableSettingName;
        private SettingEntry<bool> _enableSetting;

        private readonly GlowButton _authButton;
        private readonly GlowButton _toggleButton;
        private readonly GlowButton _descButton;
        private readonly GlowButton _reloadButton;

        private new bool _enabled;
        public new bool Enabled {
            get => _enabled;
            set {
                if (SetProperty(ref _enabled, value)) {
                    if (_enableSetting != null) _enableSetting.Value = _enabled;
                    if (_encounter != null) _encounter.Enabled = _enabled;
                    if (_toggleButton != null) _toggleButton.Checked = _enabled;
                    ToggleState = _enabled;
                }
            }
        }

        private Encounter _encounter;
        public Encounter Encounter {
            get => _encounter;
            set {
                if (!SetProperty(ref _encounter, value)) { return; }

                _enableSettingName = "TimerEnable:" + _encounter.Id;
                _enableSetting = TimersModule.ModuleInstance._timerSettingCollection.DefineSetting(_enableSettingName, _encounter.Enabled);
                Enabled = _encounter.Valid && _enableSetting.Value;

                Text = _encounter.Name + (_encounter.Valid ? "" : "\nLoading Error\nCheck Description for Details\n");
                Icon =  _encounter.Icon ?? ContentService.Textures.TransparentPixel;

                _authButton.Visible = !string.IsNullOrEmpty(_encounter.Author);
                _authButton.BasicTooltipText = "Timer Author: " + _encounter.Author;

                _descButton.Visible = !string.IsNullOrEmpty(_encounter.Description);
                _descButton.BasicTooltipText = "---Timer Description---\n" + _encounter.Description;

                _toggleButton.Checked = _encounter.Enabled;
                _toggleButton.Enabled = _encounter.Valid;
                _toggleButton.Icon = _encounter.Valid ? TimersModule.ModuleInstance.Resources.TextureEye : TimersModule.ModuleInstance.Resources.TextureX;

                if (_encounter.Valid) {
                    _toggleButton.Click += delegate {
                        Enabled = _toggleButton.Checked;
                    };
                }
            }
        }

        public TimerDetails() {
            IconSize = DetailsIconSize.Small;
            ShowVignette = false;
            HighlightType = DetailsHighlightType.LightHighlight;

            _authButton = new GlowButton {
                Icon = TimersModule.ModuleInstance.Resources.TextureDescription,
                Visible = false
            };

            _descButton = new GlowButton {
                Icon = TimersModule.ModuleInstance.Resources.TextureScout,
                Visible = false
            };

            _reloadButton = new GlowButton {
                Icon = TimersModule.ModuleInstance.Resources.TextureRefresh,
                BasicTooltipText = "Click to reload timer",
                Visible = true
            };

            ShowToggleButton = true;
            _toggleButton = new GlowButton {
                ActiveIcon = TimersModule.ModuleInstance.Resources.TextureEyeActive,
                BasicTooltipText = "Click to toggle timer",
                ToggleGlow = true
            };
        }

        public void Initialize() {
            _authButton.Parent = this;
            _descButton.Parent = this;
            _reloadButton.Parent = this;
            _toggleButton.Parent = this;
        }
    }
}
