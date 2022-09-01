using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;

namespace MiniMappingway.Window
{
    internal class NaviMapWindow : Dalamud.Interface.Windowing.Window
    {
        public override void Draw()
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public NaviMapWindow() : base("NaviMapWindow")
        {
            Flags |= ImGuiWindowFlags.NoInputs;
            Flags |= ImGuiWindowFlags.NoDecoration;
            Flags |= ImGuiWindowFlags.NoBackground;

            ForceMainWindow = true;



        }




    }
}
