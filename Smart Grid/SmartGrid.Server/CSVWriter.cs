using SmartGrid.Common;
using System;
using System.IO;

namespace SmartGrid.Server
{
    public class CSVWriter : IDisposable
    {
        private StreamWriter _writer;
        private bool _disposed;

        public CSVWriter(string path)
        {
            _writer = new StreamWriter(path, true);
        }

        public void WriteMeasurement(Measurement measurement)
        {
            string content = $"{measurement.Timestamp},{measurement.Voltage},{measurement.Current},{measurement.PowerUsage},{measurement.FaultIndicator},{measurement.Frequency}";
            _writer.WriteLine(content);
            _writer.Flush();
        }

        public void WriteReject(string reason, Measurement measurement)
        {
            string content = $"{reason},{measurement.Timestamp},{measurement.Voltage},{measurement.Current},{measurement.PowerUsage},{measurement.FaultIndicator},{measurement.Frequency}";
            _writer.WriteLine(content);
            _writer.Flush();
        }

        public void WriteLine(string content)
        {
            _writer.WriteLine(content);
            _writer.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _writer?.Dispose();
                    Console.WriteLine("Writer disposed");
                }
                _disposed = true;
            }
        }
    }
}
