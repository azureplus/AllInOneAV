using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.OneOneFive
{
    public class OneOneFiveFileShaMapping
    {
        public int OneOneFiveFileShaMappingId { get; set; }
        public string FileName { get; set; }
        public string Sha { get; set; }
        public decimal FileSize { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
