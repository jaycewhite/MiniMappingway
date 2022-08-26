using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingWay.Service;
using System;
using System.Collections.Generic;
using System.Numerics;


namespace MiniMappingWay
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private readonly Configuration configuration;
        private readonly FinderService _finderService;
        private readonly ClientState _clientState;

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

        public PluginUI(Configuration configuration, FinderService finderService, ClientState clientState)
        {
            this.configuration = configuration;
            _finderService = finderService;
            _clientState = clientState;

            updateColourArray();
        }



        public void updateColourArray()
        {
            Colours[0] = ImGui.ColorConvertFloat4ToU32(configuration.friendColour);
            Colours[1] = ImGui.ColorConvertFloat4ToU32(configuration.fcColour);
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            DrawSettingsWindow();


            if (!configuration.enabled)
            {
                return;
            }

            if (!_clientState.IsLoggedIn)
            {
                return;
            }

            if (!RunChecks())
            {
                return;
            }
            if (NaviMapManager.loading)
            {
                return;
            }

            if (configuration.showFcMembers)
            {
                PrepareDrawOnMinimap(_finderService.fcMembers, CircleCategory.fc);
            }
            if (configuration.showFriends)
            {
                PrepareDrawOnMinimap(_finderService.friends, CircleCategory.friend);
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
                var enabled = configuration.enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                {
                    configuration.enabled = enabled;
                    configuration.Save();
                }

                var showFriends = configuration.showFriends;
                if (ImGui.Checkbox("Show friends on minimap", ref showFriends))
                {
                    configuration.showFriends = showFriends;
                    configuration.Save();
                }

                var showFcMembers = configuration.showFcMembers;
                if (ImGui.Checkbox("Show FC Members on minimap", ref showFcMembers))
                {
                    configuration.showFcMembers = showFcMembers;
                    configuration.Save();
                }

                var minimapLocked = configuration.minimapLocked;
                ImGui.Text("Set this if your minimap always faces north");
                if (ImGui.Checkbox("Minimap Locked", ref minimapLocked))
                {
                    configuration.minimapLocked = minimapLocked;
                    configuration.Save();
                }

                var friendColour = configuration.friendColour;
                ImGui.Text("Friend Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("Friend", ref friendColour, ImGuiColorEditFlags.NoAlpha))
                {
                    configuration.friendColour = friendColour;
                    configuration.Save();
                    updateColourArray();

                }

                var fcColour = configuration.fcColour;
                ImGui.Text("FC Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("FC", ref fcColour, ImGuiColorEditFlags.NoAlpha))
                {
                    configuration.fcColour = fcColour;
                    configuration.Save();
                    updateColourArray();

                }

                var circleSize = configuration.circleSize;
                if (ImGui.SliderInt("Circle Size", ref circleSize, 1, 20))
                {
                    configuration.circleSize = circleSize;
                    configuration.Save();
                }

            }
            ImGui.End();
        }

        public bool RunChecks()
        {

            if (!configuration.enabled)
            {
                return false;

            }
            if (!NaviMapManager.updateNaviMap())
            {
                return false;
            }


            _finderService.LookFor();
            mapSize = new Vector2(_naviMapSize * NaviMapManager.naviScale, _naviMapSize * NaviMapManager.naviScale);
            minimapRadius = mapSize.X * 0.315f;
            mapPos = new Vector2(NaviMapManager.X, NaviMapManager.Y);
            return true;
        }

        public void PrepareDrawOnMinimap(List<GameObject> list, CircleCategory circleCategory)
        {

            if (list.Count > 0)
            {
                var PlayerRelativePosX = (_finderService.playerPos.X - _finderService.playerPos.X) * NaviMapManager.naviScale;
                var PlayerRelativePosZ = (_finderService.playerPos.Y - _finderService.playerPos.Y + NaviMapManager.yOffset) * NaviMapManager.naviScale;
                PlayerRelativePosZ = (-PlayerRelativePosZ);
                PlayerRelativePosX = (-PlayerRelativePosX);
                centerPoint = new Vector2(PlayerRelativePosX + NaviMapManager.X + mapSize.X / 2, -3.5f + PlayerRelativePosZ + NaviMapManager.Y + mapSize.Y / 2);

                foreach (var person in list)
                {
                    var relativePosX = (_finderService.playerPos.X - person.Position.X) * NaviMapManager.naviScale;
                    var relativePosZ = (_finderService.playerPos.Y - person.Position.Z + NaviMapManager.yOffset) * NaviMapManager.naviScale;
                    relativePosZ = (-relativePosZ) * NaviMapManager.zoneScale * NaviMapManager.zoom;
                    relativePosX = (-relativePosX) * NaviMapManager.zoneScale * NaviMapManager.zoom;
                    var circlePos = new Vector2(relativePosX + NaviMapManager.X + mapSize.X / 2, relativePosZ + NaviMapManager.Y + mapSize.Y / 2);


                    if (!configuration.minimapLocked)
                    {
                        circlePos = RotateForMiniMap(centerPoint, circlePos, (int)NaviMapManager.rotation);
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
                    draw_list.AddCircleFilled(circle.Position, configuration.circleSize, Colours[(int)circle.Category]);

                });
#if (DEBUG)
                ImGui.Text($"zoom {NaviMapManager.zoom}");
                ImGui.Text($"naviScale {NaviMapManager.naviScale}");
                ImGui.Text($"zoneScale {NaviMapManager.zoneScale}");
                ImGui.Text($"offsetX {NaviMapManager.offsetX}");
                ImGui.Text($"offsetY {NaviMapManager.offsetY}");
                ImGui.Text($"debug {NaviMapManager.debugValue}");
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
