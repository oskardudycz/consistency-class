namespace ConsistencyClass;

internal class AddLimitService(VirtualCreditCardDatabase virtualCreditCardDatabase)
{
    public Result AddLimit(CardId cardId, Money limit)
    {
        var card = virtualCreditCardDatabase.Find(cardId);
        var result = card.AssignLimit(limit);
        virtualCreditCardDatabase.Save(card);
        return result;
    }
}

