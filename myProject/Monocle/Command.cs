using System;

namespace Monocle
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Command : Attribute
    {
        public string Name;
        public string Help;

        public Command(string name, string help)
        {
            Name = name;
            Help = help;
        }
    }
}
