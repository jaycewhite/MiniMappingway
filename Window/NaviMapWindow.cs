using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MiniMappingway.Model;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MiniMappingway.Window
{
    internal class NaviMapWindow : Dalamud.Interface.Windowing.Window
    {
        private readonly int _naviMapSize = 218;

        Vector2 centerPoint = new Vector2();
        Vector2 mapSize = new Vector2();
        Vector2 mapPos = new Vector2();

        float minimapRadius;

        public NaviMapWindow() : base("NaviMapWindow")
        {
            Size = new Vector2(200, 200);
            Position = new Vector2(200, 200);

            Flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground;

            ForceMainWindow = true;

            Dalamud.Logging.PluginLog.Verbose("Created NaviMapWindow Object");
        }

        public unsafe override void Draw()
        {
            ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

            ServiceManager.NaviMapManager.CircleData.ForEach(circle =>
            {
                draw_list.AddCircleFilled(circle.Position, ServiceManager.Configuration.circleSize, ServiceManager.NaviMapManager.Colours[(int)circle.Category]);

            });
#if (DEBUG)
                ImGui.Text($"zoom {ServiceManager.NaviMapManager.zoom}");
                ImGui.Text($"naviScale {ServiceManager.NaviMapManager.naviScale}");
                ImGui.Text($"zoneScale {ServiceManager.NaviMapManager.zoneScale}");
                ImGui.Text($"offsetX {ServiceManager.NaviMapManager.offsetX}");
                ImGui.Text($"offsetY {ServiceManager.NaviMapManager.offsetY}");
                ImGui.Text($"debug {ServiceManager.NaviMapManager.debugValue}");
#endif

            ServiceManager.NaviMapManager.CircleData.Clear();
        }

        public override void PreOpenCheck()
        {

            if (!ServiceManager.Configuration.enabled
                || !RunChecks()
                || !ServiceManager.ClientState.IsLoggedIn
                || ServiceManager.NaviMapManager.loading
                || !ServiceManager.NaviMapManager.visible)
            {
                IsOpen = false;
                return;
            }
            IsOpen = true;
        }

        public override void PreDraw()
        {

            if (ServiceManager.Configuration.showFcMembers)
            {
                PrepareDrawOnMinimap(ServiceManager.FinderService.fcMembers, CircleCategory.fc);
            }
            if (ServiceManager.Configuration.showFriends)
            {
                PrepareDrawOnMinimap(ServiceManager.FinderService.friends, CircleCategory.friend);
            }

        }

        public bool RunChecks()
        {
            if (!ServiceManager.NaviMapManager.updateNaviMap())
            {
                return false;
            }
            ServiceManager.NaviMapManager.CheckIfLoading();


            ServiceManager.FinderService.LookFor();
            mapSize = new Vector2(_naviMapSize * ServiceManager.NaviMapManager.naviScale, _naviMapSize * ServiceManager.NaviMapManager.naviScale);
            minimapRadius = mapSize.X * 0.315f;
            mapPos = new Vector2(ServiceManager.NaviMapManager.X, ServiceManager.NaviMapManager.Y);
            Size = mapSize;
            Position = mapPos;
            return true;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }





        public void PrepareDrawOnMinimap(List<GameObject> list, CircleCategory circleCategory)
        {

            var windowLocation = ImGui.GetWindowPos();
            if (list.Count > 0)
            {
                var PlayerRelativePosX = (ServiceManager.FinderService.playerPos.X - ServiceManager.FinderService.playerPos.X) * ServiceManager.NaviMapManager.naviScale;
                var PlayerRelativePosZ = (ServiceManager.FinderService.playerPos.Y - ServiceManager.FinderService.playerPos.Y + ServiceManager.NaviMapManager.yOffset) * ServiceManager.NaviMapManager.naviScale;
                PlayerRelativePosZ = (-PlayerRelativePosZ);
                PlayerRelativePosX = (-PlayerRelativePosX);
                
                centerPoint = new Vector2(PlayerRelativePosX + ServiceManager.NaviMapManager.X + windowLocation.X + mapSize.X / 2, -3.5f + PlayerRelativePosZ + ServiceManager.NaviMapManager.Y + windowLocation.Y + mapSize.Y / 2);

                Dalamud.Logging.PluginLog.Verbose($"{centerPoint}");
                foreach (var person in list)
                {
                    var relativePosX = (ServiceManager.FinderService.playerPos.X - person.Position.X) * ServiceManager.NaviMapManager.naviScale;
                    var relativePosZ = (ServiceManager.FinderService.playerPos.Y - person.Position.Z + ServiceManager.NaviMapManager.yOffset) * ServiceManager.NaviMapManager.naviScale;
                    relativePosZ = (-relativePosZ) * ServiceManager.NaviMapManager.zoneScale * ServiceManager.NaviMapManager.zoom;
                    relativePosX = (-relativePosX) * ServiceManager.NaviMapManager.zoneScale * ServiceManager.NaviMapManager.zoom;
                    var circlePos = new Vector2(relativePosX + ServiceManager.NaviMapManager.X + windowLocation.X + mapSize.X / 2, relativePosZ + ServiceManager.NaviMapManager.Y + windowLocation.Y + mapSize.Y / 2);


                    if (!ServiceManager.Configuration.minimapLocked)
                    {
                        circlePos = RotateForMiniMap(centerPoint, circlePos, (int)ServiceManager.NaviMapManager.rotation);
                    }

                    var distance = Vector2.Distance(centerPoint, circlePos);
                    if (distance > minimapRadius)
                    {
                        var originToObject = circlePos - centerPoint;
                        originToObject *= minimapRadius / distance;
                        circlePos = centerPoint + originToObject;
                    }
                    ServiceManager.NaviMapManager.CircleData.Add(new CircleData(circlePos, circleCategory));

                }

                //for debugging center point
                //ServiceManager.NaviMapManager.CircleData.Add(new CircleData(centerPoint, circleCategory));
            }
        }

        public Vector2 RotateForMiniMap(Vector2 center, Vector2 pos, int angle)
        {
            double angleInRadians = angle * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);

            var rotatedPoint = pos;

            rotatedPoint.X = (int)(cosTheta * (pos.X - center.X) -
            sinTheta * (pos.Y - center.Y) + center.X);

            rotatedPoint.Y = (int)
            (sinTheta * (pos.X - center.X) +
            cosTheta * (pos.Y - center.Y) + center.Y);

            return rotatedPoint;
        }




    }
}
