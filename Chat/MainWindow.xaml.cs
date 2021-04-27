using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int tcpPort = 8888;
        const int udpPort = 8889;
        List<Client> clients = new List<Client>();
        List<string> history = new List<string>();
        Thread udpThread = null;
        Thread tcpThread = null;
        public static object threadHistoryLock = new object();
        public static object threadLock = new object();

        public static IPAddress ipAddress;

        public MainWindow()
        {
            InitializeComponent();

            IPAddress[] MyIPList = Dns.GetHostByName(Dns.GetHostName()).AddressList;

            foreach (IPAddress item in MyIPList)
            {
                cmbIP.Items.Add(item);
            }
            cmbIP.SelectedIndex = cmbIP.Items.Count - 1;
        }

        public void GetIP()
        {
            ipAddress = IPAddress.Parse(cmbIP.SelectedItem.ToString());
        }

        public void Send()
        {
            string message = tbMessage.Text;
            Message.SendMessage(clients, message);
            history.Add(tbLogin.Text + " (" + ipAddress + ") " + message + " " + DateTime.Now);
            tbMessage.Clear();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            Send();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            string clientName = tbLogin.Text;
            GetIP();

            //IPAddress ipBroadcast = IPAddress.Parse("192.168.100.255");
            IPAddress ipBroadcast = getBroadcast(ipAddress);

            Thread MessageUpdater = new Thread(() => { Update(); });
            MessageUpdater.Start();
            Thread GetHistory = new Thread(() => { getHistory(); });
            GetHistory.Start();

            Connection.SendBroadcastPackage(clientName, ipBroadcast, udpPort, history);
            tcpThread = new Thread(() => { Connection.ListenTCP(tcpPort, clients, history); });
            tcpThread.Start();
            udpThread = new Thread(() => { Connection.ReceiveUDP(udpPort, tcpPort, clients, history, clientName); });
            udpThread.Start();
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            Message.SendDisconnectMessage(clients);
            Environment.Exit(0);
        }

        public void DisplayHistory(List<Client> history)
        {
            lbHistory.Items.Clear();
            for (int i = 0; i < history.Count; i++)
            {
                lbHistory.Items.Add(history[i]);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Message.SendDisconnectMessage(clients);
            Environment.Exit(0);
        }

        public void Update()
        {
            int i;
            int ListCount = 0, HistoryCount;
            while (true)
            {
                Dispatcher.Invoke(() =>
                {
                    lock (threadHistoryLock)
                    {
                        HistoryCount = history.Count;
                        if (HistoryCount > ListCount)
                        {
                            lbHistory.Items.Clear();
                            for (i = 0; i < history.Count; i++)
                            {
                                if (history[i].Contains(ipAddress.ToString()))
                                {
                                    lbHistory.Items.Add("You:" + history[i].Substring(history[i].IndexOf(")") + 1));
                                }
                                else
                                {
                                    lbHistory.Items.Add(history[i]);
                                }
                            }
                            ListCount = lbHistory.Items.Count;

                            lbHistory.ScrollIntoView(lbHistory.Items[lbHistory.Items.Count - 1]);
                        }

                    }

                });

                Thread.Sleep(200);
            }

        }

        public void getHistory()
        {
            DateTime start, finish = new DateTime();
            TimeSpan elapsedSpan = new TimeSpan();
            bool isHaveHistory = false;
            start = DateTime.Now;
            while (elapsedSpan.TotalMilliseconds <= 5000 && !isHaveHistory)
            {
                if (clients.Count != 0)
                {
                    Thread.Sleep(500);
                    Dispatcher.Invoke(() =>
                    {
                        Message.SendAskHistoryMessage(clients[0]);

                    });
                }
                finish = DateTime.Now;
                elapsedSpan = TimeSpan.FromTicks(finish.Ticks - start.Ticks);
                Thread.Sleep(50);
            }

        }

        private IPAddress getBroadcast(IPAddress userIP)
        {
            IPAddress Mask = null;

            NetworkInterface[] allNets = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in allNets)
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (userIP.Equals(unicastIPAddressInformation.Address))
                        {
                            Mask = unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            byte[] byteMask = Mask.GetAddressBytes();
            byte[] userMask = userIP.GetAddressBytes();
            for (int i = 0; i < 4; i++)
            {
                byteMask[i] = (byte)((byte)~byteMask[i] | userMask[i]);
            }
            Mask = new IPAddress(byteMask);

            return Mask;
        }

        private void tbMessage_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Send();
            }
        }
    }
}
