namespace ConsistencyClass;

class WithdrawService(VirtualCreditCardDatabase virtualCreditCardDatabase, OwnershipDatabase ownershipDatabase)
{
    public Result Withdraw(CardId cardId, Money amount, OwnerId ownerId)
    {
        if (!ownershipDatabase.Find(cardId).HasAccess(ownerId))
            return Result.Failure;

        var card = virtualCreditCardDatabase.Find(cardId);
        var expectedVersion = card.Version;

        var result = card.Withdraw(amount);

        return result == Result.Success
            ? virtualCreditCardDatabase.Save(card, expectedVersion)
            : result;
    }
}

