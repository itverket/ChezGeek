using System;

namespace ChezGeek.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ChessPlayerAttribute : Attribute
    {
        public ChessPlayerAttribute()
        {
        }

        public ChessPlayerAttribute(string playerName)
        {
            PlayerName = playerName;
        }

        public string PlayerName { get; set; }
    }
}
