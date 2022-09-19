using System;

namespace MiniMappingway.Model
{
    public class PersonDetails
    {
        public IntPtr Ptr { get; }

        public string Name { get; }

        public PersonDetails(string name, IntPtr ptr)
        {
            Ptr = ptr;
            Name = name;
        }
    }
}
