using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin.Ipc;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingway.Service.Interface;

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
        private readonly ISourceService _sourceService;
        private readonly IPersonService _personService;

        private const int ApiVersionMajor = 1;
        private const int ApiVersionMinor = 0;

        private readonly ICallGateProvider<Tuple<int, int>> _getVersionIpc = ServiceManager.DalamudPluginInterface.GetIpcProvider<Tuple<int, int>>("MiniMappingway.CheckVersion");

        private readonly ICallGateProvider<string, Vector4, bool> _registerOrUpdateSourceVecIpc = ServiceManager.DalamudPluginInterface.GetIpcProvider<string,Vector4,bool>("MiniMappingway.RegisterOrUpdateSourceVec");

        private readonly ICallGateProvider<string, uint, bool> _registerOrUpdateSourceUintIpc = ServiceManager.DalamudPluginInterface.GetIpcProvider<string,uint,bool>("MiniMappingway.RegisterOrUpdateSourceUint");

        private readonly ICallGateProvider<string, List<PersonDetails>, bool> _overwriteListIpc = ServiceManager.DalamudPluginInterface.GetIpcProvider<string, List<PersonDetails>, bool>("MiniMappingway.OverwriteList");

        private readonly ICallGateProvider<string, string, uint, bool> _addPersonIpc = ServiceManager.DalamudPluginInterface.GetIpcProvider<string, string, uint, bool>("MiniMappingway.AddPerson");

        private readonly ICallGateProvider<string, string, bool> _removePersonByNameIpc = ServiceManager.DalamudPluginInterface.GetIpcProvider<string, string, bool>("MiniMappingway.RemovePersonByName");

        private readonly ICallGateProvider<uint, string, bool> _removePersonByIdIpc = ServiceManager.DalamudPluginInterface.GetIpcProvider<uint, string, bool>("MiniMappingway.RemovePersonByUint");

        private readonly ICallGateProvider<string, bool> _removeSourceAndPeopleIpc = ServiceManager.DalamudPluginInterface.GetIpcProvider<string, bool>("MiniMappingway.RemoveSourceAndPeople");

        public ApiController(IPersonService personService, ISourceService sourceService)
        {
            _getVersionIpc.RegisterFunc(CheckVersion);
            _registerOrUpdateSourceVecIpc.RegisterFunc(RegisterOrUpdateSource);
            _registerOrUpdateSourceUintIpc.RegisterFunc(RegisterOrUpdateSource);
            _overwriteListIpc.RegisterFunc(OverwriteList);
            _addPersonIpc.RegisterFunc(AddPerson);
            _removePersonByNameIpc.RegisterFunc(RemovePerson);
            _removePersonByIdIpc.RegisterFunc(RemovePerson);
            _removeSourceAndPeopleIpc.RegisterFunc(RemoveSourceAndPeople);

            _personService = personService;
            _sourceService = sourceService;
        }

        /// <summary>
        /// Get Version
        /// </summary>
        /// <returns>A tuple of Major and Minor version numbers</returns>
        private Tuple<int,int> CheckVersion()
        {
            return new Tuple<int, int>(ApiVersionMajor, ApiVersionMinor);
        }

        /// <summary>
        /// Register as a source, or update source data (currently just marker color)
        /// </summary>
        /// <param name="sourceName">Source name string, should be unique to your plugin and human readable, will show in settings pane</param>
        /// <param name="color">Color for markers in vector4 format - this can be overwritten by the user in settings</param>
        /// <returns>Success boolean</returns>
        private bool RegisterOrUpdateSource(string sourceName, Vector4 color)
        {
            return _sourceService.AddOrUpdateSource(sourceName, color);
        }

        /// <summary>
        /// Register as a source, or update source data (currently just marker color)
        /// </summary>
        /// <param name="sourceName">Source name string, should be unique to your plugin</param>
        /// <param name="color">Color for markers in uint format - this can be overwritten by the user in settings</param>
        /// <returns>Success boolean</returns>
        private bool RegisterOrUpdateSource(string sourceName, uint color)
        {
            return _sourceService.AddOrUpdateSource(sourceName, color);
        }

        /// <summary>
        /// Overwrite all people in list for source
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="list">List of people you wish to replace with</param>
        /// <returns>Success boolean</returns>
        private bool OverwriteList(string sourceName, List<PersonDetails> list)
        {
            return _personService.OverwriteWholeBag(sourceName, list);
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
            var person = ServiceManager.ObjectTable.SearchById(id);
            if (person == null)
            {
                return false;
            }
            return _personService.AddToBag(new PersonDetails(name, id, sourceName,person.Address));
        }

        /// <summary>
        /// Remove person from list for source by name
        /// </summary>
        /// <param name="name">Name of person as seen in ObjectTable</param>
        /// <param name="sourceName">Name of source</param>
        /// <returns>Success boolean</returns>
        private bool RemovePerson(string name, string sourceName)
        {
            return _personService.RemoveFromBag(name, sourceName);
        }

        /// <summary>
        /// Remove person from list for source by uint
        /// </summary>
        /// <param name="id">Id of person in ObjectTable</param>
        /// <param name="sourceName">Name of source</param>
        /// <returns>Success boolean</returns>
        private bool RemovePerson(uint id, string sourceName)
        {
            return _personService.RemoveFromBag(id, sourceName);
        }

        /// <summary>
        /// Completely remove source and all associated people
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <returns>Success boolean</returns>
        private bool RemoveSourceAndPeople(string sourceName)
        {
            return _sourceService.RemoveSourceAndPeople(sourceName);
        }

        public void Dispose()
        {
            _getVersionIpc.UnregisterFunc();
            _registerOrUpdateSourceVecIpc.UnregisterFunc();
            _registerOrUpdateSourceUintIpc.UnregisterFunc();
            _overwriteListIpc.UnregisterFunc();
            _addPersonIpc.UnregisterFunc();
            _removePersonByNameIpc.UnregisterFunc();
            _removePersonByIdIpc.UnregisterFunc();
            _removeSourceAndPeopleIpc.UnregisterFunc();
        }
    }
}
