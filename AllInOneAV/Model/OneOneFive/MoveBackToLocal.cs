using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.OneOneFive
{
    public class MoveBackToLocal
    {
        public string AvId { get; set; }
        public string AvName { get; set; }
        public string AvPic { get; set; }
        public long AvSize { get; set; }
        public string AvSizeStr { get; set; }
        public string Fid { get; set; }
    }

    public class DeleteLocal
    {
        public string AvId { get; set; }
        public string AvName { get; set; }
        public string AvPic { get; set; }
        public long AvSize { get; set; }
        public string AvSizeStr { get; set; }
        public string File { get; set; }
        public bool IsExist { get; set; }
    }
}
