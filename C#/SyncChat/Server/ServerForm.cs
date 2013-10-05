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

namespace Server
{
    public partial class ServerForm : Form
    {
        /// <summary>
        /// 保存服务端与客户端连接的用户
        /// </summary>
        private List<User> userList = new List<User>();
        /// <summary>
        /// 本机IP地址
        /// </summary>
        IPAddress localAddress;
        private const int port = 51888;
        private TcpListener myListener;
        /// <summary>
        /// 是否正常退出所有接受线程
        /// </summary>
        bool isNormalExit = false;

        public ServerForm()
        {
            InitializeComponent();
            listBoxStatus.HorizontalScrollbar = true;
            IPAddress[] addrIP = Dns.GetHostAddresses(Dns.GetHostName());
            localAddress = addrIP[0];
            btnCancelListen.Enabled = false;
        }


        /// <summary>
        /// 开始监听事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStartListen_Click(object sender, EventArgs e)
        {
            TcpClient newClient = null;
            while (true)
            {
                try
                {
                    newClient = myListener.AcceptTcpClient();
                }
                catch
                {
                    break;
                }
                User user = new User(newClient);
                userList.Add(user);
                Thread threadReceive = new Thread(ReceiveData);
                threadReceive.Start(user);
                
            }

        }

        private void ReceiveData(object userState)
        {
            User user = (User)userState;
            TcpClient client = user.client;
            while (isNormalExit == false)
            {
                String receiveString = string.Empty;
                try
                {
                    //消除边界第二种方法，包含字符串长度前缀
                    receiveString = user.br.ReadString();
                }
                catch
                {
                    if (isNormalExit == false)
                    {
                        AddItemToListBox(String.Format("与{0}失去联系，已终止接收该用户信息",client.Client.RemoteEndPoint.ToString()));
                        RemoveUser(user);
                    }
                    break;
                }
                AddItemToListBox(String.Format("来自[{0}]：{1}", user.client.Client.RemoteEndPoint.ToString(), receiveString));
                String[] splitString = receiveString.Split(',');
                switch (splitString[0])
                {
                    case "Login":
                        user.userName = splitString[1];
                        sendToAllClient(user, receiveString);
                        break;
                    case "Logout":
                        sendToAllClient(user, receiveString);
                        userList.Remove(user);
                        break;
                    case "Talk":
                        String talkString = receiveString.Substring(splitString[0].Length + splitString[1].Length + 2); //指令格式 command,username,message
                        AddItemToListBox(String.Format("{0}对{1}说：{2}",user.userName,splitString[1],talkString));
                        sendToClient(user, "talk," + user.userName + "," + talkString);
                        foreach (User target in userList)
                        {
                            if (target.userName == splitString[1] && splitString[1] != user.userName)
                            {
                                sendToClient(target, "talk," +user.userName + "," + talkString);
                                break;
                            }
                        }
                        break;
                    default:
                        AddItemToListBox("无相关指令：" + receiveString);
                        break;
                }
            }
        }


        private void sendToAllClient(User user, String message)
        {
            String command = message.Split(',')[0].ToLower();
            if (command == "login")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    sendToClient(userList[i], message);  //将登陆信息发给所有人
                    if (userList[i].userName != user.userName)
                    {
                        sendToClient(user,"login,"+userList[i].userName); //将已登录人信息发给刚登陆的人
                    }
                }
            }
            else if (command == "logout")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    if (userList[i].userName != user.userName)
                    {
                        sendToClient(userList[i], message);
                    }
                }
            }
        }

        private void sendToClient(User user, String message)
        {
            try
            {
                user.bw.Write(message);
                user.bw.Flush();  //清空缓冲区，使数据写上传送，而不是等缓冲区满再发送
                AddItemToListBox(String.Format("向[{0}]发送：{1}", user.userName, message));
            }
            catch
            {
                AddItemToListBox(String.Format("向[{0}]发送信息失败", user.userName));
            }
        }

        /// <summary>
        /// 定义委托（夸线程操作C#控件需要定义代理委托操作）
        /// </summary>
        /// <param name="str"></param>
        private delegate void AddItemToListBoxDelegate(String str);

        private void AddItemToListBox(String str)
        {
            if (listBoxStatus.InvokeRequired)
            {
                AddItemToListBoxDelegate d = AddItemToListBox;
                listBoxStatus.Invoke(d, str); //交给创建该控件的线程操作数据，通过委托
            }
            else
            {
                listBoxStatus.Items.Add(str);
                //指向控件最后一项，超出屏幕也能实现
                listBoxStatus.SelectedIndex = listBoxStatus.Items.Count - 1;
                listBoxStatus.ClearSelected();
            }
        }

        /// <summary>
        /// 用户退出，更新状态
        /// </summary>
        /// <param name="user"></param>
        private void RemoveUser(User user)
        {
            userList.Remove(user);
            user.Close();
            AddItemToListBox(String.Format("当前连接用户数：{0}", userList.Count));
        }

        /// <summary>
        /// 取消监听事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancelListen_Click(object sender, EventArgs e)
        {
            AddItemToListBox("停止服务端服务，并依次退出用户");
            isNormalExit = true;
            //逆序删除，不用移位
            for (int i = userList.Count; i >= 0; i--)
            {
                RemoveUser(userList[i]);
            }
            myListener.Stop();
            btnStartListen.Enabled = true;
            btnCancelListen.Enabled = false;
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (myListener != null)
            {
                btnCancelListen.PerformClick(); //触发停止监听事件
            }
        }
    }
}
