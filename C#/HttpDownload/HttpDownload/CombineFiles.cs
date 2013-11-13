using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HttpDownload
{
    public class CombineFiles
    {
        private bool isDownloadFinish;
        private ListBox listbox;
        private HttpDownload[] down;
        private String fileName;

        public CombineFiles() { }
        public CombineFiles(ListBox listbox, HttpDownload[] d, String fileName)
        {
            this.listbox = listbox;
            this.down = d;
            this.fileName = fileName;
        }


        /// <summary>
        /// 合并文件
        /// </summary>
        public void combine()
        {
            while (true)
            {
                isDownloadFinish = true;
                for (int i = 0; i < down.Length; i++)
                {
                    if (down[i].isFinish == false)
                    {
                        isDownloadFinish = false;
                        Thread.Sleep(100);
                        break;
                    }
                }
                if (isDownloadFinish == true)
                {
                    break;
                }
            }
            AddSatatus("下载完毕，开始合并临时文件！");
            FileStream targetFileStream = null;
            FileStream sourceFileStream = null;
            int readFile;
            byte[] bytes = new byte[8192];
            targetFileStream = new FileStream(fileName, FileMode.Create);
            for (int i = 0; i < down.Length; i++)
            {
                sourceFileStream = new FileStream(down[i].targetFileName, FileMode.Open);
                while (true)
                {
                    readFile = sourceFileStream.Read(bytes, 0, bytes.Length);
                    if (readFile > 0)
                    {
                        targetFileStream.Write(bytes, 0, readFile);
                    }
                    else
                    {
                        break;
                    }
                }
                sourceFileStream.Close();
            }
            targetFileStream.Close();

            for (int i = 0; i < down.Length; i++)
            {
                File.Delete(down[i].targetFileName);
            }

            AddSatatus("合并完毕！");
        }

        #region 维护listbox控件
        public delegate void AddStatusDelegate(String message);
        public void AddSatatus(String message)
        {
            if (this.listbox.InvokeRequired)
            {
                AddStatusDelegate d = AddSatatus;
                this.listbox.Invoke(d, message);
            }
            else
            {
                this.listbox.Items.Add(message);
                this.listbox.SelectedIndex=-1;
            }
        }
        #endregion
    }
}
