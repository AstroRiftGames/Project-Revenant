public class FusionResult
{
    public bool IsSuccess { get; }
    public FusionEntity ResultCreature { get; }
    public int RemainsAmount { get; }

    private FusionResult(bool isSuccess, FusionEntity resultCreature, int remainsAmount)
    {
        IsSuccess = isSuccess;
        ResultCreature = resultCreature;
        RemainsAmount = remainsAmount;
    }

    public static FusionResult Success(FusionEntity resultCreature)
    {
        return new FusionResult(true, resultCreature, 0);
    }

    public static FusionResult Failure(int remainsAmount)
    {
        return new FusionResult(false, null, remainsAmount);
    }
}

