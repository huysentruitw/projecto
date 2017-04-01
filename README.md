# Projecto

[![Build status](https://ci.appveyor.com/api/projects/status/ikjbo1a07y33jt0o/branch/master?svg=true)](https://ci.appveyor.com/project/huysentruitw/projecto/branch/master)

## Overview

Managed .NET (C#) library for handling CQRS/ES projections while maintaining the event sequence order.

## Get it on NuGet

    PM> Install-Package projecto

## Concepts / usage

### ProjectContext

A project context is a user-defined context object which is passed to `projector.Project()` method which in turn passes it to the `When<>()` handler inside the projection.

It's a convenient way to pass out-of-band data to the handler (f.e. the originating command id, or creation date of the event/message). It could also be used to share data between different projections.

### Projection

Inherit from `Projection<TConnection, TProjectContext>` to define a projection, where `TConnection` is the connection type used by the projection and `TProjectContext` is the user-defined context object (see previous topic).

	public class UserProfileProjection : Projection<ApplicationDbContext, MyProjectContext>
	{
		public ExampleProjection()
		{
			When<CreatedUserProfileEvent>(async (dataContext, projectContext, @event) =>
			{
				dataContext.UserProfiles.Add(...);

				await dataContext.SaveChangesAsync();
			});
		}
	}

The above projection is a simple example that does not override the `FetchNextSequenceNumber` and `IncrementNextSequenceNumber` method. If you want to persist the next event sequence number, you'll have to override those methods and fetch/store the sequence number from your store.

### Projector

The projector reflects the minimum sequence number of all registered projections.

Use the projector to project a single event/message to all registered projections.

The projector ensures that it only handles an event/message if the sequence number is equal to `NextSequenceNumber`.

The `NextSequenceNumber` of the projector can be used by the application to request a resend of missing events/messages during startup.

The create a projector, you'll need to use the `ProjectorBuilder` class.

### ProjectorBuilder

...

### ProjectScope

...
