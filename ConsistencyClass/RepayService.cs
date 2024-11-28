namespace ConsistencyClass;

internal class RepayService(VirtualCreditCardDatabase virtualCreditCardDatabase)
{
    public Result Repay(CardId cardId, Money amount)
    {
        var card = virtualCreditCardDatabase.Find(cardId);

        var result = card.Repay(amount);

        return result == Result.Success
            ? virtualCreditCardDatabase.Save(card)
            : result;
    }
}
