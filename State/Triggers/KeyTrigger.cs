using Blish_HUD;
using Blish_HUD.Input;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;


namespace Charr.Timers_BlishHUD.Models.Triggers
{
    [JsonObject(MemberSerialization.OptIn)]
    public class KeyTrigger : Trigger
    {
        // Serialized members
        // Keys can be found here: "https://docs.microsoft.com/en-us/previous-versions/windows/xna/bb197781(v=xnagamestudio.42)"
        //[JsonProperty("key")]
        //public String Key { get; set; }
        // Valid Modifier keys: None, Ctrl, Alt, Shift
        //[JsonProperty("keyModifier")]
        //public String KeyModifier { get; set; }
        [JsonProperty("keyBind")] public int KeyBind { get; set; } = 0;

        // Private members
        private EventHandler<KeyboardEventArgs> _keyPressedHandler;
        private bool _keysPressed = false;

        // Methods
        public override String Initialize()
        {
            if (CombatRequired && OutOfCombatRequired)
                return "requireCombat and requireOutOfCombat cannot both be set to true";
            if (EntryRequired && DepartureRequired)
                return "requireEntry and requireDeparture cannot both be set to true";
            if (EntryRequired || DepartureRequired)
            {
                if (Position?.Count != 3)
                    return "invalid position";
                if (Antipode?.Count != 3 && Radius <= 0)
                    return "invalid radius/size";
            }

            //if (Key != null) {
            //    try {
            //        // Convert key strings to appropriate Enum
            //        _key = (Keys)Enum.Parse(typeof(Keys), Key, true);
            //        _keyModifiers = (KeyModifier == null) ? ModifierKeys.None : (ModifierKeys)Enum.Parse(typeof(ModifierKeys), KeyModifier, true);
            //    }
            //    catch (Exception e) {
            //        return e.Message;
            //    }
            //}

            // Create handler for KeyPress events
            _keyPressedHandler = new EventHandler<KeyboardEventArgs>(HandleKeyPressEvents);

            _initialized = true;
            return null;
        }

        public override void Enable()
        {
            if (_enabled) { return; }
            _enabled = true;
            GameService.Input.Keyboard.KeyPressed += _keyPressedHandler;
            _keysPressed = false;
        }

        public override void Disable()
        {
            if (!_enabled) { return; }
            _enabled = false;
            GameService.Input.Keyboard.KeyPressed -= _keyPressedHandler;
            _keysPressed = false;
        }

        public override void Reset()
        {
            _keysPressed = false;
        }

        public override bool Triggered()
        {
            if (TimersModule.ModuleInstance._keyBindSettings[KeyBind].Value.PrimaryKey == 0) { return true; }

            // Keys must be pressed to trigger
            return _keysPressed;
        }

        private void HandleKeyPressEvents(object sender, KeyboardEventArgs args)
        {
            // Trigger is not active
            if (!_enabled)
                return;
            // Wrong keys have been pressed
            if (args.Key != TimersModule.ModuleInstance._keyBindSettings[KeyBind].Value.PrimaryKey || GameService.Input.Keyboard.ActiveModifiers != TimersModule.ModuleInstance._keyBindSettings[KeyBind].Value.ModifierKeys)
                return;
            // Player needs to be in combat but they are not
            if (!TimersModule.ModuleInstance._debugModeSetting.Value && CombatRequired && !GameService.Gw2Mumble.PlayerCharacter.IsInCombat)
                return;
            // Player needs to be out of combat but they are not
            if (OutOfCombatRequired && GameService.Gw2Mumble.PlayerCharacter.IsInCombat)
                return;

            // If EntryRequired or DepartureRequired is true, then the player must be inside or outside the specified area
            if (EntryRequired || DepartureRequired)
            {
                float x = GameService.Gw2Mumble.PlayerCharacter.Position.X;
                float y = GameService.Gw2Mumble.PlayerCharacter.Position.Y;
                float z = GameService.Gw2Mumble.PlayerCharacter.Position.Z;

                bool playerInArea;
                if (Antipode != null && Antipode.Count == 3)
                {
                    // Check if player is within specified position and antipode
                    playerInArea = x >= Math.Min(Position[0], Antipode[0]) && x <= Math.Max(Position[0], Antipode[0]) &&
                        y >= Math.Min(Position[1], Antipode[1]) && y <= Math.Max(Position[1], Antipode[1]) &&
                        z >= Math.Min(Position[2], Antipode[2]) && z <= Math.Max(Position[2], Antipode[2]);
                }
                else
                {
                    // Check if player is within a certain radius of the specified position
                    playerInArea = Math.Sqrt(
                        Math.Pow(x - Position[0], 2) +
                        Math.Pow(y - Position[1], 2) +
                        Math.Pow(z - Position[2], 2)) <= Radius;
                }

                // Player is required to be in a certain area, but player is not in the area
                // OR
                // Player is required to not be in a certain area, but player is in the area
                if ((EntryRequired && !playerInArea) || (DepartureRequired && playerInArea))
                {
                    return;
                }
            }

            // Key press has occurred and satisfies all required conditions
            _keysPressed = true;
        }
    }
}
