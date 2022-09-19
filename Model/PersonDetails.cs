using System;

namespace MiniMappingway.Model
{
    public class PersonDetails
    {
        public uint Id { get; }

        public string Name { get; }

        public PersonDetails(string name, uint id)
        {
            Id = id;
            Name = name;
        }
    }
}
