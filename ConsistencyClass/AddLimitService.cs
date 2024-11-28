namespace ConsistencyClass;

internal class AddLimitService(VirtualCreditCardDatabase virtualCreditCardDatabase)
{
    public Result AddLimit(CardId cardId, Money limit)
    {
        var card = virtualCreditCardDatabase.Find(cardId);

        var result = card.AssignLimit(limit);

        return result == Result.Success
            ? virtualCreditCardDatabase.Save(card)
            : result;
    }
}
