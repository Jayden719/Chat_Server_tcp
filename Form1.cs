using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chat_Server_tcp
{
    public partial class Form1 : Form
    {

        TcpListener server = null;
        TcpClient clientSocket = null;

        string date;
        private Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();
        private int PORT = 5000;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitForm();
            txtIP.Text = GetLocalIP();
            txtPort.Text = PORT.ToString();
        }

        private string GetLocalIP()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            String localIP = string.Empty;

            for(int i=0; i<host.AddressList.Length; i++)
            {
                if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = host.AddressList[i].ToString();
                    break;
                }
            }
            return localIP;
        }

        private void InitForm()
        {
            txtIP.Text = "";
            txtPort.Text = "";
            txtMessage.Text = "";
            richTextBox1.Text = "";
            listBox1.Items.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread thread1 = new Thread(InitSocket);
            thread1.IsBackground = true;
            thread1.Start();
        }

        private void InitSocket()
        {
            server = new TcpListener(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text));
            clientSocket = default(TcpClient);
            server.Start();
            Displaytext("System : 서버 시작");

            while (true)
            {
                try
                {
                    clientSocket = server.AcceptTcpClient();
                    NetworkStream stream = clientSocket.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string userName = Encoding.Unicode.GetString(buffer, 0, bytes);
                    userName = userName.Substring(0, userName.IndexOf("$"));
                    Displaytext("System : [" + userName + "] 접속");

                    clientList.Add(clientSocket, userName);
                    SendMessageAll(userName + " 님이 입장하였습니다.", "", false);
                    SetUserList(userName, "I");

                    HandleClient h_client = new HandleClient();
                    h_client.OnReceived += new HandleClient.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new HandleClient.DisconnectedHandler(h_client_OnDisconnected);
                    h_client.startClient(clientSocket, clientList);
                }
                catch(SocketException se) { break; }
                catch(Exception ex) { break;  }
            }
            clientSocket.Close();
            server.Stop();
        }

        private void h_client_OnDisconnected(TcpClient clientSocket)
        {
            if (clientList.ContainsKey(clientSocket))
            {
                clientList.Remove(clientSocket);
            }
        }

        private void OnReceived(string message, string userName)
        {
            if (message.Equals("LeaveChat"))
            {
                string displayMessage = "Leave user : " + userName;

                Displaytext(displayMessage);
                SendMessageAll("LeaveChat", userName, true);
                SetUserList(userName, "D");
            }
            else
            {
                string displayMessage = "From Client : " + userName + " : " + message;
                Displaytext(displayMessage);
                SendMessageAll(message, userName, true);
            }
        }

        private void SetUserList(string userName, string div)
        {
            try
            {
                if (div.Equals("I"))
                {
                    listBox1.Items.Add(userName);
                }else if (div.Equals("D"))
                {
                    listBox1.Items.Remove(userName);
                }
            }catch(Exception ex){
                MessageBox.Show(ex.ToString());
            }
        }

        private void SendMessageAll(string message, string userName, bool flag)
        {
           foreach(var c in clientList)
            {
                date = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");

                TcpClient client = c.Key as TcpClient;
                NetworkStream stream = client.GetStream();
                byte[] buffer = null;

                if (flag)
                {
                    if (message.Equals("LeaveChat"))
                    {
                        buffer = Encoding.Unicode.GetBytes(userName + " 님이 대화방을 나갔습니다");
                    }
                    else
                    {
                        buffer = Encoding.Unicode.GetBytes("[ " + date + " ] " + userName + " : " + message);
                    }
                }
                else
                {
                    buffer = Encoding.Unicode.GetBytes(message);
                }

                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
            }
        }

        private void Displaytext(string text)
        {
            richTextBox1.Invoke((MethodInvoker)delegate { 
                richTextBox1.AppendText(text + "\r\n"); 
            });

            richTextBox1.Invoke((MethodInvoker)delegate
            {
                richTextBox1.ScrollToCaret();
            });

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string userName = "Admin";
            string message = txtMessage.Text.Trim();
            string displayMessage = "[" + userName + "] : " + message;

            Displaytext(displayMessage);
            SendMessageAll(message, userName, true);
        }
    }
}
