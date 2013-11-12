using System;
using System.Collections.Generic;
using System.Linq;
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


        public HttpDownload() { }
        public HttpDownload(ListBox listbox,int threadIndex) {
            this.listbox = listbox;
            this.threadindex = threadIndex;
        }
    }
}
