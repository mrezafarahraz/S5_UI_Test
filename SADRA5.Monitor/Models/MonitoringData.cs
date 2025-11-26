using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SADRA5.Monitor.Models
{
    public class MonitoringData : INotifyPropertyChanged
    {
        private double _iu, _iv, _iw, _iavg;
        private double _vbus, _vout;
        private double _temperature;
        private double _frequency;
        private SystemStatus _status;
        private byte _faultCode;
        private DateTime _timestamp;

        public double Iu { get => _iu; set { _iu = value; OnPropertyChanged(); } }
        public double Iv { get => _iv; set { _iv = value; OnPropertyChanged(); } }
        public double Iw { get => _iw; set { _iw = value; OnPropertyChanged(); } }
        public double Iavg { get => _iavg; set { _iavg = value; OnPropertyChanged(); } }
        public double Vbus { get => _vbus; set { _vbus = value; OnPropertyChanged(); } }
        public double Vout { get => _vout; set { _vout = value; OnPropertyChanged(); } }
        public double Temperature { get => _temperature; set { _temperature = value; OnPropertyChanged(); } }
        public double Frequency { get => _frequency; set { _frequency = value; OnPropertyChanged(); } }
        
        public SystemStatus Status 
        { 
            get => _status; 
            set 
            { 
                _status = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(StatusText)); 
                OnPropertyChanged(nameof(StatusColor)); 
            } 
        }
        
        public byte FaultCode 
        { 
            get => _faultCode; 
            set 
            { 
                _faultCode = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(FaultText)); 
            } 
        }
        
        public DateTime Timestamp { get => _timestamp; set { _timestamp = value; OnPropertyChanged(); } }

        public string StatusText => Status switch
        {
            SystemStatus.Waiting => "WAITING",
            SystemStatus.Ramping => "RAMPING",
            SystemStatus.Running => "RUNNING",
            SystemStatus.Fault => "FAULT",
            _ => "UNKNOWN"
        };

        public string StatusColor => Status switch
        {
            SystemStatus.Waiting => "#FFA500",
            SystemStatus.Ramping => "#00BFFF",
            SystemStatus.Running => "#00FF00",
            SystemStatus.Fault => "#FF0000",
            _ => "#808080"
        };

        public string FaultText => Services.FaultAnalyzer.GetFaultDescription(FaultCode);

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum SystemStatus : byte
    {
        Waiting = 3,
        Ramping = 1,
        Running = 2,
        Fault = 4
    }
}
