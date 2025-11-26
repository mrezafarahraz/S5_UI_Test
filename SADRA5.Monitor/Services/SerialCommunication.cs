using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using SADRA5.Monitor.Models;
using SADRA5.Monitor.Helpers;

namespace SADRA5.Monitor.Services
{
    public class SerialCommunication : IDisposable
    {
        private readonly SerialPort _serialPort;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly PacketParser _parser;

        public event EventHandler<MonitoringData> DataReceived;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler ConnectionStatusChanged;

        public bool IsConnected => _serialPort?.IsOpen ?? false;

        public SerialCommunication(string portName, int baudRate = 9600)
        {
            _serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = baudRate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.Two,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            _parser = new PacketParser();
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    _cancellationTokenSource = new CancellationTokenSource();
                    
                    await SendCommandAsync(0x80);
                    _ = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token));
                    
                    ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    await SendCommandAsync(0x90);
                }

                _cancellationTokenSource?.Cancel();
                await Task.Delay(100);
                
                _serialPort?.Close();
                ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
            }
        }

        private async Task SendCommandAsync(byte command)
        {
            byte[] txData = { 0x0A, command, 0x00, 0x00 };
            await Task.Run(() => _serialPort.Write(txData, 0, 4));
        }

        public async Task<byte[]> ReadFaultLogAsync()
        {
            byte[] txData = { 0x0A, 0x20, 0x00, 0x00 };
            
            _serialPort.DiscardInBuffer();
            _serialPort.Write(txData, 0, 4);
            await Task.Delay(500);
            
            if (_serialPort.BytesToRead >= 10)
            {
                byte[] faultLog = new byte[10];
                _serialPort.Read(faultLog, 0, 10);
                return faultLog;
            }
            
            return new byte[10];
        }

        public async Task ClearFaultLogAsync()
        {
            byte[] txData = { 0x0A, 0x50, 0x00, 0x00 };
            _serialPort.Write(txData, 0, 4);
            await Task.Delay(100);
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[26];
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_serialPort.BytesToRead >= 26)
                    {
                        int bytesRead = await Task.Run(() => _serialPort.Read(buffer, 0, 26));
                        
                        if (bytesRead == 26)
                        {
                            var data = _parser.ParseMonitoringPacket(buffer);
                            if (data != null) DataReceived?.Invoke(this, data);
                        }
                    }
                    
                    await Task.Delay(10, cancellationToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { ErrorOccurred?.Invoke(this, $"Receive error: {ex.Message}"); }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _serialPort?.Close();
            _serialPort?.Dispose();
        }
    }
}
