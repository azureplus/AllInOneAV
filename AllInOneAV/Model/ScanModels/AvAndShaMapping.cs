using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.ScanModels
{
    public class AvAndShaMapping
    {
        public int AvAndShaMappingId { get; set; }
        public string FilePath { get; set; }
        public string Sha1 { get; set; }
        public long FileSize { get; set; }
        public int IsExist { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
