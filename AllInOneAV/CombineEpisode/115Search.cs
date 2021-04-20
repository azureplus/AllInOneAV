using Model.Common;
using Service;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utils;

namespace CombineEpisode
{
    public partial class _115Search : Form
    {
        private static readonly string imageFolder = JavINIClass.IniReadValue("Jav", "imgFolder");
        private List<MissingCheckModel> missing;

        public _115Search()
        {
            InitializeComponent();
        }

        public _115Search(List<MissingCheckModel> input)
        {
            InitializeComponent();

            missing = input;
        }

        private void _115Search_Load(object sender, EventArgs e)
        {
            foreach (var mi in missing)
            {
                listView1.LargeImageList = imageList1;
                var pic = imageFolder + mi.Av.ID + mi.Av.Name + ".jpg";

                if (File.Exists(pic))
                {
                    imageList1.Images.Add(mi.Av.Name, Image.FromFile(pic));

                    ListViewItem lvi = new ListViewItem(mi.Av.ID + " " + mi.Av.Name);
                    lvi.ImageIndex = imageList1.Images.IndexOfKey(mi.Av.Name);
                    lvi.Tag = mi;

                    listView1.Items.Add(lvi);
                }
                else
                {
                    MessageBox.Show(mi.Av.ID + "" + mi.Av.Name);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                listView1.BeginUpdate();

                Parallel.ForEach(missing, new ParallelOptions { MaxDegreeOfParallelism = 5 }, mi =>
                {
                    var cookie = OneOneFiveService.Get115Cookie();

                    var res = OneOneFiveService.Get115SearchResult(cookie, mi.Av.ID);

                    if (res)
                    {
                        listView1.Items[missing.IndexOf(mi)].BackColor = Color.Green;
                    }
                });

                listView1.EndUpdate();
            }
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && listView1.SelectedItems.Count > 0)
            {
                MissingCheckModel mcm = (MissingCheckModel)listView1.SelectedItems[0].Tag;

                Clipboard.SetDataObject(mcm.Av.ID);

                listView1.SelectedItems[0].BackColor = Color.Blue;
            }

            if (e.Button == MouseButtons.Right && listView1.SelectedItems.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                foreach (ListViewItem lvi in listView1.SelectedItems)
                {
                    var seeds = ((MissingCheckModel)lvi.Tag);
                    if (seeds != null && seeds.Seeds.Count > 0)
                    {
                        foreach (var seed in seeds.Seeds)
                        {
                            sb.AppendLine(seed.MagUrl);
                        }
                    }
                }

                Clipboard.SetDataObject(sb.ToString());
                Message ms = new Message();
                ms.ShowDialog();
            }
        }
    }
}