using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chat
{
    public class Client
    {
        public string Name;
        public IPAddress ip;
        public TcpClient tcp;

        public Client(string name, IPAddress ipAddr, TcpClient tcpClient)
        {
            Name = name;
            ip = ipAddr;
            tcp = tcpClient;
        }
    }
}
