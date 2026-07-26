using MediatR;

namespace Upkilo.Core.Interfaces.CQRS;

/// <summary>
/// Represents a CQRS Query that returns a specific response type.
/// </summary>
public interface IQuery<TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Represents a handler for a CQRS Query.
/// </summary>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}

/// <summary>
/// Represents a CQRS Command that returns a specific response type (or Unit).
/// </summary>
public interface ICommand<TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Represents a handler for a CQRS Command.
/// </summary>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}
