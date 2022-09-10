using NetStone;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMappingway.Manager
{
    public class FcManager
    {
        LodestoneClient lodestoneClient = null!;
        bool clientInitialised = false;
        public bool fcMembersLoaded = false;

        string playerName = "";
        string playerWorld = "";
        string playerId = "";
        string fcId = "";

        DateTime? lastUpdated = null;

        public List<string> FcMemberNames = new();

        public FcManager()
        {
            InitialiseFcManager();
        }

        private async void InitialiseFcManager()
        {
            lodestoneClient = await LodestoneClient.GetClientAsync();
            clientInitialised = true;
        }

        public async void LoadFcMembers()
        {
            if (!clientInitialised || !ServiceManager.ClientState.IsLoggedIn)
            {
                return;
            }
            if(lastUpdated != null && DateTime.UtcNow < lastUpdated.Value.AddMinutes(30))
            {
                return;
            }
            if (ServiceManager.ClientState.LocalPlayer == null || ServiceManager.ClientState.LocalPlayer.HomeWorld.GameData == null)
            {
                return;
            }
            fcMembersLoaded = false;

            playerName = ServiceManager.ClientState.LocalPlayer.Name.ToString();
            playerWorld = ServiceManager.ClientState.LocalPlayer.HomeWorld.GameData.Name;


            var searchResult = await lodestoneClient.SearchCharacter(new NetStone.Search.Character.CharacterSearchQuery
            {
                CharacterName = playerName,
                World = playerWorld
            });

            var character = searchResult?.Results.FirstOrDefault(x => x.Name == playerName);

            if (character == null || string.IsNullOrEmpty(character.Id))
            {
                return;
            }
            playerId = character.Id;

            var playerPage = await lodestoneClient.GetCharacter(playerId);

            if (playerPage == null)
            {
                return;
            }

            fcId = playerPage.FreeCompany.Id;

            var fc = await lodestoneClient.GetFreeCompanyMembers(fcId);

            if (fc == null)
            {
                return;
            }

            if (!fc.HasResults)
            {
                return;
            }

            FcMemberNames.AddRange(fc.Members.Select(x =>
            {
                return x.Name;
            }));

            if (fc.NumPages > 1)
            {
                for (int i = 2; i < fc.NumPages - 1; i++)
                {
                    var fcMembers = await lodestoneClient.GetFreeCompanyMembers(fcId, i);
                    if (fcMembers.HasResults)
                    {
                        FcMemberNames.AddRange(fcMembers.Members.Select(x =>
                        {
                            return x.Name;
                        }));
                    }
                }
            }

            fcMembersLoaded = true;
            lastUpdated = DateTime.UtcNow;
        }


    }
}
