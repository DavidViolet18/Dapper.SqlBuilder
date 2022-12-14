using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper.SqlBuilder.Resolver.ExpressionTree;
using Dapper.SqlBuilder.ValueObjects;

namespace Dapper.SqlBuilder.Resolver
{
    /// <summary>
    /// Provides methods to perform resolution to SQL expressions from given lambda expressions
    /// </summary>
    partial class LambdaResolver
    {
        public Node GetExpressionObject(Expression expression)
        {
            return ResolveQuery((dynamic)expression);
        }

        public void ResolveQuery<T>(Expression<Func<T, object>> expression)
        {
            var expressionTree = ResolveQuery((dynamic)expression.Body);
            BuildSql(expressionTree);
        }
        public void ResolveQuery<T>(Expression<Func<T, bool>> expression)
        {
            var expressionTree = ResolveQuery((dynamic)expression.Body);
            BuildSql(expressionTree);
        }

        public void ResolveQuery<T, T2>(Expression<Func<T, T2, bool>> expression)
        {
            var expressionTree = ResolveQuery((dynamic)expression.Body);
            BuildSql(expressionTree);
        }

        private Node ResolveQuery(ConstantExpression constantExpression)
        {
            return new ValueNode { Value = constantExpression.Value };
        }

        private Node ResolveQuery(UnaryExpression unaryExpression)
        {
            return new SingleOperationNode
            {
                Operator = unaryExpression.NodeType,
                Child = ResolveQuery((dynamic)unaryExpression.Operand)
            };
        }

        private Node ResolveQuery(BinaryExpression binaryExpression)
        {
            return new OperationNode
            {
                Left = ResolveQuery((dynamic)binaryExpression.Left),
                Operator = binaryExpression.NodeType,
                Right = ResolveQuery((dynamic)binaryExpression.Right)
            };
        }

        private Node ResolveQuery(MethodCallExpression callExpression)
        {
            if (Enum.TryParse(callExpression.Method.Name, true, out LikeMethod callFunction))
            {
                var member = callExpression.Object as MemberExpression;
                var fieldValue = (string)GetExpressionValue(callExpression.Arguments.First());

                return new LikeNode
                {
                    MemberNode = new MemberNode
                    {
                        TableName = GetTableName(member),
                        FieldName = GetColumnName(callExpression.Object)
                    },
                    Method = callFunction,
                    Value = fieldValue
                };
            }
            var value = ResolveMethodCall(callExpression);
            return new ValueNode
            {
                Value = value
            };
        }

        private Node ResolveQuery(MemberExpression memberExpression, MemberExpression rootExpression = null)
        {
#if NETCOREAPP2_1
            if (rootExpression == null)
                rootExpression = memberExpression;
#else
            rootExpression ??= memberExpression;
#endif
            switch (memberExpression.Expression.NodeType)
            {
                case ExpressionType.Parameter:
                    return new MemberNode
                    {
                        TableName = GetTableName(rootExpression),
                        FieldName = GetColumnName(rootExpression)
                    };

                case ExpressionType.MemberAccess:
                    return ResolveQuery(memberExpression.Expression as MemberExpression, rootExpression);

                case ExpressionType.Call:
                case ExpressionType.Constant:
                    return new ValueNode
                    {
                        Value = GetExpressionValue(rootExpression)
                    };

                default:
                    throw new ArgumentException("Expected member expression");
            }
        }


        public void QueryByIsIn<T>(Expression<Func<T, object>> expression, SqlBuilderBase sqlQuery)
        {
            var fieldName = GetColumnName(expression);
            Builder.QueryByIsIn(GetTableName<T>(), fieldName, sqlQuery);
        }

        public void QueryByIsIn<T, T2>(Expression<Func<T, T2>> expression, IEnumerable<T2> values)
        {
            var fieldName = GetColumnName(expression);
            Builder.QueryByIsIn(GetTableName<T>(), fieldName, values);
        }

        public void QueryByNotIn<T>(Expression<Func<T, object>> expression, SqlBuilderBase sqlQuery)
        {
            var fieldName = GetColumnName(expression);
            Builder.Not();
            Builder.QueryByIsIn(GetTableName<T>(), fieldName, sqlQuery);
        }

        public void QueryByNotIn<T, T2>(Expression<Func<T, T2>> expression, IEnumerable<T2> values)
        {
            var fieldName = GetColumnName(expression);
            Builder.Not();
            Builder.QueryByIsIn(GetTableName<T>(), fieldName, values);
        }

        public void QueryBetween<T, T2>(Expression<Func<T, T2>> expression, T2 start, T2 end, bool not)
        {
            var fieldName = GetColumnName(expression);
            Builder.QueryBetween(GetTableName<T>(), fieldName, start, end, not);
        }

        #region Helpers

        public object GetExpressionValue(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    var conVal = (expression as ConstantExpression).Value;

                    if (expression.Type.IsEnum)
                    {
                        return (int)Enum.Parse(expression.Type, conVal.ToString());
                    }

                    return conVal;
                case ExpressionType.Call:
                    return ResolveMethodCall(expression as MethodCallExpression);

                case ExpressionType.MemberAccess:
                    if (expression is MemberExpression memberExpr)
                    {
                        if (memberExpr.Expression == null)
                        {
                            var value = ResolveValue((dynamic)memberExpr.Member, null);
                            return value;
                        }
                         
                        var obj = GetExpressionValue(memberExpr.Expression);
                        return ResolveValue((dynamic)memberExpr.Member, obj); 
                    }
                    throw new ArgumentException("Invalid expression");
                case ExpressionType.Convert:
                    if (expression is UnaryExpression convertExpression)
                        return GetExpressionValue(convertExpression.Operand);

                    var expressionType = expression.GetType();
                    throw new
                        ArgumentException($"Expected some expression as {nameof(UnaryExpression)} but received {expressionType.FullName}");
                case ExpressionType.Not:
                    if (expression is UnaryExpression notExpression)
                        if (bool.TryParse(GetExpressionValue(notExpression.Operand).ToString(), out var boolValue))
                        {
                            return !boolValue;
                        }
                    var notExpressionType = expression.GetType();
                    throw new
                        ArgumentException($"Expected some expression as {nameof(UnaryExpression)} but received {notExpressionType.FullName}");
                case ExpressionType.Lambda:
                    var lmbda = (expression as LambdaExpression);
                    return GetExpressionValue(lmbda.Body);
                case ExpressionType.Quote:
                    var nqte = (expression as UnaryExpression);
                    return GetExpressionValue(nqte.Operand);
                case ExpressionType.Parameter:
                    var prmtr = (expression as ParameterExpression);
                    return prmtr.Name;
                default:
                    throw new ArgumentException("Expected constant expression");
            }
        }

        private object ResolveMethodCall(MethodCallExpression callExpression)
        {
            var arguments = callExpression.Arguments.Select(GetExpressionValue).ToArray();
            var obj = callExpression.Object != null ? GetExpressionValue(callExpression.Object) : (arguments.Length > 0 ? arguments.First() : null);

            return callExpression.Method.Invoke(obj, arguments);
        }

        private object ResolveValue(PropertyInfo property, object obj)
        {
            return property.GetValue(obj, null);
        }

        private object ResolveValue(FieldInfo field, object obj)
        {
            return field.GetValue(obj);
        }

        #endregion

        #region Fail functions

        private void ResolveQuery(Expression expression)
        {
            throw new ArgumentException(string.Format("The provided expression '{0}' is currently not supported", expression.NodeType));
        }

        #endregion
    }
}
