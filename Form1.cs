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

        TcpListener server = null; //리스닝 대기 서버
        TcpClient clientSocket = null; // TCP 클라이언트

        string date;
        private Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>(); // TCP클라이언트 + 클라이언트명 자료구조
        private int PORT = 5000;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitForm(); //변수 초기화
            txtIP.Text = GetLocalIP();
            txtPort.Text = PORT.ToString();
        }

        private string GetLocalIP()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            String localIP = string.Empty;

            for(int i=0; i<host.AddressList.Length; i++)
            {
                // 내 로컬 ip주소 얻기
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
            // 서버 실행 시 스레드 처리
            Thread thread1 = new Thread(InitSocket);
            thread1.IsBackground = true;
            thread1.Start();
        }

        private void InitSocket()
        {
            //parse를 통해 ip주소 문자열을 ipaddress 인스턴스로 변환
            server = new TcpListener(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text));
            // 클라이언트 소켓은 null로 초기화
            clientSocket = default(TcpClient);
            server.Start();
            Displaytext("System : 서버 시작");

            while (true)
            {
                try
                {
                    //클라이언트 소켓 수신
                    clientSocket = server.AcceptTcpClient();
                    //스트림 생성
                    NetworkStream stream = clientSocket.GetStream();
                    byte[] buffer = new byte[1024];
                    //스트림 읽기
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string userName = Encoding.Unicode.GetString(buffer, 0, bytes);
                    userName = userName.Substring(0, userName.IndexOf("$"));
                    Displaytext("System : [" + userName + "] 접속");

                    //접속한 client 딕셔너리에 추가
                    clientList.Add(clientSocket, userName);

                    //연결된 client에 메세지 전송
                    SendMessageAll(userName + " 님이 입장하였습니다.", "", false);

                    //클라이언트 리스트 창에 입력
                    SetUserList(userName, "I");

                    HandleClient h_client = new HandleClient();
                    h_client.OnReceived += new HandleClient.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new HandleClient.DisconnectedHandler(h_client_OnDisconnected);
                    
                    //클라이언트 연결 및 채팅시작
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
