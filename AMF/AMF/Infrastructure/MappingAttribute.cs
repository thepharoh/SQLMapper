using System;

namespace AMF.Infrastructure
{
    public class MappingAttribute : Attribute
    {
        public string ColumnName { get; set; }

        public ExclusionTypes Exclude { get; set; }

        public string ParameterName { get; set; }
    }
}