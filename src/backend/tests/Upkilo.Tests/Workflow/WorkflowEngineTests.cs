using FluentAssertions;
using Upkilo.Core.Interfaces.Workflow;
using Upkilo.Infrastructure.Workflow;

namespace Upkilo.Tests.Workflow;

/// <summary>
/// Tests for WorkflowConditionEngine — evaluates dynamic expressions against workflow context state.
/// </summary>
public class WorkflowConditionEngineTests
{
    private readonly WorkflowConditionEngine _sut = new();

    [Fact]
    public void EvaluateCondition_EmptyExpression_ReturnsTrue()
    {
        var context = new WorkflowContext { State = new Dictionary<string, object>() };
        _sut.EvaluateCondition("", context).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_NullExpression_ReturnsTrue()
    {
        var context = new WorkflowContext { State = new Dictionary<string, object>() };
        _sut.EvaluateCondition(null!, context).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_WhitespaceExpression_ReturnsTrue()
    {
        var context = new WorkflowContext { State = new Dictionary<string, object>() };
        _sut.EvaluateCondition("   ", context).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_InvalidExpression_ReturnsFalse()
    {
        var context = new WorkflowContext
        {
            State = new Dictionary<string, object> { ["Price"] = 100 }
        };
        // Malformed expression should fail safely
        _sut.EvaluateCondition("this is not valid !!!", context).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_NullState_ReturnsFalseOnPropertyAccess()
    {
        var context = new WorkflowContext { State = null! };
        _sut.EvaluateCondition("Price > 100", context).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_EmptyState_ReturnsFalseOnPropertyAccess()
    {
        var context = new WorkflowContext { State = new Dictionary<string, object>() };
        _sut.EvaluateCondition("ctx[\"Price\"] > 100", context).Should().BeFalse();
    }
}

/// <summary>
/// Tests for JsonWorkflowParser — JSON parsing and validation.
/// </summary>
public class JsonWorkflowParserTests
{
    private readonly JsonWorkflowParser _sut = new();

    [Fact]
    public void ParseSteps_EmptyString_ReturnsEmptyList()
    {
        _sut.ParseSteps("").Should().BeEmpty();
    }

    [Fact]
    public void ParseSteps_NullString_ReturnsEmptyList()
    {
        _sut.ParseSteps(null!).Should().BeEmpty();
    }

    [Fact]
    public void ParseSteps_WhitespaceString_ReturnsEmptyList()
    {
        _sut.ParseSteps("   ").Should().BeEmpty();
    }

    [Fact]
    public void ParseSteps_InvalidJson_ReturnsEmptyList()
    {
        _sut.ParseSteps("not valid json at all").Should().BeEmpty();
    }

    [Fact]
    public void ParseSteps_MalformedJson_ReturnsEmptyList()
    {
        _sut.ParseSteps("{ invalid: }").Should().BeEmpty();
    }

    [Fact]
    public void ValidateConfig_ValidJson_ReturnsTrue()
    {
        var result = _sut.ValidateConfig("{\"name\": \"test\"}", out var error);
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateConfig_ValidJsonArray_ReturnsTrue()
    {
        var result = _sut.ValidateConfig("[{\"StepName\":\"s1\",\"StepType\":\"Email\"}]", out var error);
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateConfig_InvalidJson_ReturnsFalse()
    {
        var result = _sut.ValidateConfig("not json", out var error);
        result.Should().BeFalse();
        error.Should().Contain("Invalid JSON format");
    }

    [Fact]
    public void ValidateConfig_EmptyObject_ReturnsTrue()
    {
        var result = _sut.ValidateConfig("{}", out var error);
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateConfig_EmptyArray_ReturnsTrue()
    {
        var result = _sut.ValidateConfig("[]", out var error);
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateConfig_TrailingComma_ReturnsFalse()
    {
        var result = _sut.ValidateConfig("{\"key\": \"value\",}", out var error);
        result.Should().BeFalse();
        error.Should().Contain("Invalid JSON format");
    }

    [Fact]
    public void ValidateConfig_NestedJson_ReturnsTrue()
    {
        var json = """
        {
            "steps": [
                {"StepName": "Send Email", "StepType": "Email"},
                {"StepName": "Wait 1 Day", "StepType": "Delay"}
            ]
        }
        """;
        var result = _sut.ValidateConfig(json, out var error);
        result.Should().BeTrue();
    }
}
