using System;
using System.Linq.Expressions;
using Dapper.SqlBuilder.Resolver.ExpressionTree;
using Dapper.SqlBuilder.ValueObjects;

namespace Dapper.SqlBuilder.Resolver
{
    /// <summary>
    /// Provides methods to perform resolution to SQL expressions for SELECT query from given lambda expressions
    /// </summary>
    partial class LambdaResolver
    {
        void BuildSql(Node node)
        {
            BuildSql((dynamic)node);
        }

        void BuildSql(LikeNode node)
        {
            if (node.Method == LikeMethod.Equals)
            {
                Builder.QueryByField(node.MemberNode.TableName, node.MemberNode.FieldName,
                    _operationDictionary[ExpressionType.Equal], node.Value);
            }
            else
            {
                var value = node.Value;
                switch (node.Method)
                {
                    case LikeMethod.StartsWith:
                        value = node.Value + "%";
                        break;
                    case LikeMethod.EndsWith:
                        value = "%" + node.Value;
                        break;
                    case LikeMethod.Contains:
                        value = "%" + node.Value + "%";
                        break;
                }
                Builder.QueryByFieldLike(node.MemberNode.TableName, node.MemberNode.FieldName, value);
            }
        }

        void BuildSql(OperationNode node)
        {
            if (node.Left is SingleOperationNode singllefteOp && node.Right is SingleOperationNode singleRightop)
            {
                BuildSql((dynamic)singllefteOp.Child, (dynamic)singleRightop.Child, node.Operator);
            }
            else
                BuildSql((dynamic)node.Left, (dynamic)node.Right, node.Operator);
        }

        void BuildSql(ValueNode valueNode)
        {
            Builder.QueryByConstant(valueNode.Value);
        }

        void BuildSql(MemberNode memberNode)
        {

            Builder.QueryByField(memberNode.TableName, memberNode.FieldName);

            /*if (Builder.Operation == Adapter.SqlOperations.Case)
                Builder.QueryByField(memberNode.TableName, memberNode.FieldName);
            else
                Builder.QueryByField(memberNode.TableName, memberNode.FieldName, _operationDictionary[ExpressionType.Equal], true);*/
        }

        public void BuildSql(SingleOperationNode node)
        {
            if (node.Operator == ExpressionType.Not)
                Builder.Not();
            BuildSql(node.Child);
        }

        void BuildSql(MemberNode memberNode, ValueNode valueNode, ExpressionType op)
        {
            if (valueNode.Value == null)
            {
                ResolveNullValue(memberNode, op);
            }
            else
            {
                var value = valueNode.Value;
                var tp = valueNode.Value.GetType();
                if (tp.IsEnum)
                {
                    value = (int)Enum.Parse(tp, value.ToString());
                }
                Builder.QueryByField(memberNode.TableName, memberNode.FieldName, _operationDictionary[op], value);
            }
        }

        void BuildSql(ValueNode valueNode, MemberNode memberNode, ExpressionType op)
        {
            BuildSql(memberNode, valueNode, op);
        }

        void BuildSql(MemberNode leftMember, MemberNode rightMember, ExpressionType op)
        {
            Builder.QueryByFieldComparison(leftMember.TableName, leftMember.FieldName, _operationDictionary[op], rightMember.TableName, rightMember.FieldName);
        }

        void BuildSql(SingleOperationNode leftMember, Node rightMember, ExpressionType op)
        {
            if (leftMember.Operator == ExpressionType.Not)
                BuildSql(leftMember as Node, rightMember, op);
            else
                BuildSql((dynamic)leftMember.Child, (dynamic)rightMember, op);
        }

        void BuildSql(Node leftMember, SingleOperationNode rightMember, ExpressionType op)
        {
            BuildSql(rightMember, leftMember, op);
        }

        void BuildSql(Node leftNode, Node rightNode, ExpressionType op)
        {
            Builder.BeginExpression();
            BuildSql((dynamic)leftNode);
            ResolveOperation(op);
            BuildSql((dynamic)rightNode);
            Builder.EndExpression();
        }

        void ResolveNullValue(MemberNode memberNode, ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.Equal:
                    Builder.QueryByFieldNull(memberNode.TableName, memberNode.FieldName);
                    break;
                case ExpressionType.NotEqual:
                    Builder.QueryByFieldNotNull(memberNode.TableName, memberNode.FieldName);
                    break;
            }
        }

        void ResolveSingleOperation(ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.Not:
                    Builder.Not();
                    break;
            }
        }

        void ResolveOperation(ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    Builder.And();
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    Builder.Or();
                    break;
                case ExpressionType.GreaterThan:
                    Builder.GreaterThan(false);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    Builder.GreaterThan(true);
                    break;
                case ExpressionType.LessThan:
                    Builder.LessThan(false);
                    break;
                case ExpressionType.LessThanOrEqual:
                    Builder.LessThan(true);
                    break;
                default:
                    throw new ArgumentException(string.Format("Unrecognized binary expression operation '{0}'", op.ToString()));
            }
        }
    }
}
