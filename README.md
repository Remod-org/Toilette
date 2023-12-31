# Toilette
Allow players to spawn two kinds of Rust toilet (A or B).

![](https://github.com/Remod-org/Toilette/blob/3665b7fbfa850fffebe204845f5143d84189b040/toilette.jpg)
Type A Toilet

CanBuild is checked to prevent spawning in range of a TC where the player is not authenticated, at monuments, etc.

## Command
 - toil {CHOICE} -- If /toil is run with no options, type A will be spawned.  If /toil b is run, type B will be spawned.
 - toil remove -- Removes the toilet they are looking at, if they have permission.  Admin can also remove a toilet.
 - toil kill PLAYERIDORNAME -- Admin only tool to delete all spawned toilets for a single userid/name.
 - toil killall -- Admin only tool to delete all spawned toilets.

## Configuration
```json
{
  "Options": {
    "RequirePermission": false,
    "BuildAnywhere": false,
    "RequireTC": false,
    "useFriends": false,
    "useClans": false,
    "useTeams": false,
    "tLimit": 5.0,
    "BlockMount": false
  },
  "VIPSettings": {
    "toilette.viplevel1": {
      "BuildAnywhere": false,
      "RequireTC": false,
      "tLimit": 10.0
    }
  },
  "Version": {
    "Major": 1,
    "Minor": 0,
    "Patch": 6
  }
}
```
 - `RequirePermission` -- If true, players must have the toilette.use permission.
 - `BuildAnywhere` -- If true, players can place toilets anywhere - best set to false and maybe provide for VIP perms.
 - `RequireTC` -- If true, players must be in range of a TC where they are authorized.
 - `useClans` -- Use various Clans plugins for determining relationships
 - `useFriends` -- Use various Friends plugins for determining relationships
 - `useTeams` -- Use Rust native teams for determining relationships
 - `tLimit` -- Per-player toilet limit
 - `BlockMount` -- If true, only the player or their friends can mount the toilet.
 - `VIPSettings` -- Optional, with one provided default.  Sets up a vip permission with different settings from default for BuildAnywhere, RequireTC, and tLimit.

## Permissions
 - toilette.use -- Required to use the /toil command.  Not required for admins.

