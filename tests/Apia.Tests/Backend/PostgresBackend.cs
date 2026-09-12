using Apia.Postgres;
using Apia.Tests.Entities;
using Apia.Tests.Identity;
using JasperFx;
using Marten;

namespace Apia.Tests.Backend;

/// <summary>
/// The Postgres backend, against the database the given connection string names. Marten is told which
/// member of each example entity carries its id, and every memory gets a schema of its own so that two
/// runs cannot read each other's entities.
/// </summary>
public sealed class PostgresBackend(string connection) : IBackend
{
    public IMemory Memory()
        => new PostgresMemory(
            DocumentStore.For(options =>
            {
                options.Connection(connection);
                options.AutoCreateSchemaObjects = AutoCreate.All;
                options.DatabaseSchemaName = $"apia_{Guid.NewGuid():N}";
                options.Schema.For<User>().Identity(user => user.UserId);
                options.Schema.For<Post>().Identity(post => post.PostId);
                options.Schema.For<Comment>().Identity(comment => comment.CommentId);
                options.Schema.For<Measurement>().Identity(measurement => measurement.MeasurementId);
                options.Schema.For<Note>().Identity(note => note.NoteId);
            }),
            new ExampleIdentities(),
            new Overrides());
}
