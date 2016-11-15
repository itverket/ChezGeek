using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChezGeek.Common.Actors;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Attributes;
using ChezGeek.Team1;
using ChezGeek.Team2;
using ChezGeek.Team3;
using ChezGeek.Team6;
using ChezGeek.TeamRed;
using ChezGeek.UI.ViewModels;
using ChezGeek.TeamTeal;
using ChezGeek.TeamYellow;

namespace ChezGeek.UI
{
    public static class ChessPlayerHelper
    {
        public static IEnumerable<ChessPlayerModel> GetPlayers()
        {
            var assemblies = new List<Assembly>
            {
                typeof(ITeamPinkAssembly).Assembly,
                typeof(ITeamBlueAssembly).Assembly,
                typeof(ITeamPurpleAssembly).Assembly,
                typeof(ITeamRedAssembly).Assembly,
                typeof(ITeamYellowAssembly).Assembly,
                typeof(ITeamBrownAssembly).Assembly,
                typeof(ITeamTealAssembly).Assembly,
                typeof(OnePlyerActor).Assembly
            };

            var players = new List<ChessPlayerModel>();

            players.AddRange(assemblies.SelectMany(GetPlayersFromAssembly).ToList());
            return players;
        }

        private static IEnumerable<ChessPlayerModel> GetPlayersFromAssembly(Assembly assembly)
        {
            var playerActors = assembly.GetTypes().Where(type => type.GetCustomAttribute<ChessPlayerAttribute>() != null);
            return playerActors.Select(x => new ChessPlayerModel
            {
                Type = x,
                Name = x.GetCustomAttribute<ChessPlayerAttribute>().PlayerName
            });
        }
    }
}
