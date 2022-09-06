using System.ComponentModel.DataAnnotations.Schema;

namespace Dapper.SqlBuilder.Tests.Entities;

public class MyGuid
{

}

[Table("Users")]
public class User
{
    public Uuid Uuid { get; set; }
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }

    public UserType Role { get; set; }


    public DateTimeOffset? LastChangePassword { get; set; }
    public DateTimeOffset? ModifiedDate       { get; set; }
}

public class UserWithGroupName
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string GroupName { get; set; }
}

public sealed class Uuid
{
    internal static byte[] FlipEndian(byte[] oldBytes)
    {
        var newBytes = new byte[16];
        for (var i = 8; i < 16; i++)
            newBytes[i] = oldBytes[i];

        newBytes[3] = oldBytes[0];
        newBytes[2] = oldBytes[1];
        newBytes[1] = oldBytes[2];
        newBytes[0] = oldBytes[3];
        newBytes[5] = oldBytes[4];
        newBytes[4] = oldBytes[5];
        newBytes[6] = oldBytes[7];
        newBytes[7] = oldBytes[6];

        return newBytes;
    }

    private Guid _guid = new Guid();

    public Uuid(){}
    public Uuid(byte[] input) { _guid = new Guid(input); }
    public Uuid(string input) { _guid = new Guid(input); }

    public override string ToString() => _guid.ToString();
    public byte[] ToByteArray() => _guid.ToByteArray();


    public void Parse(string input)
    {
        _guid = Guid.Parse(input);
    }

    public static bool operator == (Uuid a, Uuid b) =>a.ToString() == b.ToString();
    public static bool operator != (Uuid a, Uuid b) =>a.ToString() != b.ToString();

    public override bool Equals(object obj)
    {
        if((obj == null) || !this.GetType().Equals(obj.GetType())) return false;
        Uuid b = (Uuid)obj;
        return ToString() == b.ToString();
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

}