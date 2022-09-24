using System;
using System.Numerics;
using ImGuiNET;
using MiniMappingway.Manager;
using MiniMappingway.Service;

namespace MiniMappingway
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public class PluginUi : IDisposable
    {
        // this extra bool exists for ImGui, since you can't ref a property
        private bool _visible;
        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

        private bool _settingsVisible;
        public bool SettingsVisible
        {
            get { return _settingsVisible; }
            set { _settingsVisible = value; }
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
            if (ImGui.Begin("Mini-Mappingway Settings", ref _settingsVisible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var enabled = ServiceManager.Configuration.Enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                {
                    ServiceManager.Configuration.Enabled = enabled;
                    ServiceManager.Configuration.Save();
                }

                var showFriends = ServiceManager.Configuration.ShowFriends;
                if (ImGui.Checkbox("Show friends on minimap", ref showFriends))
                {
                    ServiceManager.Configuration.ShowFriends = showFriends;
                    ServiceManager.Configuration.Save();
                }

                if (!ServiceManager.Configuration.ShowFriends)
                {
                    ServiceManager.NaviMapManager.ClearPersonBag(FinderService.FriendKey);
                }

                var showFcMembers = ServiceManager.Configuration.ShowFcMembers;
                if (ImGui.Checkbox("Show FC Members on minimap", ref showFcMembers))
                {
                    ServiceManager.Configuration.ShowFcMembers = showFcMembers;
                    ServiceManager.Configuration.Save();
                }
                if (ServiceManager.Configuration.ShowFcMembers)
                {
                    ImGui.TextColored(new Vector4(255,0,0,255),"For now this is done by comparing FC tags.");
                    ImGui.TextColored(new Vector4(255, 0, 0, 255), "If you have a common FC tag you may wish to disable this.");

                }
                else
                {
                    ServiceManager.NaviMapManager.ClearPersonBag(FinderService.FcMembersKey);
                }
                ImGui.NewLine();

                var friendColour = ServiceManager.Configuration.FriendColour;
                ImGui.Text("Friend Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("Friend", ref friendColour, ImGuiColorEditFlags.NoAlpha))
                {
                    ServiceManager.Configuration.FriendColour = friendColour;
                    ServiceManager.Configuration.Save();

                }

                var fcColour = ServiceManager.Configuration.FcColour;
                ImGui.Text("FC Colour. Click the coloured square for a picker.");
                if (ImGui.ColorEdit4("FC", ref fcColour, ImGuiColorEditFlags.NoAlpha))
                {
                    ServiceManager.Configuration.FcColour = fcColour;
                    ServiceManager.Configuration.Save();


                }

                var circleSize = ServiceManager.Configuration.CircleSize;
                if (ImGui.SliderInt("Circle Size", ref circleSize, 1, 20))
                {
                    ServiceManager.Configuration.CircleSize = circleSize;
                    ServiceManager.Configuration.Save();
                }

            }
            ImGui.End();
        }

    }
}
