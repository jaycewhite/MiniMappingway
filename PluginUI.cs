using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MiniMappingWay.Service;
using ImGuiNET;
using ImGuizmoNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using MiniMappingway.Model;

namespace MiniMappingWay
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration configuration;
        private readonly GameGui _gameGui;
        private readonly FinderService _finderService;

        public List<Tuple<Vector2, CircleCategory>> CirclePositions = new List<Tuple<Vector2, CircleCategory>>();
        private readonly int _naviMapSize = 218;

        Vector2 centerPoint = new Vector2();
        Vector2 mapSize = new Vector2();
        Vector2 mapPos = new Vector2();

        uint[] Colours = new uint[2];
        float minimapRadius;




        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        public PluginUI(Configuration configuration, GameGui gameGui, FinderService finderService)
        {
            this.configuration = configuration;
            this._gameGui = gameGui;
            this._finderService = finderService;

            this.Colours[0] = ImGui.ColorConvertFloat4ToU32(this.configuration.friendColour);
            this.Colours[1] = ImGui.ColorConvertFloat4ToU32(this.configuration.fcColour);

        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            DrawSettingsWindow();
            if (!this.configuration.enabled)
            {
                return;
            }

            if (!RunChecks())
            {
                Dalamud.Logging.PluginLog.Error("RunChecks false");
                return;
            }

            if (this.configuration.showFcMembers)
            {
                PrepareDrawOnMinimap(this._finderService.fcMembers, CircleCategory.fc);
            }
            if (this.configuration.showFriends)
            {
                PrepareDrawOnMinimap(this._finderService.friends, CircleCategory.friend);
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
            if (ImGui.Begin("Mini-Mappingway Settings", ref this.settingsVisible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var enabled = this.configuration.enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                {
                    this.configuration.enabled = enabled;
                    this.configuration.Save();
                }

                var showFriends = this.configuration.showFriends;
                if (ImGui.Checkbox("Show friends on minimap", ref showFriends))
                {
                    this.configuration.showFriends = showFriends;
                    this.configuration.Save();
                }

                var showFcMembers = this.configuration.showFcMembers;
                if (ImGui.Checkbox("Show FC Members on minimap", ref showFcMembers))
                {
                    this.configuration.showFcMembers = showFcMembers;
                    this.configuration.Save();
                }

                var minimapLocked = this.configuration.minimapLocked;
                ImGui.Text("Set this if your minimap always faces north");
                if (ImGui.Checkbox("Minimap Locked", ref minimapLocked))
                {
                    this.configuration.minimapLocked = minimapLocked;
                    this.configuration.Save();
                }

                var friendColour = this.configuration.friendColour;
                ImGui.Text("Friend Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("Person", ref friendColour, ImGuiColorEditFlags.NoAlpha))
                {
                    this.configuration.friendColour = friendColour;
                    this.configuration.Save();
                }

                var fcColour = this.configuration.fcColour;
                ImGui.Text("FC Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("FC", ref fcColour, ImGuiColorEditFlags.NoAlpha))
                {
                    this.configuration.fcColour = fcColour;
                    this.configuration.Save();
                }

                var circleSize = this.configuration.circleSize;
                if(ImGui.SliderInt("Circle Size",ref circleSize, 1, 20))
                {
                    this.configuration.circleSize = circleSize;
                    this.configuration.Save();
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
            if (!_finderService.updateNaviMap())
            {
                return false;
            }

            _finderService.LookFor();
            mapSize = new Vector2(_naviMapSize * _finderService.naviMapInfo.scale, _naviMapSize * _finderService.naviMapInfo.scale);
            minimapRadius = mapSize.X * 0.33f;
            mapPos = new Vector2(_finderService.naviMapInfo.X, _finderService.naviMapInfo.Y);
            return true;
        }

        public void PrepareDrawOnMinimap(List<GameObject> list, CircleCategory circleCategory)
        {

            if (list.Count > 0)
            {

                {

                    var relativePosX = (_finderService.playerPos.X - _finderService.playerPos.X) * _finderService.naviMapInfo.scale;
                    var relativePosZ = (_finderService.playerPos.Y - _finderService.playerPos.Y + _finderService.naviMapInfo.yOffset) * _finderService.naviMapInfo.scale;
                    relativePosZ = (-relativePosZ);
                    relativePosX = (-relativePosX);
                    centerPoint = new Vector2(relativePosX + _finderService.naviMapInfo.X + mapSize.X / 2, relativePosZ + _finderService.naviMapInfo.Y + mapSize.Y / 2);
                }



                foreach (var person in list)
                {
                    var relativePosX = (_finderService.playerPos.X - person.Position.X) * _finderService.naviMapInfo.scale;
                    var relativePosZ = (_finderService.playerPos.Y - person.Position.Z + _finderService.naviMapInfo.yOffset) * _finderService.naviMapInfo.scale;
                    relativePosZ = (-relativePosZ) * 2f * _finderService.naviMapInfo.zoom;
                    relativePosX = (-relativePosX) * 2f * _finderService.naviMapInfo.zoom;
                    uint scale = (uint)(_finderService.naviMapInfo.scale * 10);
                    var circlePos = new Vector2(relativePosX + _finderService.naviMapInfo.X + mapSize.X / 2, relativePosZ + _finderService.naviMapInfo.Y + mapSize.Y / 2);



                    if (!this.configuration.minimapLocked)
                    {

                        circlePos = RotateForMiniMap(centerPoint, circlePos, (int)_finderService.naviMapInfo.rotation);
                    }

                    var distance = Vector2.Distance(centerPoint, circlePos);
                    if (distance > minimapRadius)
                    {
                        var originToObject = circlePos - centerPoint;
                        originToObject *= minimapRadius / distance;
                        circlePos = centerPoint + originToObject;
                    }

                    CirclePositions.Add(new Tuple<Vector2, CircleCategory>(circlePos, circleCategory));


                }

            }

        }

        public void DoDraw()
        {
            if(CirclePositions.Count == 0)
            {
                return;
            }
            ImGui.SetNextWindowSize(mapSize, ImGuiCond.Always);
            ImGui.SetNextWindowPos(mapPos);
            if (ImGui.Begin("test", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground))
            {

                ImDrawListPtr draw_list = ImGui.GetWindowDrawList();
                CirclePositions.ForEach(circle =>
                {
                    draw_list.AddCircleFilled(circle.Item1, this.configuration.circleSize, this.Colours[(int)circle.Item2]);

                });

                ImGui.End();
                CirclePositions.Clear();
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
