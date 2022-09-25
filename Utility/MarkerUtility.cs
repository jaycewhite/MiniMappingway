using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace MiniMappingway.Utility
{
    internal static class MarkerUtility
    {
        private static readonly int _naviMapSize = 218;

        public static Vector2 _mapSize;
        public static Vector2 _mapPos;
        public static Vector2 _windowPos;
        public static Vector2 _playerPos = new(0, 0);
        public static Vector2 _playerCirclePos;

        static float _minimapRadius;


        public static bool ChecksPassed;

        private static unsafe CircleData? CalculateCirclePosition(this KeyValuePair<int, PersonDetails> person)
        {
            var personObj = ServiceManager.ObjectTable.CreateObjectReference(person.Value.ptr);

            if (personObj == null || !personObj.IsValid() || ServiceManager.ObjectTable[person.Key] == null)
            {
                ServiceManager.NaviMapManager.RemoveFromBag(person.Value.Id, person.Value.SourceName);
                return null;
            }
            unsafe
            {
                var isPartyMember = ((StatusFlags)((Character*)personObj.Address)->StatusFlags).HasFlag(StatusFlags.AllianceMember);

                if (isPartyMember)
                {
                    return null;
                }
            }

            //Calculate the relative position in world coords
            var relativePersonPos = new Vector2(0, 0);

            relativePersonPos.X = _playerPos.X - personObj.Position.X;
            relativePersonPos.Y = _playerPos.Y - personObj.Position.Z;

            //Account for various scales that can affect the minimap
            relativePersonPos *= ServiceManager.NaviMapManager.ZoneScale;
            relativePersonPos *= ServiceManager.NaviMapManager.NaviScale;
            relativePersonPos *= ServiceManager.NaviMapManager.Zoom;


            //The Circle position for the "person" should be the players circle position minus the relativePosition of the person
            var personCirclePos = _playerCirclePos - relativePersonPos;



            //if the minimap is unlocked, rotate circles around the player (the center of the minimap)
            if (!ServiceManager.NaviMapManager.IsLocked)
            {
                personCirclePos = RotateForMiniMap(_playerCirclePos, personCirclePos, (int)ServiceManager.NaviMapManager.Rotation);
            }


            //If the circle would leave the minimap, clamp it to the minimap radius
            var distance = Vector2.Distance(_playerCirclePos, personCirclePos);
            if (distance > _minimapRadius)
            {
                var originToObject = personCirclePos - _playerCirclePos;
                originToObject *= _minimapRadius / distance;
                personCirclePos = _playerCirclePos + originToObject;
            }

            return new CircleData(personCirclePos, person.Value.SourceName);
        }

        private static Vector2 RotateForMiniMap(Vector2 center, Vector2 pos, int angle)
        {
            var angleInRadians = angle * (Math.PI / 180);
            var cosTheta = Math.Cos(angleInRadians);
            var sinTheta = Math.Sin(angleInRadians);

            var rotatedPoint = pos;

            rotatedPoint.X = (float)(cosTheta * (pos.X - center.X) -
            sinTheta * (pos.Y - center.Y) + center.X);

            rotatedPoint.Y = (float)
            (sinTheta * (pos.X - center.X) +
            cosTheta * (pos.Y - center.Y) + center.Y);

            return rotatedPoint;
        }

        public static void PrepareDrawOnMinimap()
        {

            //Get ffxiv window position on screen
            _windowPos = ImGui.GetWindowViewport().Pos;

            //Player Circle position will always be center of the minimap, this is also our pivot point
            _playerCirclePos = new Vector2(ServiceManager.NaviMapManager.X + (_mapSize.X / 2), ServiceManager.NaviMapManager.Y + (_mapSize.Y / 2)) + _windowPos;

            //to line up with minimap pivot better
            _playerCirclePos.Y -= 5f;

            foreach (var dict in ServiceManager.NaviMapManager.PersonDict)
            {
                foreach (var person in dict.Value)
                {
                    var marker = person.CalculateCirclePosition();
                    if (marker != null)
                    {
                        ServiceManager.NaviMapManager.CircleData.Enqueue(marker);
                    }
                }

                
            }

            ////for debugging center point
            //ServiceManager.NaviMapManager.CircleData.Add(new CircleData(playerCirclePos, circleCategory));

        }

        public static bool RunChecks()
        {
            if (!ServiceManager.ClientState.IsLoggedIn
                || !ServiceManager.NaviMapManager.Visible
                || !ServiceManager.Configuration.Enabled)
            {
                return false;
            }
            if (!ServiceManager.NaviMapManager.UpdateNaviMap())
            {
                ChecksPassed = false;
                return false;
            }
            if (ServiceManager.NaviMapManager.CheckIfLoading())
            {
                return false;
            }



            _mapSize = new Vector2(_naviMapSize * ServiceManager.NaviMapManager.NaviScale, _naviMapSize * ServiceManager.NaviMapManager.NaviScale);
            _minimapRadius = _mapSize.X * 0.315f;
            _mapPos = new Vector2(ServiceManager.NaviMapManager.X, ServiceManager.NaviMapManager.Y);
            ServiceManager.WindowManager.NaviMapWindow.Size = _mapSize;
            ServiceManager.WindowManager.NaviMapWindow.Position = _mapPos;

            unsafe
            {
                var player = (Character*)ServiceManager.ObjectTable[0]?.Address;
                if (player == null)
                {
                    return false;
                }

                _playerPos = new Vector2(player->GameObject.Position.X, player->GameObject.Position.Z);
            }
            ChecksPassed = true;
            return true;
        }

        public static int? GetObjIndexById(uint objId)
        {
            foreach (var x in Enumerable.Range(2, 200).Where(x => x % 2 == 0))
            {
                if (ServiceManager.ObjectTable[x] != null && ServiceManager.ObjectTable[x]?.ObjectId == objId)
                {
                    return x;
                }
            }

            return null;
        }
    }
}
