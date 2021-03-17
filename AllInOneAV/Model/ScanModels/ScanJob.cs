using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.ScanModels
{
    public class ScanJob
    {
        public int ScanJobId { get; set; }
        public string ScanJobName { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime EndTime { get; set; }
        public int IsFinish { get; set; }
        public string ScanParameter { get; set; }
        public int TotalItem { get; set; }
        public int CurrentItemCount { get; set; }
        public string Website { get; set; }
    }
}
