using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingway.Service;

namespace MiniMappingway
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public class PluginUi : IDisposable
    {
        // this extra bool exists for ImGui, since you can't ref a property
        private bool _settingsVisible;
        public bool SettingsVisible
        {
            get => _settingsVisible;
            set => _settingsVisible = value;
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

            ImGui.SetNextWindowSize(new Vector2(450, 405), ImGuiCond.Appearing);
            if (ImGui.Begin("Mini-Mappingway Settings", ref _settingsVisible, ImGuiWindowFlags.NoCollapse))
            {
                // can't ref a property, so use a local copy
                var enabled = ServiceManager.Configuration.Enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                {
                    ServiceManager.Configuration.Enabled = enabled;
                    ServiceManager.Configuration.Save();
                }

                ImGui.TextColored(new Vector4(255, 0, 0, 255), "For now FC members are found by comparing FC tags.");
                ImGui.TextColored(new Vector4(255, 0, 0, 255), "If you have a common FC tag you may wish to disable this.");

                ImGui.Text("Marker settings, ordered by priority:");

                foreach (var source in ServiceManager.NaviMapManager.SourceDataDict.OrderBy(x => x.Value.Priority))
                {
                    ImGui.PushID(source.Key);
                    var sourceDataLocal = new SourceData(source.Value);
                    if (ImGui.BeginListBox($"##list{source.Key}", new Vector2(-1, 210)))
                    {
                        ImGui.Text(source.Key);

                        var enabledLocal = sourceDataLocal.Enabled;
                        if (ImGui.Checkbox("Enabled", ref enabledLocal))
                        {
                            sourceDataLocal.Enabled = enabledLocal;
                        }

                        ImGui.SameLine(90);
                        
                        var tempPriority = sourceDataLocal.Priority;
                        var isPriorityError = false;

                        if (source.Key == FinderService.EveryoneKey)
                        {
                            ImGui.Text("\"Everyone\" is always the lowest priority");

                        }
                        else
                        {
                            
                            ImGui.PushItemWidth(100);

                            if (ImGui.InputInt("##priority", ref tempPriority, 1))
                            {
                                if (tempPriority < 1)
                                {
                                    tempPriority = 1;
                                }

                                if (tempPriority > 99)
                                {
                                    tempPriority = 99;
                                }
                                
                                sourceDataLocal.Priority = tempPriority;
                            }
                            isPriorityError = ServiceManager.NaviMapManager.SourceDataDict.Any(x => x.Value.Priority == tempPriority && x.Key != source.Key);

                            ImGui.PopItemWidth();
                            ImGui.SameLine();
                            if (isPriorityError)
                            {
                                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Priority {tempPriority} is already taken");
                            }
                            else
                            {
                                ImGui.Text("Priority, higher shows on top of lower");
                            }

                        }

                        var color = ImGui.ColorConvertU32ToFloat4(source.Value.Color);
                        ImGui.Text("Marker Colour. Click the colored square for a picker.");
                        if (ImGui.ColorEdit4("##color", ref color, ImGuiColorEditFlags.NoAlpha))
                        {

                            var uintColour = ImGui.ColorConvertFloat4ToU32(color);
                            sourceDataLocal.Color = uintColour;
                            sourceDataLocal.BorderValid = false;




                        }
                        var circleSizeLocal = source.Value.CircleSize;
                        if (ImGui.SliderInt("Circle Size", ref circleSizeLocal, 1, 20))
                        {
                            sourceDataLocal.CircleSize = circleSizeLocal;
                        }

                        var border = sourceDataLocal.ShowBorder;
                        if (ImGui.Checkbox("Show Border", ref border))
                        {
                            sourceDataLocal.ShowBorder = border;
                        }

                        var darkeningAmount = sourceDataLocal.BorderDarkeningAmount;
                        if (ImGui.SliderFloat("Border Brightness", ref darkeningAmount, 0.0f, 2f))
                        {
                            sourceDataLocal.BorderDarkeningAmount = darkeningAmount;
                            sourceDataLocal.BorderValid = false;
                        }

                        var borderRadius = sourceDataLocal.BorderRadius;
                        if (ImGui.SliderInt("Border Radius", ref borderRadius, 1, 10))
                        {
                            sourceDataLocal.BorderRadius = borderRadius;
                        }

                        ServiceManager.NaviMapManager.SourceDataDict.AddOrUpdate(source.Key, sourceDataLocal, (_, _) => sourceDataLocal);
                        if (sourceDataLocal != ServiceManager.Configuration.SourceConfigs[source.Key])
                        {
                            if (!isPriorityError)
                            {
                                ServiceManager.Configuration.SourceConfigs[source.Key] = sourceDataLocal;

                            }
                            ServiceManager.Configuration.Save();
                        }
                        ImGui.EndListBox();

                    }

                    ImGui.PopID();

                }

            }
            ImGui.End();


        }
        

    }
}
