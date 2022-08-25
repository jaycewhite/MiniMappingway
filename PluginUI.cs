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
using System.Threading.Tasks;
using System.Linq;
using Dalamud.Game.ClientState;

namespace MiniMappingWay
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration configuration;
        private readonly FinderService _finderService;
        private readonly ClientState _clientState;

        public List<CircleData> CircleData = new List<CircleData>();
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

        public PluginUI(Configuration configuration, FinderService finderService, ClientState clientState)
        {
            this.configuration = configuration;
            this._finderService = finderService;
            this._clientState = clientState;

            clientState.TerritoryChanged+= (i,x) => { this._finderService.updateMap(); };
            updateColourArray();
        }

        public void updateColourArray()
        {
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

            if (!_clientState.IsLoggedIn)
            {
                return;
            }

            if (!RunChecks())
            {
                Dalamud.Logging.PluginLog.Error("RunChecks false");
                return;
            }
            if (_finderService.naviMapInfo.loading)
            {
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
                if (ImGui.ColorEdit4("Friend", ref friendColour, ImGuiColorEditFlags.NoAlpha))
                {
                    this.configuration.friendColour = friendColour;
                    this.configuration.Save();
                    updateColourArray();

                }

                var fcColour = this.configuration.fcColour;
                ImGui.Text("FC Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("FC", ref fcColour, ImGuiColorEditFlags.NoAlpha))
                {
                    this.configuration.fcColour = fcColour;
                    this.configuration.Save();
                    updateColourArray();

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
            mapSize = new Vector2(_naviMapSize * _finderService.naviMapInfo.naviScale, _naviMapSize * _finderService.naviMapInfo.naviScale);
            minimapRadius = mapSize.X * 0.315f;
            mapPos = new Vector2(_finderService.naviMapInfo.X, _finderService.naviMapInfo.Y);
            return true;
        }

        public void PrepareDrawOnMinimap(List<GameObject> list, CircleCategory circleCategory)
        {

            if (list.Count > 0)
            {

                {

                    var relativePosX = (_finderService.playerPos.X - _finderService.playerPos.X) * _finderService.naviMapInfo.naviScale;
                    var relativePosZ = (_finderService.playerPos.Y - _finderService.playerPos.Y + _finderService.naviMapInfo.yOffset) * _finderService.naviMapInfo.naviScale;
                    relativePosZ = (-relativePosZ);
                    relativePosX = (-relativePosX);
                    centerPoint = new Vector2(relativePosX + _finderService.naviMapInfo.X + mapSize.X / 2, -3.5f +relativePosZ + _finderService.naviMapInfo.Y + mapSize.Y / 2);
                }


                //Parallel.ForEach(
                //    list,
                //    () => new List<CircleData>(),
                //(person,loopstate,localstate) =>
                foreach(var person in list)
                {
                    var relativePosX = (_finderService.playerPos.X - person.Position.X) * _finderService.naviMapInfo.naviScale;
                    var relativePosZ = (_finderService.playerPos.Y - person.Position.Z + _finderService.naviMapInfo.yOffset) * _finderService.naviMapInfo.naviScale;
                    relativePosZ = (-relativePosZ) * _finderService.naviMapInfo.zoneScale * _finderService.naviMapInfo.zoom;
                    relativePosX = (-relativePosX) * _finderService.naviMapInfo.zoneScale * _finderService.naviMapInfo.zoom;
                    uint scale = (uint)(_finderService.naviMapInfo.naviScale * 10);
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
                    CircleData.Add(new CircleData(circlePos, circleCategory));
                    


                }
                
                CircleData.Add(new CircleData(centerPoint, circleCategory));



            }

        }

        public void DoDraw()
        {
            //if (CircleData.Count == 0)
            //{
            //    return;
            //}
            ImGui.SetNextWindowSize(mapSize, ImGuiCond.Always);
            ImGui.SetNextWindowPos(mapPos);
            if (ImGui.Begin("test", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground))
            {

                ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

                CircleData.ForEach(circle =>
                {
                    draw_list.AddCircleFilled(circle.Position, this.configuration.circleSize, this.Colours[(int)circle.Category]);

                });
                ImGui.Text($"zoom {_finderService.naviMapInfo.zoom}");
                ImGui.Text($"naviScale {_finderService.naviMapInfo.naviScale}");
                ImGui.Text($"zoneScale {_finderService.naviMapInfo.zoneScale}");
                ImGui.Text($"offsetX {_finderService.naviMapInfo.offsetX}");
                ImGui.Text($"offsetY {_finderService.naviMapInfo.offsetY}");
                ImGui.Text($"debug {_finderService.naviMapInfo.debugValue}");

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
