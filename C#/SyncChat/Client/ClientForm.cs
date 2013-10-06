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
        private bool isExit = false;
        private TcpClient client;
        private BinaryReader br;
        private BinaryWriter bw;

        public ClientForm()
        {
            InitializeComponent();
            Random r = new Random((int)DateTime.Now.Ticks);
            textBoxUserName.Text = "user" + r.Next(100, 999);
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
            sendMessage("Login," + textBoxUserName.Text);   //将登录信息发送服务器
            AddOnline(textBoxUserName.Text);
            Thread threadReceive = new Thread(new ThreadStart(receiveData));
            threadReceive.IsBackground = true;
            threadReceive.Start();
        }

        private void receiveData()
        {
            String receiveString = String.Empty;
            while (isExit==false)
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
                switch (command)
                {
                    case "login":
                        AddOnline(splitString[1]);
                        break;
                    case "logout":
                        RemoveUserName(splitString[1]);
                        AddTalkMessage(String.Format("[{0}]退出聊天室",splitString[1]));
                        break;
                    case "talk":
                        AddTalkMessage(splitString[1]+"对你说：");
                        AddTalkMessage(receiveString.Substring(splitString[0].Length+splitString[1].Length+2));
                        break;
                    default:
                        AddTalkMessage("无相关指令："+receiveString);
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
                AddTalkMessage(String.Format("我说：\n{0}", textBoxMessage.Text));
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

        #region 用户退出
        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client != null)
            {
                sendMessage("Logout," + textBoxUserName.Text);
                isExit = true;
                br.Close();
                bw.Close();
                client.Close();
            }
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
    }
}
