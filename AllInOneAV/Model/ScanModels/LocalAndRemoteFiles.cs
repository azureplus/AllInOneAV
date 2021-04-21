using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.ScanModels
{
    public class LocalAndRemoteFiles
    {
        public bool IsLocal { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FileNameWithoutExtension { get; set; }
        public long FileSize { get; set; }
        public string FileSizeStr { get; set; }
        public string FileLocation { get; set; }
        public string FileAvId { get; set; }
        public bool IsChinese { get; set; }
        public string PickCode { get; set; }
    }
}
