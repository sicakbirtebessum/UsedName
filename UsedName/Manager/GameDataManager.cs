using Dalamud.Hooking;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UsedName.Structs;
using static FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyCommonList.CharacterData;

namespace UsedName.Manager
{
    internal class GameDataManager : IDisposable
    {
        internal unsafe delegate void GetSocialListDelegate(uint targetId, IntPtr SocialList);
        internal Hook<GetSocialListDelegate> GetSocialListHook { get; set; } = null!;
        public GameDataManager()
        {
            if (Service.Scanner.TryScanText("48 89 5c 24 ?? 56 48 ?? ?? ?? 48 ?? ?? ?? ?? ?? ?? 48 ?? ?? e8 ?? ?? ?? ?? 48 ?? ?? 48 ?? ?? 0f 84 ?? ?? ?? ?? 0f", out var ptr0))
            {
                this.GetSocialListHook = Service.GameInteropProvider.HookFromAddress<GetSocialListDelegate>(ptr0, GetSocialListDetour);
            }
            if (Service.Configuration.EnableAutoUpdate)
                this.GetSocialListHook?.Enable();

            // first time
            //if (Service.Configuration.playersNameList.Count <= 0)
            //{
            //    this.UpdateDataFromXivCommon();
            //}
        }
        public void Dispose()
        {
            this.GetSocialListHook?.Disable();
            this.GetSocialListHook?.Dispose();
        }


        private void GetSocialListDetour(uint targetId, IntPtr data)
        {
#if DEBUG
            int startIndex = 0x10;
            int endIndex = startIndex + 0x70;
            var bytes = new byte[endIndex - startIndex];
            Marshal.Copy(data + startIndex, bytes, 0, bytes.Length);
            Service.PluginLog.Debug($"GetSocialListDetour 1: {BitConverter.ToString(bytes)}");
            startIndex = endIndex;
            endIndex = startIndex + 0x70;
            Marshal.Copy(data + startIndex, bytes, 0, bytes.Length);
            Service.PluginLog.Debug($"GetSocialListDetour 2: {BitConverter.ToString(bytes)}");
            startIndex = endIndex;
            endIndex = startIndex + 0x70;
            Marshal.Copy(data + startIndex, bytes, 0, bytes.Length);
            Service.PluginLog.Debug($"GetSocialListDetour 3: {BitConverter.ToString(bytes)}");
            //int startIndex = 0;
            //int endIndex = startIndex + 0x70 + 50;
            //var bytes = new byte[endIndex - startIndex];
            //Marshal.Copy(data + startIndex, bytes, 0, bytes.Length);
            //Service.PluginLog.Debug($"GetSocialListDetour: {BitConverter.ToString(bytes)}");
#endif
            SocialListResult socialList;
            try
            {
                socialList = Marshal.PtrToStructure<SocialListResult>(data);
            }
            catch (Exception)
            {
                this.GetSocialListHook?.Original(targetId, data);
                return;
            }

            this.GetSocialListHook?.Original(targetId, data);
            var listType = socialList.ListType;
            Service.PluginLog.Debug($"CommunityID:{socialList.CommunityID:X}:{socialList.Index}:{socialList.NextIndex}:{socialList.RequestKey}:{socialList.RequestParam}");
            Service.PluginLog.Debug($"ListType:{socialList.ListType}");
            // type: 1 = Party List; 2 = Friend List; 3 = Linkshells 4 = Player Search;
            // 5 = Members Online and on Home World; 6 = company member; 7 = Application of Company;
            // 10 = Mentor;11 = New Adventurer/Returner; 

            if (listType is null)
            {
#if DEBUG
                var hint = $"UsedName: Find Unknown type: {socialList.ListTypeByte}, please contact developer";
                Service.Chat.Print(hint);
                foreach (var character in socialList.CharacterEntries)
                {
                    hint += $"\n{character.CharacterID:X}:{character.CharacterName}";
                    //hint += $"\n{character.CharacterID:X}:{character.CharacterName}";
                }
                Service.PluginLog.Warning(hint);
#endif
                return;
            }
            bool recordAllPlayersInList = (listType == ListType.PartyList && Service.Configuration.UpdateFromPartyList) ||
                                          (listType == ListType.FriendList && Service.Configuration.UpdateFromFriendList) ||
                                          (listType == ListType.CompanyMember && Service.Configuration.UpdateFromCompanyMember) ||
                                          (listType == ListType.PlayerSearch && Service.Configuration.UpdateFromPlayerSearch);

            var subList = new List<String>(Service.PlayersNamesManager.Subscriptions);
            // if (!recordAllPlayersInList && subList.Count <= 0)
            //     return;
            var result = new Dictionary<ulong, string>();
            // var notInGameFriendListFriend = Service.PlayersNamesManager.NotInGameFriendListFriend();
            foreach (var c in socialList.CharacterEntries)
            {
#if DEBUG
                Service.PluginLog.Debug($"{c.CharacterName} {c.FcTag} {c.CharacterID} " +
                    $"AccId:{c.AccountId} " +
                    $"Job:{c.CurrentClassJob.GameData.Value.Abbreviation} ({c.CurrentClassJob.Id}) " +
                    $"JobLvl:{c.CurrentJobLevel} " +
                    $"HWorld:{c.HomeWorld.GameData.Value.Name} ({c.HomeWorld.Id}) " +
                    $"HasComment:{(c.HasSearchComment != 0).ToString()} " +
                    $"GC:{(FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany)c.GrandCompanyId} " +
                    $"Languages:{c.Languages} " +
                    $"ClientLanguage:{c.ClientLanguage} " +
                    $"Statuses:{(InfoProxyCommonList.CharacterData.OnlineStatus)c.OnlineStatusBytes} " +
                    $"Territory:{c.TerritoryType?.GameData.Value.PlaceName.Value.Name}");
#endif
                if (c.CharacterID == 0 ||
                    c.CharacterID == Service.ClientState.LocalContentId ||
                    c.CharacterName.IsNullOrEmpty())
                    continue;

                if (subList.RemoveAll(x => x == c.CharacterName) > 0 ||
                    // notInGameFriendListFriend.Exists(id => id == c.CharacterID) ||
                    Service.Configuration.playersNameList.ContainsKey(c.CharacterID) ||
                    recordAllPlayersInList)
                {
                    if (!result.TryAdd(c.CharacterID, c.CharacterName))
                    {
                        Service.PluginLog.Warning($"Duplicate entry {c.CharacterID} {c.CharacterName}");
                    }
                }
                //if (c.ContentId == 0 ||
                //    c.ContentId == Service.ClientState.LocalContentId ||
                //    c.NameString.IsNullOrEmpty())
                //    continue;

                //if (subList.RemoveAll(x => x == c.NameString) > 0 ||
                //    // notInGameFriendListFriend.Exists(id => id == c.CharacterID) ||
                //    Service.Configuration.playersNameList.ContainsKey(c.ContentId) ||
                //    recordAllPlayersInList)
                //{
                //    if (!result.TryAdd(c.ContentId, c.NameString))
                //    {
                //        Service.PluginLog.Warning($"Duplicate entry {c.ContentId} {c.NameString}");
                //    }
                //}

            }
            // show different of subList
            var difference = subList.Except(Service.PlayersNamesManager.Subscriptions).ToList();
            if (result.Count <= 0)
                return;
            Service.PlayersNamesManager.UpdatePlayerNames(result, difference, false);

        }


        //internal IDictionary<ulong, string>? GetDataFromXivCommon()
        //{
        //    var friendList = Service.Common.Functions.FriendList.List;
        //    if (friendList.Count <= 0)
        //    {
        //        return null;
        //    }
        //    var friendListEnumerator = Service.Common.Functions.FriendList.List.GetEnumerator();
        //    IDictionary<ulong, string> currentPlayersList = new Dictionary<ulong, string>();
        //    while (friendListEnumerator.MoveNext())
        //    {
        //        var player = friendListEnumerator.Current;
        //        var contentId = player.ContentId;
        //        var name = player.Name.ToString();
        //        try
        //        {
        //            currentPlayersList.Add(contentId, name);
        //        }
        //        catch (ArgumentException e)
        //        {
        //            Service.PluginLog.Warning($"{e}");
        //            Service.PluginLog.Warning($"Unknown problem at {name}-{contentId}");
        //            Service.Chat.PrintError(Service.Loc.Localize($"Update Player List Fail\nMay cause by incompatible version of XivCommon\nPlease contact to developer"));
        //            return null;
        //        }

        //    }
        //    return currentPlayersList;
        //}
        //internal void UpdateDataFromXivCommon()
        //{
        //    var currentPlayersList = GetDataFromXivCommon();
        //    if(currentPlayersList is { Count: > 0 })
        //        Service.PlayersNamesManager.UpdatePlayerNames(currentPlayersList, new List<String>());
        //}
        /*
        public XivCommon.Functions.FriendList.FriendListEntry GetPlayerByNameFromFriendList(string name)
        {
            var friendList = Service.Common.Functions.FriendList.List.GetEnumerator();
            while (friendList.MoveNext())
            {
                var player = friendList.Current;
                if (player.Name.ToString().Equals(name))
                {
                    return player;
                }
            }
            return new XivCommon.Functions.FriendList.FriendListEntry();
        }
        */

    }
}
