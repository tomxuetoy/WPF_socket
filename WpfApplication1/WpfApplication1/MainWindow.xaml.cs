using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using System.Windows.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SocketListener listener;
        public MainWindow()
        {
            InitializeComponent();

            InitServer();
        }

        private void InitServer()
        {
            System.Timers.Timer t = new System.Timers.Timer(2000);
            //实例化Timer类，设置间隔时间为5000毫秒；
            t.Elapsed += new System.Timers.ElapsedEventHandler(CheckListen);
            //到达时间的时候执行事件； 
            t.AutoReset = true;
            t.Start();
        }

        private void CheckListen(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (listener != null && listener.Connection != null)
            {
                //label2.Content = listener.Connection.Count.ToString();
                ShowText("连接数：" + listener.Connection.Count.ToString());
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Thread th = new Thread(new ThreadStart(SocketListen));
            th.Start();
        }

        private void SocketListen()
        {
            listener = new SocketListener();
            listener.ReceiveTextEvent += new SocketListener.ReceiveTextHandler(ShowText);
            listener.StartListen();
        }

        public delegate void ShowTextHandler(string text);
        ShowTextHandler setText;

        private void ShowText(string text)
        {
            if (System.Threading.Thread.CurrentThread != txtSocketInfo.Dispatcher.Thread)
            {
                if (setText == null)
                {
                    setText = new ShowTextHandler(ShowText);
                }
                txtSocketInfo.Dispatcher.BeginInvoke(setText, DispatcherPriority.Normal, new string[] { text });
            }
            else
            {
                txtSocketInfo.AppendText(text + "\n");
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            ClientWindow client = new ClientWindow();
            client.Show();
        }
    }

    public class Connection
    {
        Socket _connection;

        public Connection(Socket socket)
        {
            _connection = socket;
        }

        public void WaitForSendData()
        {
            while (true)
            {
                byte[] bytes = new byte[1024];
                string data = "";

                //等待接收消息
                int bytesRec = this._connection.Receive(bytes);

                if (bytesRec == 0)
                {
                    ReceiveText("客户端[" + _connection.RemoteEndPoint.ToString() + "]连接关闭...");
                    break;
                }

                data += Encoding.UTF8.GetString(bytes, 0, bytesRec);
                ReceiveText("收到消息：" + data);

                string sendStr = "服务端已经收到信息！";
                byte[] bs = Encoding.UTF8.GetBytes(sendStr);
                _connection.Send(bs, bs.Length, 0);
            }
        }

        public delegate void ReceiveTextHandler(string text);
        public event ReceiveTextHandler ReceiveTextEvent;
        private void ReceiveText(string text)
        {
            if (ReceiveTextEvent != null)
            {
                ReceiveTextEvent(text);
            }
        }
    }

    public class SocketListener
    {
        public Hashtable Connection = new Hashtable();

        public void StartListen()
        {
            try
            {
                //端口号、IP地址
                int port = 2000;
                string host = "127.0.0.1";
                IPAddress ip = IPAddress.Parse(host);
                IPEndPoint ipe = new IPEndPoint(ip, port);

                //创建一个Socket类
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Bind(ipe);//绑定2000端口
                s.Listen(0);//开始监听

                ReceiveText("启动Socket监听...");

                while (true)
                {
                    Socket connectionSocket = s.Accept();//为新建连接创建新的Socket

                    ReceiveText("客户端[" + connectionSocket.RemoteEndPoint.ToString() + "]连接已建立...");

                    Connection gpsCn = new Connection(connectionSocket);
                    gpsCn.ReceiveTextEvent += new Connection.ReceiveTextHandler(ReceiveText);

                    Connection.Add(connectionSocket.RemoteEndPoint.ToString(), gpsCn);

                    //在新线程中启动新的socket连接，每个socket等待，并保持连接
                    Thread thread = new Thread(new ThreadStart(gpsCn.WaitForSendData));
                    thread.Name = connectionSocket.RemoteEndPoint.ToString();
                    thread.Start();
                }
            }
            catch (ArgumentNullException ex1)
            {
                ReceiveText("ArgumentNullException:" + ex1);
            }
            catch (SocketException ex2)
            {
                ReceiveText("SocketException:" + ex2);
            }
        }

        public delegate void ReceiveTextHandler(string text);
        public event ReceiveTextHandler ReceiveTextEvent;
        private void ReceiveText(string text)
        {
            if (ReceiveTextEvent != null)
            {
                ReceiveTextEvent(text);
            }
        }
    }
}
