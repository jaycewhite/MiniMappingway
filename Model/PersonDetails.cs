using System;

namespace MiniMappingway.Model
{
    public class PersonDetails
    {
        public string Name { get; }

        public uint Id { get; }

        public string SourceName { get; }

        public IntPtr Ptr { get; }

        //public bool HighestPriority { get; set; }

        public PersonDetails(string name, uint id, string sourceName, IntPtr ptr)
        {
            Name = name;
            Id = id;
            SourceName = sourceName;
            Ptr = ptr;
        }
    }
}
