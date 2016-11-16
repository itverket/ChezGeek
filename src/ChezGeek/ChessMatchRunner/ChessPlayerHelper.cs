using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Attributes;

namespace ChessMatchRunner
{
    public static class ChessPlayerHelper
    {
        public static IEnumerable<Type> GetPlayers()
        {
            var allAssemblies = new List<Assembly>();
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


            var assemblies = new List<Assembly>();

            assemblies.AddRange(Directory.GetFiles(path, "*.dll").Select(Assembly.LoadFile));

            var players = new List<Type>();
            foreach (var assembly in assemblies)
            {
                Type[] assemblyTypes = assembly.GetTypes();
                if (assemblyTypes.Any(t => t.IsInterface && t.Name == "IAmAChessPlayer"))
                    players.AddRange(GetPlayersFromAssembly(assembly));
            }

            return players;
        }

        private static IEnumerable<Type> GetPlayersFromAssembly(Assembly assembly)
        {
            var playerActors = assembly.GetTypes().Where(type => type.GetCustomAttribute<ChessPlayerAttribute>() != null);
            return playerActors;
        }
    }
}
