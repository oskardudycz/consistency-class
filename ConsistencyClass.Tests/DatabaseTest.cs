using System.Collections.Concurrent;

namespace ConsistencyClass.Tests;

public class DatabaseTest
{
    [Fact]
    public void FindForNonExistingRecordReturnsEmpty()
    {
        var collection = Database.Collection<DummyVersionedEntity>();
        var id = Guid.NewGuid().ToString();

        var result = collection.Find(id);

        Assert.Null(result);
    }

    [Fact]
    public void FindForExistingRecordReturnsEntity()
    {
        var collection = Database.Collection<DummyVersionedEntity>();
        var id = Guid.NewGuid().ToString();
        var entity = new DummyVersionedEntity(id, 0);
        collection.Save(id, entity);

        var result = collection.Find(id);

        Assert.NotNull(result);
        Assert.Equal(id, result?.Id);
    }

    [Fact]
    public void SaveBumpsVersionForVersionedEntity()
    {
        var collection = Database.Collection<DummyVersionedEntity>();
        var id = Guid.NewGuid().ToString();
        var entity = new DummyVersionedEntity(id, 0);

        var result = collection.Save(id, entity);

        Assert.Equal(Result.Success, result);
        Assert.Equal(1, entity.Version);
    }

    [Fact]
    public void SaveStoresNonVersionedEntity()
    {
        var collection = Database.Collection<DummyEntity>();
        var id = Guid.NewGuid().ToString();
        var entity = new DummyEntity(id);

        var result = collection.Save(id, entity);

        Assert.Equal(Result.Success, result);
        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void SaveBumpsVersionInDatabaseForVersionedEntity()
    {
        var collection = Database.Collection<DummyVersionedEntity>();
        var id = Guid.NewGuid().ToString();
        var entity = new DummyVersionedEntity(id, 0);

        collection.Save(id, entity);

        var entityFromDb = collection.Find(id);
        Assert.NotNull(entityFromDb);
        Assert.Equal(1, entityFromDb?.Version);
    }

    [Fact]
    public void SaveVersionedEntityCanBeRunMultipleTimesSequentially()
    {
        var collection = Database.Collection<DummyVersionedEntity>();
        var id = Guid.NewGuid().ToString();
        var currentVersion = 0;
        var entity = new DummyVersionedEntity(id, currentVersion);

        do
        {
            var result = collection.Save(id, entity);

            Assert.Equal(Result.Success, result);
            Assert.Equal(++currentVersion, entity.Version);
        } while (currentVersion < 5);
    }

    [Fact]
    public void SaveEntityCanBeRunMultipleTimesSequentiallyWithBumpedVersion()
    {
        var collection = Database.Collection<DummyEntity>();
        var id = Guid.NewGuid().ToString();
        var currentVersion = 0;
        var entity = new DummyEntity(id);

        do
        {
            var result = collection.Save(id, entity, currentVersion++);

            Assert.Equal(Result.Success, result);
        } while (currentVersion < 5);
    }

    [Fact]
    public void SaveVersionedEntityCanBeRunMultipleTimesSequentiallyWithBumpedVersion()
    {
        var collection = Database.Collection<DummyVersionedEntity>();
        var id = Guid.NewGuid().ToString();
        var currentVersion = 0;
        var entity = new DummyVersionedEntity(id, currentVersion);

        do
        {
            var result = collection.Save(id, entity, currentVersion);

            Assert.Equal(Result.Success, result);
            Assert.Equal(++currentVersion, entity.Version);
        } while (currentVersion < 5);
    }

    [Fact]
    public void SaveVersionedEntitySucceedsWhenExplicitExpectedVersionMatches()
    {
        var collection = Database.Collection<DummyVersionedEntity>();
        var id = Guid.NewGuid().ToString();
        var entity = new DummyVersionedEntity(id, 0);

        collection.Save(id, entity, 0);

        var result = collection.Save(id, entity, 1);

        Assert.Equal(Result.Success, result);
        Assert.Equal(2, entity.Version);
    }

    [Fact]
    public void SaveEntitySucceedsWhenExplicitExpectedVersionMatches()
    {
        var collection = Database.Collection<DummyEntity>();
        var id = Guid.NewGuid().ToString();
        var entity = new DummyEntity(id);

        collection.Save(id, entity, 0);

        var result = collection.Save(id, entity, 1);

        Assert.Equal(Result.Success, result);
    }

    [Fact]
    public void SaveFailsWhenExplicitExpectedVersionDoesNotMatch()
    {
        var collection = Database.Collection<DummyVersionedEntity>();
        var id = Guid.NewGuid().ToString();
        var entity = new DummyVersionedEntity(id, 0);
        var oldEntity = new DummyVersionedEntity(id, 0);

        collection.Save(id, entity);

        var result = collection.Save(id, oldEntity);

        Assert.Equal(Result.Failure, result);
        Assert.Equal(1, entity.Version);
    }

    [Fact]
    public void CantUpdateConcurrently()
    {
        var collection = Database.Collection<DummyVersionedEntity>();
        var id = Guid.NewGuid().ToString();

        var results = new ConcurrentBag<Result>();
        Parallel.For(0, 10000, _ =>
        {
            var entity = collection.Find(id) ?? new DummyVersionedEntity(id, 0);
            results.Add(collection.Save(id, entity));
        });

        Assert.Contains(Result.Failure, results);
        Assert.True((collection.Find(id)?.Version ?? 0) < 10000);
    }
}

public record DummyEntity(string Id);

public class DummyVersionedEntity(string id, int version): IVersionedWithAutoIncrement
{
    public string Id { get; } = id;
    public int Version { get; private set; } = version;

    public void SetVersion(int version) => Version = version;
}
