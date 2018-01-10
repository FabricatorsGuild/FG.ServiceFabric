using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.InMemory;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors.Query;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Services.Runtime.With_StateSessionManager
{
    public abstract class StateSession_transacted_scope
    {
        protected abstract IStateSessionManager GetStateSessionManager();

        private string _path;

        [SetUp]
        public abstract void Setup();

        [TearDown]
        public abstract void Teardown();
        
        [Test]
        public async Task _should_be_able_to_page_findbykey_results()
        {
            var manager = GetStateSessionManager();

            var session1 = manager.Writable.CreateSession();

            var keys = new List<string>();
            for (int i = 'a'; i <= (int)'z'; i++)
            {
                var key = ((char)i).ToString();
                keys.Add(key);
                await session1.SetValueAsync("values", key, $"Value from session1 schema values key {key}",
                    null,
                    CancellationToken.None);
            }
            await session1.CommitAsync();

            var results = new List<string>();
            var pages = 0;
            ContinuationToken token = null;
            do
            {
                var foundKeys =
                    await session1.FindByKeyPrefixAsync("values", null, 10, token, CancellationToken.None);
                results.AddRange(foundKeys.Items);
                token = foundKeys.ContinuationToken;
                Console.WriteLine(
                    $"Found {foundKeys.Items.Count()} {foundKeys.Items.First()}-{foundKeys.Items.Last()} with next token {token?.Marker}");
                pages++;
            } while (token != null);

            results.Should().BeEquivalentTo(keys);
            pages.Should().Be(3);
        }

        [Test]
        public async Task should_not_be_available_from_another_session()
        {
            var manager = GetStateSessionManager();

            var tasks = new List<Task>();
            var task = new Task(async () => await SessionAWorker(manager));
            task.Start();
            tasks.Add(task);

            task = new Task(async () => await SessionBWorker(manager));
            task.Start();
            tasks.Add(task);

            await Task.WhenAll(tasks);
        }


        private async Task SessionBWorker(IStateSessionManager manager)
        {
            using (var session2 = manager.Writable.CreateSession())
            {
                await session2.SetValueAsync("values", "b", "Value from session2 schema values key b", null,
                    CancellationToken.None);

                var session2ValueAPreCommit =
                    await session2.TryGetValueAsync<string>("values", "a", CancellationToken.None);
                var session2ValueBPreCommit =
                    await session2.TryGetValueAsync<string>("values", "b", CancellationToken.None);

                session2ValueAPreCommit.HasValue.Should().Be(false);

                session2ValueBPreCommit.Value.Should().Be("Value from session2 schema values key b");

                await session2.CommitAsync();
            }
            using (var session2 = manager.CreateSession())
            {
                var session2ValueBPostCommit =
                    await session2.GetValueAsync<string>("values", "b", CancellationToken.None);

                session2ValueBPostCommit.Should().Be("Value from session2 schema values key b");
            }
        }


        private async Task SessionAWorker(IStateSessionManager manager)
        {
            using (var session1 = manager.Writable.CreateSession())
            {
                await session1.SetValueAsync("values", "a", "Value from session1 schema values key a", null,
                    CancellationToken.None);

                var session1ValueBPreCommit =
                    await session1.TryGetValueAsync<string>("values", "b", CancellationToken.None);
                var session1ValueAPreCommit =
                    await session1.TryGetValueAsync<string>("values", "a", CancellationToken.None);

                session1ValueBPreCommit.HasValue.Should().Be(false);

                session1ValueAPreCommit.Value.Should().Be("Value from session1 schema values key a");

                await session1.CommitAsync();
            }
            using (var session1 = manager.Writable.CreateSession())
            {
                var session1ValueAPostCommit =
                    await session1.GetValueAsync<string>("values", "a", CancellationToken.None);

                session1ValueAPostCommit.Should().Be("Value from session1 schema values key a");
            }
        }

        [Test]
        public async Task should_not_be_included_in_FindBykey()
        {
            var manager = GetStateSessionManager();

            var session1 = manager.Writable.CreateSession();

            await session1.SetValueAsync("values", "a", "Value from session1 schema values key a", null,
                CancellationToken.None);
            await session1.SetValueAsync("values", "b", "Value from session1 schema values key b", null,
                CancellationToken.None);
            await session1.SetValueAsync("values", "c", "Value from session1 schema values key c", null,
                CancellationToken.None);
            await session1.SetValueAsync("values", "d", "Value from session1 schema values key d", null,
                CancellationToken.None);

            await session1.CommitAsync();

            var committedResults =
                await session1.FindByKeyPrefixAsync("values", null, 10000, null, CancellationToken.None);

            committedResults.Items.ShouldBeEquivalentTo(new[] { "a", "b", "c", "d" });

            await session1.SetValueAsync("values", "e", "Value from session1 schema values key e", null,
                CancellationToken.None);
            await session1.SetValueAsync("values", "f", "Value from session1 schema values key f", null,
                CancellationToken.None);
            await session1.SetValueAsync("values", "g", "Value from session1 schema values key g", null,
                CancellationToken.None);
            await session1.RemoveAsync<string>("values", "a", CancellationToken.None);
            await session1.RemoveAsync<string>("values", "b", CancellationToken.None);
            await session1.RemoveAsync<string>("values", "c", CancellationToken.None);
            await session1.RemoveAsync<string>("values", "d", CancellationToken.None);

            var uncommittedResults =
                await session1.FindByKeyPrefixAsync("values", null, 10000, null, CancellationToken.None);
            uncommittedResults.Items.ShouldBeEquivalentTo(new[] { "a", "b", "c", "d" });

            committedResults.ShouldBeEquivalentTo(uncommittedResults);
        }

        [Test]
        public async Task should_not_be_included_in_enumerateSchemaNames()
        {
            var manager = GetStateSessionManager();

            var session1 = manager.Writable.CreateSession();

            var schemas = new[] { "a-series", "b-series", "c-series" };
            foreach (var schema in schemas)
            {
                await session1.SetValueAsync(schema, "a", $"Value from session1 schema {schema} key a", null,
                    CancellationToken.None);
                await session1.SetValueAsync(schema, "b", $"Value from session1 schema {schema} key b", null,
                    CancellationToken.None);
                await session1.SetValueAsync(schema, "c", $"Value from session1 schema {schema} key c", null,
                    CancellationToken.None);
                await session1.SetValueAsync(schema, "d", $"Value from session1 schema {schema} key d", null,
                    CancellationToken.None);
            }

            var schemaKeysPreCommit = await session1.EnumerateSchemaNamesAsync("a", CancellationToken.None);

            schemaKeysPreCommit.Should().HaveCount(0);

            await session1.CommitAsync();

            var schemaKeysPostCommit = await session1.EnumerateSchemaNamesAsync("a", CancellationToken.None);

            schemaKeysPostCommit.ShouldBeEquivalentTo(schemas);
        }
    }
}