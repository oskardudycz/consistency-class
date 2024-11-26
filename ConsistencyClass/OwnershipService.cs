namespace ConsistencyClass;

internal class OwnershipService(OwnershipDatabase ownershipDatabase)
{
    public Result AddAccess(CardId cardId, OwnerId ownerId)
    {
        var ownership = ownershipDatabase.Find(cardId);

        if (ownership.Size >= 2)
        {
            return Result.Failure;
        }

        var updatedOwnership = ownership.AddAccess(ownerId);
        ownershipDatabase.Save(cardId, updatedOwnership);

        return Result.Success;
    }

    public Result RevokeAccess(CardId cardId, OwnerId ownerId)
    {
        var ownership = ownershipDatabase.Find(cardId);
        var updatedOwnership = ownership.Revoke(ownerId);

        ownershipDatabase.Save(cardId, updatedOwnership);

        return Result.Success;
    }
}

