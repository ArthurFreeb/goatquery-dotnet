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

    public static Result<Expression> Evaluate(QueryExpression expression, ParameterExpression parameterExpression, Dictionary<string, string> propertyMapping)
    {
        if (expression == null) return Result.Fail("Expression cannot be null");
        if (parameterExpression == null) return Result.Fail("Parameter expression cannot be null");
        if (propertyMapping == null) return Result.Fail("Property mapping cannot be null");

        var context = new FilterEvaluationContext(parameterExpression, propertyMapping);
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

        var propertyPathResult = BuildPropertyPath(propertyPath, baseExpression, context.PropertyMapping);
        if (propertyPathResult.IsFailed) return Result.Fail(propertyPathResult.Errors);

        var (finalProperty, nullChecks) = propertyPathResult.Value;

        if (exp.Right is NullLiteral)
        {
            var nullComparison = CreateNullComparison(exp, finalProperty);
            return CombineWithNullChecks(nullComparison, nullChecks);
        }

        var comparisonResult = EvaluateValueComparison(exp, finalProperty);
        if (comparisonResult.IsFailed) return comparisonResult;

        return CombineWithNullChecks(comparisonResult.Value, nullChecks);
    }

    private static Result<(MemberExpression Property, List<Expression> NullChecks)> BuildPropertyPath(
        PropertyPath propertyPath,
        Expression startExpression,
        Dictionary<string, string> propertyMapping)
    {
        var current = startExpression;
        var nullChecks = new List<Expression>();

        foreach (var (segment, isLast) in propertyPath.Segments.Select((s, i) => (s, i == propertyPath.Segments.Count - 1)))
        {
            if (!propertyMapping.TryGetValue(segment, out var propertyName))
                return Result.Fail($"Invalid property '{segment}' in path");

            current = Expression.Property(current, propertyName);

            // Add null check for intermediate reference types only (not the final property)
            if (!isLast && IsNullableReferenceType(current.Type))
            {
                nullChecks.Add(Expression.NotEqual(current, Expression.Constant(null, current.Type)));
            }
        }

        return Result.Ok(((MemberExpression)current, nullChecks));
    }

    private static bool IsNullableReferenceType(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
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

    private static Result<Expression> CombineWithNullChecks(Expression comparison, List<Expression> nullChecks)
    {
        if (!nullChecks.Any())
        {
            return comparison;
        }

        var allNullChecks = nullChecks.Aggregate(Expression.AndAlso);

        return Expression.AndAlso(allNullChecks, comparison);
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

    private static Result<Expression> CreateComparisonExpression(string operatorKeyword, MemberExpression property, ConstantExpression value)
    {
        return operatorKeyword switch
        {
            Keywords.Eq => Expression.Equal(property, value),
            Keywords.Ne => Expression.NotEqual(property, value),
            Keywords.Contains => CreateContainsExpression(property, value),
            Keywords.Lt => Expression.LessThan(property, value),
            Keywords.Lte => Expression.LessThanOrEqual(property, value),
            Keywords.Gt => Expression.GreaterThan(property, value),
            Keywords.Gte => Expression.GreaterThanOrEqual(property, value),
            _ => Result.Fail($"Unsupported operator: {operatorKeyword}")
        };
    }

    private static Result<(ConstantExpression Value, MemberExpression Property)> CreateConstantExpression(QueryExpression literal, MemberExpression property)
    {
        return literal switch
        {
            IntegerLiteral intLit => CreateIntegerConstant(intLit.Value, property),
            DateLiteral dateLit => Result.Ok(CreateDateConstant(dateLit, property)),
            GuidLiteral guidLit => Result.Ok<(ConstantExpression, MemberExpression)>((Expression.Constant(guidLit.Value, property.Type), property)),
            DecimalLiteral decLit => Result.Ok<(ConstantExpression, MemberExpression)>((Expression.Constant(decLit.Value, property.Type), property)),
            FloatLiteral floatLit => Result.Ok<(ConstantExpression, MemberExpression)>((Expression.Constant(floatLit.Value, property.Type), property)),
            DoubleLiteral dblLit => Result.Ok<(ConstantExpression, MemberExpression)>((Expression.Constant(dblLit.Value, property.Type), property)),
            StringLiteral strLit => Result.Ok<(ConstantExpression, MemberExpression)>((Expression.Constant(strLit.Value, property.Type), property)),
            DateTimeLiteral dtLit => Result.Ok<(ConstantExpression, MemberExpression)>((Expression.Constant(dtLit.Value, property.Type), property)),
            BooleanLiteral boolLit => Result.Ok<(ConstantExpression, MemberExpression)>((Expression.Constant(boolLit.Value, property.Type), property)),
            NullLiteral _ => Result.Ok<(ConstantExpression, MemberExpression)>((Expression.Constant(null, property.Type), property)),
            _ => Result.Fail($"Unsupported literal type: {literal.GetType().Name}")
        };
    }

    private static Result<(ConstantExpression, MemberExpression)> CreateIntegerConstant(int value, MemberExpression property)
    {
        var integerConstant = GetIntegerExpressionConstant(value, property.Type);
        if (integerConstant.IsFailed)
        {
            return Result.Fail(integerConstant.Errors);
        }

        return Result.Ok<(ConstantExpression, MemberExpression)>((integerConstant.Value, property));
    }

    private static (ConstantExpression, MemberExpression) CreateDateConstant(DateLiteral dateLiteral, MemberExpression property)
    {
        if (property.Type == typeof(DateTime?))
        {
            return (Expression.Constant(dateLiteral.Value.Date, typeof(DateTime)), property);
        }
        else
        {
            var dateProperty = Expression.Property(property, DatePropertyName);
            return (Expression.Constant(dateLiteral.Value.Date, dateProperty.Type), dateProperty);
        }
    }

    private static Expression CreateContainsExpression(MemberExpression property, ConstantExpression value)
    {
        var propertyToLower = Expression.Call(property, StringToLowerMethod);
        var valueToLower = Expression.Call(value, StringToLowerMethod);
        return Expression.Call(propertyToLower, StringContainsMethod, valueToLower);
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

        if (!context.PropertyMapping.TryGetValue(identifier, out var propertyName))
        {
            return Result.Fail($"Invalid property '{identifier}' within filter");
        }

        var baseExpression = context.IsInLambdaScope ?
            (Expression)context.CurrentLambda.Parameter :
            context.RootParameter;

        var identifierProperty = Expression.Property(baseExpression, propertyName);
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

        // Enter lambda scope
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

        var collectionResult = ResolveCollectionProperty(lambdaExp.Property, baseExpression, context.PropertyMapping);
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

    private static Result<MemberExpression> ResolveCollectionProperty(QueryExpression property, Expression baseExpression, Dictionary<string, string> propertyMapping)
    {
        switch (property)
        {
            case Identifier identifier:
                if (!propertyMapping.TryGetValue(identifier.TokenLiteral(), out var propertyName))
                {
                    return Result.Fail($"Invalid property '{identifier.TokenLiteral()}' in lambda expression");
                }
                return Expression.Property(baseExpression, propertyName);

            case PropertyPath propertyPath:
                var current = baseExpression;
                for (int i = 0; i < propertyPath.Segments.Count; i++)
                {
                    var segment = propertyPath.Segments[i];
                    if (!propertyMapping.TryGetValue(segment, out var segmentPropertyName))
                        return Result.Fail($"Invalid property '{segment}' in lambda expression property path");
                    current = Expression.Property(current, segmentPropertyName);
                }
                return (MemberExpression)current;

            default:
                return Result.Fail($"Unsupported property type in lambda expression: {property.GetType().Name}");
        }
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
            return Result.Fail($"Lambda parameter '{context.CurrentLambda.ParameterName}' cannot be used directly in comparisons");
        }

        if (!context.PropertyMapping.TryGetValue(identifierName, out var propertyName))
        {
            return Result.Fail($"Invalid property '{identifierName}' within filter");
        }

        var identifierProperty = Expression.Property(context.RootParameter, propertyName);
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
        // Skip the first segment (lambda parameter name) and build property path from lambda parameter
        var current = (Expression)lambdaParameter;
        var elementType = lambdaParameter.Type;

        // Create property mapping for the element type
        var lambdaPropertyMapping = PropertyMappingHelper.CreatePropertyMapping(elementType);

        const int firstSegmentAfterParameter = 1;
        for (int i = firstSegmentAfterParameter; i < propertyPath.Segments.Count; i++)
        {
            var segment = propertyPath.Segments[i];

            // Get the actual property name using mapping
            if (!lambdaPropertyMapping.TryGetValue(segment, out var propertyName))
            {
                // If not found in current type, update mapping for nested type and try again
                if (current is MemberExpression memberExp)
                {
                    lambdaPropertyMapping = PropertyMappingHelper.CreatePropertyMapping(memberExp.Type);
                    if (!lambdaPropertyMapping.TryGetValue(segment, out propertyName))
                    {
                        return Result.Fail($"Invalid property '{segment}' in lambda property path");
                    }
                }
                else
                {
                    return Result.Fail($"Invalid property '{segment}' in lambda property path");
                }
            }

            current = Expression.Property(current, propertyName);
        }

        var finalProperty = (MemberExpression)current;

        // Handle null comparisons
        if (exp.Right is NullLiteral)
        {
            return exp.Operator == Keywords.Eq
                ? Expression.Equal(finalProperty, Expression.Constant(null, finalProperty.Type))
                : Expression.NotEqual(finalProperty, Expression.Constant(null, finalProperty.Type));
        }

        // Handle value comparisons
        return EvaluateValueComparison(exp, finalProperty);
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

    private static Type GetCollectionElementType(Type collectionType)
    {
        // Handle IEnumerable<T>
        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length == 1 &&
                typeof(IEnumerable<>).MakeGenericType(genericArgs[0]).IsAssignableFrom(collectionType))
            {
                return genericArgs[0];
            }
        }

        // Handle arrays
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
            // Fetch the underlying type if it's nullable.
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