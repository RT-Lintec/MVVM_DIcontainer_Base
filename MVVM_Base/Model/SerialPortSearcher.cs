using MVVM_Base.Model;
using System.Collections.Generic;
using System.Management;

/// <summary>
/// シリアル通信ポートの検出クラス
/// </summary>
public static class SerialPortSearcher
{
    /// <summary>
    /// 検出処理
    /// </summary>
    /// <returns></returns>
    public static List<SerialPortInfo> GetPortList()
    {
        var list = new List<SerialPortInfo>();
        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
        {
            foreach (var obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (name == null) continue;

                var portName = name.Substring(name.LastIndexOf("(COM")).Trim('(', ')');
                list.Add(new SerialPortInfo
                {
                    PortName = portName,
                    FriendlyName = name.Replace($"({portName})", "").Trim()
                });
            }
        }
        return list;
    }
}
