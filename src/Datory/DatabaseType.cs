using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace Datory
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DatabaseType
    {
        [Display(Name = "MySql")]
        MySql,
        [Display(Name = "SqlServer")]
        SqlServer,
        [Display(Name = "PostgreSql")]
        PostgreSql,
        [Display(Name = "Oracle")]
        Oracle,
        [Display(Name = "SQLite")]
        SQLite
    }
}