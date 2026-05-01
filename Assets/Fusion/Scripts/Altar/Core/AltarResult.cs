namespace Altar.Core
{
    public class AltarResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public UnitData RewardedUnit { get; }

        private AltarResult(bool isSuccess, string message, UnitData rewardedUnit)
        {
            IsSuccess = isSuccess;
            Message = message;
            RewardedUnit = rewardedUnit;
        }

        public static AltarResult Success(UnitData rewardedUnit)
        {
            return new AltarResult(true, "Sacrifice accepted.", rewardedUnit);
        }

        public static AltarResult Failure(string message)
        {
            return new AltarResult(false, message, null);
        }
    }
}
