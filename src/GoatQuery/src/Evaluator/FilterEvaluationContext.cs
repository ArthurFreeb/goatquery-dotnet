using System;
using System.Collections.Generic;
using System.Linq.Expressions;

internal class FilterEvaluationContext
{
    public ParameterExpression RootParameter { get; }
    public PropertyMappingTree PropertyMappingTree { get; }
    public Stack<LambdaScope> LambdaScopes { get; } = new Stack<LambdaScope>();

    public FilterEvaluationContext(ParameterExpression rootParameter, PropertyMappingTree propertyMappingTree)
    {
        RootParameter = rootParameter;
        PropertyMappingTree = propertyMappingTree;
    }

    public bool IsInLambdaScope => LambdaScopes.Count > 0;
    public LambdaScope CurrentLambda => LambdaScopes.Peek();

    public void EnterLambdaScope(string parameterName, ParameterExpression parameter, Type elementType)
    {
        LambdaScopes.Push(new LambdaScope
        {
            ParameterName = parameterName,
            Parameter = parameter,
            ElementType = elementType
        });
    }

    public void ExitLambdaScope() => LambdaScopes.Pop();
}

internal class LambdaScope
{
    public string ParameterName { get; set; }
    public ParameterExpression Parameter { get; set; }
    public Type ElementType { get; set; }
}