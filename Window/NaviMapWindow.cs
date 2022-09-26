using System.Linq;
using System.Numerics;
using ImGuiNET;
using MiniMappingway.Manager;
using MiniMappingway.Utility;

namespace MiniMappingway.Window
{
    internal class NaviMapWindow : Dalamud.Interface.Windowing.Window
    {
        


        public NaviMapWindow() : base("NaviMapWindow")
        {
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

            if (ServiceManager.NaviMapManager.DebugMode)
            {
                ImGui.Text($"zoom {ServiceManager.NaviMapManager.Zoom}");
                ImGui.Text($"naviScale {ServiceManager.NaviMapManager.NaviScale}");
                ImGui.Text($"zoneScale {ServiceManager.NaviMapManager.ZoneScale}");
                ImGui.Text($"offsetX {ServiceManager.NaviMapManager.OffsetX}");
                ImGui.Text($"offsetY {ServiceManager.NaviMapManager.OffsetY}");
                ImGui.Text($"x {ServiceManager.NaviMapManager.X}");
                ImGui.Text($"y {ServiceManager.NaviMapManager.Y}");
                ImGui.Text($"islocked {ServiceManager.NaviMapManager.IsLocked}");
                ImGui.Text($"se {ServiceManager.Configuration.SourceConfigs.Count}");
                ImGui.Text($"circles {ServiceManager.NaviMapManager.CircleData.Values.SelectMany(x => x).Count()}");


                int count = 0;
                foreach (var dict in ServiceManager.NaviMapManager.PersonDict)
                {
                    count += dict.Value.Count;

                }
                ImGui.Text($"people {count}");
            }

            //foreach (var keyValuePair in ServiceManager.NaviMapManager.CircleData.Where(x => x.Value.Any()))
            foreach (var i in ServiceManager.NaviMapManager.CircleData.Where(x => x.Value.Any()).OrderBy(x => x.Key))
            {
                ServiceManager.NaviMapManager.CircleData.TryGetValue(i.Key, out var keyValuePair);
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

                    var circleConfig = ServiceManager.NaviMapManager.SourceDataDict[circle.SourceName];
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

        public override bool DrawConditions()
        {
            if (!MarkerUtility.RunChecks())
            {
                return false;
            }
            return true;
        }

        public override void PreDraw()
        {
            MarkerUtility.PrepareDrawOnMinimap();

        }



        public void Dispose()
        {
        }





        

        
    }
}
