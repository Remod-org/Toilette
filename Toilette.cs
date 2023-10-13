#region License (GPL v2)
/*
    Toilet spawner
    Copyright (c) RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; version 2
    of the License only.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License (GPL v2)
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Toilette", "RFC1920", "1.0.2")]
    [Description("Spawn a toilet!")]
    internal class Toilette : RustPlugin
    {
        private const string prefabA = "assets/bundled/prefabs/static/toilet_a.static.prefab";
        private const string prefabB = "assets/bundled/prefabs/static/toilet_b.static.prefab";
        private const string permUse = "toilette.use";
        private Dictionary<ulong, List<Toilet>> toilettes = new Dictionary<ulong, List<Toilet>>();
        private Dictionary<NetworkableId, ulong> toiletToOwner = new Dictionary<NetworkableId, ulong>();
        private ConfigData configData;

        [PluginReference]
        private readonly Plugin Friends, Clans, RustIO;

        public class Toilet
        {
            public NetworkableId Id;
            public ulong OwnerId;
            public string Name;
            public string prefab;
            public Vector3 position;
            public Quaternion rotation;
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permUse, this);
            AddCovalenceCommand("toil", "cmdToil");
            LoadConfigVariables();
            LoadData();
            RespawnToilets();
        }

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["noperm"] = "No toilets for you!",
                ["notours"] = "Invalid toilet - not in database.",
                ["nobuild"] = "Can't build here.",
                ["notc"] = "Must be in range of an authed TC.",
                ["atlimit"] = "No more toilets for you!"
            }, this);
        }

        private void Unload() => KillToilets();

        private void RespawnToilets()
        {
            foreach (KeyValuePair<ulong, List<Toilet>> toilets in new Dictionary<ulong, List<Toilet>>(toilettes))
            {
                foreach (Toilet toilet in toilets.Value)
                {
                    GameObject prefab = SpawnPrefab(toilet.prefab, toilet.position, toilet.rotation, true);
                    if (prefab == null) continue;
                    BaseEntity entity = prefab?.GetComponent<BaseEntity>();
                    entity?.Spawn();
                    entity.OwnerID = toilets.Key;

                    // Update with new net.ID
                    Toilet find = toilettes[toilets.Key].First(x => x.Id == toilet.Id);
                    if (find != null) find.Id = entity.net.ID;

                    toiletToOwner.Add(entity.net.ID, toilet.OwnerId);
                }
            }
            SaveData();
        }

        private void KillToilets(bool all = false, ulong userid = 0)
        {
            if (all)
            {
                toilettes.Clear();
                SaveData();
                return;
            }

            foreach (KeyValuePair<ulong, List<Toilet>> toiletA in toilettes)
            {
                if (userid > 0 && toiletA.Key != userid) continue;
                foreach (Toilet toilet in toiletA.Value)
                {
                    BaseNetworkable t = BaseNetworkable.serverEntities.Find(toilet.Id);
                    if (t == null) continue;
                    t.Kill();
                    toiletToOwner.Remove(toilet.Id);
                }
            }
            SaveData();
        }

        [Command("toil")]
        private void cmdToil(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (configData.Options.RequirePermission && !iplayer.HasPermission(permUse) && !player.IsAdmin) { Message(iplayer, "noperm"); return; }

            string pre = prefabA;
            if (args.Length == 1 && string.Equals(args[0], "b", StringComparison.OrdinalIgnoreCase))
            {
                pre = prefabB;
            }
            if (args.Length == 1 && string.Equals(args[0], "remove", StringComparison.OrdinalIgnoreCase))
            {
                BaseEntity target = RaycastAll<BaseEntity>(player.eyes.HeadRay()) as BaseEntity;
                if (target == null) return;
                if (!target.ShortPrefabName.Contains("toilet")) return;

                // Defined toilets check
                Toilet find = toilettes[player.userID].First(x => x.Id == target.net.ID);
                if (find == null)
                {
                    Message(iplayer, "notours");
                    return;
                }

                // Friend check
                object res = IsFriend(player.userID, target.OwnerID);
                bool friend = false;
                if (res is bool && (bool)res)
                {
                    friend = true;
                }

                if (!friend && !player.IsAdmin) return;
                target.Kill();
                return;
            }
            if (player.IsAdmin && args.Length == 1 && string.Equals(args[0], "killall", StringComparison.OrdinalIgnoreCase))
            {
                KillToilets(true);
                return;
            }
            if (player.IsAdmin && args.Length == 2 && string.Equals(args[0], "kill", StringComparison.OrdinalIgnoreCase))
            {
                BasePlayer pl = BasePlayer.FindAwakeOrSleeping(args[1]);
                if (pl != null)
                {
                    KillToilets(false, pl.userID);
                }
                return;
            }

            // Building Privilege checks
            if (!player.CanBuild() && !player.IsAdmin)
            {
                Message(iplayer, "nobuild");
                return;
            }
            if (player.GetBuildingPrivilege() == null && configData.Options.RequireTC)
            {
                Message(iplayer, "notc");
                return;
            }

            // Limit check
            if (toilettes.ContainsKey(player.userID) && configData.Options.tLimit > 0 && toilettes[player.userID].Count >= configData.Options.tLimit)
            {
                Message(iplayer, "atlimit");
                return;
            }

            Quaternion rotation = player.GetNetworkRotation();
            Vector3 forward = rotation * Vector3.forward;
            // Make straight perpendicular to up axis so we don't spawn into ground or above player's head.
            Vector3 straight = Vector3.Cross(Vector3.Cross(Vector3.up, forward), Vector3.up).normalized;
            Vector3 position = player.transform.position + (straight * 5f);
            //position.y = TerrainMeta.HeightMap.GetHeight(position);
            GameObject prefab = SpawnPrefab(pre, position + new Vector3(2, 0, 0), rotation, true);

            if (prefab == null) return;

            BaseEntity entity = prefab?.GetComponent<BaseEntity>();
            entity?.Spawn();
            entity.OwnerID = player.userID;

            if (!toilettes.ContainsKey(player.userID))
            {
                toilettes[player.userID] = new List<Toilet>();
            }

            toilettes[player.userID].Add(new Toilet()
            {
                OwnerId = player.userID,
                Id = entity.net.ID,
                Name = player.name,
                prefab = pre,
                position = position,
                rotation = rotation
            });
            toiletToOwner.Add(entity.net.ID, player.userID);
            SaveData();
        }

        private object RaycastAll<T>(Ray ray) where T : BaseEntity
        {
            RaycastHit[] hits = Physics.RaycastAll(ray);
            GamePhysics.Sort(hits);
            const float distance = 6f;
            object target = false;
            foreach (RaycastHit hit in hits)
            {
                BaseEntity ent = hit.GetEntity();
                if (ent is T && hit.distance < distance)
                {
                    target = ent;
                    break;
                }
            }

            return target;
        }

        // From Build.cs
        private static GameObject SpawnPrefab(string prefabname, Vector3 pos, Quaternion angles, bool active)
        {
            GameObject prefab = GameManager.server.CreatePrefab(prefabname, pos, angles, active);

            if (prefab == null) return null;

            prefab.transform.position = pos;
            prefab.transform.rotation = angles;
            prefab.gameObject.SetActive(active);

            return prefab;
        }

        #region config
        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                Options = new Options()
                {
                    RequirePermission = true,
                    RequireTC = true,
                    useClans = false,
                    useFriends = false,
                    useTeams = false,
                    tLimit = 5
                },
                Version = Version
            };
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        public class ConfigData
        {
            public Options Options = new Options();
            public VersionNumber Version;
        }

        public class Options
        {
            public bool RequirePermission;
            public bool RequireTC;
            public bool useFriends;
            public bool useClans;
            public bool useTeams;

            public float tLimit;
        }

        private void DoLog(string message, int indent = 0)
        {
            Debug.LogWarning("".PadLeft(indent, ' ') + message);
        }

        #endregion config
        private object IsFriend(ulong playerid, ulong ownerid)
        {
            if (configData.Options.useFriends && Friends != null)
            {
                object fr = Friends?.CallHook("AreFriends", playerid, ownerid);
                if (fr != null && (bool)fr)
                {
                    DoLog($"Friends plugin reports that {playerid} and {ownerid} are friends.");
                    return true;
                }
            }
            if (configData.Options.useClans && Clans != null)
            {
                string playerclan = (string)Clans?.CallHook("GetClanOf", playerid);
                string ownerclan = (string)Clans?.CallHook("GetClanOf", ownerid);
                if (playerclan != null && ownerclan != null && playerclan == ownerclan)
                {
                    DoLog($"Clans plugin reports that {playerid} and {ownerid} are clanmates.");
                    return true;
                }
                //object isMember = Clans?.Call("IsClanMember", ownerid.ToString(), playerid.ToString());
                //if (isMember != null)
                //{
                //    DoLog($"Clans plugin reports that {playerid} and {ownerid} are clanmates.");
                //    return (bool)isMember;
                //}
            }
            if (configData.Options.useTeams)
            {
                RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindPlayersTeam(playerid);
                if (playerTeam?.members.Contains(ownerid) == true)
                {
                    DoLog($"Rust teams reports that {playerid} and {ownerid} are on the same team.");
                    return true;
                }
            }
            return false;
        }

        private void LoadData()
        {
            toilettes = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<Toilet>>>(Name + "/toilettes");
            foreach (KeyValuePair<ulong, List<Toilet>> toi  in toilettes)
            {
                foreach (Toilet toilet in toi.Value)
                {
                    toiletToOwner.Add(toilet.Id, toi.Key);
                }
            }
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/toilettes", toilettes);
        }
    }
}
