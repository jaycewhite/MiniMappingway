using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingway.Service.Interface;

namespace MiniMappingway.Service
{
    internal class MarkerService : IMarkerService
    {
        public ConcurrentDictionary<int, Queue<Marker>> MarkerData { get; set; } = new();

        private readonly IPersonService _personService;
        private readonly IGameStateService _gameStateService;
        private readonly IMapService _mapService;
        private readonly ISourceService _sourceService;

        public MarkerService(IPersonService personService, IGameStateService gameStateService, IMapService mapService, ISourceService sourceService)
        {
            _personService = personService;
            _gameStateService = gameStateService;
            _mapService = mapService;
            _sourceService = sourceService;
        }

        public void CalculateMarkers()
        {
            //Get ffxiv window position on screen
            var windowPos = ImGui.GetWindowViewport().Pos;

            ////Player Circle position will always be center of the minimap, this is also our pivot point
            var playerCirclePos = new Vector2(_mapService.Data.X + _mapService.Data.MapSize.X / 2, _mapService.Data.Y + _mapService.Data.MapSize.Y / 2) + windowPos;

            ////to line up with minimap pivot better
            //PlayerCirclePos.Y -= 5f;

            foreach (var dict in _personService.PersonDict)
            {
                var priority = _sourceService.SourceDataDict[dict.Key].Priority;


                if (!MarkerData.ContainsKey(priority))
                {
                    MarkerData[priority] = new Queue<Marker>();
                }
                foreach (var person in dict.Value)
                {
                    var marker = CalculateMarkerPosition(person, playerCirclePos);
                    if (marker != null)
                    {
                        MarkerData[priority].Enqueue(marker);
                    }
                }


            }
        }


        private unsafe Marker? CalculateMarkerPosition(KeyValuePair<int, PersonDetails> person, Vector2 playerCirclePos)
        {
            var personObj = ServiceManager.ObjectTable.CreateObjectReference(person.Value.Ptr);

            if (personObj == null || !personObj.IsValid() || ServiceManager.ObjectTable[person.Key] == null)
            {
                _personService.RemoveFromBag(person.Value.Id, person.Value.SourceName);
                return null;
            }

            var isPartyMember = ((StatusFlags)((Character*)personObj.Address)->StatusFlags).HasFlag(StatusFlags.AllianceMember);

            if (isPartyMember)
            {
                return null;
            }

            //Calculate the relative position in world coords
            var relativePersonPos = new Vector2(0, 0);
            if (_gameStateService.Player == null)
            {
                return null;
            }
            relativePersonPos.X = _gameStateService.Player.Position.X - personObj.Position.X;
            relativePersonPos.Y = _gameStateService.Player.Position.Y - personObj.Position.Z;

            //Account for various scales that can affect the minimap
            relativePersonPos *= _mapService.Data.ZoneScale;
            relativePersonPos *= _mapService.Data.NaviScale;
            relativePersonPos *= _mapService.Data.Zoom;


            //The Circle position for the "person" should be the players circle position minus the relativePosition of the person
            var personCirclePos = playerCirclePos - relativePersonPos;



            //if the minimap is unlocked, rotate circles around the player (the center of the minimap)
            if (!_mapService.Data.IsLocked)
            {
                personCirclePos = RotateForMiniMap(playerCirclePos, personCirclePos, (int)_mapService.Data.Rotation);
            }


            //If the circle would leave the minimap, clamp it to the minimap radius
            var distance = Vector2.Distance(playerCirclePos, personCirclePos);
            if (distance > _mapService.Data.MinimapRadius)
            {
                var originToObject = personCirclePos - playerCirclePos;
                originToObject *= _mapService.Data.MinimapRadius / distance;
                personCirclePos = playerCirclePos + originToObject;
            }

            return new Marker(personCirclePos, person.Value.SourceName);
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


    }
}
