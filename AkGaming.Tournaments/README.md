# AkGaming.Tournaments

## Description

Tournament management module for AkGaming e.V.

## Vision
The idea is to build a platform that is both specifically designed for the LoL tournament and open for other types of tournaments
### Specifics
- Team Captains can register their teams. Optimally, durign signup, it is enforced that he is on the discord
- Ranks are automatically checked during signup
- During signup, captains have to agree to out fair play policy
- Teams are registered by captains and can be used for multiple tournaments. Tournaments have teams as participants, single player tournaments are realized via single player teams

## Architecture
- We'll use a dotnet backend and a Blazor frontend
- Tournament is the main entity, has settings, teams, matches etc.
- Ak Gaming identity is used for authentication. Admins and captains have to be registered users. Players can be can be invited to get access to a team (so captains dont own a team, they just have owner access to it, whcih can also be moved to another user)