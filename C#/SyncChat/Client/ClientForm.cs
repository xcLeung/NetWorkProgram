using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class ClientForm : Form
    {
        private TcpCommon tcpCommon = new TcpCommon();

        private bool isExit = false;
        private TcpClient client;
        private BinaryReader br;
        private BinaryWriter bw;

        private UdpClient receiveUdpClient;

        private TcpListener myListener;
        private List<User> userList = new List<User>();
        private List<User> myClientList = new List<User>();
        bool isNormalExit = false;

        public ClientForm()
        {
            InitializeComponent();
            Random r = new Random((int)DateTime.Now.Ticks);
            textBoxUserName.Text = "user" + r.Next(100, 999);   //用户名
            listBoxOnlineStatus.HorizontalScrollbar = true;
        }


        /// <summary>
        /// 登录连接服务器事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLogin_Click(object sender, EventArgs e)
        {
            btnLogin.Enabled = false;
            try
            {
                client = new TcpClient(Dns.GetHostName(), 51888);    //创建一个绑定服务器IP和端口的套接字

                IPEndPoint iep = client.Client.LocalEndPoint as IPEndPoint;
                myListener = new TcpListener(Dns.GetHostAddresses(Dns.GetHostName())[0], iep.Port);  //初始化监听tcp,监听本地端口
                myListener.Start();
                Thread threadMyListener = new Thread(listenClientConnect);
                threadMyListener.IsBackground = true;
                threadMyListener.Start();

                IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
                IPEndPoint udpiep = new IPEndPoint(ips[ips.Length-1], iep.Port);
                receiveUdpClient = new UdpClient(udpiep);
                AddTalkMessage("连接成功");
            }
            catch
            {
                AddTalkMessage("连接失败");
                btnLogin.Enabled = true;
                return;
            }

            NetworkStream networkstream = client.GetStream();  //创建字节流与服务器端交互信息
            br = new BinaryReader(networkstream);
            bw = new BinaryWriter(networkstream);
            sendMessage("Login," + textBoxUserName.Text+","+client.Client.LocalEndPoint.ToString());   //将登录信息发送服务器
            AddOnline(textBoxUserName.Text);

            Thread threadReceive = new Thread(new ThreadStart(receiveData));
            threadReceive.IsBackground = true;
            threadReceive.Start();

            Thread threadUdpReceive = new Thread(receiveUdpData);
            threadUdpReceive.IsBackground = true;
            threadUdpReceive.Start();
        }

        #region C-S-C 通信
        private void receiveData()
        {
            String receiveString = String.Empty;
            while (isExit == false)
            {
                try
                {
                    receiveString = br.ReadString();
                }
                catch
                {
                    if (isExit == false)
                    {
                        MessageBox.Show("与服务器失去联系");
                    }
                    break;
                }
                String[] splitString = receiveString.Split(',');
                String command = splitString[0].ToLower();
                String IPpoint = splitString[2];
                switch (command)
                {
                    case "login":
                        AddOnline(splitString[1] + "," + IPpoint);
                        break;
                    case "logout":
                        RemoveUserName(splitString[1]);
                        AddTalkMessage(String.Format("[{0}]退出聊天室", splitString[1]));
                        break;
                    case "talk":
                        AddTalkMessage(splitString[1] + "对你说：");
                        AddTalkMessage(receiveString.Substring(splitString[0].Length + splitString[1].Length + 2));
                        break;
                    default:
                        AddTalkMessage("无相关指令：" + receiveString);
                        break;
                }
            }
            Application.Exit();
        }

        private void sendMessage(String message)
        {
            try
            {
                bw.Write(message);
                bw.Flush();
            }
            catch
            {
                AddTalkMessage("发送失败！");
            }
        }


        /// <summary>
        /// 发送按钮事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (listBoxOnlineStatus.SelectedIndex != -1)
            {
                sendMessage("Talk," + listBoxOnlineStatus.SelectedItem + "," + textBoxMessage.Text);
                AddTalkMessage(String.Format("我说：{0}", textBoxMessage.Text));
                textBoxMessage.Clear();
            }
            else
            {
                //这里做一个广播处理
                sendMessage("All," + textBoxMessage.Text);
                AddTalkMessage(String.Format("我对所有人说：\n{0}", textBoxMessage.Text));
                textBoxMessage.Clear();
                //MessageBox.Show("请先在[当前在线]中选择一个对话者");
            }
        }
        #endregion

        #region C-C tcp 通信
        /// <summary>
        /// 监听是否有客户接入
        /// </summary>
        private void listenClientConnect()
        {
            TcpClient newClient = null;
            while (true)   //循环监听
            {
                try
                {
                    newClient = myListener.AcceptTcpClient();   //有客户端接入，新建一个与客户端通信的套接字
                    //MessageBox.Show("listen："+newClient.Client.RemoteEndPoint.ToString());
                }
                catch
                {
                    break;
                }
                User user = new User(newClient);
                Thread threadReceive = new Thread(receiveTcpData);  //开启与客户端通信的线程
                threadReceive.Start(user);
                userList.Add(user);
            }
           
        }

        /// <summary>
        /// tcp接收数据
        /// </summary>
        /// <param name="objUser"></param>
        private void receiveTcpData(Object objUser)
        {
            User user = (User)objUser;
            TcpClient client = user.client;
            while (true)
            {
                String receiveString = string.Empty;
                try
                {
                    //消除边界第二种方法，包含字符串长度前缀
                    receiveString = user.br.ReadString();

                    if (receiveString.ToLower() == "SendFile".ToLower())
                    {
                        receiveString = user.br.ReadString();
                        String filePath= Path.Combine(Environment.CurrentDirectory,"receiveFile"+receiveString);
                        AddTalkMessage(String.Format("开始接收文件，存放位置：{0}",filePath));
                        if (ReceiveFile(filePath, client.GetStream()))
                        {
                            AddTalkMessage(String.Format("接收文件完毕！"));
                        }
                        else
                        {
                            AddTalkMessage(String.Format("接收文件失败！"));
                        }
                    }
                    else
                    {
                        AddTalkMessage(String.Format("[{0}]对你说：{1}", client.Client.RemoteEndPoint.ToString(), receiveString));
                    }
                }
                catch
                {
                    if (isNormalExit == false)
                    {
                        RemoveUser(user);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// C-C tcp发送处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCC_Click(object sender, EventArgs e)
        {
            if (listBoxOnlineStatus.SelectedIndex != -1)
            {
                String userName = listBoxOnlineStatus.SelectedItem.ToString().Split(',')[0];
                String targetIPpoint = listBoxOnlineStatus.SelectedItem.ToString().Split(',')[1];  //取出对方ipendpoint
                int port = 0;
                splitIPEndPoint(targetIPpoint, ref port);
                if (port == 0)
                {
                    MessageBox.Show("发送失败！");
                    return;
                }


                TcpClient newClient = new TcpClient(Dns.GetHostName(), port);
                User newUserClient = new User(newClient);
                myClientList.Add(newUserClient);

                sendToClient(newUserClient, textBoxMessage.Text);

                AddTalkMessage(String.Format("[C-C]我说：{0}", textBoxMessage.Text));
                textBoxMessage.Clear();
            }
            else
            {
                MessageBox.Show("请先在[当前在线]中选择一个对话者");
            }
        }

        private void sendToClient(User user, String message)
        {
            try
            {
                user.bw.Write(message);
                user.bw.Flush();  //清空缓冲区，使数据写上传送，而不是等缓冲区满再发送
            }
            catch
            {
                MessageBox.Show("发送失败！");
            }
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendFile_Click(object sender, EventArgs e)
        {
            if (listBoxOnlineStatus.SelectedIndex != -1)
            {
                String userName = listBoxOnlineStatus.SelectedItem.ToString().Split(',')[0];
                String targetIPpoint = listBoxOnlineStatus.SelectedItem.ToString().Split(',')[1];  //取出对方ipendpoint
                int port = 0;
                splitIPEndPoint(targetIPpoint, ref port);
                if (port == 0)
                {
                    MessageBox.Show("发送文件失败！");
                    return;
                }


                TcpClient newClient = new TcpClient(Dns.GetHostName(), port);
                User newUserClient = new User(newClient);
                myClientList.Add(newUserClient);

                sendToClient(newUserClient, "SendFile");
                AddTalkMessage(String.Format("开始发送文件：{0}", txtFileName.Text));
                sendToClient(newUserClient, txtFileName.Text.Substring(txtFileName.Text.LastIndexOf('.')));
                if (SendFile(txtFileName.Text, newUserClient.client.GetStream()))
                {
                    AddTalkMessage(String.Format("发送文件完毕！"));
                }
                else
                {
                    AddTalkMessage(String.Format("传输文件失败"));
                }
                
                textBoxMessage.Clear();
            }
            else
            {
                MessageBox.Show("请先在[当前在线]中选择一个对话者");
            }
        }
        #endregion

        #region C-C udp 通信
        private void receiveUdpData()
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);   //接收所有远程主机的信息
            while (true)
            {
                try
                {
                    byte[] receiveBytes = receiveUdpClient.Receive(ref remote);
                    String receiveMessage = Encoding.Unicode.GetString(receiveBytes, 0, receiveBytes.Length);
                    AddTalkMessage(String.Format("[{0}]对你说：{1}", remote, receiveMessage));
                }
                catch
                {
                    break;
                }
            }
        }

        private void btnUdpSend_Click(object sender, EventArgs e)
        {
            if (listBoxOnlineStatus.SelectedIndex != -1)
            {
              
                String userName = listBoxOnlineStatus.SelectedItem.ToString().Split(',')[0];
                String targetIPpoint = listBoxOnlineStatus.SelectedItem.ToString().Split(',')[1];  //取出对方ipendpoint
                int port = 0;
                splitIPEndPoint(targetIPpoint, ref port);
                if (port == 0)
                {
                    MessageBox.Show("发送失败！");
                    return;
                }

                UdpClient sendUdpClient = new UdpClient(0);   //初始化udp
                byte[] bytes = System.Text.Encoding.Unicode.GetBytes(textBoxMessage.Text);   //转成字节流
                IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
                IPEndPoint remoteiep = new IPEndPoint(ips[ips.Length-1], port);

                //for (int i = 0; i < ips.Length; i++)
                //{
                //    MessageBox.Show(ips[i].ToString());
                //}

                sendUdpClient.Send(bytes, bytes.Length, remoteiep);

                AddTalkMessage(String.Format("[UDP]我说：{0}", textBoxMessage.Text));
                textBoxMessage.Clear();
            }
            else
            {
                MessageBox.Show("请先在[当前在线]中选择一个对话者");
            }

            
            
        }
        #endregion

           
        /// <summary>
        /// 分隔地址和端口
        /// </summary>
        /// <param name="target"></param>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        private void splitIPEndPoint(String target, ref int port)
        {
            try
            {
                int index = target.LastIndexOf(':');
                port = Convert.ToInt32(target.Substring(index + 1, target.Length - index- 1));
            }
            catch
            {
                MessageBox.Show("远程IP格式不正确！");
            }
        }

        #region 用户退出
        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (client != null)
                {
                    sendMessage("Logout," + textBoxUserName.Text);
                    isExit = true;
                    br.Close();
                    bw.Close();
                    client.Close();
                }
                myListener.Stop();
                receiveUdpClient.Close();

            }
            catch
            {
                MessageBox.Show("退出异常！");
            }
        }


        private void RemoveUser(User user)
        {
            userList.Remove(user);
            user.Close();
        }
        #endregion

        #region 维护对话信息控件
        private delegate void MessageDelegate(String message);
        private void AddTalkMessage(String message)
        {
            if (richTextBoxTalkInfo.InvokeRequired)
            {
                MessageDelegate d = new MessageDelegate(AddTalkMessage);
                richTextBoxTalkInfo.Invoke(d, new object[] { message });
            }
            else
            {
                richTextBoxTalkInfo.AppendText(message+"\n");
                richTextBoxTalkInfo.ScrollToCaret();
            }
        }
        #endregion

        #region 维护当前用户在线控件
        private delegate void AddOnlineDelegate(String message);
        private void AddOnline(String message)
        {
            if (listBoxOnlineStatus.InvokeRequired)
            {
                AddOnlineDelegate d = new AddOnlineDelegate(AddOnline);
                listBoxOnlineStatus.Invoke(d, new object[] { message });
            }
            else
            {
                listBoxOnlineStatus.Items.Add(message);
                listBoxOnlineStatus.SelectedIndex = listBoxOnlineStatus.Items.Count-1;
                listBoxOnlineStatus.ClearSelected();
            }
        }

        private delegate void RemoveUserNameDelegate(String userName);
        private void RemoveUserName(String userName)
        {
            if (listBoxOnlineStatus.InvokeRequired)
            {
                RemoveUserNameDelegate d = new RemoveUserNameDelegate(RemoveUserName);
                listBoxOnlineStatus.Invoke(d, new object[] { userName });
            }
            else
            {
                listBoxOnlineStatus.Items.Remove(userName);
                listBoxOnlineStatus.SelectedIndex = listBoxOnlineStatus.Items.Count - 1;
                listBoxOnlineStatus.ClearSelected();
            }
        }
        #endregion

        #region TcpCommon所有方法
        public bool SendFile(String filePath,NetworkStream netstream)
        {
            return tcpCommon.SendFile(filePath, netstream);
        }


        public bool ReceiveFile(string filePath, NetworkStream netstream)
        {
            return tcpCommon.ReceiveFile(filePath, netstream);
        }
        #endregion


        /// <summary>
        /// 选择一个文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnChooseFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog1 = new OpenFileDialog();
            fileDialog1.InitialDirectory = "d://";
            fileDialog1.FilterIndex = 1;
            fileDialog1.RestoreDirectory = true;
            if (fileDialog1.ShowDialog() == DialogResult.OK)
            {
                 txtFileName.Text = fileDialog1.FileName;
            }
            else
            {
                txtFileName.Text = String.Empty;
            } 
        }
        
    }
}
