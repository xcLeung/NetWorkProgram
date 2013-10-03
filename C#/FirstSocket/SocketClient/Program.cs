using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


//客户端程序
namespace SocketClient
{
    class Program
    {
        private static byte[] result = new Byte[1024];

        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");  //服务器地址
            Socket clientSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

            try
            {
                clientSocket.Connect(new IPEndPoint(ip, 8889));
                Console.WriteLine("连接服务器成功");
                Console.WriteLine("本机地址：{0}", clientSocket.LocalEndPoint.ToString());
            }
            catch
            {
                Console.WriteLine("连接服务器失败，请按回车键退出");
                return;
            }

            Thread sendThread = new Thread(sendMessage); // 发送信息线程
            sendThread.Start(clientSocket);

            while (true)
            {
                try
                {
                    int receiveLength = clientSocket.Receive(result);
                    Console.WriteLine("接收服务器消息：{0}：", Encoding.ASCII.GetString(result, 0, receiveLength));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    break;
                }
            }

            
            //for (int i = 0; i < 10; i++)
            //{
            //    try
            //    {
            //        Thread.Sleep(1000);
            //        string sendMessage = "hello i am lxc Client" + DateTime.Now;
            //        clientSocket.Send(Encoding.ASCII.GetBytes(sendMessage));
            //        Console.WriteLine("向服务器发送消息：{0}", sendMessage);
            //    }
            //    catch
            //    {
            //        clientSocket.Shutdown(SocketShutdown.Both);
            //        clientSocket.Close();
            //        return;
            //    }
            //}
            //Console.WriteLine("发送完毕，按回车键退出键");
           // Console.ReadLine();
        }

        private static void sendMessage(Object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;
            while (true)
            {
                try
                {
                    Console.WriteLine("向服务器{0}发送信息：", myClientSocket.RemoteEndPoint.ToString());
                    String msg = Console.ReadLine();
                    myClientSocket.Send(Encoding.ASCII.GetBytes(msg));
                }
                catch (Exception ex)
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
