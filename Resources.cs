using Blish_HUD;
using Blish_HUD.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;

namespace Charr.Timers_BlishHUD.Controls
{
    public class Resources : IDisposable
    {
        // Config
        public readonly int TICKRATE = 1000;
        public readonly float TICKINTERVAL;
        public readonly int MAX_ALERT_WIDTH = 366;
        public readonly int MAX_ALERT_HEIGHT = 128;

        // Assets
        public readonly Effect MasterScrollEffect;
        public readonly Texture2D TextureFillCrest;
        public readonly Texture2D TextureVignette;
        public readonly BitmapFont Font;
        public readonly Texture2D TextureEye;
        public readonly Texture2D TextureEyeActive;
        public readonly Texture2D TextureFade;
        public readonly Texture2D TextureX;
        public readonly Texture2D TextureScout;
        public readonly Texture2D TextureRefresh;
        public readonly Texture2D TextureDescription;
        public readonly Texture2D TextureTimerEmblem;
        public readonly Texture2D AlertSettingsBackground;
        public readonly Texture2D WindowTitleBarLeft;
        public readonly Texture2D WindowTitleBarRight;
        public readonly Texture2D WindowTitleBarLeftActive;
        public readonly Texture2D WindowTitleBarRightActive;
        public readonly Texture2D WindowCorner;
        public readonly Texture2D WindowBackground;
        public readonly Texture2D BigWigBackground;

        private readonly Dictionary<string, string> _iconURIs = new Dictionary<string, string>() {
            {"raid", "9F5C23543CB8C715B7022635C10AA6D5011E74B3/1302679"},
            {"boss", "7554DCAF5A1EA1BDF5297352A203AF2357BE2B5B/498983"},
            {"onepath", "1B3B7103E5FFFEB4B94137CFEF6AC5A528AE1BA8/1730771"},
            {"slayer", "E00460A2CAD85D47406EAB4213D1010B3E80C9B0/42675"},
            {"hero", "CD94B9A33CD82E9C7BBE59ADB051CE7CE00929AC/42679"},
            {"weaponmaster", "E57F44931D5D1C0DEB16A27803A4744492B834E2/42682"},
            {"community", "AED92D932A30A6990F0B5C35073AEB4C4556E2F3/42681"},
            {"daybreak", "056C32B97B15F04E2BD6A660FC451946ED086040/1895981"},
            {"noquarter", "2D305E1F34A7985BF572D40717062096E3BD58BA/2293615"},
            {"transferchaser", "203A4F05DD7DF36A4DEBF9B4D9DE90AEC8A7155A/1769806"},
            {"conservation", "21B767B52BC1C40F0698B1D9C77EDDDD22E6B46D/1769807"},
            {"winter", "2A2DA0B946A85A0DB5D59E0703796C26AB4D650D/1914854"},
            {"foefire", "CFAAC3D0D89BF997BD55FF647F1E546CACFCD795/1635137"},
            {"djinn", "3504199B06996B43237C0ACF10084950716DCF38/2063558"},
            {"pvp", "7F4E2835316DE912B1493CCF500A9D5CF4A83B4A/42676"},
            {"wvw", "2BBA251A24A2C1A0A305D561580449AF5B55F54F/338457"},
            {"event", "C2E37DE77D0C024B06F1E0A5F738524A07E9CF2B/797625"},
            {"dungeon", "37A6BBC111E4EF34CDFF93314A26992A4858EF14/602776"},
            {"fractal", "9A6791950A5F3EBD15C91C2942F1E3C8D5221B28/602779"}
        };

        private readonly Dictionary<string, AsyncTexture2D> _iconTextures;
        private const string DEFAULT_ICON = "raid";

        public Resources() {
            TICKINTERVAL = 1 / TICKRATE;

            MasterScrollEffect = ContentService.Content.ContentManager.Load<Effect>(@"effects\menuitem");
            MasterScrollEffect.Parameters["Mask"].SetValue(GameService.Content.GetTexture("156072"));
            MasterScrollEffect.Parameters["Overlay"].SetValue(GameService.Content.GetTexture("156071"));

            TextureFillCrest = GameService.Content.GetTexture(@"controls/detailsbutton/605004");
            TextureVignette = GameService.Content.GetTexture(@"controls/detailsbutton/605003");
            Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size22,
                ContentService.FontStyle.Regular);

            TextureEye = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\605021.png");
            TextureEyeActive = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\605019.png");
            TextureDescription = GameService.Content.GetTexture("102530");
            TextureTimerEmblem = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\841720.png");
            TextureFade = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\uniformclouds_blur30.png");
            TextureX = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\x.png");
            TextureScout = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\scout.png");
            TextureRefresh = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\refresh.png");

            AlertSettingsBackground = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\502049.png");

            WindowTitleBarLeft = GameService.Content.GetTexture("titlebar-inactive");
            WindowTitleBarRight = GameService.Content.GetTexture("window-topright");
            WindowTitleBarLeftActive = GameService.Content.GetTexture("titlebar-active");
            WindowTitleBarRightActive = GameService.Content.GetTexture("window-topright-active");
            WindowCorner = GameService.Content.GetTexture(@"controls/window/156008");
            WindowBackground = GameService.Content.GetTexture(@"controls/notification/notification-gray");

            BigWigBackground = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\1234872.png");

            _iconTextures = new Dictionary<string, AsyncTexture2D>();
            GetIcon(DEFAULT_ICON);
        }

        public AsyncTexture2D GetIcon() {
            return GetIcon(DEFAULT_ICON);
        }

        public AsyncTexture2D GetIcon(string name) {
            name = name.Trim().ToLower();

            // Try to get the texture
            AsyncTexture2D asyncTexture2D;
            if (_iconTextures.TryGetValue(name, out asyncTexture2D)) {
                return asyncTexture2D;
            }
            else {
                // Texture cannot be found, try to get the URI and obtain the texture
                string URI;
                if (_iconURIs.TryGetValue(name, out URI)) {
                    asyncTexture2D = GameService.Content.GetRenderServiceTexture(URI);
                    _iconTextures.Add(name, asyncTexture2D);
                    return asyncTexture2D;
                }

                // URI cannot be found
                return null;
            }
        }

        // Static Utility Functions
        public static Color ParseColor(int r, int g, int b, float a = 1.0f) {
            r = Math.Min(Math.Max(0, r), 255);
            g = Math.Min(Math.Max(0, g), 255);
            b = Math.Min(Math.Max(0, b), 255);
            int aInt = (int)(Math.Min(Math.Max(0f, a), 1.0f) * 255);
            return new Color(r, g, b, aInt);
        }

        public static Color ParseColor(Color fallbackColor, List<float> values) {
            if (values?.Count == 3) {
                return ParseColor((int)values[0], (int)values[1], (int)values[2]);
            }
            else if (values?.Count == 4) {
                return ParseColor((int)values[0], (int)values[1], (int)values[2], values[3]);
            }
            else {
                return fallbackColor;
            }
        }

        public void Dispose() {
            TextureFillCrest.Dispose();
            TextureVignette.Dispose();
            TextureEye.Dispose();
            TextureEyeActive.Dispose();
            TextureFade.Dispose();
            TextureX.Dispose();
            TextureDescription.Dispose();
            TextureTimerEmblem.Dispose();

            AlertSettingsBackground.Dispose();

            WindowTitleBarLeft.Dispose();
            WindowTitleBarRight.Dispose();
            WindowTitleBarLeftActive.Dispose();
            WindowTitleBarRightActive.Dispose();
            WindowCorner.Dispose();
            WindowBackground.Dispose();

            foreach (AsyncTexture2D icon in _iconTextures.Values) {
                icon.Dispose();
            }
            _iconTextures.Clear();
            _iconURIs.Clear();
        }
    }
}