using System.Text;
using Dapper.SqlBuilder.Tests.Entities;

namespace Dapper.SqlBuilder.Tests;

public class SqlBuilderCloneTests : AbstractTests
{

    public SqlBuilderCloneTests()
    {
        SqlBuilder.SetAdapter(new Adapter.MySqlAdapter());
    }

    

    [Fact]
    public void CloneQuery()
    {
        var query = SqlBuilder.Select<User>().Where(_ => _.Id == 10);
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Id = @Param1", query.CommandText);

        var copyQuery = query.Clone().Or(x => x.Id == 11);
        Assert.Equal("SELECT Users.* FROM Users WHERE Users.Id = @Param1 OR Users.Id = @Param2", copyQuery.CommandText);

        copyQuery = query.Clone().Select(x => x.Id, x => x.Email);
        Assert.Equal("SELECT Users.Id, Users.Email FROM Users WHERE Users.Id = @Param1", copyQuery.CommandText);

        copyQuery = query.Clone().SelectCount(x => x.Id);
        Assert.Equal("SELECT COUNT(Users.Id) FROM Users WHERE Users.Id = @Param1", copyQuery.CommandText);
    }

}