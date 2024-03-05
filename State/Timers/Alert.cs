using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Charr.Timers_BlishHUD.Controls;
using Charr.Timers_BlishHUD.Controls.BigWigs;
using Charr.Timers_BlishHUD.Pathing.Content;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Charr.Timers_BlishHUD.Models.Timers
{
    public class Alert : Timer, IDisposable
    {
        public Alert()
        {
            Name = "Unnamed Alert";
            _showTimer = true;
        }
        // Serialized
        [JsonProperty("warningDuration")] public float WarningDuration { get; set; } = 15.0f;
        [JsonProperty("alertDuration")] public float AlertDuration { get; set; } = 5.0f;
        [JsonProperty("warning")] public string WarningText { get; set; }
        [JsonProperty("warningColor")] public List<float> WarningTextColor { get; set; }
        [JsonProperty("alert")] public string AlertText { get; set; }
        [JsonProperty("alertColor")] public List<float> AlertTextColor { get; set; }
        [JsonProperty("icon")] public string IconString { get; set; } = "raid";
        [JsonProperty("fillColor")] public List<float> FillColor { get; set; }

        // Non-serialized properties
        public bool ShowAlert
        {
            get { return _showTimer; }
            set
            {
                if (activePanels != null)
                {
                    foreach (var entry in activePanels)
                    {
                        entry.Value.ShouldShow = value;
                    }
                }

                _showTimer = value;
            }
        }

        public Color Fill { get; set; } = Color.DarkGray;
        public Color WarningColor { get; set; } = Color.White;
        public Color AlertColor { get; set; } = Color.White;
        public AsyncTexture2D Icon { get; set; }
        public Dictionary<float, IAlertPanel> activePanels { get; set; }


        public string Initialize(PathableResourceManager resourceManager)
        {
            if (string.IsNullOrEmpty(WarningText))
                WarningDuration = 0;
            if (string.IsNullOrEmpty(AlertText))
                AlertDuration = 0;

            if (Timestamps == null || Timestamps.Count == 0)
                return WarningText + "/" + AlertText + " timestamps property invalid";

            Fill = Resources.ParseColor(Fill, FillColor);
            WarningColor = Resources.ParseColor(WarningColor, WarningTextColor);
            AlertColor = Resources.ParseColor(AlertColor, AlertTextColor);

            Icon = TimersModule.ModuleInstance.Resources.GetIcon(IconString);
            if (Icon == null)
                Icon = resourceManager.LoadTexture(IconString);

            activePanels = new Dictionary<float, IAlertPanel>();

            return null;
        }

        public override void Activate()
        {
            if (Activated || activePanels == null)
            {
                return;
            }

            _activated = true;
        }

        public override void Stop()
        {
            if (!Activated)
            {
                return;
            }

            Dispose();
        }

        public override void Deactivate()
        {
            if (!Activated)
            {
                return;
            }

            Dispose();
            _activated = false;
        }

        private IAlertPanel CreatePanel()
        {
            IAlertPanel panel = TimersModule.ModuleInstance._alertSizeSetting.Value == AlertType.BigWigStyle
                                    ? new BigWigAlert()
                                    : new AlertPanel()
                                    {
                                        ControlPadding = new Vector2(8, 8),
                                        PadLeftBeforeControl = true,
                                        PadTopBeforeControl = true,
                                    };

            panel.Text = string.IsNullOrEmpty(WarningText) ? AlertText : WarningText;
            panel.TextColor = string.IsNullOrEmpty(WarningText) ? AlertColor : WarningColor;
            panel.Icon = Texture2DExtension.Duplicate(Icon);
            panel.FillColor = Fill;
            panel.MaxFill = string.IsNullOrEmpty(WarningText) ? 0.0f : WarningDuration;
            panel.CurrentFill = 0.0f;
            panel.ShouldShow = ShowAlert;

            ((Control)panel).Parent = TimersModule.ModuleInstance._alertContainer;

            return panel;
        }

        public override void Update(float elapsedTime)
        {
            if (!Activated)
            {
                return;
            }

            foreach (float time in Timestamps)
            {
                IAlertPanel activePanel;
                if (!activePanels.TryGetValue(time, out activePanel))
                {
                    if (string.IsNullOrEmpty(WarningText) &&
                        elapsedTime >= time &&
                        elapsedTime < time + AlertDuration)
                    {
                        // If no warning, initialize on alert
                        activePanels.Add(time, CreatePanel());
                    }
                    else if (!string.IsNullOrEmpty(WarningText) &&
                             elapsedTime >= time - WarningDuration &&
                             elapsedTime < time + AlertDuration)
                    {
                        // If warning, initialize any time in duration
                        activePanels.Add(time, CreatePanel());
                    }
                }
                else
                {
                    // For on-going timers...
                    float activeTime = elapsedTime - (time - WarningDuration);
                    if (activeTime >= WarningDuration + AlertDuration)
                    {
                        activePanel.Dispose();
                        activePanels.Remove(time);
                    }
                    else if (activeTime >= WarningDuration)
                    {
                        // Show alert text on completed timers.
                        if (activePanel.CurrentFill != WarningDuration)
                            activePanel.CurrentFill = WarningDuration;
                        activePanel.Text = string.IsNullOrEmpty(AlertText) ? WarningText : AlertText;
                        activePanel.TimerText = "";
                        activePanel.TextColor = AlertColor;
                    }
                    else
                    {
                        // Update incomplete timers.
                        activePanel.CurrentFill = activeTime + TimersModule.ModuleInstance.Resources.TICKINTERVAL;
                        if (WarningDuration - activeTime < 5)
                        {
                            activePanel.TimerText = ((float)Math.Round((decimal)(WarningDuration - activeTime), 1))
                                .ToString("0.0");
                            activePanel.TimerTextColor = Color.Yellow;
                        }
                        else
                        {
                            activePanel.TimerText =
                                ((float)Math.Floor((decimal)(WarningDuration - activeTime))).ToString();
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (activePanels != null)
            {
                foreach (KeyValuePair<float, IAlertPanel> entry in activePanels)
                {
                    Debug.WriteLine(entry.Value.Text + " dispose");
                    entry.Value.Dispose();
                }
                activePanels.Clear();
            }
        }
    }
}