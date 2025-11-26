using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SADRA5.Monitor.Models;

namespace SADRA5.Monitor.Services
{
    public class DataLogger
    {
        private readonly List<MonitoringData> _dataBuffer;
        private readonly object _lock = new object();
        private const int MaxBufferSize = 10000;

        public DataLogger()
        {
            _dataBuffer = new List<MonitoringData>();
        }

        public void LogData(MonitoringData data)
        {
            lock (_lock)
            {
                _dataBuffer.Add(data);

                if (_dataBuffer.Count > MaxBufferSize)
                {
                    _dataBuffer.RemoveAt(0);
                }
            }
        }

        public async Task ExportToCsv(string filename)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_dataBuffer.Count == 0) return;

                    var sb = new StringBuilder();
                    sb.AppendLine("Timestamp,Iu(A),Iv(A),Iw(A),Iavg(A),Vbus(V),Vout(V),Temp(°C),Freq(Hz),Status,FaultCode,FaultDescription");

                    foreach (var data in _dataBuffer)
                    {
                        sb.AppendLine($"{data.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                                     $"{data.Iu:F2},{data.Iv:F2},{data.Iw:F2},{data.Iavg:F2}," +
                                     $"{data.Vbus:F2},{data.Vout:F2},{data.Temperature:F2}," +
                                     $"{data.Frequency:F2},{data.StatusText}," +
                                     $"{data.FaultCode},\"{data.FaultText}\"");
                    }

                    File.WriteAllText(filename, sb.ToString());
                }
            });
        }

        public void ClearBuffer()
        {
            lock (_lock)
            {
                _dataBuffer.Clear();
            }
        }
    }
}
