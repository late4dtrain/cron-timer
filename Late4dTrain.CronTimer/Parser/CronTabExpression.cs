using System;

namespace Late4dTrain.CronTimer.Parser;

public class CronTabExpression
{
    public readonly string Expression;
    public readonly CronFormats Formats;
    public CronTabExpressionField Seconds { get; private set; }
    public CronTabExpressionField Minutes { get; private set; }
    public CronTabExpressionField Hours { get; private set; }
    public CronTabExpressionField Days { get; private set; }
    public CronTabExpressionField Months { get; private set; }
    public CronTabExpressionField DaysOfWeek { get; private set; }

    private CronTabExpression(string expression, CronFormats formats)
    {
        Expression = expression;
        Formats = formats;
    }

    public static CronTabExpression Create(string expression, CronFormats formats)
    {
        var cronTabExpression = new CronTabExpression(expression, formats);
        cronTabExpression.ParseExpression();
        return cronTabExpression;
    }

    private void ParseExpression()
    {
        var parts = Expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var index = 0;

        if (Formats.HasFlag(CronFormats.IncludeSeconds))
        {
            Seconds = new CronTabExpressionField(parts[index++]);
        }

        Minutes = new CronTabExpressionField(parts[index++]);
        Hours = new CronTabExpressionField(parts[index++]);
        Days = new CronTabExpressionField(parts[index++]);
        Months = new CronTabExpressionField(parts[index++]);
        DaysOfWeek = new CronTabExpressionField(parts[index]);
    }
}