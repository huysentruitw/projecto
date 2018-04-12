# Projecto

[![Build status](https://ci.appveyor.com/api/projects/status/ikjbo1a07y33jt0o/branch/master?svg=true)](https://ci.appveyor.com/project/huysentruitw/projecto/branch/master)

## Overview

Managed .NET 4.7.1 / .NET Core library for handling CQRS/ES projections while maintaining the event sequence order.

## Get it on [NuGet](https://www.nuget.org/packages/Projecto/)

    PM> Install-Package Projecto

## Concepts / usage

### MessageEnvelope

A message envelope is a user-defined object (must derive from MessageEnvelope) that wraps a message before it is passed to `projector.Project()` which in turn passes the envelope to the `When<>()` handler inside the projection.

It's a convenient way to pass out-of-band data to the handler (f.e. the originating command id, or creation date of the event/message).

### Projection

Inherit from `Projection<string, TConnection, TMessageEnvelope>` to define a projection, where `string` is the type of the unique key for identifying projections, `TConnection` is the connection type used by the projection and `TMessageEnvelope` is the user-defined message envelope object (see previous topic).

```csharp
public class ExampleProjection : Projection<string, ApplicationDbContext, MyMessageEnvelope>
{
    public ExampleProjection()
    {
        When<CreatedUserProfileEvent>(async (dataContext, envelope, @event) =>
        {
            dataContext.UserProfiles.Add(...);

            // Data can be saved directly or during collection disposal for batching queries (see `ProjectScope`)
            await dataContext.SaveChangesAsync();
        });
    }
}
```

### Projector

The projector reflects the sequence number of the most out-dated projection.

Use the projector to project one or more events/messages to all registered projections.

The projector ensures that it only handles events/messages in the correct order, starting with event/message with a sequence number equal to the number as returned from `GetNextSequenceNumber()`.

The number returned by the `GetNextSequenceNumber()` method of the projector can be used by the application to request a resend of missing events/messages during startup.

The `ProjectorBuilder` class is used to build a projector instance.

### ProjectorBuilder

```csharp
var disposalCallbacks = new ExampleCollectionDisposalCallbacks();

var projector = new ProjectorBuilder()
    .Register(new ExampleProjection())
    .SetDependencyLifetimeScopeFactory(new ExampleDependencyLifetimeScopeFactory())
    .Build<MyNextSequenceNumberRepository>();
```

### DependencyLifetimeScope

A dependency lifetime scope is a scope that is created and disposed inside the call to `projector.Project`. The projector uses the `DependencyLifetimeScopeFactory` to create a disposable `DependencyLifetimeScope`. The dependency lifetime scope is responsible for resolving the NextSequenceNumberRepository as well as connections on request by one or more projections and has `ScopeEnded` and `DependencyResolved` events to have more control over the lifetime of the used dependencies.

```csharp
public class ExampleDependencyLifetimeScopeFactory : IDependencyLifetimeScopeFactory
{
    public IDependencyLifetimeScope BeginLifetimeScope()
    {
        return new ExampleDependencyLifetimeScope();
    }
}

public class ExampleDependencyLifetimeScope : IDependencyLifetimeScope
{
    public ExampleDependencyLifetimeScope()
    {
    }

    public void Dispose()
    {
        // Called when the project scope gets disposed
    }

    public object Resolve(Type dependencyType)
    {
        if (dependencyType == typeof(ApplicationDbContext)) return new ApplicationDbContext();
        if (dependencyType == typeof(MyNextSequenceNumberRepository)) return new MyNextSequenceNumberRepository();
        throw new Exception($"Can't resolve unknown dependency type {dependencyType.Name}");
    }
}
```

### Autofac integration

An additional package exists for integration with Autofac. Get it on [NuGet](https://www.nuget.org/packages/Projecto.Autofac/):

    PM> Install-Package Projecto.Autofac
    
Now the configuration of the projector can be simplified to this:

```csharp
autofacContainer.RegisterType<ExampleProjection>().SingleInstance();

autofacContainer.Register(ctx => new ProjectorBuilder<ProjectionMessageEnvelope>()
    .RegisterProjectionsFromAutofac(ctx)
    .UseAutofacDependencyLifetimeScopeFactory(ctx)
    .Build<MyNextSequenceNumberRepository>()
).AsSelf().SingleInstance();
```
