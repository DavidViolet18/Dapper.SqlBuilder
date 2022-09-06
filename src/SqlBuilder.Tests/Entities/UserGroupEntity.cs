using System.ComponentModel.DataAnnotations.Schema;

namespace Dapper.SqlBuilder.Tests.Entities;

[Table("UserGroups")]
public class UserGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
}