using BugTracker.Models.Workflow;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BugTracker.Services.Workflow;

/// <summary>
/// Rule engine for evaluating workflow conditions and validation rules
/// </summary>
public class WorkflowRuleEngineService : IWorkflowRuleEngine
{
    private readonly ILogger<WorkflowRuleEngineService> _logger;

    public WorkflowRuleEngineService(ILogger<WorkflowRuleEngineService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> EvaluateConditionsAsync(List<WorkflowCondition> conditions, Dictionary<string, object> context)
    {
        if (!conditions.Any())
            return true;

        var results = new List<bool>();
        WorkflowConditionLogic? currentLogic = null;

        foreach (var condition in conditions)
        {
            var result = await EvaluateConditionAsync(condition, context);
            
            if (currentLogic == null)
            {
                results.Add(result);
                currentLogic = condition.Logic;
            }
            else if (currentLogic == WorkflowConditionLogic.And)
            {
                // For AND logic, combine with previous result
                var previousResult = results.LastOrDefault();
                results[results.Count - 1] = previousResult && result;
                currentLogic = condition.Logic;
            }
            else if (currentLogic == WorkflowConditionLogic.Or)
            {
                // For OR logic, add as separate result
                results.Add(result);
                currentLogic = condition.Logic;
            }
        }

        // Final evaluation: if any OR group is true, return true
        // If all are AND groups, all must be true
        return results.Any(r => r);
    }

    public async Task<bool> EvaluateConditionAsync(WorkflowCondition condition, Dictionary<string, object> context)
    {
        try
        {
            var actualValue = GetValueFromContext(condition.Field, context);
            var expectedValue = condition.Value;

            var result = condition.Operator switch
            {
                WorkflowConditionOperator.Equals => AreEqual(actualValue, expectedValue),
                WorkflowConditionOperator.NotEquals => !AreEqual(actualValue, expectedValue),
                WorkflowConditionOperator.GreaterThan => CompareValues(actualValue, expectedValue) > 0,
                WorkflowConditionOperator.LessThan => CompareValues(actualValue, expectedValue) < 0,
                WorkflowConditionOperator.GreaterThanOrEqual => CompareValues(actualValue, expectedValue) >= 0,
                WorkflowConditionOperator.LessThanOrEqual => CompareValues(actualValue, expectedValue) <= 0,
                WorkflowConditionOperator.Contains => Contains(actualValue, expectedValue),
                WorkflowConditionOperator.NotContains => !Contains(actualValue, expectedValue),
                WorkflowConditionOperator.StartsWith => StartsWith(actualValue, expectedValue),
                WorkflowConditionOperator.EndsWith => EndsWith(actualValue, expectedValue),
                WorkflowConditionOperator.In => IsIn(actualValue, expectedValue),
                WorkflowConditionOperator.NotIn => !IsIn(actualValue, expectedValue),
                WorkflowConditionOperator.IsNull => actualValue == null,
                WorkflowConditionOperator.IsNotNull => actualValue != null,
                _ => false
            };

            _logger.LogDebug("Condition evaluation: {Field} {Operator} {Expected} = {Result} (Actual: {Actual})", 
                condition.Field, condition.Operator, expectedValue, result, actualValue);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating condition {ConditionId}: {Field} {Operator} {Value}", 
                condition.ConditionId, condition.Field, condition.Operator, condition.Value);
            return false;
        }
    }

    public async Task<WorkflowValidationResult> ValidateInputAsync(List<WorkflowValidationRule> rules, Dictionary<string, object> input)
    {
        var result = new WorkflowValidationResult();

        foreach (var rule in rules)
        {
            try
            {
                var value = GetValueFromContext(rule.Field, input);
                var isValid = await ValidateRuleAsync(rule, value);

                if (!isValid)
                {
                    result.IsValid = false;
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = rule.Field,
                        ErrorCode = rule.Type.ToString(),
                        Message = string.IsNullOrEmpty(rule.ErrorMessage) 
                            ? $"Validation failed for field {rule.Field}" 
                            : rule.ErrorMessage,
                        Value = value
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating rule {RuleId} for field {Field}", rule.RuleId, rule.Field);
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = rule.Field,
                    ErrorCode = "VALIDATION_ERROR",
                    Message = "An error occurred during validation",
                    Value = null
                });
            }
        }

        return result;
    }

    private async Task<bool> ValidateRuleAsync(WorkflowValidationRule rule, object? value)
    {
        return rule.Type switch
        {
            WorkflowValidationType.Required => value != null && !string.IsNullOrWhiteSpace(value.ToString()),
            WorkflowValidationType.MinLength => ValidateMinLength(value, rule.Value),
            WorkflowValidationType.MaxLength => ValidateMaxLength(value, rule.Value),
            WorkflowValidationType.Pattern => ValidatePattern(value, rule.Value),
            WorkflowValidationType.Range => ValidateRange(value, rule.Value),
            WorkflowValidationType.Custom => await ValidateCustomRule(rule, value),
            _ => true
        };
    }

    private object? GetValueFromContext(string field, Dictionary<string, object> context)
    {
        // Support nested field access with dot notation
        var fieldParts = field.Split('.');
        object? current = context;

        foreach (var part in fieldParts)
        {
            if (current is Dictionary<string, object> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return null;
            }
            else if (current != null)
            {
                // Try to access property using reflection for complex objects
                var property = current.GetType().GetProperty(part);
                if (property != null)
                {
                    current = property.GetValue(current);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    private bool AreEqual(object? actual, object? expected)
    {
        if (actual == null && expected == null) return true;
        if (actual == null || expected == null) return false;

        // Handle different types
        if (actual.GetType() != expected.GetType())
        {
            // Try to convert for comparison
            try
            {
                if (expected is string expectedStr)
                {
                    return actual.ToString() == expectedStr;
                }
                if (actual is string actualStr && IsNumeric(expectedStr))
                {
                    return double.Parse(actualStr) == Convert.ToDouble(expected);
                }
            }
            catch
            {
                // Conversion failed, try direct comparison
            }
        }

        return actual.Equals(expected);
    }

    private int CompareValues(object? actual, object? expected)
    {
        if (actual == null && expected == null) return 0;
        if (actual == null) return -1;
        if (expected == null) return 1;

        // Handle numeric comparisons
        if (IsNumeric(actual) && IsNumeric(expected))
        {
            var actualNum = Convert.ToDouble(actual);
            var expectedNum = Convert.ToDouble(expected);
            return actualNum.CompareTo(expectedNum);
        }

        // Handle string comparisons
        if (actual is string actualStr && expected is string expectedStr)
        {
            return string.Compare(actualStr, expectedStr, StringComparison.OrdinalIgnoreCase);
        }

        // Handle DateTime comparisons
        if (actual is DateTime actualDate && expected is DateTime expectedDate)
        {
            return actualDate.CompareTo(expectedDate);
        }

        // Default string comparison
        return string.Compare(actual.ToString(), expected.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private bool Contains(object? actual, object? expected)
    {
        if (actual == null || expected == null) return false;

        var actualStr = actual.ToString();
        var expectedStr = expected.ToString();

        return actualStr?.Contains(expectedStr, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private bool StartsWith(object? actual, object? expected)
    {
        if (actual == null || expected == null) return false;

        var actualStr = actual.ToString();
        var expectedStr = expected.ToString();

        return actualStr?.StartsWith(expectedStr, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private bool EndsWith(object? actual, object? expected)
    {
        if (actual == null || expected == null) return false;

        var actualStr = actual.ToString();
        var expectedStr = expected.ToString();

        return actualStr?.EndsWith(expectedStr, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private bool IsIn(object? actual, object? expected)
    {
        if (actual == null || expected == null) return false;

        // Handle array/list of values
        if (expected is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonElement.EnumerateArray())
            {
                if (AreEqual(actual, item.GetString()))
                    return true;
            }
            return false;
        }

        // Handle comma-separated string
        if (expected is string expectedStr)
        {
            var values = expectedStr.Split(',').Select(v => v.Trim());
            return values.Any(v => AreEqual(actual, v));
        }

        return false;
    }

    private bool ValidateMinLength(object? value, object? minLengthObj)
    {
        if (value == null) return false;
        if (!int.TryParse(minLengthObj?.ToString(), out var minLength)) return true;

        var str = value.ToString();
        return str?.Length >= minLength;
    }

    private bool ValidateMaxLength(object? value, object? maxLengthObj)
    {
        if (value == null) return true; // null values are valid for max length
        if (!int.TryParse(maxLengthObj?.ToString(), out var maxLength)) return true;

        var str = value.ToString();
        return str?.Length <= maxLength;
    }

    private bool ValidatePattern(object? value, object? patternObj)
    {
        if (value == null) return false;
        var pattern = patternObj?.ToString();
        if (string.IsNullOrEmpty(pattern)) return true;

        try
        {
            var regex = new Regex(pattern);
            return regex.IsMatch(value.ToString() ?? string.Empty);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid regex pattern: {Pattern}", pattern);
            return false;
        }
    }

    private bool ValidateRange(object? value, object? rangeObj)
    {
        if (value == null) return false;
        if (!IsNumeric(value)) return false;

        try
        {
            var actualNum = Convert.ToDouble(value);
            
            // Range should be in format "min,max" or just "min" for minimum only
            var rangeStr = rangeObj?.ToString();
            if (string.IsNullOrEmpty(rangeStr)) return true;

            var rangeParts = rangeStr.Split(',');
            if (rangeParts.Length == 1)
            {
                // Minimum only
                if (double.TryParse(rangeParts[0], out var min))
                {
                    return actualNum >= min;
                }
            }
            else if (rangeParts.Length == 2)
            {
                // Min and max
                if (double.TryParse(rangeParts[0], out var min) && double.TryParse(rangeParts[1], out var max))
                {
                    return actualNum >= min && actualNum <= max;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ValidateCustomRule(WorkflowValidationRule rule, object? value)
    {
        // For now, custom rules always pass
        // This can be extended to support custom validation logic
        _logger.LogDebug("Custom validation rule {RuleId} for field {Field} - defaulting to true", rule.RuleId, rule.Field);
        return true;
    }

    private bool IsNumeric(object? value)
    {
        if (value == null) return false;
        return double.TryParse(value.ToString(), out _);
    }
}