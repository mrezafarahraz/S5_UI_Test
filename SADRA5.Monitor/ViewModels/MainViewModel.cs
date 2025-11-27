using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using LiveCharts;
using SADRA5.Monitor.Models;
using SADRA5.Monitor.Services;

namespace SADRA5.Monitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private SerialCommunication _serial;
        private readonly DataLogger _dataLogger;
        
        private MonitoringData _currentData;
        private bool _isConnected;
        private string _connectionStatus;
        private string _selectedPort;

        public MonitoringData CurrentData
        {
            get => _currentData;
            set { _currentData = value; OnPropertyChanged(); }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConnectionButtonText)); }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { _connectionStatus = value; OnPropertyChanged(); }
        }

        public string SelectedPort
        {
            get => _selectedPort;
            set { _selectedPort = value; OnPropertyChanged(); }
        }

        public string ConnectionButtonText => IsConnected ? "Disconnect" : "Connect";

        public ChartValues<double> IuChartValues { get; set; }
        public ChartValues<double> IvChartValues { get; set; }
        public ChartValues<double> IwChartValues { get; set; }
        public ChartValues<double> VbusChartValues { get; set; }
        public ChartValues<double> TempChartValues { get; set; }

        public ObservableCollection<string> AvailablePorts { get; set; }
        public ObservableCollection<string> LogEntries { get; set; }

        public ICommand ConnectCommand { get; }
        public ICommand ReadFaultLogCommand { get; }
        public ICommand ClearFaultLogCommand { get; }
        public ICommand ExportDataCommand { get; }

        public MainViewModel()
        {
            CurrentData = new MonitoringData();
            AvailablePorts = new ObservableCollection<string>(System.IO.Ports.SerialPort.GetPortNames());
            LogEntries = new ObservableCollection<string>();

            IuChartValues = new ChartValues<double>();
            IvChartValues = new ChartValues<double>();
            IwChartValues = new ChartValues<double>();
            VbusChartValues = new ChartValues<double>();
            TempChartValues = new ChartValues<double>();

            _dataLogger = new DataLogger();

            ConnectCommand = new RelayCommand(async () => await ToggleConnection());
            ReadFaultLogCommand = new RelayCommand(async () => await ReadFaultLog());
            ClearFaultLogCommand = new RelayCommand(async () => await ClearFaultLog());
            ExportDataCommand = new RelayCommand(async () => await ExportData());

            ConnectionStatus = "Disconnected";
            
            if (AvailablePorts.Count > 0)
                SelectedPort = AvailablePorts[0];
        }

        private async Task ToggleConnection()
        {
            if (!IsConnected)
            {
                if (string.IsNullOrEmpty(SelectedPort))
                {
                    AddLog("Please select a COM port");
                    return;
                }

                _serial = new SerialCommunication(SelectedPort);
                _serial.DataReceived += OnDataReceived;
                _serial.ErrorOccurred += OnError;

                bool success = await _serial.ConnectAsync();
                
                if (success)
                {
                    IsConnected = true;
                    ConnectionStatus = $"Connected to {SelectedPort}";
                    AddLog($"Connected to {SelectedPort}");
                }
                else
                {
                    AddLog("Connection failed");
                }
            }
            else
            {
                await _serial.DisconnectAsync();
                IsConnected = false;
                ConnectionStatus = "Disconnected";
                AddLog("Disconnected");
            }
        }

        private void OnDataReceived(object sender, MonitoringData data)
        {
            CurrentData = data;

            UpdateChart(IuChartValues, data.Iu);
            UpdateChart(IvChartValues, data.Iv);
            UpdateChart(IwChartValues, data.Iw);
            UpdateChart(VbusChartValues, data.Vbus);
            UpdateChart(TempChartValues, data.Temperature);

            _dataLogger.LogData(data);

            if (data.FaultCode != 0)
            {
                AddLog($"FAULT: {data.FaultText}");
            }
        }

        private void UpdateChart(ChartValues<double> chart, double value)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                chart.Add(value);
                
                if (chart.Count > 100)
                    chart.RemoveAt(0);
            });
        }

        private void OnError(object sender, string error)
        {
            AddLog($"ERROR: {error}");
        }

        private async Task ReadFaultLog()
        {
            if (!IsConnected) return;

            var faultLog = await _serial.ReadFaultLogAsync();
            
            AddLog("=== Fault Log ===");
            for (int i = 0; i < faultLog.Length; i++)
            {
                if (faultLog[i] != 0)
                {
                    AddLog($"Entry {i + 1}: {FaultAnalyzer.GetFaultDescription(faultLog[i])}");
                }
            }
        }

        private async Task ClearFaultLog()
        {
            if (!IsConnected) return;

            await _serial.ClearFaultLogAsync();
            AddLog("Fault log cleared");
        }

        private async Task ExportData()
        {
            string filename = $"SADRA5_Log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            await _dataLogger.ExportToCsv(filename);
            AddLog($"Data exported to {filename}");
        }

        private void AddLog(string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LogEntries.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                
                if (LogEntries.Count > 100)
                    LogEntries.RemoveAt(LogEntries.Count - 1);
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object parameter) => await _execute();

        public event EventHandler CanExecuteChanged
        {
            add => System.Windows.Input.CommandManager.RequerySuggested += value;
            remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
        }
    }
}
