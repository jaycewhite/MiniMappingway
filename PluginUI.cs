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
            
        }

        public void Dispose()
        {
        }


        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(350, 310), ImGuiCond.Appearing);
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
                if (ServiceManager.Configuration.showFcMembers)
                {
                    ImGui.TextColored(new Vector4(255,0,0,255),"For now this is done by comparing FC tags.");
                    ImGui.TextColored(new Vector4(255, 0, 0, 255), "If you have a common FC tag you may wish to disable this.");

                }
                ImGui.NewLine();

                var friendColour = ServiceManager.Configuration.friendColour;
                ImGui.Text("Friend Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("Friend", ref friendColour, ImGuiColorEditFlags.NoAlpha))
                {
                    ServiceManager.Configuration.friendColour = friendColour;
                    ServiceManager.Configuration.Save();
                    ServiceManager.NaviMapManager.updateColourArray();

                }

                var fcColour = ServiceManager.Configuration.fcColour;
                ImGui.Text("FC Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("FC", ref fcColour, ImGuiColorEditFlags.NoAlpha))
                {
                    ServiceManager.Configuration.fcColour = fcColour;
                    ServiceManager.Configuration.Save();
                    ServiceManager.NaviMapManager.updateColourArray();


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

    }
}
