using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.WebModel
{
    public class SystemTreeVM
    {
        public string text { get; set; }
        public string icon { get; set; }
        public string selectedIcon { get; set; }
        public string color {get;set;}
        public string backColor { get; set; }
        public string href { get; set; }
        public bool selectable { get; set; }
        public SystemTreeState state { get; set; }
        public List<string> tags { get; set; }
        public List<SystemTreeVM> nodes { get; set; }
    }

    public class SystemTreeState
    {
        public bool @checked { get; set; }
        public bool disabled { get; set; }
        public bool expanded { get; set; }
        public bool selected { get; set; }
    }
}
