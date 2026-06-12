using SmartGrid.Common;
using System;
using System.Collections.Generic;

namespace SmartGrid.Server.Analytics
{
    public class CurrentAnalytics
    {
        private readonly double _currentThreshold;
        private readonly double _averageDeviationThreshold;
        private readonly List<AnalyticsRecord> _records = new List<AnalyticsRecord>();

        private double _currentSum;
        private int _currentCount;
        private double _previousCurrent = double.NaN;

        public CurrentAnalytics(double currentThreshold, double averageDeviationThreshold)
        {
            _currentThreshold = currentThreshold;
            _averageDeviationThreshold = averageDeviationThreshold;
        }

        public void Reset()
        {
            _records.Clear();
            _currentSum = 0.0;
            _currentCount = 0;
            _previousCurrent = double.NaN;
        }

        public void Analyze(int sampleIndex, double current)
        {
            _currentSum += current;
            _currentCount++;
            double currentMean = _currentSum / _currentCount;

            DetectCurrentSpike(sampleIndex, current);
            DetectOutOfBandCurrent(sampleIndex, current, currentMean);

            _previousCurrent = current;
        }

        private void DetectCurrentSpike(int sampleIndex, double current)
        {
            if (double.IsNaN(_previousCurrent))
                return;

            double deltaI = current - _previousCurrent;
            if (Math.Abs(deltaI) <= _currentThreshold)
                return;

            string reportDirection = deltaI > 0 ? "above expected" : "under expected";

            _records.Add(new AnalyticsRecord
            {
                Type = "CurrentSpike",
                SampleIndex = sampleIndex,
                Direction = reportDirection,
                ActualValue = current,
                ReferenceValue = _previousCurrent,
                Delta = deltaI,
                Message = $"Current spike detected on sample #{sampleIndex}: DeltaI={deltaI:F4}, direction={reportDirection}"
            });
        }

        private void DetectOutOfBandCurrent(int sampleIndex, double current, double currentMean)
        {
            if (_currentCount <= 1)
                return;

            double lowerLimit = (1 - _averageDeviationThreshold) * currentMean;
            double upperLimit = (1 + _averageDeviationThreshold) * currentMean;

            if (current < lowerLimit)
            {
                AddOutOfBandRecord(sampleIndex, current, currentMean, current - currentMean, "under expected");
            }
            else if (current > upperLimit)
            {
                AddOutOfBandRecord(sampleIndex, current, currentMean, current - currentMean, "above expected");
            }
        }

        public List<AnalyticsRecord> GetRecords()
        {
            return new List<AnalyticsRecord>(_records);
        }

        private void AddOutOfBandRecord(int sampleIndex, double current, double currentMean, double delta, string direction)
        {
            _records.Add(new AnalyticsRecord
            {
                Type = "OutOfBandWarning",
                SampleIndex = sampleIndex,
                Direction = direction,
                ActualValue = current,
                ReferenceValue = currentMean,
                Delta = delta,
                Message = $"Current out of band on sample #{sampleIndex}: I={current:F4}, Imean={currentMean:F4}, direction={direction}"
            });
        }
    }
}
