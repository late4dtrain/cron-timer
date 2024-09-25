using System;

namespace Late4dTrain.CronTimer.Parser;

public class CronTab
{
    public CronTab(string expression, CronFormats formats)
    {
        Expression = expression;
        Formats = formats;
        Id = Guid.NewGuid();
        this.ValidateExpression();
    }

    public Guid Id { get; set; }
    public string Expression { get; set; }
    public CronFormats Formats { get; set; }
}
