using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.ScanModels
{
    public class LocalShaMapping
    {
        public int LocalShaMappingId { get; set; }
        public string FilePath { get; set; }
        public string Sha1 { get; set; }
        public long FileSize { get; set; }
        public string FileFolder { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
