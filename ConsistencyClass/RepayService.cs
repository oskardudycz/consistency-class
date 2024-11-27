namespace ConsistencyClass;

internal class RepayService(BillingCycleDatabase billingCycleDatabase)
{
    public Result Repay(BillingCycleId cycleId, Money amount)
    {
        var billingCycle = billingCycleDatabase.Find(cycleId);
        var expectedVersion = billingCycle.Version;

        var result = billingCycle.Repay(amount);

        return result == Result.Success
            ? billingCycleDatabase.Save(billingCycle, expectedVersion)
            : result;
    }
}
