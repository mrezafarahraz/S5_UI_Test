using System.Collections.Generic;

namespace SADRA5.Monitor.Services
{
    public static class FaultAnalyzer
    {
        private static readonly Dictionary<byte, string> FaultDescriptions = new Dictionary<byte, string>
        {
            { 0, "No Fault" },
            { 1, "DC Bus Over Voltage" },
            { 2, "DC Bus Under Voltage" },
            { 3, "Phase U Positive Peak OC" },
            { 4, "Phase U Negative Peak OC" },
            { 5, "Phase V Positive Peak OC" },
            { 6, "Phase V Negative Peak OC" },
            { 7, "Phase W Positive Peak OC" },
            { 8, "Phase W Negative Peak OC" },
            { 12, "Input Power Failure" },
            { 16, "Over Temperature" },
            { 17, "Phase U RMS Overcurrent" },
            { 18, "IGBT Leg U Fault" },
            { 20, "Phase V RMS Overcurrent" },
            { 21, "IGBT Leg V Fault" },
            { 23, "Phase W RMS Overcurrent" },
            { 24, "IGBT Leg W Fault" },
            { 26, "Current Unbalance" },
            { 27, "DC Bus Average Low" },
            { 28, "DC Bus Average High" }
        };

        public static string GetFaultDescription(byte faultCode)
        {
            if (FaultDescriptions.TryGetValue(faultCode, out string description))
            {
                return description;
            }
            return "Unknown Fault";
        }
    }
}
