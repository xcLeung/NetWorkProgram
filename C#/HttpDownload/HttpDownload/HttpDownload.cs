using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HttpDownload
{
    public class HttpDownload
    {
        private ListBox listbox;
        private int threadindex;

        public bool isFinish { get; set; }
        public String targetFileName { get; set; }
        public int startPosition { get; set; }
        public int fileSize { get; set; }
        public String sourceUrl { get; set; }
        public int downloadPosition { get; set; }


        public HttpDownload() { }
        public HttpDownload(ListBox listbox,int threadIndex) {
            this.listbox = listbox;
            this.threadindex = threadIndex;
        }


        /// <summary>
        /// 接收数据
        /// </summary>
        public void Receive(){
            AddStatus(String.Format("线程{0}开始接收",threadindex));
            int totalBytes = 0;
            using (FileStream fs = new FileStream(targetFileName, System.IO.FileMode.Create))
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(sourceUrl);
                    request.AddRange(downloadPosition,startPosition+fileSize-1);
                    Stream stream = request.GetResponse().GetResponseStream();
                    byte[] receiveBytes = new byte[1024];
                    
                    int readbyte = stream.Read(receiveBytes, 0, receiveBytes.Length);
                    while (readbyte > 0)
                    {
                        fs.Write(receiveBytes, 0, readbyte);
                        totalBytes += readbyte;
                        downloadPosition = totalBytes;
                        readbyte = stream.Read(receiveBytes,0,receiveBytes.Length); 
                    }
                    stream.Close();
                }
                catch(Exception ex)
                {
                    AddStatus(String.Format("线程{0}接收出错；{1}", threadindex, ex.Message));
                }
            }
            ChangeStatus(String.Format("线程{0}开始接收",threadindex),"接收完毕",totalBytes);
            this.isFinish = true;
        }



        #region 维护listbox控件
        public delegate void AddStatusDelegate(String message);
        public void AddStatus(String message)
        {
            if (this.listbox.InvokeRequired)
            {
                AddStatusDelegate d = AddStatus;
                this.listbox.Invoke(d, message);
            }
            else
            {
                this.listbox.Items.Add(message);
            }
        }

        public delegate void ChangeStatusDelegate(String oldMessage, String newMessage, int number);
        public void ChangeStatus(String oldMessage, String newMessage, int number)
        {
            if (this.listbox.InvokeRequired)
            {
                ChangeStatusDelegate d = ChangeStatus;
                this.listbox.Invoke(d, oldMessage, newMessage, number);
            }
            else
            {
                int i = this.listbox.FindString(oldMessage);
                if (i != -1)
                {
                    String[] items = new String[this.listbox.Items.Count];
                    this.listbox.Items.CopyTo(items, 0); //从items指定位置开始复制
                    items[i] = String.Format("{0} {1} 接收字节数：{2}KB--{3}B", oldMessage, newMessage,Math.Ceiling(number/1024.0f),number);
                    this.listbox.Items.Clear();
                    this.listbox.Items.AddRange(items);
                    this.listbox.SelectedIndex = i;

                }
            }
        }
        #endregion
    }
}
