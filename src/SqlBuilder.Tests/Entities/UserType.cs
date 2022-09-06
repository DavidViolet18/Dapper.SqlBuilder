using System.ComponentModel.DataAnnotations.Schema;

namespace Dapper.SqlBuilder.Tests.Entities;

public enum UserType
{
    GUEST = 1,
    ADMIN = 2,
    DEVELOPER = 3
}