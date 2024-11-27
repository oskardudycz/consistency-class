namespace ConsistencyClass;

class WithdrawService(BillingCycleDatabase billingCycleDatabase, OwnershipDatabase ownershipDatabase)
{
    public Result Withdraw(BillingCycleId cycleId, Money amount, OwnerId ownerId)
    {
        if (!ownershipDatabase.Find(cycleId.CardId).HasAccess(ownerId))
            return Result.Failure;

        var billingCycle = billingCycleDatabase.Find(cycleId);
        var expectedVersion = billingCycle.Version;

        var result = billingCycle.Withdraw(amount);

        return result == Result.Success
            ? billingCycleDatabase.Save(billingCycle, expectedVersion)
            : result;
    }
}

