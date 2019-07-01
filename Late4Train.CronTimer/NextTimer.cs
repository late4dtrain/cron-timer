namespace Late4Train.CronTimer {
    using System;
    using System.Linq;
    using Extensions;

    internal class NextTimer
    {
        private readonly CronExpressionAdapter[] _expressions;
        private NextOccasion _nextOccasion;
        public Guid CronId => _nextOccasion.CronId;
        public string CronExpression => _nextOccasion.CronExpression;

        private NextTimer(CronExpressionAdapter[] expressions)
        {
            _expressions = expressions;
        }

        internal static NextTimer Create(CronOption option)
        {
            return new NextTimer(option.Expressions.Select(e => e.ToExpressionAdapter()).ToArray());
        }

        internal long GetNextTimeElapse()
        {
            var now = DateTime.UtcNow.ToFlat();

            _nextOccasion = _expressions.OrderByDescending(e => e.Expression.GetIntervalToNext(now))
                .Select(e => e.ToNextOccasion(now)).FirstOrDefault();

            return (_nextOccasion?.Interval ?? 0) == 0? 0 : _nextOccasion.Interval - now.Ticks;

        }
    }
}