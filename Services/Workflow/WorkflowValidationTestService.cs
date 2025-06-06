using BugTracker.Models.Workflow;
using System.Text.Json;

namespace BugTracker.Services.Workflow;

/// <summary>
/// Service for testing and validating workflow definitions
/// </summary>
public class WorkflowValidationTestService
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowRuleEngine _ruleEngine;
    private readonly ILogger<WorkflowValidationTestService> _logger;

    public WorkflowValidationTestService(
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowRuleEngine ruleEngine,
        ILogger<WorkflowValidationTestService> logger)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    /// <summary>
    /// Tests the bug assessment workflow with the Major severity scenario
    /// </summary>
    public async Task<WorkflowTestResult> TestMajorSeverityBugScenario()
    {
        var result = new WorkflowTestResult
        {
            TestName = "Major Severity Bug Assessment",
            TestDescription = "Tests that Major severity bugs are correctly routed to 'Keep as New + ImpactConfirmed'"
        };

        try
        {
            // Load the bug assessment workflow
            var workflowDefinition = await _workflowDefinitionService.LoadWorkflowDefinitionAsync("Bug Assessment Workflow");
            if (workflowDefinition == null)
            {
                result.Success = false;
                result.ErrorMessage = "Bug Assessment Workflow not found";
                return result;
            }

            var schema = workflowDefinition.GetWorkflowSchema();
            
            // Create a temporary workflow definition for validation
            var tempDefinition = new WorkflowDefinition
            {
                Name = schema.Name,
                Description = schema.Description,
                Version = "1.0.0"
            };
            tempDefinition.SetWorkflowSchema(schema);
            
            // Validate the workflow definition
            var isValid = await _workflowDefinitionService.ValidateWorkflowDefinitionAsync(tempDefinition);
            if (!isValid)
            {
                result.Success = false;
                result.ErrorMessage = "Workflow definition is invalid";
                return result;
            }

            // Test context for Major severity bug
            var testContext = new Dictionary<string, object>
            {
                ["bugSeverity"] = "Major",
                ["productVersion"] = "2024.1.2",
                ["affectedVersions"] = new List<string> { "2024.1.2", "2024.1.3" },
                ["versionAffected"] = true,
                ["severityIsMajorOrCritical"] = true
            };

            // Test the complete workflow path
            var pathResult = await TestWorkflowPath(schema, testContext);
            
            result.Success = pathResult.Success;
            result.ErrorMessage = pathResult.ErrorMessage;
            result.ExecutionPath = pathResult.ExecutionPath;
            result.ExpectedFinalStep = "keep-as-new";
            result.ActualFinalStep = pathResult.FinalStepId;

            if (result.Success && result.ActualFinalStep != result.ExpectedFinalStep)
            {
                result.Success = false;
                result.ErrorMessage = $"Expected final step '{result.ExpectedFinalStep}' but got '{result.ActualFinalStep}'";
            }

            _logger.LogInformation("Major severity bug test completed. Success: {Success}", result.Success);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Test execution failed: {ex.Message}";
            _logger.LogError(ex, "Error during Major severity bug test");
        }

        return result;
    }

    /// <summary>
    /// Tests workflow path execution from start to finish
    /// </summary>
    private async Task<WorkflowPathTestResult> TestWorkflowPath(WorkflowSchema schema, Dictionary<string, object> context)
    {
        var result = new WorkflowPathTestResult();
        var executionPath = new List<string>();

        try
        {
            var currentStepId = schema.InitialStepId;
            var maxSteps = 20; // Prevent infinite loops
            var stepCount = 0;

            while (!string.IsNullOrEmpty(currentStepId) && stepCount < maxSteps)
            {
                stepCount++;
                var currentStep = schema.Steps.FirstOrDefault(s => s.StepId == currentStepId);
                if (currentStep == null)
                {
                    result.ErrorMessage = $"Step not found: {currentStepId}";
                    return result;
                }

                executionPath.Add($"{currentStep.Name} ({currentStepId})");

                // If this is a terminal step, we're done
                if (currentStep.IsTerminal)
                {
                    result.Success = true;
                    result.FinalStepId = currentStepId;
                    break;
                }

                // Find the next step based on the current step type
                currentStepId = await DetermineNextStepInTest(schema, currentStep, context);
                
                if (string.IsNullOrEmpty(currentStepId))
                {
                    result.ErrorMessage = $"No valid transition found from step: {currentStep.Name}";
                    return result;
                }
            }

            if (stepCount >= maxSteps)
            {
                result.ErrorMessage = "Workflow execution exceeded maximum steps (possible infinite loop)";
                return result;
            }

            result.ExecutionPath = executionPath;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Path execution error: {ex.Message}";
        }

        return result;
    }

    private async Task<string?> DetermineNextStepInTest(WorkflowSchema schema, WorkflowStepDefinition currentStep, Dictionary<string, object> context)
    {
        var transitions = schema.Transitions.Where(t => t.FromStepId == currentStep.StepId).ToList();

        foreach (var transition in transitions)
        {
            // Determine if this transition should be taken based on step type
            bool shouldTakeTransition = currentStep.Type switch
            {
                WorkflowStepType.AutoCheck => await ShouldTakeAutoCheckTransition(transition, context),
                WorkflowStepType.Decision => ShouldTakeDecisionTransition(transition, currentStep, context),
                WorkflowStepType.Manual => transition.TriggerAction == "complete",
                _ => transition.TriggerAction == "complete"
            };

            if (shouldTakeTransition)
            {
                // Evaluate transition conditions
                if (transition.Conditions.Any())
                {
                    var conditionsMet = await _ruleEngine.EvaluateConditionsAsync(transition.Conditions, context);
                    if (conditionsMet)
                    {
                        return transition.ToStepId;
                    }
                }
                else
                {
                    return transition.ToStepId;
                }
            }
        }

        return null;
    }

    private async Task<bool> ShouldTakeAutoCheckTransition(WorkflowTransition transition, Dictionary<string, object> context)
    {
        // For auto-check steps, determine based on the context
        if (transition.FromStepId == "version-check")
        {
            var versionAffected = context.ContainsKey("versionAffected") && (bool)context["versionAffected"];
            return (versionAffected && transition.ToStepId == "clone-bug") ||
                   (!versionAffected && transition.ToStepId == "not-affected-terminal");
        }
        
        if (transition.FromStepId == "check-severity")
        {
            var isMajorOrCritical = context.ContainsKey("severityIsMajorOrCritical") && (bool)context["severityIsMajorOrCritical"];
            return (isMajorOrCritical && transition.ToStepId == "keep-as-new") ||
                   (!isMajorOrCritical && transition.ToStepId == "close-wont-fix");
        }

        return transition.TriggerAction == "auto_evaluate";
    }

    private bool ShouldTakeDecisionTransition(WorkflowTransition transition, WorkflowStepDefinition currentStep, Dictionary<string, object> context)
    {
        // For test purposes, make "optimistic" decisions that lead to the main path
        if (currentStep.StepId == "check-preconditions")
        {
            return transition.TriggerAction == "decide_yes"; // Assume preconditions apply
        }
        
        if (currentStep.StepId == "test-reproduction")
        {
            return transition.TriggerAction == "decide_yes"; // Assume bug reproduces
        }

        return transition.TriggerAction == "decide_yes";
    }

    /// <summary>
    /// Runs all workflow validation tests
    /// </summary>
    public async Task<List<WorkflowTestResult>> RunAllTestsAsync()
    {
        var results = new List<WorkflowTestResult>();

        try
        {
            // Test 1: Major severity scenario
            results.Add(await TestMajorSeverityBugScenario());

            // Test 2: Minor severity scenario (should go to Won't Fix)
            results.Add(await TestMinorSeverityBugScenario());

            // Test 3: Version not affected scenario
            results.Add(await TestVersionNotAffectedScenario());

            _logger.LogInformation("Completed {TestCount} workflow validation tests. Passed: {PassedCount}", 
                results.Count, results.Count(r => r.Success));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running workflow validation tests");
        }

        return results;
    }

    private async Task<WorkflowTestResult> TestMinorSeverityBugScenario()
    {
        var result = new WorkflowTestResult
        {
            TestName = "Minor Severity Bug Assessment",
            TestDescription = "Tests that Minor severity bugs are correctly routed to 'Close as Won't Fix + ImpactConfirmed'"
        };

        var testContext = new Dictionary<string, object>
        {
            ["bugSeverity"] = "Minor",
            ["productVersion"] = "2024.1.2",
            ["affectedVersions"] = new List<string> { "2024.1.2", "2024.1.3" },
            ["versionAffected"] = true,
            ["severityIsMajorOrCritical"] = false
        };

        var workflowDefinition = await _workflowDefinitionService.LoadWorkflowDefinitionAsync("Bug Assessment Workflow");
        var schema = workflowDefinition!.GetWorkflowSchema();
        var pathResult = await TestWorkflowPath(schema, testContext);

        result.Success = pathResult.Success;
        result.ErrorMessage = pathResult.ErrorMessage;
        result.ExecutionPath = pathResult.ExecutionPath;
        result.ExpectedFinalStep = "close-wont-fix";
        result.ActualFinalStep = pathResult.FinalStepId;

        if (result.Success && result.ActualFinalStep != result.ExpectedFinalStep)
        {
            result.Success = false;
            result.ErrorMessage = $"Expected final step '{result.ExpectedFinalStep}' but got '{result.ActualFinalStep}'";
        }

        return result;
    }

    private async Task<WorkflowTestResult> TestVersionNotAffectedScenario()
    {
        var result = new WorkflowTestResult
        {
            TestName = "Version Not Affected Scenario",
            TestDescription = "Tests that unaffected versions are correctly routed to 'Don't Clone Bug'"
        };

        var testContext = new Dictionary<string, object>
        {
            ["bugSeverity"] = "Major",
            ["productVersion"] = "2024.2.0",
            ["affectedVersions"] = new List<string> { "2024.1.2", "2024.1.3" },
            ["versionAffected"] = false,
            ["severityIsMajorOrCritical"] = true
        };

        var workflowDefinition = await _workflowDefinitionService.LoadWorkflowDefinitionAsync("Bug Assessment Workflow");
        var schema = workflowDefinition!.GetWorkflowSchema();
        var pathResult = await TestWorkflowPath(schema, testContext);

        result.Success = pathResult.Success;
        result.ErrorMessage = pathResult.ErrorMessage;
        result.ExecutionPath = pathResult.ExecutionPath;
        result.ExpectedFinalStep = "not-affected-terminal";
        result.ActualFinalStep = pathResult.FinalStepId;

        if (result.Success && result.ActualFinalStep != result.ExpectedFinalStep)
        {
            result.Success = false;
            result.ErrorMessage = $"Expected final step '{result.ExpectedFinalStep}' but got '{result.ActualFinalStep}'";
        }

        return result;
    }
}

public class WorkflowTestResult
{
    public string TestName { get; set; } = string.Empty;
    public string TestDescription { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ExecutionPath { get; set; } = new();
    public string? ExpectedFinalStep { get; set; }
    public string? ActualFinalStep { get; set; }
}

public class WorkflowPathTestResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ExecutionPath { get; set; } = new();
    public string? FinalStepId { get; set; }
}