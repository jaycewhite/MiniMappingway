using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using System;
using System.Collections.Generic;
using System.Numerics;


namespace MiniMappingway
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public class PluginUI : IDisposable
    {

        public List<CircleData> CircleData = new List<CircleData>();
        private readonly int _naviMapSize = 218;

        Vector2 centerPoint = new Vector2();
        Vector2 mapSize = new Vector2();
        Vector2 mapPos = new Vector2();
        readonly uint[] Colours = new uint[2];
        float minimapRadius;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return settingsVisible; }
            set { settingsVisible = value; }
        }

        public PluginUI()
        {
            updateColourArray();
        }

        public void updateColourArray()
        {
            Colours[0] = ImGui.ColorConvertFloat4ToU32(ServiceManager.Configuration.friendColour);
            Colours[1] = ImGui.ColorConvertFloat4ToU32(ServiceManager.Configuration.fcColour);
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            DrawSettingsWindow();


            if (!ServiceManager.Configuration.enabled)
            {
                return;
            }

            if (!ServiceManager.ClientState.IsLoggedIn)
            {
                return;
            }

            if (!RunChecks())
            {
                return;
            }
            if (ServiceManager.NaviMapManager.loading)
            {
                return;
            }

            if (ServiceManager.Configuration.showFcMembers)
            {
                PrepareDrawOnMinimap(ServiceManager.FinderService.fcMembers, CircleCategory.fc);
            }
            if (ServiceManager.Configuration.showFriends)
            {
                PrepareDrawOnMinimap(ServiceManager.FinderService.friends, CircleCategory.friend);
            }

            DoDraw();
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(350, 290), ImGuiCond.Appearing);
            if (ImGui.Begin("Mini-Mappingway Settings", ref settingsVisible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var enabled = ServiceManager.Configuration.enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                {
                    ServiceManager.Configuration.enabled = enabled;
                    ServiceManager.Configuration.Save();
                }

                var showFriends = ServiceManager.Configuration.showFriends;
                if (ImGui.Checkbox("Show friends on minimap", ref showFriends))
                {
                    ServiceManager.Configuration.showFriends = showFriends;
                    ServiceManager.Configuration.Save();
                }

                var showFcMembers = ServiceManager.Configuration.showFcMembers;
                if (ImGui.Checkbox("Show FC Members on minimap", ref showFcMembers))
                {
                    ServiceManager.Configuration.showFcMembers = showFcMembers;
                    ServiceManager.Configuration.Save();
                }

                var minimapLocked = ServiceManager.Configuration.minimapLocked;
                ImGui.Text("Set this if your minimap always faces north");
                if (ImGui.Checkbox("Minimap Locked", ref minimapLocked))
                {
                    ServiceManager.Configuration.minimapLocked = minimapLocked;
                    ServiceManager.Configuration.Save();
                }

                var friendColour = ServiceManager.Configuration.friendColour;
                ImGui.Text("Friend Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("Friend", ref friendColour, ImGuiColorEditFlags.NoAlpha))
                {
                    ServiceManager.Configuration.friendColour = friendColour;
                    ServiceManager.Configuration.Save();
                    updateColourArray();

                }

                var fcColour = ServiceManager.Configuration.fcColour;
                ImGui.Text("FC Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("FC", ref fcColour, ImGuiColorEditFlags.NoAlpha))
                {
                    ServiceManager.Configuration.fcColour = fcColour;
                    ServiceManager.Configuration.Save();
                    updateColourArray();

                }

                var circleSize = ServiceManager.Configuration.circleSize;
                if (ImGui.SliderInt("Circle Size", ref circleSize, 1, 20))
                {
                    ServiceManager.Configuration.circleSize = circleSize;
                    ServiceManager.Configuration.Save();
                }

            }
            ImGui.End();
        }

        public bool RunChecks()
        {

            if (!ServiceManager.Configuration.enabled)
            {
                return false;

            }
            if (!ServiceManager.NaviMapManager.updateNaviMap())
            {
                return false;
            }
            ServiceManager.NaviMapManager.CheckIfLoading();


            ServiceManager.FinderService.LookFor();
            mapSize = new Vector2(_naviMapSize * ServiceManager.NaviMapManager.naviScale, _naviMapSize * ServiceManager.NaviMapManager.naviScale);
            minimapRadius = mapSize.X * 0.315f;
            mapPos = new Vector2(ServiceManager.NaviMapManager.X, ServiceManager.NaviMapManager.Y);
            return true;
        }

        public void PrepareDrawOnMinimap(List<GameObject> list, CircleCategory circleCategory)
        {

            if (list.Count > 0)
            {
                var PlayerRelativePosX = (ServiceManager.FinderService.playerPos.X - ServiceManager.FinderService.playerPos.X) * ServiceManager.NaviMapManager.naviScale;
                var PlayerRelativePosZ = (ServiceManager.FinderService.playerPos.Y - ServiceManager.FinderService.playerPos.Y + ServiceManager.NaviMapManager.yOffset) * ServiceManager.NaviMapManager.naviScale;
                PlayerRelativePosZ = (-PlayerRelativePosZ);
                PlayerRelativePosX = (-PlayerRelativePosX);
                centerPoint = new Vector2(PlayerRelativePosX + ServiceManager.NaviMapManager.X + mapSize.X / 2, -3.5f + PlayerRelativePosZ + ServiceManager.NaviMapManager.Y + mapSize.Y / 2);

                foreach (var person in list)
                {
                    var relativePosX = (ServiceManager.FinderService.playerPos.X - person.Position.X) * ServiceManager.NaviMapManager.naviScale;
                    var relativePosZ = (ServiceManager.FinderService.playerPos.Y - person.Position.Z + ServiceManager.NaviMapManager.yOffset) * ServiceManager.NaviMapManager.naviScale;
                    relativePosZ = (-relativePosZ) * ServiceManager.NaviMapManager.zoneScale * ServiceManager.NaviMapManager.zoom;
                    relativePosX = (-relativePosX) * ServiceManager.NaviMapManager.zoneScale * ServiceManager.NaviMapManager.zoom;
                    var circlePos = new Vector2(relativePosX + ServiceManager.NaviMapManager.X + mapSize.X / 2, relativePosZ + ServiceManager.NaviMapManager.Y + mapSize.Y / 2);


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
                    CircleData.Add(new CircleData(circlePos, circleCategory));

                }
                //for debugging center point
                //CircleData.Add(new CircleData(centerPoint, circleCategory));
            }
        }

        public void DoDraw()
        {
            ImGui.SetNextWindowSize(mapSize, ImGuiCond.Always);
            ImGui.SetNextWindowPos(mapPos);
            if (ImGui.Begin("minimapOverlay", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground))
            {
                ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

                CircleData.ForEach(circle =>
                {
                    draw_list.AddCircleFilled(circle.Position, ServiceManager.Configuration.circleSize, Colours[(int)circle.Category]);

                });
#if (DEBUG)
                ImGui.Text($"zoom {ServiceManager.NaviMapManager.zoom}");
                ImGui.Text($"naviScale {ServiceManager.NaviMapManager.naviScale}");
                ImGui.Text($"zoneScale {ServiceManager.NaviMapManager.zoneScale}");
                ImGui.Text($"offsetX {ServiceManager.NaviMapManager.offsetX}");
                ImGui.Text($"offsetY {ServiceManager.NaviMapManager.offsetY}");
                ImGui.Text($"debug {ServiceManager.NaviMapManager.debugValue}");
#endif

                ImGui.End();
                CircleData.Clear();
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
