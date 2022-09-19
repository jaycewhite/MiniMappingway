using Dalamud.Plugin.Ipc;
using MiniMappingway.Model;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MiniMappingway.Api
{
    /// <summary>
    /// For integration over IPC.
    /// Each method has comments explaining what the arguments are below.
    /// General flowchart for usage is:
    /// GetVersion (to see if plugin active/check compatibility),
    /// RegisterOrUpdateSourceVec/RegisterOrUpdateSourceUint to register as a source of markers,
    /// AddPerson/OverwriteList to add the people you wish to show, whether in bulk or one by one,
    /// RemovePersonByName/RemovePersonByUint as needed,
    /// RemoveSourceAndPeople to remove your plugin as a source, and remove the list containing the people.
    /// 
    /// NB: people will be removed from the list automatically if they leave the ObjectTable.
    /// Trying to remove a person that isn't in the list is safe, and will return false.
    /// Trying to add a person that is already in the list is safe, and will return false.
    /// </summary>
    public class ApiController : IDisposable
    {
        private const int apiVersionMajor = 1;
        private const int apiVersionMinor = 0;

        ICallGateProvider<Tuple<int, int>> GetVersionIPC = ServiceManager.DalamudPluginInterface.GetIpcProvider<Tuple<int, int>>("MiniMappingway.CheckVersion");

        ICallGateProvider<string, Vector4, bool> RegisterOrUpdateSourceVecIPC = ServiceManager.DalamudPluginInterface.GetIpcProvider<string,Vector4,bool>("MiniMappingway.RegisterOrUpdateSourceVec");

        ICallGateProvider<string, uint, bool> RegisterOrUpdateSourceUintIPC = ServiceManager.DalamudPluginInterface.GetIpcProvider<string,uint,bool>("MiniMappingway.RegisterOrUpdateSourceUint");

        ICallGateProvider<string, List<PersonDetails>, bool> OverwriteListIPC = ServiceManager.DalamudPluginInterface.GetIpcProvider<string, List<PersonDetails>, bool>("MiniMappingway.OverwriteList");

        ICallGateProvider<string, string, uint, bool> AddPersonIPC = ServiceManager.DalamudPluginInterface.GetIpcProvider<string, string, uint, bool>("MiniMappingway.AddPerson");

        ICallGateProvider<string, string, bool> RemovePersonByNameIPC = ServiceManager.DalamudPluginInterface.GetIpcProvider<string, string, bool>("MiniMappingway.RemovePersonByName");

        ICallGateProvider<string, uint, bool> RemovePersonByIdIPC = ServiceManager.DalamudPluginInterface.GetIpcProvider<string, uint, bool>("MiniMappingway.RemovePersonByUint");

        ICallGateProvider<string, bool> RemoveSourceAndPeopleIPC = ServiceManager.DalamudPluginInterface.GetIpcProvider<string, bool>("MiniMappingway.RemoveSourceAndPeople");

        public ApiController()
        {
            GetVersionIPC.RegisterFunc(CheckVersion);
            RegisterOrUpdateSourceVecIPC.RegisterFunc(RegisterOrUpdateSource);
            RegisterOrUpdateSourceUintIPC.RegisterFunc(RegisterOrUpdateSource);
            OverwriteListIPC.RegisterFunc(OverwriteList);
            AddPersonIPC.RegisterFunc(AddPerson);
            RemovePersonByNameIPC.RegisterFunc(RemovePerson);
            RemovePersonByIdIPC.RegisterFunc(RemovePerson);
            RemoveSourceAndPeopleIPC.RegisterFunc(RemoveSourceAndPeople);
        }

        /// <summary>
        /// Get Version
        /// </summary>
        /// <returns>A tuple of Major and Minor version numbers</returns>
        private Tuple<int,int> CheckVersion()
        {
            return new Tuple<int, int>(apiVersionMajor, apiVersionMinor);
        }

        /// <summary>
        /// Register as a source, or update source data (currently just marker color)
        /// </summary>
        /// <param name="sourceName">Source name string, should be unique to your plugin</param>
        /// <param name="color">Color for markers in vector4 format</param>
        /// <returns>Success boolean</returns>
        private bool RegisterOrUpdateSource(string sourceName, Vector4 color)
        {
            return ServiceManager.NaviMapManager.AddOrUpdateSource(sourceName, color);
        }

        /// <summary>
        /// Register as a source, or update source data (currently just marker color)
        /// </summary>
        /// <param name="sourceName">Source name string, should be unique to your plugin</param>
        /// <param name="color">Color for markers in uint format</param>
        /// <returns>Success boolean</returns>
        private bool RegisterOrUpdateSource(string sourceName, uint color)
        {
            return ServiceManager.NaviMapManager.AddOrUpdateSource(sourceName, color);
        }

        /// <summary>
        /// Overwrite all people in list for source
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="list">List of people you wish to replace with</param>
        /// <returns>Success boolean</returns>
        private bool OverwriteList(string sourceName, List<PersonDetails> list)
        {
            return ServiceManager.NaviMapManager.OverwriteWholeBag(sourceName, list);
        }

        /// <summary>
        /// Add person to list for source
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="name">Name of person as seen in ObjectTable</param>
        /// <param name="id">Id of person in ObjectTable</param>
        /// <returns>Success boolean</returns>
        private bool AddPerson(string sourceName, string name, uint id)
        {
            return ServiceManager.NaviMapManager.AddToBag(sourceName, new PersonDetails(name, id));
        }

        /// <summary>
        /// Remove person from list for source by name
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="Name">Name of person as seen in ObjectTable</param>
        /// <returns>Success boolean</returns>
        private bool RemovePerson(string sourceName, string Name)
        {
            return ServiceManager.NaviMapManager.RemoveFromBag(sourceName, Name);
        }

        /// <summary>
        /// Remove person from list for source by uint
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="Id">Id of person in ObjectTable</param>
        /// <returns>Success boolean</returns>
        private bool RemovePerson(string sourceName, uint id)
        {
            return ServiceManager.NaviMapManager.RemoveFromBag(sourceName, id);
        }

        /// <summary>
        /// Completely remove source and all associated people
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <returns>Success boolean</returns>
        private bool RemoveSourceAndPeople(string sourceName)
        {
            return ServiceManager.NaviMapManager.RemoveSourceAndPeople(sourceName);
        }

        public void Dispose()
        {
            GetVersionIPC.UnregisterFunc();
            RegisterOrUpdateSourceVecIPC.UnregisterFunc();
            RegisterOrUpdateSourceUintIPC.UnregisterFunc();
            OverwriteListIPC.UnregisterFunc();
            AddPersonIPC.UnregisterFunc();
            RemovePersonByNameIPC.UnregisterFunc();
            RemovePersonByIdIPC.UnregisterFunc();
            RemoveSourceAndPeopleIPC.UnregisterFunc();
        }
    }
}
