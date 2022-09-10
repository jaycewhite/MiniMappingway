using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using MiniMappingway.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MiniMappingway.Window
{
    internal class NaviMapWindow : Dalamud.Interface.Windowing.Window
    {
        private readonly int _naviMapSize = 218;

        Vector2 mapSize = new Vector2();
        Vector2 mapPos = new Vector2();
        Vector2 windowPos = new Vector2();

        float minimapRadius;

        List<uint> colours = new List<uint>();

        public NaviMapWindow() : base("NaviMapWindow")
        {
            Size = new Vector2(200, 200);
            Position = new Vector2(200, 200);
            
            Flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing |ImGuiWindowFlags.NoNavFocus;

            ForceMainWindow = true;
        }

        public unsafe override void Draw()
        {
            ImDrawListPtr draw_list = ImGui.GetWindowDrawList();
            ServiceManager.NaviMapManager.CircleData.ForEach(circle =>
            {
                draw_list.AddCircleFilled(circle.Position, ServiceManager.Configuration.circleSize, ServiceManager.NaviMapManager.sourceDataDict[circle.SourceName].Color);
            });
            if (ServiceManager.NaviMapManager.debugMode)
            {
                ImGui.Text($"zoom {ServiceManager.NaviMapManager.zoom}");
                ImGui.Text($"naviScale {ServiceManager.NaviMapManager.naviScale}");
                ImGui.Text($"zoneScale {ServiceManager.NaviMapManager.zoneScale}");
                ImGui.Text($"offsetX {ServiceManager.NaviMapManager.offsetX}");
                ImGui.Text($"offsetY {ServiceManager.NaviMapManager.offsetY}");
                ImGui.Text($"x {ServiceManager.NaviMapManager.X}");
                ImGui.Text($"y {ServiceManager.NaviMapManager.Y}");
                ImGui.Text($"windowPos {windowPos}");
            }



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

            PrepareDrawOnMinimap();

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





        public void PrepareDrawOnMinimap()
        {
            //Get ffxiv window position on screen
            windowPos = ImGui.GetWindowViewport().Pos;

            foreach (var keyValuePair in ServiceManager.NaviMapManager.personListsDict)
            {

                if (keyValuePair.Value.Count > 0)
                {
                    //Player position from GameObject
                    Vector2 playerPos = new Vector2(ServiceManager.FinderService.playerPos.X, ServiceManager.FinderService.playerPos.Y);

                //Player Circle position will always be center of the minimap, this is also our pivot point
                Vector2 playerCirclePos = new Vector2(ServiceManager.NaviMapManager.X + (mapSize.X / 2), ServiceManager.NaviMapManager.Y + (mapSize.Y / 2)) + windowPos;

                    //to line up with minimap pivot better
                    playerCirclePos.Y -= 5f;
                    var removeList = new List<IntPtr>();
                    foreach (var item in keyValuePair.Value)
                    {
                        unsafe
                        {
                            if(((GameObject*)item)->ObjectIndex == 0)
                            {
                                removeList.Add(item);
                            }
                            

                        }
                    };

                    foreach(var item in removeList)
                    {
                        keyValuePair.Value.Remove(item);
                    }

                    foreach (var person in keyValuePair.Value)
                    {
                        var personObj = ServiceManager.ObjectTable.CreateObjectReference(person);
                        if (personObj == null || !personObj.IsValid())
                        {
                            continue;
                        }

                        //Calculate the relative position in world coords
                        Vector2 relativePersonPos = new Vector2(0, 0);

                        relativePersonPos.X = playerPos.X - personObj.Position.X;
                        relativePersonPos.Y = playerPos.Y - personObj.Position.Z;

                        //Account for various scales that can affect the minimap
                        relativePersonPos *= ServiceManager.NaviMapManager.zoneScale;
                        relativePersonPos *= ServiceManager.NaviMapManager.naviScale;
                        relativePersonPos *= ServiceManager.NaviMapManager.zoom;


                        //The Circle position for the "person" should be the players circle position minus the relativePosition of the person
                        var personCirclePos = playerCirclePos - relativePersonPos;



                        //if the minimap is unlocked, rotate circles around the player (the center of the minimap)
                        if (!ServiceManager.Configuration.minimapLocked)
                        {
                            personCirclePos = RotateForMiniMap(playerCirclePos, personCirclePos, (int)ServiceManager.NaviMapManager.rotation);
                        }


                        //If the circle would leave the minimap, clamp it to the minimap radius
                        var distance = Vector2.Distance(playerCirclePos, personCirclePos);
                        if (distance > minimapRadius)
                        {
                            var originToObject = personCirclePos - playerCirclePos;
                            originToObject *= minimapRadius / distance;
                            personCirclePos = playerCirclePos + originToObject;
                        }



                        ServiceManager.NaviMapManager.CircleData.Add(new CircleData(personCirclePos, keyValuePair.Key));

                    }

                    ////for debugging center point
                    //ServiceManager.NaviMapManager.CircleData.Add(new CircleData(playerCirclePos, circleCategory));
                }
            }

            //Get ffxiv window position on screen, 60,60 if multi-monitor mode is off



        }

        public Vector2 RotateForMiniMap(Vector2 center, Vector2 pos, int angle)
        {
            double angleInRadians = angle * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);

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
