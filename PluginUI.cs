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

        float minimapRadius;

        uint _personColourUint = 0;



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

        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.
            DrawSettingsWindow();
            if (!RunChecks())
            {
                Dalamud.Logging.PluginLog.Error("RunChecks false");
                return;
            }

            PrepareDrawOnMinimap(this._finderService.friends, CircleCategory.friend);
            PrepareDrawOnMinimap(this._finderService.fcMembers, CircleCategory.fc);

            DoDraw();
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(350, 220), ImGuiCond.Appearing);
            if (ImGui.Begin("Mini-Mappingway Settings", ref this.settingsVisible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var showFcMembers = this.configuration.showFcMembers;
                if (ImGui.Checkbox("Show FC Members on minimap", ref showFcMembers))
                {
                    this.configuration.showFcMembers = showFcMembers;
                    this.configuration.Save();
                }

                var enabled = this.configuration.enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                {
                    this.configuration.enabled = enabled;
                    this.configuration.Save();
                }

                var minimapLocked = this.configuration.minimapLocked;
                ImGui.Text("Set this if your minimap always faces north");
                if (ImGui.Checkbox("Minimap Locked", ref minimapLocked))
                {
                    this.configuration.minimapLocked = minimapLocked;
                    this.configuration.Save();
                }

                var personColour = this.configuration.friendColour;
                ImGui.Text("Person Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("", ref personColour, ImGuiColorEditFlags.NoAlpha))
                {
                    this.configuration.friendColour = personColour;
                    this.configuration.Save();
                }

                var fcColour = this.configuration.friendColour;
                ImGui.Text("FC Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("", ref fcColour, ImGuiColorEditFlags.NoAlpha))
                {
                    this.configuration.fcColour = fcColour;
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
            ImGui.SetNextWindowSize(mapSize, ImGuiCond.Always);
            ImGui.SetNextWindowPos(mapPos);
            if (ImGui.Begin("test", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground))
            {

                ImGui.Text($"{_finderService.naviMapInfo.zoom}");
                ImDrawListPtr draw_list = ImGui.GetWindowDrawList();
                CirclePositions.ForEach(circle =>
                {
                    ImGui.Text(circle.Item1.ToString());
                    switch (circle.Item2)
                    {
                        case CircleCategory.fc:
                            draw_list.AddCircleFilled(circle.Item1, 4, ImGui.ColorConvertFloat4ToU32(this.configuration.fcColour));
                            break;

                        case CircleCategory.friend:
                            draw_list.AddCircleFilled(circle.Item1, 4, ImGui.ColorConvertFloat4ToU32(this.configuration.friendColour));
                            break;
                    }

                });

                ImGui.End();
                CirclePositions.Clear();
            }
        }

        public static uint ColorConvert(byte r, byte g, byte b, byte a) { uint ret = a; ret <<= 8; ret += b; ret <<= 8; ret += g; ret <<= 8; ret += r; return ret; }

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

        uint ConvertVector4ToUint(Vector4 colour)
        {
            return (uint)((((int)colour.Z << 24) | ((int)colour.W << 16) | ((int)colour.X << 8) | (int)colour.Y) & 0xffffffffL);
        }


    }


}
