using Dapper.SqlBuilder.ValueObjects;

namespace Dapper.SqlBuilder.Resolver.ExpressionTree
{
    class LikeNode : Node
    {
        public LikeMethod Method { get; set; }
        public MemberNode MemberNode { get; set; }
        public string Value { get; set; }
    }
}
