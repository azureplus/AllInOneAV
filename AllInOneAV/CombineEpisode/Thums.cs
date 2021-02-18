using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CombineEpisode
{
    public partial class Thums : Form
    {
        public List<List<string>> pics = new List<List<string>>();

        public Thums()
        {
            InitializeComponent();
        }

        public Thums(List<List<string>> pics)
        {
            InitializeComponent();
            this.pics = pics;
        }

        private void Thums_Load(object sender, EventArgs e)
        {
            var row = pics.Count;
            var col = pics.FirstOrDefault().Count;

            TableLayoutPanel tlp = new TableLayoutPanel();
            tlp.RowCount = row;
            tlp.ColumnCount = col;
            tlp.Dock = DockStyle.Fill;

            for (int i = 0; i < pics.Count; i++)
            {
                for (int j = 0; j < pics.FirstOrDefault().Count; j++)
                {
                    PictureBox pb = new PictureBox();
                    pb.Width = 200;
                    pb.Height = 150;
                    pb.SizeMode = PictureBoxSizeMode.Zoom;
                    pb.Image = Image.FromFile(pics[i][j]);

                    tlp.Controls.Add(pb, j, i);
                }
            }

            this.Height = row * 150 + 30;
            this.Width = col * 200 + 20;

            this.panel1.Controls.Add(tlp);
        }
    }
}
