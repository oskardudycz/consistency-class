namespace ConsistencyClass;

internal class OwnershipService(OwnershipDatabase ownershipDatabase)
{
    public Result AddAccess(CardId cardId, OwnerId ownerId)
    {
        var ownership = ownershipDatabase.Find(cardId);
        var expectedVersion = ownership.Version;

        if (ownership.Size >= 2)
        {
            return Result.Failure;
        }

        ownership = ownership.AddAccess(ownerId);

        return ownershipDatabase.Save(cardId, ownership, expectedVersion);
    }

    public Result RevokeAccess(CardId cardId, OwnerId ownerId)
    {
        var ownership = ownershipDatabase.Find(cardId);
        var expectedVersion = ownership.Version;

        ownership = ownership.Revoke(ownerId);

        return ownershipDatabase.Save(cardId, ownership, expectedVersion);
    }
}
