namespace ConsistencyClass;

class WithdrawService(VirtualCreditCardDatabase virtualCreditCardDatabase, OwnershipDatabase ownershipDatabase)
{
    public Result Withdraw(CardId cardId, Money amount, OwnerId ownerId)
    {
        if (!ownershipDatabase.Find(cardId).HasAccess(ownerId))
            return Result.Failure;

        var card = virtualCreditCardDatabase.Find(cardId);
        var result = card.Withdraw(amount);
        virtualCreditCardDatabase.Save(card);
        return result;
    }
}

