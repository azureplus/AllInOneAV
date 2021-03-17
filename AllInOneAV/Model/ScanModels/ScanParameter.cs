using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.ScanModels
{
    public class ScanParameter
    {
        public List<string> StartingPage { get; set; }
        public bool IsAsc { get; set; }
        public int PageSize { get; set; }
        public int ScanJobId { get; set; }
        public string Website { get; set; }
    }
}
