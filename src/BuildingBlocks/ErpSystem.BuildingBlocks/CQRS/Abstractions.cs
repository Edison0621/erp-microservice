using MediatR;

namespace ErpSystem.BuildingBlocks.CQRS;

/// <summary>
/// CQRS Abstractions - Command Query Responsibility Segregation base types.
/// </summary>

/// <summary>
/// Base interface for all commands (write operations)
/// </summary>
public interface ICommand : IRequest { }

/// <summary>
/// Base interface for commands with return value
/// </summary>
public interface ICommand<out TResult> : IRequest<TResult> { }

/// <summary>
/// Base interface for all queries (read operations)
/// </summary>
public interface IQuery<out TResult> : IRequest<TResult> { }

/// <summary>
/// Command handler base interface
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand { }

/// <summary>
/// Command handler with result
/// </summary>
public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult> { }

/// <summary>
/// Query handler base interface
/// </summary>
public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult> { }
