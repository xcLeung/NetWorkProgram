using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HttpDownload
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            textBox1.Text = String.Format("http://ww4.sinaimg.cn/bmiddle/641167cfjw1eahxnkppmqj20c80853yr.jpg");
            textBox2.Text = String.Format("downfile1.jpg");
        }

        public const int threadNumber = 2;
        HttpDownload[] d = new HttpDownload[threadNumber];
        Thread[] threads = new Thread[threadNumber];

        private void btnDownload_Click(object sender, EventArgs e)
        {
            HttpDownloadFile(textBox1.Text,textBox2.Text);
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="downloadUrl"></param>
        /// <param name="fileName"></param>
        private void HttpDownloadFile(String downloadUrl,String fileName)
        {
            if (isWebResourceUrlAvailable(downloadUrl) == false)
            {
                MessageBox.Show("指定的资源无效！");
                return;
            }
            else
            {
                listBoxThreadStatus.Items.Add(String.Format("同时接收线程数：{0}", threadNumber));
                HttpWebRequest request=null;
                long fileSize = 0;
                try
                {
                    request = (HttpWebRequest)HttpWebRequest.Create(downloadUrl);
                    request.Method = "Head";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    fileSize = response.ContentLength;
                    listBoxThreadStatus.Items.Add(String.Format("文件大小：{0}KB----{1}B",Math.Ceiling(fileSize/ 1024.0f),fileSize)); //B单位
                    response.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                int downloadFileSize = (int)(fileSize / threadNumber); //每个线程下载文件的大小
                listBoxThreadStatus.Items.Add(String.Format("每个线程下载文件的大小：{0}B", downloadFileSize));
                

                for (int i = 0; i < threadNumber; i++)
                {
                    d[i] = new HttpDownload(listBoxThreadStatus, i);
                    d[i].startPosition = i * downloadFileSize;
                    d[i].downloadPosition = d[i].startPosition;
                    if (i < threadNumber - 1)
                    {
                        d[i].fileSize = downloadFileSize;
                    }
                    else
                    {
                        d[i].fileSize = (int)(fileSize - downloadFileSize*(i-1)); //最后一个文件下载多点预留空间
                    }
                    listBoxThreadStatus.Items.Add(String.Format("线程{0}下载文件大小：{1}B", i, d[i].fileSize));
                    d[i].isFinish = false;
                    d[i].sourceUrl = textBox1.Text;
                    d[i].targetFileName = Path.GetFileNameWithoutExtension(fileName) + ".$$" + i;
                }
                
                for (int i = 0; i < threadNumber; i++)
                {
                    threads[i] = new Thread(d[i].Receive);
                    threads[i].Start();
                }

                CombineFiles c = new CombineFiles(listBoxThreadStatus, d, textBox2.Text);
                Thread combineThread = new Thread(c.combine);
                combineThread.Start();

            }
        }


        /// <summary>
        /// 继续下载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnContinue_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < threadNumber; i++)
            {
                threads[i] = new Thread(d[i].Receive);
                threads[i].Start();
            }
        }

        /// <summary>
        /// 暂停下载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPause_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < threadNumber; i++)
            {
                threads[i].Abort();
            }
        }

        /// <summary>
        /// 退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
        /// <summary>
        /// 判断资源是否存在
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static bool isWebResourceUrlAvailable(String url)
        {
            try
            {              
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "Head";
                request.Timeout = 2000;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                return (response.StatusCode==HttpStatusCode.OK);
            }
            catch(WebException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }    
    }
}
