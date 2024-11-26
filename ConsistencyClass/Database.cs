namespace ConsistencyClass;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

interface IVersioned
{
    public int Version { get; }
}

interface IVersionedWithAutoIncrement : IVersioned
{
    public void SetVersion(int version);
}

public record RecordWithVersion(object? Record, int Version)
{
    public static readonly RecordWithVersion NoRecord = new(null, 0);
}

public class Database
{
    public static DatabaseCollection<T> Collection<T>() => new();
}

public class DatabaseCollection<T>
{
    private readonly ConcurrentDictionary<string, RecordWithVersion> entries = new();

    public Result Save(string id, T record)
    {
        var expectedVersion = record is IVersioned versioned
            ? versioned.Version
            : entries.GetValueOrDefault(id, RecordWithVersion.NoRecord).Version;

        return Save(id, record, expectedVersion);
    }

    public Result Save(string id, T record, int expectedVersion)
    {
        var newExpectedVersion = expectedVersion + 1;
        var wasUpdated = false;

        entries.AddOrUpdate(
            id,
            _ =>
            {
                if (expectedVersion != RecordWithVersion.NoRecord.Version)
                    return RecordWithVersion.NoRecord;

                if (record is IVersionedWithAutoIncrement versioned)
                    versioned.SetVersion(newExpectedVersion);

                wasUpdated = true;
                return new RecordWithVersion(record, newExpectedVersion);
            },
            (_, currentValue) =>
            {
                var currentVersion = currentValue.Version;

                if (currentVersion != expectedVersion){
                    // Version conflict, don't update the value
                    return currentValue;  // Keep the currentValue in the map
                }

                if (record is IVersionedWithAutoIncrement versioned)
                    versioned.SetVersion(newExpectedVersion);

                wasUpdated = true;
                return new RecordWithVersion(record, newExpectedVersion);
            });

        return wasUpdated ? Result.Success : Result.Failure;
    }

    public T? Find(string id)
    {
        return entries.TryGetValue(id, out var recordWithVersion)
            ? (T?)recordWithVersion.Record
            : default;
    }

    public Result Handle(string id, Func<T?, T> handle, Func<T> getDefault)
    {
        var entry = entries.GetValueOrDefault(id, RecordWithVersion.NoRecord);

        var result = handle(entry.Record != null
            ? (T)entry.Record
            : getDefault());

        return Save(id, result, entry.Version);
    }
}
