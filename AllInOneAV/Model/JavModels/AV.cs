using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.JavModels
{
    public class AV
    {
        public AV()
        {
            Name = "";
            Company = "";
            Publisher = "";
            Director = "";
            Category = "";
            Actress = "";
            PictureURL = "";
            URL = "";
        }
        public int AvId { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string Publisher { get; set; }
        public string Director{ get; set; }
        public string Category { get; set; }
        public string Actress { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int AvLength { get; set; }
        public DateTime CreateTime { get; set; }
        public string PictureURL { get; set; }
        public string URL { get; set; }
        public string AvIdAndName { get; set; }
    }
}
