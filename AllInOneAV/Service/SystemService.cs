using Model.WebModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class SystemService
    {
        public static SystemTreeVM GetSystemTreeVM(bool excludeFiles = false, bool exculdeCDrive = true)
        {
            SystemTreeVM ret = new SystemTreeVM
            {
                text = "System",
                selectable = true,
                icon = "fa fa-terminal"
            };

            List<SystemTreeVM> subs = new List<SystemTreeVM>();
            ret.nodes = subs;

            var drives = Environment.GetLogicalDrives();

            if (exculdeCDrive)
            {
                drives = drives.Skip(1).ToArray();
            }

            foreach (var d in drives)
            {
                SystemTreeVM sub = new SystemTreeVM
                {
                    text = d,
                    icon = "fa fa-folder",
                    selectable = true,
                    selectedIcon = "fa fa-folder-open"
                };

                ret.nodes.Add(sub);

                GetSystemTreeRecursively(sub, d);
            }

            return ret;
        }

        private static void GetSystemTreeRecursively(SystemTreeVM sub, string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                return;
            }

            List<SystemTreeVM> subs = new List<SystemTreeVM>();
            sub.nodes = subs;

            var folders = new DirectoryInfo(root).GetDirectories("*.*", SearchOption.TopDirectoryOnly).Where(x => (x.Attributes & FileAttributes.System) == 0);
            //var files = Directory.GetFiles(root);

            foreach (var fo in folders)
            {
                var tempNode = new SystemTreeVM()
                {
                    text = fo.Name,
                    selectable = true,
                    icon = "fa fa-folder",
                    selectedIcon = "fa fa-folder-open"
                };

                sub.nodes.Add(tempNode);

                //GetSystemTreeRecursively(tempNode, fo.FullName);
            }

            //foreach (var fi in files)
            //{
            //    SystemTreeVM treeNode = new SystemTreeVM
            //    {
            //        text = fi,
            //        selectable = true,
            //        icon = "fa fa-file",
            //    };

            //    sub.nodes.Add(treeNode);
            //}
        }
    }
}
