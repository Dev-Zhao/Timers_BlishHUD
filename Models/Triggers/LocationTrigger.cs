using Blish_HUD;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Charr.Timers_BlishHUD.Models.Triggers
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LocationTrigger : Trigger
    {
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
            else {
                return "At least one of requireEntry and requireDeparture must be set to true";
            }
            if (!CombatRequired && !OutOfCombatRequired && !EntryRequired && !DepartureRequired)
                return "No possible trigger conditions.";

            _initialized = true;
            return null;
        }
        public override void Enable()
        {
            _enabled = true;
        }
        public override void Disable()
        {
            _enabled = false;
        }
        public override void Reset()
        {
            /* NOOP */
        }
        public override bool Triggered()
        {
            if (!_enabled)
                return false;

            bool debugMode = TimersModule.ModuleInstance._debugModeSetting.Value;
            if (!debugMode && CombatRequired && !GameService.Gw2Mumble.PlayerCharacter.IsInCombat)
                return false;
            if (!debugMode && OutOfCombatRequired && GameService.Gw2Mumble.PlayerCharacter.IsInCombat)
                return false;

            // If EntryRequired or DepartureRequired is true, then the player must enter/leave the specified area to trigger
            if (debugMode || EntryRequired || DepartureRequired)
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

                if ((EntryRequired && !playerInArea) || (DepartureRequired && playerInArea))
                {
                    // Trigger cannot occur if:
                    //  Player is required to be in a certain area, but player is not in the area
                    // OR
                    //  Player is required to not be in a certain area, but player is in the area
                    return false;
                }
            }

            return true;
        }
    }
}
