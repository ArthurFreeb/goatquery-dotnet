using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentResults;

public static class FilterEvaluator
{
    private static Result<Expression> EvaluatePropertyPathExpression(
        InfixExpression exp,
        PropertyPath propertyPath,
        ParameterExpression parameterExpression,
        Dictionary<string, string> propertyMapping)
    {
        // Build property path with null checks for intermediate properties
        var current = (Expression)parameterExpression;
        var nullChecks = new List<Expression>();

        for (int i = 0; i < propertyPath.Segments.Count; i++)
        {
            var segment = propertyPath.Segments[i];
            if (!propertyMapping.TryGetValue(segment, out var propertyName))
                return Result.Fail($"Invalid property '{segment}' in path");

            current = Expression.Property(current, propertyName);

            // Add null check for intermediate reference types only
            if (i < propertyPath.Segments.Count - 1 && (!current.Type.IsValueType || Nullable.GetUnderlyingType(current.Type) != null))
            {
                nullChecks.Add(Expression.NotEqual(current, Expression.Constant(null, current.Type)));
            }
        }

        var finalProperty = (MemberExpression)current;

        // Handle null comparisons
        if (exp.Right is NullLiteral)
        {
            var nullComparison = exp.Operator == Keywords.Eq
                ? Expression.Equal(finalProperty, Expression.Constant(null, finalProperty.Type))
                : Expression.NotEqual(finalProperty, Expression.Constant(null, finalProperty.Type));

            return CombineWithNullChecks(nullComparison, nullChecks);
        }

        // Handle value comparisons
        var comparisonResult = EvaluateInfixExpression(exp, finalProperty);
        if (comparisonResult.IsFailed) return comparisonResult;

        return CombineWithNullChecks(comparisonResult.Value, nullChecks);
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

    private static Result<Expression> EvaluateInfixExpression(InfixExpression exp, MemberExpression property)
    {
        var valueResult = CreateConstantExpression(exp.Right, property);
        if (valueResult.IsFailed) return Result.Fail(valueResult.Errors);

        var (value, updatedProperty) = valueResult.Value;

        // Handle special nullable DateTime comparison
        if (updatedProperty.Type == typeof(DateTime?) && exp.Right is DateLiteral)
        {
            var hasValueProperty = Expression.Property(updatedProperty, "HasValue");
            var valueProperty = Expression.Property(updatedProperty, "Value");
            var dateProperty = Expression.Property(valueProperty, "Date");

            Expression dateComparison = exp.Operator switch
            {
                Keywords.Eq => Expression.Equal(dateProperty, value),
                Keywords.Ne => Expression.NotEqual(dateProperty, value),
                Keywords.Lt => Expression.LessThan(dateProperty, value),
                Keywords.Lte => Expression.LessThanOrEqual(dateProperty, value),
                Keywords.Gt => Expression.GreaterThan(dateProperty, value),
                Keywords.Gte => Expression.GreaterThanOrEqual(dateProperty, value),
                _ => throw new ArgumentException($"Unsupported operator for nullable date comparison: {exp.Operator}")
            };

            return exp.Operator == Keywords.Ne
                ? Expression.OrElse(Expression.Not(hasValueProperty), dateComparison)
                : Expression.AndAlso(hasValueProperty, dateComparison);
        }

        return exp.Operator switch
        {
            Keywords.Eq => Expression.Equal(updatedProperty, value),
            Keywords.Ne => Expression.NotEqual(updatedProperty, value),
            Keywords.Contains => CreateContainsExpression(updatedProperty, value),
            Keywords.Lt => Expression.LessThan(updatedProperty, value),
            Keywords.Lte => Expression.LessThanOrEqual(updatedProperty, value),
            Keywords.Gt => Expression.GreaterThan(updatedProperty, value),
            Keywords.Gte => Expression.GreaterThanOrEqual(updatedProperty, value),
            _ => Result.Fail($"Unsupported operator: {exp.Operator}")
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
            var dateProperty = Expression.Property(property, "Date");
            return (Expression.Constant(dateLiteral.Value.Date, dateProperty.Type), dateProperty);
        }
    }

    private static Expression CreateContainsExpression(MemberExpression property, ConstantExpression value)
    {
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
        var propertyToLower = Expression.Call(property, toLowerMethod);
        var valueToLower = Expression.Call(value, toLowerMethod);
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        return Expression.Call(propertyToLower, containsMethod, valueToLower);
    }

    public static Result<Expression> Evaluate(QueryExpression expression, ParameterExpression parameterExpression, Dictionary<string, string> propertyMapping)
    {
        switch (expression)
        {
            case InfixExpression exp when exp.Left is PropertyPath:
                var propertyPath = exp.Left as PropertyPath;
                return EvaluatePropertyPathExpression(exp, propertyPath, parameterExpression, propertyMapping);

            case InfixExpression exp when exp.Left is Identifier:
                if (!propertyMapping.TryGetValue(exp.Left.TokenLiteral(), out var propertyName))
                {
                    return Result.Fail($"Invalid property '{exp.Left.TokenLiteral()}' within filter");
                }

                var identifierProperty = Expression.Property(parameterExpression, propertyName);
                return EvaluateInfixExpression(exp, identifierProperty);
            case InfixExpression exp:
                // Handle logical operators (AND/OR)
                var left = Evaluate(exp.Left, parameterExpression, propertyMapping);
                if (left.IsFailed)
                {
                    return left;
                }

                var right = Evaluate(exp.Right, parameterExpression, propertyMapping);
                if (right.IsFailed)
                {
                    return right;
                }

                switch (exp.Operator)
                {
                    case Keywords.And:
                        return Expression.AndAlso(left.Value, right.Value);
                    case Keywords.Or:
                        return Expression.OrElse(left.Value, right.Value);
                    default:
                        return Result.Fail($"Unsupported logical operator: {exp.Operator}");
                }
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