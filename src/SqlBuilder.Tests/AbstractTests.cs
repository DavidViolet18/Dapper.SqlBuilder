using System.Text;
using Dapper.SqlBuilder.Tests.Entities;

namespace Dapper.SqlBuilder.Tests;

public abstract class AbstractTests
{
    protected void Debug<T>(ISqlBuilder<T> query)
    {
        Console.WriteLine(query.CommandText);
        query.CommandParameters.ToList().ForEach(x =>{
            Console.WriteLine($"{x.Key} : {x.Value}");
        });
    }

    protected void Debug(SqlBuilderCollection query)
    {
        Console.WriteLine(query.CommandText);
        query.CommandParameters.ToList().ForEach(x =>{
            Console.WriteLine($"{x.Key} : {x.Value}");
        });
    }
}