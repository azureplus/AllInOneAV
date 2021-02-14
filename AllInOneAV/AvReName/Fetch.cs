using DataBaseManager.JavDataBaseHelper;
using Model.JavModels;
using Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utils;

namespace AvReName
{
    public partial class Fetch : Form
    {
        private static string imgFolder = JavINIClass.IniReadValue("Jav", "imgFolder");
        private AV av = null;

        public Fetch()
        {
            InitializeComponent();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            if (av != null)
            {
                if (!JavDataBaseManager.HasAv(av.URL))
                {
                    JavDataBaseManager.InsertAV(av);
                }

                string result = "";
                if (!File.Exists(imgFolder + av.ID + av.Name + ".jpg"))
                {
                    result = DownloadHelper.DownloadFile(av.PictureURL, imgFolder + av.ID + av.Name + ".jpg");
                }

                this.DialogResult = DialogResult.Yes;
                this.Close();
            }
        }

        private void fetchBtn_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(utlText.Text))
            {
                FetchInfo(utlText.Text);
            }

            if (av != null)
            {
                pictureBox1.Image = Image.FromStream(WebRequest.Create(av.PictureURL).GetResponse().GetResponseStream());
                nameLabel.Text = av.Name;
                directorLabel.Text = av.Director;
                publisherLabel.Text = av.Publisher;
                categoryLabel.Text = av.Category;
                dateLabel.Text = av.ReleaseDate.ToString("yyyy-MM-dd");
                companyLabel.Text = av.Company;
            }
            else
            {
                MessageBox.Show("没有找到该AV", "警告");
            }
        }

        private void FetchInfo(string url)
        {
            av = JavLibraryHelper.GetAvFromUrl(url);
        }

        private void Fetch_Load(object sender, EventArgs e)
        {

        }
    }
}
