using System;

namespace AMF.Data
{
    internal class PropertyList
    {
        public string PropertyName { get; set; }

        public string ColumnName { get; set; }

        public Type PropertyType { get; set; }

        public string ParameterName { get; set; }
    }
}