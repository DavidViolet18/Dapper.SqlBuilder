using System.Text;
using Dapper.SqlBuilder.Tests.Entities;

namespace Dapper.SqlBuilder.Tests;

public class SqlBuilderQueryTests : AbstractTests
{

    public SqlBuilderQueryTests()
    {
        SqlBuilder.SetAdapter(new Adapter.MySqlAdapter());
    }

    [Fact]
    public void QueryCount()
    {
        var query = SqlBuilder.Count<User>(_ => _.Id).Where(_ => _.Id > 10);
        Assert.Equal("SELECT COUNT(Users.Id) FROM Users WHERE Users.Id > @Param1", query.CommandText);

        query = SqlBuilder.Count<User>().Where(_ => _.Id > 10);
        Assert.Equal("SELECT COUNT(*) FROM Users WHERE Users.Id > @Param1", query.CommandText);
    }

    [Fact]
    public void QueryWithPagination()
    {
        var query = SqlBuilder.Select<User>()
            .OrderBy(_ => _.Id)
            .Take(10);
        Assert.Equal("SELECT Users.* FROM Users ORDER BY Users.Id LIMIT 10", query.CommandText);

        query = SqlBuilder.Select<User>()
            .OrderBy(_ => _.Id)
            .Take(10)
            .Skip(20);
        Assert.Equal("SELECT Users.* FROM Users ORDER BY Users.Id LIMIT 20, 10", query.CommandText);

        query = SqlBuilder.Select<User>(x => new User { Id = x.Id, Email = x.Email })
            .OrderBy(_ => _.Id)
            .Take(10);
        Assert.Equal("SELECT Users.Id, Users.Email FROM Users ORDER BY Users.Id LIMIT 10", query.CommandText);

        query = SqlBuilder.Select<User>()
            .Where(_ => _.ModifiedDate > DateTimeOffset.Now.Date.AddDays(-50))
            .OrderBy(_ => _.Id)
            .Take(10)
            .Skip(1);
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.ModifiedDate > @Param1 ORDER BY Users.Id LIMIT 1, 10", query.CommandText);
    }

    [Fact]
    public void FindByFieldValue()
    {
        var userEmail = "user@domain1.com";

        var query = SqlBuilder.Select<User>().Where(user => user.Email == userEmail);
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Email = @Param1", query.CommandText);
        Assert.Equal(userEmail, query.CommandParameters.First().Value);
    }

    [Fact]
    public void FindByFieldValueAndGetOnlyOneResult()
    {
        var userEmail = "user@domain1.com";

        var query = SqlBuilder.SelectSingle<User>().Where(user => user.Email == userEmail);
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Email = @Param1 LIMIT 1", query.CommandText);
        Assert.Equal(userEmail, query.CommandParameters.First().Value);
    }

    [Fact]
    public void FindByFieldValueLike()
    {
        const string searchTerm = "domain.com";

        var query = SqlBuilder.Select<User>().Where(user => user.Email.Contains(searchTerm));
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Email LIKE @Param1", query.CommandText);
        Assert.Equal($"%{searchTerm}%", query.CommandParameters.First().Value);
    }

    [Fact]
    public void OrderEntitiesByField()
    {
        var query = SqlBuilder.Select<User>().OrderBy(x => x.Email);
        Assert.Equal("SELECT Users.* FROM Users ORDER BY Users.Email", query.CommandText);
    }

    [Fact]
    public void OrderEntitiesByFieldDescending()
    {
        var query = SqlBuilder.Select<User>().OrderByDescending(x => x.Email);
        Assert.Equal("SELECT Users.* FROM Users ORDER BY Users.Email DESC", query.CommandText);
    }

    [Fact]
    public void WhereEnum()
    {
        var userType = UserType.DEVELOPER;
        var query = SqlBuilder.Select<User>().Where(x => x.Role == userType);
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Role = @Param1", query.CommandText);
        Assert.Equal(3, query.CommandParameters.First().Value);

        query = SqlBuilder.Select<User>().Where(x => x.Role == UserType.DEVELOPER);
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Role = @Param1", query.CommandText);
        Assert.Equal(3, query.CommandParameters.First().Value);
    }

    [Fact]
    public void WhereIsIn()
    {
        var query = SqlBuilder.Select<User>().WhereIsIn(x => x.Id, new List<int> { 1, 2, 4 });
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Id IN (@Param1,@Param2,@Param3)", query.CommandText);
        Assert.Equal(3, query.CommandParameters.Count);

        query = SqlBuilder.Select<User>().WhereIsIn(x => x.Id, new int[] { 1, 2, 4 });
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Id IN (@Param1,@Param2,@Param3)", query.CommandText);
        Assert.Equal(3, query.CommandParameters.Count);

        var users = new List<User>{
            new User { Id = 1 },
            new User { Id = 2 },
            new User { Id = 4 }
        };
        query = SqlBuilder.Select<User>().WhereIsIn(x => x.Id, users.Select(y => y.Id));
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Id IN (@Param1,@Param2,@Param3)", query.CommandText);
        Assert.Equal(3, query.CommandParameters.Count);
    }

    [Fact]
    public void WhereBetween()
    {
        var query = SqlBuilder.Select<User>()
            .Where(x => x.Email == "Test")
            .AndBetween(x => x.ModifiedDate, DateTime.Now, DateTime.Now.AddDays(1));
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Email = @Param1 AND (Users.ModifiedDate BETWEEN @Param2 AND @Param3)", query.CommandText);
        Assert.Equal(3, query.CommandParameters.Count);
    }

    [Fact]
    public void MultipleQuery()
    {
        var query = SqlBuilder
            .From<User>(x => x.Where(y => y.Id == 2))
            .From<UserGroup>(x => x.Where(y => y.Id == 5));

        var commandQry = new StringBuilder();
        commandQry.Append("SELECT Users.* FROM Users WHERE Users.Id = @Param1");
        commandQry.Append("\r\n");
        commandQry.Append("SELECT UserGroups.* FROM UserGroups WHERE UserGroups.Id = @Param2");

        Assert.Equal(commandQry.ToString(), query.CommandText);
        Assert.Equal(2, query.CommandParameters.Count);

    }

    [Fact]
    public void Result()
    {
        var query = SqlBuilder
            .Select<User>().Where(x => x.FirstName.Contains("name"))
            .LeftJoin<UserGroup>((user, group) => user.Id == group.Id)
            .Result<User, UserWithGroupName>((x, y) => new UserWithGroupName { Id = y.Id, Email = y.Email, GroupName = x.Name });
        Assert.Equal("SELECT Users.Id, Users.Email, UserGroups.Name GroupName " +
                        "FROM Users " +
                        "LEFT JOIN UserGroups ON Users.Id = UserGroups.Id " +
                        "WHERE Users.FirstName LIKE @Param1", query.CommandText);

    }

    [Fact]
    public void WhereRaw()
    {
        var query = SqlBuilder.Select<User>().Where("BIN_TO_UUID(@0) = @1", x => x.Uuid, _ => "123456789");
        Assert.Equal("SELECT Users.* FROM Users WHERE BIN_TO_UUID(Users.Uuid) = @Param1", query.CommandText);
        Assert.Equal(1, query.CommandParameters.Count);

        int id1 = 1;
        string id2 = "2";
        query = SqlBuilder.Select<User>().Where("@0 = @1 OR @0 = @2 OR @0 = @3 OR @0 = @4", x => x.Uuid, _ => id1, _ => id2, _ => 3, _ => "4");
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Uuid = @Param1 OR Users.Uuid = @Param2 OR Users.Uuid = @Param3 OR Users.Uuid = @Param4", query.CommandText);
        Assert.Equal(4, query.CommandParameters.Count);
    }

}