using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Chat
{
    class Connection
    {
        public static void SendBroadcastPackage(string name, IPAddress ipBroadcast, int port, List<string> history)
        {
            try
            {
                UdpClient udpClient = new UdpClient();
                udpClient.Connect(ipBroadcast, port);

                byte[] nameData = Encoding.UTF8.GetBytes(Message.CONNECT + name);
                int sendedBytes = udpClient.Send(nameData, nameData.Length);

                if (sendedBytes == nameData.Length)
                {
                    history.Add(name + " joined the chat " + DateTime.Now);
                }
                
                udpClient.Close();
            }
            catch
            {
                MessageBox.Show("Error sending broadcast message");
            }
        }

        public static void ReceiveUDP(int udpPort, int tcpPort, List<Client> clients, List<string> history, string name)
        {
            UdpClient udpClient = new UdpClient();
            IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, udpPort);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.ExclusiveAddressUse = false;
            udpClient.Client.Bind(ipEnd); 

            try
            {
                while (true)
                {
                    byte[] receivedData = udpClient.Receive(ref ipEnd);
                    string clientName = Encoding.UTF8.GetString(receivedData);
                    clientName = clientName.Substring(1);

                    if ((MainWindow.ipAddress != ipEnd.Address) && (clients.Find(item => item.ip == ipEnd.Address) == null))
                    {
                        TcpClient tcpClient = new TcpClient();
                        tcpClient.Connect(ipEnd.Address, tcpPort);

                        Client newClient = new Client(clientName, ipEnd.Address, tcpClient);
                        clients.Add(newClient);
                        history.Add(newClient.Name + " (" + newClient.ip + ") joined the chat " + DateTime.Now);

                        Thread clientThread = new Thread(() => { Message.TCPMessage(newClient, history, clients); });
                        clientThread.Start();

                        Message.SendNameMessage(tcpClient, name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                udpClient.Close();
            }
        }

        public static void ListenTCP(int port, List<Client> clients, List<string> history)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();

            try
            {
                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    IPAddress ipSender = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
                    Client sender = clients.Find(item => item.ip == ipSender);

                    if (sender == null)
                    {
                        lock(MainWindow.threadLock)
                        {
                            Client client = new Client(null, ipSender, tcpClient);
                            clients.Add(client);
                            sender = client;
                        }
                    }

                    Thread clientThread = new Thread(() => { Message.TCPMessage(sender, history, clients); });
                    clientThread.Start();
                }
            }
            finally
            {
                tcpListener.Stop();
            }
        }
    }
}
