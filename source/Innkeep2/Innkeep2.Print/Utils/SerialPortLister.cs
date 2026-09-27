using System.IO.Ports;

namespace Innkeep2.Print.Utils;

public static class SerialPortLister
{
    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();
}