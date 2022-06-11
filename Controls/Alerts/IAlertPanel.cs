using Blish_HUD.Content;
using Microsoft.Xna.Framework;
using System;

namespace Charr.Timers_BlishHUD.Controls
{
    public interface IAlertPanel : IDisposable
    {

        public float MaxFill { get; set; }
        public float CurrentFill { get; set; }

        public string Text { get; set; }

        public string TimerText { get; set; }

        public Color TextColor { get; set; }

        public Color FillColor { get; set; }

        public Color TimerTextColor { get; set; }

        public bool ShouldShow { get; set; }

        public AsyncTexture2D Icon { get; set; }

    }
}
