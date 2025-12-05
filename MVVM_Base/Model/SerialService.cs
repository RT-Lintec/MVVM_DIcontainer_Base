using System.IO.Ports;

namespace MVVM_Base.Model
{
    public class SerialService /*: ISerialConnectionService*/
    {
        SerialPort? _serialPort;

        public void Connect(SerialPortInfo serialPortInfo)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            _serialPort = new SerialPort(serialPortInfo.PortName, serialPortInfo.Baudrate);
            _serialPort.Open();
        }

        public void Disconnect()
        {
            if (_serialPort != null)
            {
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        public SerialPort? Port => _serialPort;
    }

}