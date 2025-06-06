using BugTracker.Models.Workflow;

namespace BugTracker.Services.Workflow;

/// <summary>
/// Service interface for evaluating workflow rules and conditions
/// </summary>
public interface IWorkflowRuleEngine
{
    /// <summary>
    /// Evaluates workflow conditions against the given context
    /// </summary>
    Task<bool> EvaluateConditionsAsync(List<WorkflowCondition> conditions, Dictionary<string, object> context);
    
    /// <summary>
    /// Evaluates a single workflow condition
    /// </summary>
    Task<bool> EvaluateConditionAsync(WorkflowCondition condition, Dictionary<string, object> context);
    
    /// <summary>
    /// Validates workflow validation rules
    /// </summary>
    Task<List<string>> ValidateRulesAsync(List<WorkflowValidationRule> rules, Dictionary<string, object> context);
    
    /// <summary>
    /// Determines the next step based on workflow transitions and current context
    /// </summary>
    Task<string?> DetermineNextStepAsync(List<WorkflowTransition> transitions, string currentStepId, string actionId, Dictionary<string, object> context);
    
    /// <summary>
    /// Evaluates an expression in the given context
    /// </summary>
    Task<object?> EvaluateExpressionAsync(string expression, Dictionary<string, object> context);
}