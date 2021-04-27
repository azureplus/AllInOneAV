using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.OneOneFive
{
    public class DeleteFileBackup
    {
        public string Drive { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public string AvId { get; set; }
        public long FileSize { get; set; }
        public string FileSizeStr { get; set; }
        public string Sha { get; set; }
    }
}
