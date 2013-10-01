using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


//服务器程序
namespace SocketServer
{
    class Program
    {
        private static byte[] result = new Byte[1024];
        private static int mypoot = 8889;
        static Socket serverSocket;

        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            serverSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ip, mypoot));
            serverSocket.Listen(10);
            Console.WriteLine("启动监听{0}成功",serverSocket.LocalEndPoint.ToString());

            Thread myThread = new Thread(ListenClientConnect);  //监听线程
            myThread.Start();
            Console.ReadLine();
        }

        private static void ListenClientConnect()
        {
            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                clientSocket.Send(Encoding.ASCII.GetBytes("Server say hello!"));
                Thread reciveThread = new Thread(ReceiveMessage);
                reciveThread.Start(clientSocket);
            }
        }

        private static void ReceiveMessage(Object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;
            while (true)
            {
                try
                {
                    int receiveNumber = myClientSocket.Receive(result);
                    Console.WriteLine("接收客户端{0}消息{1}",myClientSocket.RemoteEndPoint.ToString(),Encoding.ASCII.GetString(result,0,receiveNumber));

                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    myClientSocket.Shutdown(SocketShutdown.Both);
                    myClientSocket.Close();
                    break;
                }
            }
        }
    }
}
