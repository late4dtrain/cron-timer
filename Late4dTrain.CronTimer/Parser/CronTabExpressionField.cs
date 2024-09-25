using System.Collections.Generic;

namespace Late4dTrain.CronTimer.Parser
{
    public class CronTabExpressionField
    {
        private readonly string _rawExpression;
        public List<CronTabExpressionData> Data { get; private set; } = new();

        public CronTabExpressionField(string expression)
        {
            _rawExpression = expression;
            ParseExpression();
        }

        // Implicit conversion from string to ExpressionField
        public static implicit operator CronTabExpressionField(string expression)
        {
            return new CronTabExpressionField(expression);
        }

        // Explicit conversion from ExpressionField to string
        public static explicit operator string(CronTabExpressionField field)
        {
            return field._rawExpression;
        }

        // Override ToString for easier string representation
        public override string ToString()
        {
            return _rawExpression;
        }

        private void ParseExpression()
        {
            // Trim any whitespace
            var expression = _rawExpression.Trim();

            var parts = expression.Split(',');

            foreach (var part in parts)
            {
                var slashSplit = part.Split("/");
                var dataPart = slashSplit[0];

                var stepPart = string.Empty;
                var endRange = string.Empty;
                if (part.Contains("/"))
                {
                    stepPart = slashSplit[1];
                }
                if (dataPart.Contains("-"))
                {
                    var rangeSplit = dataPart.Split('-');
                    dataPart = rangeSplit[0];
                    endRange = rangeSplit[1];
                }

                Data.Add(new CronTabExpressionData
                {
                    Start = dataPart,
                    End = endRange,
                    Step = stepPart
                });
            }

        }
    }
}
