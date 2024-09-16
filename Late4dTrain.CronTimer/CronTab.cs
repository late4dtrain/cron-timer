using System;

namespace Late4dTrain.CronTimer
{
    public class CronTab
    {
        public CronTab(string expression, CronExpressionType expressionType)
        {
            (Expression, ExpressionType, Id) = (expression, expressionType, Guid.NewGuid());
        }

        public Guid Id { get; set; }
        public string Expression { get; set; }
        public CronExpressionType ExpressionType { get; set; }
    }
}
