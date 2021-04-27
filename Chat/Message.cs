using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chat
{
    class Message
    {
        public const char MESSAGE = (char)0;
        public const char CONNECT = (char)1;
        public const char DISCONNECT = (char)2;
        public const char NAME = (char)3;
        public const char ASK_HISTORY = (char)4;
        public const char HISTORY = (char)5;

        public static void SendMessage(List<Client> clients, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(MESSAGE + message);
            foreach (Client client in clients)
            {
                client.tcp.GetStream().Write(data, 0, data.Length);
            }
        }

        public static void SendDisconnectMessage(List<Client> clients)
        {
            byte[] data = Encoding.UTF8.GetBytes(DISCONNECT.ToString());
            foreach (Client client in clients)
            {
                client.tcp.GetStream().Write(data, 0, data.Length);
            }
        }

        public static void SendNameMessage(TcpClient tcpClient, string name)
        {
            NetworkStream netStream = tcpClient.GetStream();
            byte[] login = Encoding.UTF8.GetBytes(NAME + name);
            netStream.Write(login, 0, login.Length);
        }

        public static void SendAskHistoryMessage(Client client)
        {
            byte[] data = Encoding.UTF8.GetBytes(ASK_HISTORY.ToString());
            client.tcp.GetStream().Write(data, 0, data.Length);
        }

        public static void SendHistoryMessage(List<string> history, Client client)
        {
            byte[] data;
            string buf = "";
            foreach (string item in history)
            {
                buf += item + MESSAGE;
            }
            data = Encoding.UTF8.GetBytes(HISTORY + buf);
            client.tcp.GetStream().Write(data, 0, data.Length);
        }

        public static void TCPMessage(Client sender, List<string> history, List<Client> clients)
        {
            NetworkStream clientStream = sender.tcp.GetStream();
            bool IsConnect = true;
            
            try
            {
                while (IsConnect)
                {
                    byte[] data = new byte[6400];
                    StringBuilder builder = new StringBuilder();
                    int receivedBytes = 0;

                    do
                    {
                        receivedBytes = clientStream.Read(data, 0, data.Length);
                        builder.Append(Encoding.UTF8.GetString(data, 0, receivedBytes));
                    } while (clientStream.DataAvailable);

                    string message = builder.ToString();

                    if (message.Length != 0)
                    {
                        if (message[0] == MESSAGE)
                        {
                            history.Add(sender.Name + " (" + (sender.ip).ToString() + ") " + message.Substring(1) + " " + DateTime.Now);
                        }
                        else if (message[0] == NAME)
                        {
                            sender.Name = message.Substring(1);
                            history.Add(sender.Name + " (" + (sender.ip).ToString() + ") joined the chat " + DateTime.Now);
                        }
                        else if (message[0] == DISCONNECT)
                        {
                            history.Add(sender.Name + " (" + (sender.ip).ToString() + ") left the chat " + DateTime.Now);
                            lock (MainWindow.threadLock)
                            {
                                clients.RemoveAll(item => item.ip == sender.ip);
                            }
                            IsConnect = false;
                        }
                        else if (message[0] == ASK_HISTORY)
                        {
                            SendHistoryMessage(history, sender);
                        }
                        else if (message[0] == HISTORY)
                        {
                            string receivedHistory = message.Substring(1);
                            //history.Clear();
                            List<string> bufHistory = new List<string>();
                            while (receivedHistory != "")
                            {
                                bufHistory.Add(receivedHistory.Substring(0, receivedHistory.IndexOf(MESSAGE)));
                                receivedHistory = receivedHistory.Substring(receivedHistory.IndexOf(MESSAGE) + 1);
                            }

                            lock (MainWindow.threadHistoryLock)
                            {
                                history.Clear();
                                foreach (string item in bufHistory)
                                {
                                    history.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            catch(System.IO.IOException)
            {
                history.Add(sender.Name + " (" + (sender.ip).ToString() + ") left the chat " + DateTime.Now);
                lock (MainWindow.threadLock)
                {
                    clients.RemoveAll(item => item.ip == sender.ip);
                }
            }
            finally
            {
                if (clientStream != null)
                {
                    clientStream.Close();
                }
                if (sender.tcp != null)
                {
                    sender.tcp.Close();
                }
            }
        }
    }
}
