using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChezGeek.Common.Actors;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Attributes;
using ChezGeek.Team3;
using ChezGeek.UI.ViewModels;

namespace ChezGeek.UI
{
    public static class ChessPlayerHelper
    {
        public static IEnumerable<ChessPlayerModel> GetPlayers()
        {
            var assemblies = new List<Assembly>
            {
                typeof(ITeamPurpleAssembly).Assembly,
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
