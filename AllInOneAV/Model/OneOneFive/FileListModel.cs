using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.OneOneFive
{
    public class FileListModel
    {
        public int count { get; set; }
        public int cur { get; set; }
        public int offset { get; set; }
        public int page_size { get; set; }
        public List<FileItemModel> data { get; set; }
    }

    public class FileItemModel
    { 
        public string cid { get; set; }
        public string aid { get; set; }
        public string uid { get; set; }
        public string @class { get; set; }
        public string fid { get; set; }
        public string ico { get; set; }
        public string n { get; set; }
        public string pc { get; set; }
        public long play_long { get; set; }
        public long s { get; set; }
        public string sha { get; set; }
        public string t { get; set; }
        public DateTime time { 
            get {
                DateTime ret = new DateTime(1970, 1, 1);
                if (DateTime.TryParse(t, out ret))
                {
                    return ret;
                }
                else
                {
                    return ret.AddMilliseconds(double.Parse(t));
                }
            } 
        }
    }

    public enum OneOneFiveSearchType
    { 
        All = 0,
        Video = 4,
    }
}
