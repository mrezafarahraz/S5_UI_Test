using System;
using SADRA5.Monitor.Models;

namespace SADRA5.Monitor.Helpers
{
    public class PacketParser
    {
        private readonly NTCConverter _ntcConverter = new NTCConverter();

        public MonitoringData ParseMonitoringPacket(byte[] packet)
        {
            if (packet == null || packet.Length != 26) return null;
            if (packet[0] != 0xFE || packet[1] != 0xFE) return null;

            var data = new MonitoringData
            {
                Iv = (packet[2] << 8 | packet[3]) / 1.0,
                Vbus = (packet[4] << 8 | packet[5]) / 1.0,
                Iw = (packet[6] << 8 | packet[7]) / 1.0,
                Temperature = _ntcConverter.Convert(packet[8] << 8 | packet[9]),
                Iu = (packet[10] << 8 | packet[11]) / 1.0,
                Iavg = (packet[12] << 8 | packet[13]) / 1.0,
                Vout = (packet[14] << 8 | packet[15]) / 1.0,
                FaultCode = packet[21],
                Status = (SystemStatus)packet[23],
                Frequency = (packet[24] << 8 | packet[25]) / 10.0,
                Timestamp = DateTime.Now
            };

            return data;
        }
    }
}
