# Toilette
Allow players to spawn two kinds of Rust toilet (A or B).

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
    "RequireTC": true,
    "useFriends": false,
    "useClans": false,
    "useTeams": false,
    "tLimit": 5.0
  },
  "Version": {
    "Major": 1,
    "Minor": 0,
    "Patch": 1
  }
}
```
 - `RequirePermission` -- If true, players must have the toilette.use permission.
 - `RequireTC` -- If true, players must be in range of a TC where they are authorized.
 - `useClans` -- Use various Clans plugins for determining relationships
 - `useFriends` -- Use various Friends plugins for determining relationships
 - `useTeams` -- Use Rust native teams for determining relationships
 - `tLimit` -- Per-player toilet limit

## Permissions
 - toilette.use -- Required to use the /toil command.  Not required for admins.

## TODO
 - Add Friends/Teams/Clans support
 - Add configs for useFriends, etc.
 - Optionally require permission to sit on someone else's toilet
 - Set limits and possibly VIP limits

