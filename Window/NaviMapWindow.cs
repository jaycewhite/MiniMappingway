using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using ImGuiNET;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingway.Service;
using MiniMappingway.Service.Interface;

namespace MiniMappingway.Window
{
    public class NaviMapWindow : Dalamud.Interface.Windowing.Window
    {
        private readonly IGameStateService _gameStateService;
        private readonly IMapService _mapService;
        private readonly IMarkerService _markerService;
        private readonly IConfigurationService _configurationService;
        private readonly IPersonService _personService;
        private readonly ISourceService _sourceService;

        public NaviMapWindow(IGameStateService gameStateService, IMapService mapService, IMarkerService markerService,
            IConfigurationService configurationService, IPersonService personService, ISourceService sourceService) : base("NaviMapWindow")
        {
            _gameStateService = gameStateService;
            _mapService = mapService;
            _markerService = markerService;
            _configurationService = configurationService;
            _personService = personService;
            _sourceService = sourceService;

            Size = new Vector2(200, 200);
            Position = new Vector2(200, 200);

            Flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus;

            ForceMainWindow = true;
            IsOpen = true;
        }

        public override void Draw()
        {
            var drawList = ImGui.GetWindowDrawList();

            if (_gameStateService.DebugMode)
            {
                ImGui.Text($"zoom {_mapService.Data.Zoom}");
                ImGui.Text($"naviScale {_mapService.Data.NaviScale}");
                ImGui.Text($"zoneScale {_mapService.Data.ZoneScale}");
                ImGui.Text($"offsetX {_mapService.Data.OffsetX}");
                ImGui.Text($"offsetY {_mapService.Data.OffsetY}");
                ImGui.Text($"x {_mapService.Data.X}");
                ImGui.Text($"y {_mapService.Data.Y}");
                ImGui.Text($"islocked {_mapService.Data.IsLocked}");
                ImGui.Text($"se {_configurationService.GetConfiguration().SourceConfigs.Count}");
                ImGui.Text($"circles {_markerService.MarkerData.Values.SelectMany(x => x).Count()}");


                var count = 0;
                foreach (var dict in _personService.PersonDict)
                {
                    count += dict.Value.Count;

                }
                ImGui.Text($"people {count}");
            }

            //foreach (var keyValuePair in ServiceManager.MapService.Marker.Where(x => x.Value.Any()))
            foreach (var i in _markerService.MarkerData.Where(x => x.Value.Any()).OrderBy(x => x.Key))
            {
                _markerService.MarkerData.TryGetValue(i.Key, out var keyValuePair);
                if (keyValuePair == null)
                {
                    continue;
                }
                while (keyValuePair.Any())
                {
                    keyValuePair.TryDequeue(out var circle);
                    if (circle == null)
                    {
                        continue;
                    }

                    var circleConfig = _sourceService.SourceDataDict[circle.SourceName];
                    if (!circleConfig.Enabled)
                    {
                        continue;
                    }
                    drawList.AddCircleFilled(circle.Position, circleConfig.CircleSize, circleConfig.Color);
                    if (circleConfig.ShowBorder)
                    {
                        drawList.AddCircle(circle.Position, circleConfig.CircleSize, circleConfig.AutoBorderColour, 0, circleConfig.BorderRadius);

                    }
                }
                
            }
            
        }

        private void RunChecks()
        {

        }

        public override bool DrawConditions()
        {
            if (!_gameStateService.RunChecks())
            {
                return false;
            }
            if (_gameStateService.InCombat)
            {
                return false;
            }

            if (ServiceManager.ClientState.IsPvPExcludingDen)
            {
                return false;
            }

            return true;
        }

        public override void PreDraw()
        {
            PrepareDrawOnMinimap();
            Size = _mapService.Data.MapSize;
            Position = new Vector2(_mapService.Data.X, _mapService.Data.Y);
        }

        public void PrepareDrawOnMinimap()
        {
            

            ////for debugging center point
            //ServiceManager.MapService.Marker.Add(new Marker(playerCirclePos, circleCategory));

        }
    }
}
