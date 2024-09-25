namespace Late4dTrain.CronTimer.Parser
{
    public class CronTabExpressionFieldData
    {
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
        public string Step { get; set; } = string.Empty;
        public bool IsRange => !string.IsNullOrEmpty(End);
        public bool HasStep => !string.IsNullOrEmpty(Step);
    }
}
