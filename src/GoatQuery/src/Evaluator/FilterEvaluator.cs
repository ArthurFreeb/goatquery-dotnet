using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FluentResults;

public static class FilterEvaluator
{
    private const string HasValuePropertyName = "HasValue";
    private const string ValuePropertyName = "Value";
    private const string DatePropertyName = "Date";

    private static readonly MethodInfo EnumerableAnyWithPredicate = GetEnumerableMethod("Any", 2);
    private static readonly MethodInfo EnumerableAnyWithoutPredicate = GetEnumerableMethod("Any", 1);
    private static readonly MethodInfo EnumerableAllWithPredicate = GetEnumerableMethod("All", 2);
    private static readonly MethodInfo StringToLowerMethod = GetStringMethod("ToLower");
    private static readonly MethodInfo StringContainsMethod = GetStringMethod("Contains", typeof(string));

    private static MethodInfo GetEnumerableMethod(string methodName, int parameterCount) =>
        typeof(Enumerable).GetMethods().First(m => m.Name == methodName && m.GetParameters().Length == parameterCount);

    private static MethodInfo GetStringMethod(string methodName, params Type[] parameterTypes) =>
        typeof(string).GetMethod(methodName, parameterTypes ?? Type.EmptyTypes);

    public static Result<Expression> Evaluate(QueryExpression expression, ParameterExpression parameterExpression, PropertyMappingTree propertyMappingTree)
    {
        if (expression == null) return Result.Fail("Expression cannot be null");
        if (parameterExpression == null) return Result.Fail("Parameter expression cannot be null");
        if (propertyMappingTree == null) return Result.Fail("Property mapping tree cannot be null");

        var context = new FilterEvaluationContext(parameterExpression, propertyMappingTree);
        return EvaluateExpression(expression, context);
    }

    private static Result<Expression> EvaluateExpression(QueryExpression expression, FilterEvaluationContext context)
    {
        return expression switch
        {
            InfixExpression exp => EvaluateInfixExpression(exp, context),
            QueryLambdaExpression lambdaExp => EvaluateLambdaExpression(lambdaExp, context),
            _ => Result.Fail($"Unsupported expression type: {expression.GetType().Name}")
        };
    }

    private static Result<Expression> EvaluatePropertyPathExpression(
        InfixExpression exp,
        PropertyPath propertyPath,
        FilterEvaluationContext context)
    {
        var baseExpression = context.IsInLambdaScope ?
            (Expression)context.CurrentLambda.Parameter :
            context.RootParameter;

        var safePathResult = BuildPropertyPathWithGuard(propertyPath.Segments, baseExpression, context.PropertyMappingTree);
        if (safePathResult.IsFailed) return Result.Fail(safePathResult.Errors);

        var (finalProperty, guard, container) = safePathResult.Value;

        if (exp.Right is NullLiteral)
        {
            return ComposeNestedNullComparison(finalProperty, guard, exp.Operator);
        }

        var comparisonResult = EvaluateValueComparison(exp, finalProperty);
        if (comparisonResult.IsFailed) return comparisonResult;

        var comparison = comparisonResult.Value;

        var requireFinalNotNull = RequiresFinalNotNull(exp.Operator, finalProperty, exp.Right);
        var combinedGuard = requireFinalNotNull
            ? Expression.AndAlso(guard, Expression.NotEqual(finalProperty, Expression.Constant(null, finalProperty.Type)))
            : guard;

        return Expression.AndAlso(combinedGuard, comparison);
    }

    private static Result<MemberExpression> BuildPropertyPath(
        PropertyPath propertyPath,
        Expression startExpression,
        PropertyMappingTree propertyMappingTree)
    {
        var current = startExpression;
        var currentMappingTree = propertyMappingTree;

        foreach (var (segment, isLast) in propertyPath.Segments.Select((s, i) => (s, i == propertyPath.Segments.Count - 1)))
        {
            if (!currentMappingTree.TryGetProperty(segment, out var propertyNode))
                return Result.Fail($"Invalid property '{segment}' in path");

            current = Expression.Property(current, propertyNode.ActualPropertyName);

            if (!isLast)
            {
                if (!propertyNode.HasNestedMapping)
                    return Result.Fail($"Property '{segment}' does not support nested navigation");

                currentMappingTree = propertyNode.NestedMapping;
            }
        }

        return Result.Ok((MemberExpression)current);
    }

    private static Result<(MemberExpression Final, Expression Guard, Expression Container)> BuildPropertyPathWithGuard(
        IList<string> segments,
        Expression startExpression,
        PropertyMappingTree propertyMappingTree)
    {
        if (segments == null || segments.Count == 0)
            return Result.Fail("Property path segments cannot be empty");

        var current = startExpression;
        var currentMappingTree = propertyMappingTree;

        Expression guard = Expression.Constant(true);
        Expression container = startExpression;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var isLast = i == segments.Count - 1;

            if (!currentMappingTree.TryGetProperty(segment, out var propertyNode))
                return Result.Fail($"Invalid property '{segment}' in path");

            var next = Expression.Property(current, propertyNode.ActualPropertyName);

            if (!isLast)
            {
                if (!propertyNode.HasNestedMapping)
                    return Result.Fail($"Property '{segment}' does not support nested navigation");

                if (!next.Type.IsValueType || Nullable.GetUnderlyingType(next.Type) != null)
                {
                    var notNull = Expression.NotEqual(next, Expression.Constant(null, next.Type));
                    guard = Expression.AndAlso(guard, notNull);
                }

                current = next;
                container = current;
                currentMappingTree = propertyNode.NestedMapping;
            }
            else
            {
                var final = Expression.Property(current, propertyNode.ActualPropertyName);
                return Result.Ok(((MemberExpression)final, guard, container));
            }
        }

        return Result.Fail("Invalid property path");
    }

    private static Result<MemberExpression> ResolvePropertyPathForCollection(
        PropertyPath propertyPath,
        Expression baseExpression,
        PropertyMappingTree propertyMappingTree)
    {
        var current = baseExpression;
        var currentMappingTree = propertyMappingTree;

        for (int i = 0; i < propertyPath.Segments.Count; i++)
        {
            var segment = propertyPath.Segments[i];

            if (!currentMappingTree.TryGetProperty(segment, out var propertyNode))
                return Result.Fail($"Invalid property '{segment}' in lambda expression property path");

            current = Expression.Property(current, propertyNode.ActualPropertyName);
            if (!isLast)
            if (i < propertyPath.Segments.Count - 1)
            {
                if (!propertyNode.HasNestedMapping)
                    return Result.Fail($"Property '{segment}' does not support nested navigation in lambda expression");

                currentMappingTree = propertyNode.NestedMapping;
            }
        }

        return (MemberExpression)current;
    }

    private static bool IsNullableReferenceType(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
               type == typeof(DateTime) || type == typeof(Guid) ||
               Nullable.GetUnderlyingType(type) != null;
    }

    private static Expression CreateNullComparison(InfixExpression exp, MemberExpression property)
    {
        return exp.Operator == Keywords.Eq
            ? Expression.Equal(property, Expression.Constant(null, property.Type))
            : Expression.NotEqual(property, Expression.Constant(null, property.Type));
    }

    private static bool IsNullableDateTimeComparison(MemberExpression property, QueryExpression rightExpression)
    {
        return property.Type == typeof(DateTime?) && rightExpression is DateLiteral;
    }

    private static Expression CreateNullableDateTimeComparison(MemberExpression property, ConstantExpression value, string operatorKeyword)
    {
        var hasValueProperty = Expression.Property(property, HasValuePropertyName);
        var valueProperty = Expression.Property(property, ValuePropertyName);
        var dateProperty = Expression.Property(valueProperty, DatePropertyName);

        var dateComparison = CreateDateComparison(dateProperty, value, operatorKeyword);

        return operatorKeyword == Keywords.Ne
            ? Expression.OrElse(Expression.Not(hasValueProperty), dateComparison)
            : Expression.AndAlso(hasValueProperty, dateComparison);
    }

    private static Expression CreateDateComparison(Expression dateProperty, ConstantExpression value, string operatorKeyword)
    {
        return operatorKeyword switch
        {
            Keywords.Eq => Expression.Equal(dateProperty, value),
            Keywords.Ne => Expression.NotEqual(dateProperty, value),
            Keywords.Lt => Expression.LessThan(dateProperty, value),
            Keywords.Lte => Expression.LessThanOrEqual(dateProperty, value),
            Keywords.Gt => Expression.GreaterThan(dateProperty, value),
            Keywords.Gte => Expression.GreaterThanOrEqual(dateProperty, value),
            _ => throw new ArgumentException($"Unsupported operator for date comparison: {operatorKeyword}")
        };
    }


    private static Result<Expression> EvaluateValueComparison(InfixExpression exp, MemberExpression property)
    {
        var valueResult = CreateConstantExpression(exp.Right, property);
        if (valueResult.IsFailed) return Result.Fail(valueResult.Errors);

        var (value, updatedProperty) = valueResult.Value;

        if (IsNullableDateTimeComparison(updatedProperty, exp.Right))
        {
            return CreateNullableDateTimeComparison(updatedProperty, value, exp.Operator);
        }

        return CreateComparisonExpression(exp.Operator, updatedProperty, value);
    }

    private static Result<Expression> EvaluateValueComparison(InfixExpression exp, Expression expression)
    {
        var valueResult = CreateConstantExpression(exp.Right, expression);
        if (valueResult.IsFailed) return Result.Fail(valueResult.Errors);

        return CreateComparisonExpression(exp.Operator, expression, valueResult.Value);
    }

    private static Result<Expression> CreateComparisonExpression(string operatorKeyword, Expression expression, ConstantExpression value)
    {
        return operatorKeyword switch
        {
            Keywords.Eq => Expression.Equal(expression, value),
            Keywords.Ne => Expression.NotEqual(expression, value),
            Keywords.Contains => CreateContainsExpression(expression, value),
            Keywords.Lt => Expression.LessThan(expression, value),
            Keywords.Lte => Expression.LessThanOrEqual(expression, value),
            Keywords.Gt => Expression.GreaterThan(expression, value),
            Keywords.Gte => Expression.GreaterThanOrEqual(expression, value),
            _ => Result.Fail($"Unsupported operator: {operatorKeyword}")
        };
    }

    private static Result<Expression> CreateComparisonExpression(string operatorKeyword, MemberExpression property, ConstantExpression value)
    {
        return CreateComparisonExpression(operatorKeyword, (Expression)property, value);
    }

    private static Result<ConstantExpression> CreateConstantExpression(QueryExpression literal, Expression expression)
    {
        return literal switch
        {
            IntegerLiteral intLit => CreateIntegerConstant(intLit.Value, expression),
            DateLiteral dateLit => Result.Ok(CreateDateConstant(dateLit, expression)),
            GuidLiteral guidLit => Result.Ok(Expression.Constant(guidLit.Value, expression.Type)),
            DecimalLiteral decLit => Result.Ok(Expression.Constant(decLit.Value, expression.Type)),
            FloatLiteral floatLit => Result.Ok(Expression.Constant(floatLit.Value, expression.Type)),
            DoubleLiteral dblLit => Result.Ok(Expression.Constant(dblLit.Value, expression.Type)),
            StringLiteral strLit => Result.Ok(Expression.Constant(strLit.Value, expression.Type)),
            DateTimeLiteral dtLit => Result.Ok(Expression.Constant(dtLit.Value, expression.Type)),
            BooleanLiteral boolLit => Result.Ok(Expression.Constant(boolLit.Value, expression.Type)),
            NullLiteral _ => Result.Ok(Expression.Constant(null, expression.Type)),
            _ => Result.Fail($"Unsupported literal type: {literal.GetType().Name}")
        };
    }

    private static Result<(ConstantExpression Value, MemberExpression Property)> CreateConstantExpression(QueryExpression literal, MemberExpression property)
    {
        var constantResult = CreateConstantExpression(literal, (Expression)property);
        if (constantResult.IsFailed) return Result.Fail(constantResult.Errors);

        return Result.Ok((constantResult.Value, property));
    }

    private static Result<ConstantExpression> CreateIntegerConstant(int value, Expression expression)
    {
        return GetIntegerExpressionConstant(value, expression.Type);
    }

    private static ConstantExpression CreateDateConstant(DateLiteral dateLiteral, Expression expression)
    {
        if (expression.Type == typeof(DateTime?))
        {
            return Expression.Constant(dateLiteral.Value.Date, typeof(DateTime));
        }
        else
        {
            return Expression.Constant(dateLiteral.Value.Date, expression.Type);
        }
    }

    private static Expression CreateContainsExpression(Expression expression, ConstantExpression value)
    {
        var expressionToLower = Expression.Call(expression, StringToLowerMethod);
        var valueToLower = Expression.Call(value, StringToLowerMethod);
        return Expression.Call(expressionToLower, StringContainsMethod, valueToLower);
    }

    private static Result<Expression> EvaluateInfixExpression(InfixExpression exp, FilterEvaluationContext context)
    {
        if (exp.Left is PropertyPath propertyPath)
            return EvaluatePropertyPathExpression(exp, propertyPath, context);

        if (exp.Left is Identifier)
            return EvaluateIdentifierExpression(exp, context);

        if (string.IsNullOrEmpty(exp.Operator) && exp.Left is QueryLambdaExpression lambda)
            return EvaluateLambdaExpression(lambda, context);

        return EvaluateLogicalExpression(exp, context);
    }

    private static Result<Expression> EvaluateIdentifierExpression(InfixExpression exp, FilterEvaluationContext context)
    {
        var identifier = exp.Left.TokenLiteral();

        if (!context.PropertyMappingTree.TryGetProperty(identifier, out var propertyNode))
        {
            return Result.Fail($"Invalid property '{identifier}' within filter");
        }

        var baseExpression = context.IsInLambdaScope ?
            (Expression)context.CurrentLambda.Parameter :
            context.RootParameter;

        var identifierProperty = Expression.Property(baseExpression, propertyNode.ActualPropertyName);
        return EvaluateValueComparison(exp, identifierProperty);
    }

    private static Result<Expression> EvaluateLogicalExpression(InfixExpression exp, FilterEvaluationContext context)
    {
        var left = EvaluateExpression(exp.Left, context);
        if (left.IsFailed) return left;

        var right = EvaluateExpression(exp.Right, context);
        if (right.IsFailed) return right;

        return exp.Operator switch
        {
            Keywords.And => Expression.AndAlso(left.Value, right.Value),
            Keywords.Or => Expression.OrElse(left.Value, right.Value),
            _ => Result.Fail($"Unsupported logical operator: {exp.Operator}")
        };
    }

    private static Result<Expression> EvaluateLambdaExpression(QueryLambdaExpression lambdaExp, FilterEvaluationContext context)
    {
        var setupResult = SetupLambdaEvaluation(lambdaExp, context);
        if (setupResult.IsFailed) return Result.Fail(setupResult.Errors);

        var (collectionProperty, elementType, lambdaParameter) = setupResult.Value;

        context.EnterLambdaScope(lambdaExp.Parameter, lambdaParameter, elementType);

        try
        {
            var bodyResult = EvaluateLambdaBody(lambdaExp.Body, context);
            if (bodyResult.IsFailed) return bodyResult;

            var lambdaExpr = Expression.Lambda(bodyResult.Value, lambdaParameter);
            return CreateLambdaLinqCall(lambdaExp.Function, collectionProperty, lambdaExpr, elementType);
        }
        finally
        {
            context.ExitLambdaScope();
        }
    }

    private static Result<(MemberExpression Collection, Type ElementType, ParameterExpression Parameter)> SetupLambdaEvaluation(
        QueryLambdaExpression lambdaExp,
        FilterEvaluationContext context)
    {
        var baseExpression = context.IsInLambdaScope ?
            (Expression)context.CurrentLambda.Parameter :
            context.RootParameter;

        var collectionResult = ResolveCollectionProperty(lambdaExp.Property, baseExpression, context.PropertyMappingTree);
        if (collectionResult.IsFailed) return Result.Fail(collectionResult.Errors);

        var collectionProperty = collectionResult.Value;
        var elementType = GetCollectionElementType(collectionProperty.Type);

        if (elementType == null)
        {
            return Result.Fail($"Property '{lambdaExp.Property.TokenLiteral()}' is not a collection");
        }

        var lambdaParameter = Expression.Parameter(elementType, lambdaExp.Parameter);
        return Result.Ok((collectionProperty, elementType, lambdaParameter));
    }

    private static Expression CreateLambdaLinqCall(string function, MemberExpression collection, LambdaExpression lambda, Type elementType)
    {
        return function.Equals(Keywords.Any, StringComparison.OrdinalIgnoreCase)
            ? CreateAnyExpression(collection, lambda, elementType)
            : CreateAllExpression(collection, lambda, elementType);
    }

    private static Result<MemberExpression> ResolveCollectionProperty(QueryExpression property, Expression baseExpression, PropertyMappingTree propertyMappingTree)
    {
        switch (property)
        {
            case Identifier identifier:
                if (!propertyMappingTree.TryGetProperty(identifier.TokenLiteral(), out var propertyNode))
                {
                    return Result.Fail($"Invalid property '{identifier.TokenLiteral()}' in lambda expression");
                }
                return Expression.Property(baseExpression, propertyNode.ActualPropertyName) as MemberExpression;

            case PropertyPath propertyPath:
                return ResolvePropertyPathForCollection(propertyPath, baseExpression, propertyMappingTree);

            default:
                return Result.Fail($"Unsupported property type in lambda expression: {property.GetType().Name}");
        }
    }

    private static bool RequiresFinalNotNull(string operatorKeyword, MemberExpression finalProperty, QueryExpression right)
    {
        if (operatorKeyword.Equals(Keywords.Contains, StringComparison.OrdinalIgnoreCase))
            return true;

        if (right is NullLiteral)
            return false;

        var type = finalProperty.Type;
        if (!type.IsValueType)
            return true;

        return Nullable.GetUnderlyingType(type) != null;
    }

    private static Expression ComposeNestedNullComparison(MemberExpression finalProperty, Expression guard, string operatorKeyword)
    {
        var isEq = operatorKeyword.Equals(Keywords.Eq, StringComparison.OrdinalIgnoreCase);
        var isNe = operatorKeyword.Equals(Keywords.Ne, StringComparison.OrdinalIgnoreCase);
        var nullConst = Expression.Constant(null, finalProperty.Type);
        var finalEqNull = Expression.Equal(finalProperty, nullConst);
        var finalNeNull = Expression.NotEqual(finalProperty, nullConst);
        var notGuard = Expression.Not(guard);

        if (isEq)
        {
            return Expression.OrElse(notGuard, finalEqNull);
        }
        else if (isNe)
        {
            return Expression.AndAlso(guard, finalNeNull);
        }

        return Expression.AndAlso(guard, finalEqNull);
    }

    private static Result<Expression> EvaluateLambdaBody(QueryExpression expression, FilterEvaluationContext context)
    {
        return expression switch
        {
            InfixExpression exp when exp.Left is PropertyPath propertyPath =>
                EvaluateLambdaBodyPropertyPath(exp, propertyPath, context),

            InfixExpression exp when exp.Left is Identifier identifier =>
                EvaluateLambdaBodyIdentifier(exp, identifier, context),

            InfixExpression exp when IsNestedLambdaExpression(exp) =>
                EvaluateLambdaExpression((QueryLambdaExpression)exp.Left, context),

            InfixExpression exp =>
                EvaluateLambdaBodyLogicalOperator(exp, context),

            _ => Result.Fail($"Unsupported expression type in lambda context: {expression.GetType().Name}")
        };
    }

    private static bool IsNestedLambdaExpression(InfixExpression exp) =>
        string.IsNullOrEmpty(exp.Operator) && exp.Left is QueryLambdaExpression;

    private static Result<Expression> EvaluateLambdaBodyPropertyPath(InfixExpression exp, PropertyPath propertyPath, FilterEvaluationContext context)
    {
        var isLambdaParameterPath = propertyPath.Segments.Count > 0 &&
            propertyPath.Segments[0].Equals(context.CurrentLambda.ParameterName, StringComparison.OrdinalIgnoreCase);

        return isLambdaParameterPath
            ? EvaluateLambdaPropertyPath(exp, propertyPath, context.CurrentLambda.Parameter)
            : EvaluatePropertyPathExpression(exp, propertyPath, context);
    }

    private static Result<Expression> EvaluateLambdaBodyIdentifier(InfixExpression exp, Identifier identifier, FilterEvaluationContext context)
    {
        var identifierName = identifier.TokenLiteral();

        if (identifierName.Equals(context.CurrentLambda.ParameterName, StringComparison.OrdinalIgnoreCase))
        {
            if (IsPrimitiveType(context.CurrentLambda.ElementType))
            {
                return EvaluateValueComparison(exp, context.CurrentLambda.Parameter);
            }

            return Result.Fail($"Lambda parameter '{context.CurrentLambda.ParameterName}' cannot be used directly in comparisons for complex types");
        }

        if (!context.PropertyMappingTree.TryGetProperty(identifierName, out var propertyNode))
        {
            return Result.Fail($"Invalid property '{identifierName}' within filter");
        }

        var identifierProperty = Expression.Property(context.RootParameter, propertyNode.ActualPropertyName);
        return EvaluateValueComparison(exp, identifierProperty);
    }

    private static Result<Expression> EvaluateLambdaBodyLogicalOperator(InfixExpression exp, FilterEvaluationContext context)
    {
        var left = EvaluateLambdaBody(exp.Left, context);
        if (left.IsFailed) return left;

        var right = EvaluateLambdaBody(exp.Right, context);
        if (right.IsFailed) return right;

        return exp.Operator switch
        {
            Keywords.And => Expression.AndAlso(left.Value, right.Value),
            Keywords.Or => Expression.OrElse(left.Value, right.Value),
            _ => Result.Fail($"Unsupported logical operator: {exp.Operator}")
        };
    }

    private static Result<Expression> EvaluateLambdaPropertyPath(InfixExpression exp, PropertyPath propertyPath, ParameterExpression lambdaParameter)
    {
        var elementType = lambdaParameter.Type;
        var mapping = PropertyMappingTreeBuilder.BuildMappingTree(elementType, GetDefaultMaxDepth());

        var safePathResult = BuildPropertyPathWithGuard(propertyPath.Segments.Skip(1).ToList(), lambdaParameter, mapping);
        if (safePathResult.IsFailed) return Result.Fail(safePathResult.Errors);

        var (finalProperty, guard, container) = safePathResult.Value;

        if (exp.Right is NullLiteral)
        {
            return ComposeNestedNullComparison(finalProperty, guard, exp.Operator);
        }

        var comparisonResult = EvaluateValueComparison(exp, finalProperty);
        if (comparisonResult.IsFailed) return comparisonResult;

        var comparison = comparisonResult.Value;
        var requireFinalNotNull = RequiresFinalNotNull(exp.Operator, finalProperty, exp.Right);
        var combinedGuard = requireFinalNotNull
            ? Expression.AndAlso(guard, Expression.NotEqual(finalProperty, Expression.Constant(null, finalProperty.Type)))
            : guard;

        return Expression.AndAlso(combinedGuard, comparison);
    }

    private static Expression CreateAnyExpression(MemberExpression collection, LambdaExpression lambda, Type elementType)
    {
        var genericMethod = EnumerableAnyWithPredicate.MakeGenericMethod(elementType);
        return Expression.Call(genericMethod, collection, lambda);
    }

    private static Expression CreateAllExpression(MemberExpression collection, LambdaExpression lambda, Type elementType)
    {
        var allMethod = EnumerableAllWithPredicate.MakeGenericMethod(elementType);
        var anyMethod = EnumerableAnyWithoutPredicate.MakeGenericMethod(elementType);

        var hasElements = Expression.Call(anyMethod, collection);
        var allMatch = Expression.Call(allMethod, collection, lambda);

        return Expression.AndAlso(hasElements, allMatch);
    }

    private static Result<Expression> BuildLambdaPropertyPath(Expression startExpression, List<string> segments, Type elementType)
    {
        var current = startExpression;
        var currentMappingTree = PropertyMappingTreeBuilder.BuildMappingTree(elementType, GetDefaultMaxDepth());

        foreach (var segment in segments)
        {
            if (!currentMappingTree.TryGetProperty(segment, out var propertyNode))
            {
                return Result.Fail($"Invalid property '{segment}' in lambda property path");
            }

            current = Expression.Property(current, propertyNode.ActualPropertyName);

            if (propertyNode.HasNestedMapping)
            {
                currentMappingTree = propertyNode.NestedMapping;
            }
        }

        return Result.Ok(current);
    }

    private static int GetDefaultMaxDepth()
    {
        return new QueryOptions().MaxPropertyMappingDepth;
    }

    private static Type GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length == 1 &&
                typeof(IEnumerable<>).MakeGenericType(genericArgs[0]).IsAssignableFrom(collectionType))
            {
                return genericArgs[0];
            }
        }

        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        return null;
    }

    private static Result<ConstantExpression> GetIntegerExpressionConstant(int value, Type targetType)
    {
        try
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            var type = underlyingType ?? targetType;

            object convertedValue = type switch
            {
                Type t when t == typeof(int) => value,
                Type t when t == typeof(long) => Convert.ToInt64(value),
                Type t when t == typeof(short) => Convert.ToInt16(value),
                Type t when t == typeof(byte) => Convert.ToByte(value),
                Type t when t == typeof(uint) => Convert.ToUInt32(value),
                Type t when t == typeof(ulong) => Convert.ToUInt64(value),
                Type t when t == typeof(ushort) => Convert.ToUInt16(value),
                Type t when t == typeof(sbyte) => Convert.ToSByte(value),
                _ => throw new NotSupportedException($"Unsupported numeric type: {targetType.Name}")
            };

            return Expression.Constant(convertedValue, targetType);
        }
        catch (OverflowException)
        {
            return Result.Fail($"Value {value} is too large for type {targetType.Name}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error converting {value} to {targetType.Name}: {ex.Message}");
        }
    }
}