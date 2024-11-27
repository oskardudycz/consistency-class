namespace ConsistencyClass;

internal record Ownership(HashSet<OwnerId> Owners, int Version): IVersioned
{
    public int Size => Owners.Count;

    public static Ownership Of(params OwnerId[] owners) =>
        new([..owners], 0);

    public static Ownership Empty() =>
        new([], 0);

    public bool HasAccess(OwnerId ownerId) =>
        Owners.Contains(ownerId);

    public Ownership AddAccess(OwnerId ownerId)
    {
        var newOwners = new HashSet<OwnerId>(Owners) { ownerId };
        return new Ownership(newOwners, Version + 1);
    }

    public Ownership Revoke(OwnerId ownerId)
    {
        var newOwners = new HashSet<OwnerId>(Owners);
        newOwners.Remove(ownerId);
        return new Ownership(newOwners, Version + 1);
    }
}

internal record OwnerId(Guid Id)
{
    public static OwnerId Random() => new(Guid.NewGuid());

    public override string ToString() =>
        $"Owner:{Id}";
}
