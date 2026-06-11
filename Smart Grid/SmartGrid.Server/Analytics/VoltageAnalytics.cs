using SmartGrid.Common;
using System;
using System.Collections.Generic;

namespace SmartGrid.Server.Analytics
{
    public class VoltageAnalytics
    {
        private readonly double _voltageThreshold;
        private readonly List<AnalyticsRecord> _records = new List<AnalyticsRecord>();
        private double _previousVoltage = double.NaN;

        public VoltageAnalytics(double voltageThreshold)
        {
            _voltageThreshold = voltageThreshold;
        }

        public void Reset()
        {
            _records.Clear();
            _previousVoltage = double.NaN;
        }

        public void Analyze(int sampleIndex, double voltage)
        {
            if (!double.IsNaN(_previousVoltage))
            {
                double deltaV = voltage - _previousVoltage;
                if (Math.Abs(deltaV) > _voltageThreshold)
                {
                    string direction = deltaV > 0 ? "above expected" : "under expected";
                    string message = $"Voltage spike detected on sample #{sampleIndex}: DeltaV={deltaV:F4}, direction={direction}";

                    _records.Add(new AnalyticsRecord
                    {
                        Type = "VoltageSpike",
                        SampleIndex = sampleIndex,
                        Direction = direction,
                        ActualValue = voltage,
                        ReferenceValue = _previousVoltage,
                        Delta = deltaV,
                        Message = message
                    });

                    Console.WriteLine($"[VOLTAGE SPIKE] Uzorak #{sampleIndex}: deltaV={deltaV:F4}, smer={direction}");
                }
            }

            _previousVoltage = voltage;
        }

        public List<AnalyticsRecord> GetRecords()
        {
            return new List<AnalyticsRecord>(_records);
        }
    }
}
