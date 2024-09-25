using System;

namespace Late4dTrain.CronTimer.Parser;

public class CronTab
{
    public CronTab(string expression, CronFormats formats)
    {
        Expression = expression;
        CronTabExpression = CronTabExpression.Create(expression, formats);
        Formats = formats;
        Id = Guid.NewGuid();
        this.ValidateExpression();
    }

    public Guid Id { get; set; }

    public CronTabExpression CronTabExpression { get; private set; }
    public string Expression { get; }
    public CronFormats Formats { get; }
}
