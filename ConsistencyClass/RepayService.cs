namespace ConsistencyClass;

internal class RepayService(VirtualCreditCardDatabase virtualCreditCardDatabase)
{
    public Result Repay(CardId cardId, Money amount)
    {
        var card = virtualCreditCardDatabase.Find(cardId);
        var expectedVersion = card.Version;

        var result = card.Repay(amount);

        return result == Result.Success
            ? virtualCreditCardDatabase.Save(card, expectedVersion)
            : result;
    }
}
