namespace ConsistencyClass;

class RepayService(VirtualCreditCardDatabase virtualCreditCardDatabase)
{
    public Result Repay(CardId cardId, Money amount)
    {
        var card = virtualCreditCardDatabase.Find(cardId);
        var result = card.Repay(amount);
        virtualCreditCardDatabase.Save(card);
        return result;
    }
}
