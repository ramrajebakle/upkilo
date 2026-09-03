using System.Linq;
using System.Reflection;
using FluentAssertions;
using Upkilo.API.Controllers;
using Upkilo.Core.Interfaces;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// Keeps the AI stack out of the constructors of controllers that are not about AI.
///
/// IAIService resolves AzureOpenAIService, which depends on ContentModerationService. A
/// CONSTRUCTOR dependency is resolved for every action on the controller, so a single
/// AI-backed endpoint dragged the whole AI stack into every unrelated request. When
/// ContentModerationService threw in Production, GET /api/v1/services answered 400 and the
/// Services page showed "Couldn't load services" — a CRUD listing broken by a dependency it
/// never used. VerticalsController injected IAIService and did not use it at all.
///
/// AI endpoints take it via [FromServices] instead, so a failure in the AI stack can only
/// reach the endpoints that actually use AI.
/// </summary>
public class AiDependencyScopeTests
{
    /// <summary>
    /// AIController is genuinely an AI controller — 12 of its 17 endpoints call the service —
    /// so constructor injection is right there and it is exempt.
    /// </summary>
    private static readonly string[] AllowedConstructorInjection = { "AIController" };

    private static bool IsController(System.Type t) =>
        typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t) && !t.IsAbstract;

    [Fact]
    public void NonAiControllers_DoNotTakeIAIServiceInTheirConstructor()
    {
        var offenders = typeof(ServicesController).Assembly.GetTypes()
            .Where(IsController)
            .Where(t => !AllowedConstructorInjection.Contains(t.Name))
            .Where(t => t.GetConstructors()
                .Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(IAIService))))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        offenders.Should().BeEmpty(
            "a constructor dependency on IAIService is resolved for EVERY action, so a fault in "
            + "the AI stack breaks unrelated endpoints. Take it as [FromServices] on the action "
            + "that needs it.");
    }

    [Fact]
    public void ServicesController_IsFreeOfTheAiStack()
    {
        // The specific regression: a plain GET /api/v1/services must not construct AI.
        typeof(ServicesController).GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Should().NotContain(p => p.ParameterType == typeof(IAIService));
    }

    [Fact]
    public void TheExemptionListDoesNotSilentlyGrow()
    {
        // If this list grows, the guarantee above is being eroded rather than upheld.
        AllowedConstructorInjection.Should().HaveCount(1);
    }
}
