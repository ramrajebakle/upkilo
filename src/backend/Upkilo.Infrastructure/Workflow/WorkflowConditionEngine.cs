using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Collections.Generic;
using Upkilo.Core.Interfaces.Workflow;

namespace Upkilo.Infrastructure.Workflow;

public class WorkflowConditionEngine
{
    private readonly ParsingConfig _config;

    public WorkflowConditionEngine()
    {
        _config = new ParsingConfig();
    }

    /// <summary>
    /// Evaluates dynamic expressions like 'Booking.Price > 100' or 'Client.Type == VIP'
    /// </summary>
    public bool EvaluateCondition(string expression, WorkflowContext context)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;

        try
        {
            // We use System.Linq.Dynamic.Core to evaluate the expression against the context state
            // we wrap the state in a dynamic object for easier access in the expression
            
            var state = context.State ?? new Dictionary<string, object>();
            
            // To make expressions more readable, we can flatten nested objects if needed,
            // or just ensure the expression matches the dictionary keys.
            // Example: "PaymentStatus == 'Failed'" maps to state["PaymentStatus"]
            
            var p = System.Linq.Expressions.Expression.Parameter(typeof(IDictionary<string, object>), "ctx");
            
            // Dynamic LINQ can work directly on dictionaries if we use the correct syntax [ "Key" ]
            // However, a more user-friendly way is to create a dynamic object or use a custom property resolver.
            
            // For Upkilo, we'll support direct field access: "Price > 100"
            // We'll use a simple approach: compile a lambda that takes the state.
            
            var lambda = DynamicExpressionParser.ParseLambda(_config, false, new[] { p }, typeof(bool), expression);
            return (bool)lambda.Compile().DynamicInvoke(state)!;
        }
        catch (Exception ex)
        {
            // Silently fail evaluation for safety, log if needed
            System.Diagnostics.Debug.WriteLine($"Condition evaluation failed: {ex.Message}");
            return false;
        }
    }
}

