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

        ownership = ownership.AddAccess(ownerId);

        return ownershipDatabase.Save(cardId, ownership);
    }

    public Result RevokeAccess(CardId cardId, OwnerId ownerId)
    {
        var ownership = ownershipDatabase.Find(cardId);

        ownership = ownership.Revoke(ownerId);

        return ownershipDatabase.Save(cardId, ownership);
    }
}
