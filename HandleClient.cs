using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chat_Server_tcp
{
    internal class HandleClient
    {
        TcpClient clientSocket = null;
        public Dictionary<TcpClient, string> clientList = null;

        public void startClient(TcpClient clientSocket, Dictionary<TcpClient, string> clientList)
        {
            this.clientSocket = clientSocket;
            this.clientList = clientList;

            Thread t_handler = new Thread(doChat);
            t_handler.IsBackground = true;
            t_handler.Start();
        }

        // 대리자와 이벤트 핸들러 선언(옵저버 패턴) 
        public delegate void MessageDisplayHandler(string message, string userName);
        public event MessageDisplayHandler OnReceived; //클라이언트 상태 받기

        public delegate void DisconnectedHandler(TcpClient clientSocket);
        public event DisconnectedHandler OnDisconnected; //클라이언트 연결 끊기

        private void doChat()
        {
            NetworkStream stream = null;
            try
            {
                byte[] buffer = new byte[1024];
                string msg = string.Empty;
                int bytes = 0;
                int MessageCount = 0;

                while (true)
                {
                    MessageCount++;
                    stream = clientSocket.GetStream();
                    bytes = stream.Read(buffer, 0, buffer.Length);
                    msg = Encoding.Unicode.GetString(buffer, 0, bytes);
                    msg = msg.Substring(0, msg.IndexOf("$"));

                    if(OnReceived != null)
                    {
                        OnReceived(msg, clientList[clientSocket].ToString());
                    }
                }
            }catch(SocketException se)
            {
                Trace.WriteLine(string.Format("DoChat - SocketException : {0}", se.Message));

                if (clientSocket != null)
                {
                    if (OnDisconnected != null)
                        OnDisconnected(clientSocket);

                    clientSocket.Close();
                    stream.Close();
                }
            }catch(Exception ex)
            {
                Trace.WriteLine(string.Format("DoChat - Exception : {0}", ex.Message));

                if (clientSocket != null)
                {
                    if (OnDisconnected != null)
                        OnDisconnected(clientSocket);

                    clientSocket.Close();
                    stream.Close();
                }
            }
        }
    }
}
