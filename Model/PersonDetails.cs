using System;

namespace MiniMappingway.Model
{
    public class PersonDetails
    {
        public string Name { get; set; }

        public uint Id { get; set; }

        public string SourceName { get; set; }

        public IntPtr Ptr { get; set; }

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
