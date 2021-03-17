using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using Model.Common;
using Model.JavModels;
using Model.ScanModels;
using Newtonsoft.Json;
using Service;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utils;

namespace CombineEpisode
{
    public partial class Main : Form

    {
        #region GlobalVar

        private static List<string> formats = JavINIClass.IniReadValue("Scan", "Format").Split(',').ToList();
        private static List<string> excludes = JavINIClass.IniReadValue("Scan", "Exclude").Split(',').ToList();
        private static readonly string imageFolder = JavINIClass.IniReadValue("Jav", "imgFolder");
        private static readonly string ffmpeg = "c:\\setting\\ffmpeg.exe";
        private static readonly string combineFilePath = "c:\\setting\\combinefile\\";
        private static readonly string thumsFolder = "c:\\setting\\thumbs\\";
        private static List<Model.ScanModels.Match> matchesAV = new List<Model.ScanModels.Match>();
        private static List<FileInfo> recentFi = new List<FileInfo>();
        private static List<RefreshModel> refreshModel = new List<RefreshModel>();
        private static List<MissingCheckModel> missingCheckForFavi = new List<MissingCheckModel>();
        private static List<ScanResult> scanResult = new List<ScanResult>();
        private static List<ScanResult> toBePlay = new List<ScanResult>();
        private static int lastPlayPage = 1;
        private static int lastPlaySize = 200;
        private Process p;
        private bool OkToStart = true;
        private string[] ImportedFiles = null;
        private Font font = new Font("微软雅黑", 10);

        public static List<string> FaviUrls = new List<string>();
        public delegate void ProcessPb(ProgressBar pb, int value);
        public delegate void ProcessListView(ListView lv, ListViewItem lvi, int column, string content, List<SeedMagnetSearchModel> model);
        public delegate void ProcessListViewItem(ListView lv, ListViewItem lvi);

        #endregion

        #region 行为

        public Main()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            Reset();
            ImportedFiles = ImportFile();

            if (ImportedFiles != null && ImportedFiles.Length > 0)
            {
                ShowList();

                txtSave.Text = new FileInfo(ImportedFiles[0]).DirectoryName;
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            Reset();
            Init();
        }

        private void Main_DragDrop(object sender, DragEventArgs e)
        {
            ImportedFiles = null;

            Array file = (Array)e.Data.GetData(DataFormats.FileDrop);

            foreach (object I in file)
            {
                string str = I.ToString();

                FileInfo info = new System.IO.FileInfo(str);
            }
        }

        private void Main_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SetSave();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            var basicCheck = Check();

            if (!string.IsNullOrEmpty(basicCheck))
            {
                MessageBox.Show(basicCheck);
                return;
            }

            FileInfo first = new FileInfo(listView1.Items[0].Tag.ToString());
            var targetFile = first.Name.Replace(first.Extension, "") + ".mp4";
            var fileName = txtSave.Text + "\\" + targetFile;

            if (!string.IsNullOrEmpty(txtHopeName.Text))
            {
                fileName = txtSave.Text + "\\" + txtHopeName.Text + ".mp4";
            }

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            var combineFile = GenerateCombineFile();

            StartCombine(combineFile, fileName);
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (p != null)
            {
                p.Kill();
                p.Close();
                p.Dispose();
            }
        }

        private void btnLook_Click(object sender, EventArgs e)
        {
            RestAutoCombineFile();

            GetFilesToGenerateCombineFile();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            InitAutoCombineFile();

            GenerateAutoCombineFile();
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            treeView2.Nodes.Clear();
            Preview();
        }

        private void btnAuto_Click(object sender, EventArgs e)
        {
            AutoCombie();
        }

        private void btnAutoSave_Click(object sender, EventArgs e)
        {
            AutoSave();
        }

        private void btnConvertImport_Click(object sender, EventArgs e)
        {
            GetFilesForAutoConvert();
        }

        private void btnConvertStart_Click(object sender, EventArgs e)
        {
            GetFilesForAutoConvertSave();
        }

        private async void btStartConvert_Click(object sender, EventArgs e)
        {
            var d = await Convert();

            MessageBox.Show(d.TotalSeconds + " seconds");
        }

        private void btnCheckISO_Click(object sender, EventArgs e)
        {
            treeView3.Nodes.Clear();
            ImportISO();
        }

        private void treeView3_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ClickTree(e.Node);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DeleteMultiple();
            treeView1.Nodes.Clear();
        }

        private void btnScanRedundant_Click(object sender, EventArgs e)
        {
            ScanRedundantClick();
        }

        private void treeView4_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.ByMouse && e.Node.Parent == null)
            {
                foreach (TreeNode n in e.Node.Nodes)
                {
                    n.Checked = false;
                }
            }
        }

        private void btnScanDelete_Click(object sender, EventArgs e)
        {
            ScanDeleteClick();
        }

        private void btnScanClear_Click(object sender, EventArgs e)
        {
            ClearCheck();
        }

        private void btnScanUnmathced_Click(object sender, EventArgs e)
        {
            treeView5.Nodes.Clear();
            ScanUnmatchedClick();
        }

        private void btnScanUnmatchedSelect_Click(object sender, EventArgs e)
        {
            SelectAllUnmatchedSubTreeNode();
        }

        private void treeView5_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            if (e.Button == MouseButtons.Right)
            {
                if (e.Node.Parent == null)
                {
                    foreach (TreeNode tn in e.Node.Nodes)
                    {
                        sb.AppendLine((string)tn.Tag);
                    }

                    e.Node.Collapse();
                }
                else
                {
                    sb.AppendLine((string)e.Node.Tag);

                    e.Node.Parent.Collapse();
                }

                Clipboard.SetDataObject(sb.ToString());

                Message ms = new Message(); ;
                ms.ShowDialog();
            }
        }

        private void btnRemoveFolderScan_Click(object sender, EventArgs e)
        {
            InitRemoveFolder();
            RemovceFolderScanClick();
        }

        private void btnRemoveFolderStart_Click(object sender, EventArgs e)
        {
            RemoveFolderStartClick();
        }

        private void btnRenameStart_Click(object sender, EventArgs e)
        {
            RenameStartClick();
        }

        private void btnRenameScan_Click(object sender, EventArgs e)
        {
            RenameScanClick();
        }

        private void btnSearchSeed_Click(object sender, EventArgs e)
        {
            listView3.Items.Clear();
            SearchSeedClick();
        }

        private void listView3_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && listView3.SelectedItems.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                foreach (ListViewItem lvi in listView3.SelectedItems)
                {
                    sb.AppendLine((string)lvi.Tag);
                }

                Clipboard.SetDataObject(sb.ToString());

                var ret = OneOneFiveService.Add115MagTask("", sb.ToString(), "340200422", "");

                if (ret.Item1)
                {
                    Message ms = new Message();
                    ms.ShowDialog();
                }
                else
                {
                    MessageBox.Show(ret.Item2);
                }
            }
        }

        private void rbCate_CheckedChanged(object sender, EventArgs e)
        {
            ShowJavScanPreset("Category");
        }

        private void rbActress_CheckedChanged(object sender, EventArgs e)
        {
            ShowJavScanPreset("Actress");
        }

        private void rbCom_CheckedChanged(object sender, EventArgs e)
        {
            ShowJavScanPreset("Company");
        }

        private void rbDir_CheckedChanged(object sender, EventArgs e)
        {
            ShowJavScanPreset("Director");
        }

        private void btnJavScanDaily_Click(object sender, EventArgs e)
        {
            JavScanAsync("daily");
        }

        private void btnJavScan_Click(object sender, EventArgs e)
        {
            JavScanAsync("certain");
        }

        private void richTextBox3_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            richTextBox3.SelectionStart = richTextBox3.Text.Length;
            richTextBox3.ScrollToCaret();
        }

        private void btnManualRename_Click(object sender, EventArgs e)
        {
            var exeFile = "G:\\AllInOneAV\\AvReName\\bin\\Debug\\AvRename.exe";
            if (File.Exists(exeFile))
            {
                Process.Start(exeFile);
            }
        }

        private void btnMatch_Click(object sender, EventArgs e)
        {
            StartScanAndMatch();
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtFind.Text))
            {
                DoFindMovie();
            }
        }

        private void txtFind_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!string.IsNullOrEmpty(txtFind.Text))
                {
                    DoFindMovie();
                }
            }
        }

        private void lvwFind_Validated(object sender, EventArgs e)
        {
            if (lvwFind.FocusedItem != null)
            {
                lvwFind.FocusedItem.BackColor = SystemColors.Highlight;
                lvwFind.FocusedItem.ForeColor = Color.White;
            }
        }

        private void lvwFind_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = lvwFind.HitTest(e.X, e.Y);
            if (info.Item != null)
            {
                Process.Start(@"" + info.Item.SubItems[2].Text);
            }
        }

        private void lvwFind_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var current = lvwRecnet.GetItemAt(e.X, e.Y);

                Delete(current);
            }
        }

        private void lvwFind_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            e.Item.ForeColor = Color.Black;
            e.Item.BackColor = SystemColors.Window;

            if (lvwFind.FocusedItem != null)
            {
                lvwFind.FocusedItem.Selected = true;
            }
        }

        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            //退格删除播放记录
            if (e.KeyCode == Keys.Back && tabControl1.SelectedTab.Name == "tabPage14")
            {
                if (lvwFind.Focused)
                {
                    if (lvwFind.SelectedItems != null && lvwFind.SelectedItems.Count > 0)
                    {
                        foreach (ListViewItem item in lvwFind.SelectedItems)
                        {
                            item.BackColor = Color.Transparent;
                            ScanDataBaseManager.RemoveViewHistory(FileUtility.ReplaceInvalidChar(item.Tag + ""));
                        }
                    }
                }
                else 
                {
                    if (lvwRecnet.SelectedItems != null && lvwRecnet.SelectedItems.Count > 0)
                    {
                        foreach (ListViewItem item in lvwRecnet.SelectedItems)
                        {
                            item.BackColor = Color.Transparent;
                            ScanDataBaseManager.RemoveViewHistory(FileUtility.ReplaceInvalidChar(item.Tag + ""));
                        }
                    }
                }
            }

            //空格播放事件
            if (e.KeyCode == Keys.Space && tabControl1.SelectedTab.Name == "tabPage14")
            {
                //找视频选中
                if (lvwFind.Focused)
                {
                    if (lvwFind.SelectedItems != null && lvwFind.SelectedItems.Count > 0)
                    {
                        if (lvwFind.SelectedItems.Count == 1)
                        {
                            Play(lvwFind.SelectedItems[0]);
                        }
                        else
                        {
                            List<string> list = new List<string>();

                            foreach (ListViewItem item in lvwFind.SelectedItems)
                            {
                                list.Add(item.Tag.ToString());

                                item.BackColor = Color.Green;
                                ScanDataBaseManager.InsertViewHistory(FileUtility.ReplaceInvalidChar(item.Tag + ""));
                            }

                            PlayPlist(PlayerHelper.GeneratePotPlayerPlayList(list));
                        }
                    }
                }
                //播放最近选中
                else
                {
                    if (lvwRecnet.SelectedItems != null && lvwRecnet.SelectedItems.Count > 0)
                    {
                        if (lvwRecnet.SelectedItems.Count == 1)
                        {
                            Play(lvwRecnet.SelectedItems[0]);
                        }
                        else
                        {
                            List<string> list = new List<string>();

                            foreach (ListViewItem item in lvwRecnet.SelectedItems)
                            {
                                list.Add(item.Tag.ToString());

                                item.BackColor = Color.Green;
                                ScanDataBaseManager.InsertViewHistory(FileUtility.ReplaceInvalidChar(item.Tag + ""));
                            }

                            PlayPlist(PlayerHelper.GeneratePotPlayerPlayList(list));
                        }
                    }
                }
            }

            if (e.KeyCode == Keys.Space && tabControl1.SelectedTab.Name == "tabPage16")
            {
                if (lvPlay.SelectedItems != null && lvPlay.SelectedItems.Count > 0)
                {
                    if (lvPlay.SelectedItems.Count == 1)
                    {
                        Play(lvPlay.SelectedItems[0]);
                    }
                    else
                    {
                        List<string> list = new List<string>();

                        foreach (ListViewItem item in lvPlay.SelectedItems)
                        {
                            list.Add(item.Tag.ToString());

                            item.BackColor = Color.Green;
                            ScanDataBaseManager.InsertViewHistory(FileUtility.ReplaceInvalidChar(item.Tag + ""));
                        }

                        PlayPlist(PlayerHelper.GeneratePotPlayerPlayList(list));
                    }
                }
            }
        }

        private void btnRecent_Click(object sender, EventArgs e)
        {
            ListRecentItems();
        }

        private void txtRecent_MouseClick(object sender, MouseEventArgs e)
        {
            ChooseRecentFolder();
        }

        private void rbDateA_CheckedChanged(object sender, EventArgs e)
        {
            WhenClickRb();
            ShowContent();
        }

        private void rbDateD_CheckedChanged(object sender, EventArgs e)
        {
            WhenClickRb();
            ShowContent();
        }

        private void rbSizeA_CheckedChanged(object sender, EventArgs e)
        {
            WhenClickRb();
            ShowContent();
        }

        private void rbSizeD_CheckedChanged(object sender, EventArgs e)
        {
            WhenClickRb();
            ShowContent();
        }

        private void lvwRecnet_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && lvwRecnet.SelectedItems.Count == 1)
            {
                var current = lvwRecnet.GetItemAt(e.X, e.Y);

                Delete(current);

                ListRecentItems();
            }

            if (e.Button == MouseButtons.Right && lvwRecnet.SelectedItems.Count > 1)
            {
                List<FileInfo> deleteFiles = new List<FileInfo>();

                foreach (ListViewItem current in lvwRecnet.SelectedItems)
                {
                    deleteFiles.Add(new FileInfo((string)current.Tag));
                }

                var res = MessageBox.Show(string.Format("是否要删除 {0} 个文件，总大小 {1}", lvwRecnet.SelectedItems.Count, FileSize.GetAutoSizeString(deleteFiles.Sum(x => x.Length), 1)), "警告", MessageBoxButtons.YesNo);

                if (res == DialogResult.Yes)
                {
                    foreach (ListViewItem current in lvwRecnet.SelectedItems)
                    {
                        File.Delete(current.Tag + "");

                        listView1.Items.Remove(current);
                    }

                    ListRecentItems();
                }
            }
        }

        private void lvwRecnet_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var current = lvwRecnet.GetItemAt(e.X, e.Y);

            Play(current);
        }

        private void btnReport_Click(object sender, EventArgs e)
        {
            StartReport();
        }

        private void btMissing_Click(object sender, EventArgs e)
        {
            lvMissing.Items.Clear();
            pbMissing.Value = 0;

            MissingSearch();
        }

        private void lvwMissing_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && lvMissing.SelectedItems.Count > 0)
            {
                List<SeedMagnetSearchModel> list = (List<SeedMagnetSearchModel>)lvMissing.SelectedItems[0].Tag;

                if (list != null && list.Count > 0)
                {
                    SeedList sl = new SeedList(list, "", 0);
                    sl.ShowDialog();
                }
            }

            if (e.Button == MouseButtons.Left && lvMissing.SelectedItems.Count > 0)
            {
                Clipboard.SetDataObject(lvMissing.SelectedItems[0].SubItems[1].Text);
            }
        }

        private void treeView4_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TreeNode tn = treeView4.SelectedNode;
            Process.Start(@"" + ((FileInfo)tn.Tag).FullName);
        }

        private void btnMissing115_Click(object sender, EventArgs e)
        {
            List<MissingCheckModel> list = new List<MissingCheckModel>();

            foreach (ListViewItem item in lvMissing.Items)
            {
                MissingCheckModel temp = (MissingCheckModel)item.Tag;

                if (temp.IsMatch == false && temp.Seeds != null && temp.Seeds.Count > 0)
                {
                    list.Add(temp);
                }
            }

            _115Search _115 = new _115Search(list);

            _115.ShowDialog();
        }

        private void btnDaily_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtDailyPage.Text))
            {
                DoDailyRefesh(txtDailyPage.Text);
            }
        }

        private void lwDaily_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && lwDaily.SelectedItems.Count > 0)
            {
                List<SeedMagnetSearchModel> list = (List<SeedMagnetSearchModel>)lwDaily.SelectedItems[0].Tag;

                if (list != null && list.Count > 0)
                {
                    SeedList sl = new SeedList(list, "", 0);
                    sl.ShowDialog();
                }
            }

            if (e.Button == MouseButtons.Left && lwDaily.SelectedItems.Count > 0)
            {
                Clipboard.SetDataObject(lwDaily.SelectedItems[0].SubItems[1].Text);
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            int pageSize = int.Parse(txtPlayPageSize.Text);
            BtnPlayClick(1, pageSize);
        }

        private void playBack_Click(object sender, EventArgs e)
        {
            BtnPlayClick(lastPlayPage, lastPlaySize, true);
        }

        private void btnPlayRefresh_Click(object sender, EventArgs e)
        {
            RefreshPlayUi();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage16)
            {
                RefreshPlayUi();
            }
        }

        private void lvPlay_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && lvPlay.SelectedItems.Count > 0)
            {
                Play(lvPlay.SelectedItems[0]);
            }
        }

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var rs = MessageBox.Show("确定要删除 " + ((FileInfo)treeView1.SelectedNode.Tag).FullName + " ?", "警告", MessageBoxButtons.YesNo);

            if (rs == DialogResult.Yes)
            {
                ((FileInfo)treeView1.SelectedNode.Tag).Delete();

                treeView1.Nodes.Clear();
                pb2.Value = 0;

                ShowTree(txtLook.Text);
            }
        }

        private void 重命名ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReName rn = new ReName();

            rn.OriFile = ((FileInfo)treeView1.SelectedNode.Tag).FullName;

            var rs = rn.ShowDialog();

            if (rs == DialogResult.Yes)
            {
                treeView1.Nodes.Clear();
                pb2.Value = 0;

                ShowTree(txtLook.Text);
            }
        }

        private void treeView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && treeView1.SelectedNode != null)
            {
                Point p = new Point();
                p.X = e.Location.X + this.Location.X + 5;
                p.Y = e.Location.Y + this.Location.Y + 100;

                contextMenuStrip3.Show(p);
            }
        }

        private void lwDaily_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && lwDaily.SelectedItems.Count > 0)
            {
                if (lwDaily.SelectedItems[0].BackColor == Color.Blue || lwDaily.SelectedItems[0].BackColor == Color.Yellow)
                {
                    Process.Start(@"" + lwDaily.SelectedItems[0].SubItems[2].Text);
                }
            }
        }

        private void btnDailyGenerateFav_Click_1(object sender, EventArgs e)
        {
            JavFaviScan();
        }

        private void btnDailyFav_Click_1(object sender, EventArgs e)
        {
            FaviUrls = new List<string>();

            FaviList fl = new FaviList();

            var rs = fl.ShowDialog();

            if (rs == DialogResult.Yes)
            {
                rbMissingFavi.Checked = true;

                txtMissing.Text = string.Join(",", FaviUrls);
            }
        }

        private void lvMissing_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && lvMissing.SelectedItems.Count > 0)
            {
                if (lvMissing.SelectedItems[0].BackColor == Color.Blue || lvMissing.SelectedItems[0].BackColor == Color.Yellow)
                {
                    Process.Start(@"" + lvMissing.SelectedItems[0].SubItems[2].Text);
                }
            }
        }

        private void btnScanBatch_Click(object sender, EventArgs e)
        {
            JavBatchScanAsync("certain");
        }

        private void btnPlayPre_Click(object sender, EventArgs e)
        {
            if (toBePlay != null && toBePlay.Count >= 0)
            {
                int pageSize = int.Parse(txtPlayPageSize.Text);
                var pageArray = lbPlayPage.Text.Split('/');

                int current = 1;
                int total = 1;

                int.TryParse(pageArray[0], out current);
                int.TryParse(pageArray[1], out total);

                if (current - 1 >= 1)
                {
                    current--;
                    BtnPlayClick(current, pageSize);
                    lbPlayPage.Text = current + " / " + total;
                }
            }
        }

        private void btnPlayNext_Click(object sender, EventArgs e)
        {
            int pageSize = int.Parse(txtPlayPageSize.Text);
            var pageArray = lbPlayPage.Text.Split('/');

            int current = 1;
            int total = 1;

            int.TryParse(pageArray[0], out current);
            int.TryParse(pageArray[1], out total);

            if (current + 1 <= total)
            {
                current++;
                BtnPlayClick(current, pageSize);
                lbPlayPage.Text = current + " / " + total;
            }
        }

        private void btnRemoveOpen_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtRemoveFolderTxt.Text))
            {
                System.Diagnostics.Process.Start(txtRemoveFolderTxt.Text);
            }
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)    //最小化到系统托盘
            {
                notifyIcon1.Visible = true;    //显示托盘图标
                this.Hide();    //隐藏窗口
            }
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            //注意判断关闭事件Reason来源于窗体按钮，否则用菜单退出时无法退出!
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;    //取消"关闭窗口"事件
                this.WindowState = FormWindowState.Minimized;    //使关闭时窗口向右下角缩小的效果
                notifyIcon1.Visible = true;
                this.Hide();
                return;
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                notifyIcon1.Visible = true;
                this.Show();
                WindowState = FormWindowState.Normal;
                this.Focus();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            notifyIcon1.Visible = true;
            this.Show();
            WindowState = FormWindowState.Normal;
            this.Focus();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你确定要退出？", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
            {

                this.notifyIcon1.Visible = false;
                this.Close();
                this.Dispose();
                System.Environment.Exit(System.Environment.ExitCode);

            }
        }

        private void lvPlay_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && lvPlay.SelectedItems.Count > 0)
            {
                contextMenuStrip4 = new ContextMenuStrip();
                ToolStripItem searchItem = new ToolStripMenuItem("查询");
                ToolStripItem playItem = new ToolStripMenuItem("播放");
                ToolStripItem seedItem = new ToolStripMenuItem("搜种子");

                contextMenuStrip4.Items.Add(searchItem);
                contextMenuStrip4.Items.Add(playItem);
                contextMenuStrip4.Items.Add(seedItem);

                playItem.Click += new EventHandler(playItemClick);
                seedItem.Click += new EventHandler(seedItemClick);

                var av = ScanDataBaseManager.GetMatchedAv(int.Parse(lvPlay.SelectedItems[0].Text.Split(' ').LastOrDefault()));

                if (av != null)
                {
                    var actress = av.Actress.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();
                    var category = av.Category.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();
                    var prefix = av.ID.Split('-')[0];

                    if (actress != null && actress.Count > 0)
                    {
                        ToolStripItem actItem = new ToolStripMenuItem("演员");
                        ((ToolStripDropDownItem)(contextMenuStrip4.Items[0])).DropDownItems.Add(actItem);

                        foreach (var act in actress)
                        {
                            ToolStripItem temp = new ToolStripMenuItem(act);

                            ToolStripItem search = new ToolStripMenuItem("搜索");
                            search.Tag = "actress-" + act;
                            search.Click += new EventHandler(searchItemClick);
                            ToolStripItem favi = new ToolStripMenuItem("添加收藏");
                            favi.Tag = "actress-" + act;
                            favi.Click += new EventHandler(searchItemClick);
                            ToolStripItem top = new ToolStripMenuItem("扫描前3");
                            top.Tag = "actress-" + act;
                            top.Click += new EventHandler(searchItemClick);
                            ToolStripItem all = new ToolStripMenuItem("扫描全部");
                            all.Tag = "actress-" + act;
                            all.Click += new EventHandler(searchItemClick);

                            ((ToolStripDropDownItem)temp).DropDownItems.Add(search);
                            ((ToolStripDropDownItem)temp).DropDownItems.Add(favi);
                            ((ToolStripDropDownItem)temp).DropDownItems.Add(top);
                            ((ToolStripDropDownItem)temp).DropDownItems.Add(all);

                            ((ToolStripDropDownItem)actItem).DropDownItems.Add(temp);
                        }
                    }

                    if (category != null && category.Count > 0)
                    {
                        ToolStripItem cateItem = new ToolStripMenuItem("类型");
                        ((ToolStripDropDownItem)(contextMenuStrip4.Items[0])).DropDownItems.Add(cateItem);

                        foreach (var cate in category)
                        {
                            ToolStripItem temp = new ToolStripMenuItem(cate);

                            ToolStripItem search = new ToolStripMenuItem("搜索");
                            search.Tag = "category-" + cate;
                            search.Click += new EventHandler(searchItemClick);
                            ToolStripItem favi = new ToolStripMenuItem("添加收藏");
                            favi.Tag = "category-" + cate;
                            favi.Click += new EventHandler(searchItemClick);
                            ToolStripItem top = new ToolStripMenuItem("扫描前3");
                            top.Tag = "category-" + cate;
                            top.Click += new EventHandler(searchItemClick);
                            ToolStripItem all = new ToolStripMenuItem("扫描全部");
                            all.Tag = "category-" + cate;
                            all.Click += new EventHandler(searchItemClick);

                            ((ToolStripDropDownItem)temp).DropDownItems.Add(search);
                            ((ToolStripDropDownItem)temp).DropDownItems.Add(favi);
                            ((ToolStripDropDownItem)temp).DropDownItems.Add(top);
                            ((ToolStripDropDownItem)temp).DropDownItems.Add(all);

                            ((ToolStripDropDownItem)cateItem).DropDownItems.Add(temp);
                        }
                    }

                    if (!string.IsNullOrEmpty(prefix))
                    {
                        ToolStripItem prefixItem = new ToolStripMenuItem("前缀");
                        ((ToolStripDropDownItem)(contextMenuStrip4.Items[0])).DropDownItems.Add(prefixItem);

                        ToolStripItem temp = new ToolStripMenuItem(prefix);

                        ToolStripItem search = new ToolStripMenuItem("搜索");
                        search.Tag = "prefix-" + prefix;
                        search.Click += new EventHandler(searchItemClick);
                        ToolStripItem favi = new ToolStripMenuItem("添加收藏");
                        favi.Tag = "prefix-" + prefix;
                        favi.Click += new EventHandler(searchItemClick);
                        ToolStripItem top = new ToolStripMenuItem("扫描前3");
                        top.Tag = "prefix-" + prefix;
                        top.Click += new EventHandler(searchItemClick);
                        ToolStripItem all = new ToolStripMenuItem("扫描全部");
                        all.Tag = "prefix-" + prefix;
                        all.Click += new EventHandler(searchItemClick);

                        ((ToolStripDropDownItem)temp).DropDownItems.Add(search);
                        ((ToolStripDropDownItem)temp).DropDownItems.Add(favi);
                        ((ToolStripDropDownItem)temp).DropDownItems.Add(top);
                        ((ToolStripDropDownItem)temp).DropDownItems.Add(all);

                        ((ToolStripDropDownItem)prefixItem).DropDownItems.Add(temp);
                    }

                    Point p = new Point();
                    p.X = e.Location.X + this.Location.X + 5;
                    p.Y = e.Location.Y + this.Location.Y + 100;

                    contextMenuStrip4.Show(p);
                }
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            txtPlaySkipClick(sender, e);
        }

        private void seedItemClick(object sender, EventArgs e)
        {
            var content = lvPlay.SelectedItems[0].Text.Split(' ')[1];
            var list = MagService.SearchSukebei(content);

            if (list != null && list.Count > 0)
            {
                SeedList sl = new SeedList(list, "", 0);
                sl.ShowDialog();
            }
            else
            {
                MessageBox.Show("没有搜到");
            }
        }

        private void playItemClick(object sender, EventArgs e)
        {
            Play(lvPlay.SelectedItems[0]);
        }

        private void searchItemClick(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;

            var type = item.Tag.ToString().Split('-')[0];
            var content = item.Tag.ToString().Split('-')[1];

            if (item.Text == "搜索")
            {
                if (type == "actress")
                {
                    cbPlayActress.Text = item.Text;
                }

                if (type == "category")
                {
                    cbPlayCategory.Text = item.Text;
                }

                if (type == "prefix")
                {
                    cbPlayPrefix.Text = item.Text;
                }

                int pageSize = int.Parse(txtPlayPageSize.Text);

                lastPlaySize = pageSize;

                if (!string.IsNullOrEmpty(lbPlayPage.Text))
                {
                    lastPlayPage = int.Parse(lbPlayPage.Text.Split('/')[0].Trim());
                }

                BtnPlayClick(1, pageSize);
            }
            else
            {
                var url = "";

                if (type == "actress")
                {
                    url = JavDataBaseManager.GetActress().FirstOrDefault(x => x.Name == content).URL;

                    if (!string.IsNullOrEmpty(url))
                    {
                        if (item.Text == "扫描前3")
                        {

                            ScanDataBaseManager.InsertScanJob($"APP扫描{content}前3页", JsonConvert.SerializeObject(new ScanParameter()
                            {
                                IsAsc = true,
                                PageSize = 3,
                                StartingPage = new List<string> { url }
                            }), "jav");
                        }

                        if (item.Text == "扫描全部")
                        {
                            ScanDataBaseManager.InsertScanJob($"APP扫描{content}全部", JsonConvert.SerializeObject(new ScanParameter()
                            {
                                IsAsc = true,
                                PageSize = -1,
                                StartingPage = new List<string> { url }
                            }), "jav");
                        }
                    }
                }
                else if (type == "category")
                {
                    url = JavDataBaseManager.GetCategories().FirstOrDefault(x => x.Name == content).Url;

                    if (!string.IsNullOrEmpty(url))
                    { 
                        if (item.Text == "扫描前3")
                        {
                            ScanDataBaseManager.InsertScanJob($"APP扫描{content}前3页", JsonConvert.SerializeObject(new ScanParameter()
                            {
                                IsAsc = true,
                                PageSize = 3,
                                StartingPage = new List<string> { url }
                            }), "jav");
                        }

                        if (item.Text == "扫描全部")
                        {
                            ScanDataBaseManager.InsertScanJob($"APP扫描{content}全部", JsonConvert.SerializeObject(new ScanParameter()
                            {
                                IsAsc = true,
                                PageSize = -1,
                                StartingPage = new List<string> { url }
                            }), "jav");
                        }
                    }
                }
                else if(type == "prefix")
                {
                    url = $"http://www.javlibrary.com/cn/vl_searchbyid.php?&keyword={content}";

                    if (!string.IsNullOrEmpty(url))
                    {
                        if (item.Text == "扫描前3")
                        {
                            ScanDataBaseManager.InsertScanJob($"APP扫描{content}前3页", JsonConvert.SerializeObject(new ScanParameter()
                            {
                                IsAsc = false,
                                PageSize = 3,
                                StartingPage = new List<string> { url }
                            }), "jav");
                        }

                        if (item.Text == "扫描全部")
                        {
                            ScanDataBaseManager.InsertScanJob($"APP扫描{content}全部", JsonConvert.SerializeObject(new ScanParameter()
                            {
                                IsAsc = false,
                                PageSize = -1,
                                StartingPage = new List<string> { url }
                            }), "jav");
                        }
                    }
                }

                if (item.Text == "添加收藏")
                {
                    ScanDataBaseManager.InsertFaviScan(new FaviScan()
                    {
                        Category = type,
                        Name = content,
                        Url = url
                    });
                }

            }
        }

        private void lvPlay_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        private void richTextBox2_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
        }

        private void rtbMatch_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            rtbMatch.SelectionStart = rtbMatch.Text.Length;
            rtbMatch.ScrollToCaret();
        }

        private void contextMenuStrip3_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        //TODO
        private void 看截图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = treeView1.SelectedNode;
            if (node.Level == 0)
            {
                List<List<string>> pics = new List<List<string>>();

                foreach (TreeNode tn in node.Nodes)
                {
                    pics.Add(Directory.GetFiles(thumsFolder + ((FileInfo)tn.Tag).Name).ToList());
                }

                Thums th = new Thums(pics);
                th.ShowDialog();
            }
            else
            {
                FileInfo fi = (FileInfo)(node).Tag;

                if (Directory.Exists(thumsFolder + fi.Name))
                {
                    var pics = Directory.GetFiles(thumsFolder + fi.Name);

                    if (pics != null && pics.Length > 0)
                    {
                        var list = new List<List<string>>();
                        list.Add(pics.ToList());

                        Thums th = new Thums(list);
                        th.ShowDialog();
                    }
                }
            }
        }

        private void btnRecentFIndMove_Click(object sender, EventArgs e)
        {
            FindMove();
        }
        #endregion

        #region 方法

        private void Reset()
        {
            txtSave.Text = "";
            cbDelete.Checked = false;
            cbMove.Checked = false;
            pb.Value = 0;
            pb.Minimum = 0;
            pb.Maximum = 100;
            listView1.Items.Clear();
            ImportedFiles = null;
        }

        private void Init()
        {
            if (!Directory.Exists(combineFilePath))
            {
                Directory.CreateDirectory(combineFilePath);
            }
        }

        private void SetSave()
        {
            this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;

            var rs = folderBrowserDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                txtSave.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private string[] ImportFile()
        {
            openFileDialog1.Multiselect = true;
            var rs = openFileDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                var files = openFileDialog1.FileNames;

                return files;
            }
            else
            {
                return null;
            }
        }

        private void ShowList()
        {
            listView1.BeginUpdate();
            foreach (var file in ImportedFiles)
            {
                var fi = new FileInfo(file);

                if (formats.Contains(fi.Extension.ToLowerInvariant()))
                {
                    listView1.Items.Add(GenerateListViewItem(fi));
                }
            }
            listView1.EndUpdate();
        }

        private ListViewItem GenerateListViewItem(FileInfo fi)
        {
            ListViewItem lvi = new ListViewItem(fi.Name);

            string length = FileUtility.GetDuration(fi.FullName, ffmpeg);
            string size = FileSize.GetAutoSizeString(fi.Length, 1);
            lvi.SubItems.Add(length);
            lvi.SubItems.Add(size);
            lvi.Tag = fi.FullName;

            return lvi;
        }

        private string Check()
        {
            if (listView1.Items.Count <= 0)
            {
                return "没有需要合并的视频文件";
            }

            if (string.IsNullOrEmpty(txtSave.Text))
            {
                return "没有选择目的地";
            }

            return "";
        }

        private string GenerateCombineFile()
        {
            var fileName = combineFilePath + "combine" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".txt";

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            File.Create(fileName).Close();

            StringBuilder sb = new StringBuilder();
            StreamWriter sw = new StreamWriter(fileName);

            foreach (ListViewItem lvi in listView1.Items)
            {
                sb.AppendLine(string.Format("file '{0}'", lvi.Tag.ToString()));
            }

            sw.WriteLine(sb.ToString());
            sw.Close();

            return fileName;
        }

        private int CalculateTotalTime()
        {
            int ret = 0;

            foreach (ListViewItem lvi in listView1.Items)
            {
                ret += FileUtility.ConvertDurationToInt(lvi.SubItems[1].Text);
            }

            return ret;
        }

        private void Output(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                if (output.Data.StartsWith("frame"))
                {
                    var time = output.Data.Substring(output.Data.IndexOf("time="), 16).Replace("time=", "");
                    var process = FileUtility.ConvertDurationToInt(time);

                    JDuBar(pb, process);
                }
                else if (output.Data.StartsWith("video:"))
                {
                    MessageBox.Show("finish");
                }
            }
        }

        private void StartCombine(string combineFile, string fileName)
        {
            pb.Maximum = CalculateTotalTime();
            pb.Minimum = 0;

            p = new Process();//建立外部调用线程
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = ffmpeg;//要调用外部程序的绝对路径
            p.StartInfo.Arguments = string.Format("-f concat -safe 0 -i {0} -c:v hevc_nvenc -preset:v fast \"{1}\"", combineFile, fileName);
            p.StartInfo.UseShellExecute = false;//不使用操作系统外壳程序启动线程(一定为FALSE,详细的请看MSDN)
            p.StartInfo.RedirectStandardError = true;//把外部程序错误输出写到StandardError流中(这个一定要注意,FFMPEG的所有输出信息,都为错误输出流,用StandardOutput是捕获不到任何消息的...这是我耗费了2个多月得出来的经验...mencoder就是用standardOutput来捕获的)
            p.ErrorDataReceived += Output;//外部程序(这里是FFMPEG)输出流时候产生的事件,这里是把流的处理过程转移到下面的方法中,详细请查阅MSDN

            Task.Run(() => FfmpegHelper.ConvertVideo(Output, p));
        }

        private void JDuBar(ProgressBar jd, int v)
        {
            if (jd.InvokeRequired)
            {
                jd.Invoke(new ProcessPb(JDuBar), jd, v);
            }
            else
            {
                if (v < jd.Maximum && v >= 0)
                {
                    jd.Value = v;
                }
                else
                {
                    jd.Value = jd.Maximum;
                }
            }
        }

        private void ListViewItemUpdate(ListView lv, ListViewItem lvi, int column, string content, List<SeedMagnetSearchModel> model)
        {
            if (lv.InvokeRequired)
            {
                lv.Invoke(new ProcessListView(ListViewItemUpdate), lv, lvi, column, content, model);
            }
            else
            {
                lvi.SubItems[column].Text = content;
                ((MissingCheckModel)lvi.Tag).Seeds = model;
                lvi.BackColor = Color.Green;
            }
        }

        private void ListViewItemUpdate2(ListView lv, ListViewItem lvi)
        {
            if (lv.InvokeRequired)
            {
                lv.Invoke(new ProcessListViewItem(ListViewItemUpdate2), lv, lvi);
            }
            else
            {
                lv.Items.Add(lvi);
            }
        }

        private void GetFilesToGenerateCombineFile()
        {
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;

            var rs = folderBrowserDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                txtLook.Text = folderBrowserDialog1.SelectedPath;

                ShowTree(txtLook.Text);
            }
        }

        private async void ShowTree(string path)
        {
            var files = FileUtility.GetVideoHasMultipleEpisode(path);

            treeView1.BeginUpdate();
            foreach (var f in files)
            {
                TreeNode tn = new TreeNode(f.Key);

                foreach (var s in f.Value)
                {
                    FileInfo fi = new FileInfo(s);

                    TreeNode stn = new TreeNode(fi.FullName + " " + FileSize.GetAutoSizeString(fi.Length, 2))
                    {
                        Tag = fi
                    };
                    tn.Nodes.Add(stn);

                    if (cbThums.Checked)
                    {
                        await FileUtility.GetThumbnails(fi.FullName, ffmpeg, thumsFolder, fi.Name, 6, false);
                    }
                }

                treeView1.Nodes.Add(tn);
            }

            treeView1.ExpandAll();
            treeView1.EndUpdate();
        }

        private void InitAutoCombineFile()
        {
            var path = "c:\\setting\\autocombine\\";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                Directory.GetFiles(path).ToList().ForEach(x => File.Delete(x));
            }
        }

        private string CheckAutoCombine()
        {
            if (treeView1.Nodes.Count <= 0)
            {
                return "没有文件";
            }

            return "";
        }

        private void GenerateAutoCombineFile()
        {
            var check = CheckAutoCombine();

            if (!string.IsNullOrEmpty(check))
            {
                MessageBox.Show(check);
                return;
            }

            pb2.Maximum = treeView1.Nodes.Count;
            pb2.Minimum = 0;
            int index = 0;

            foreach (TreeNode node in treeView1.Nodes)
            {
                RealGenerateAutoCombineFile(node);

                index++;

                JDuBar(pb2, index);
            }
        }

        private void DeleteMultiple()
        {
            double size = 0;
            int totalCount = 0;
            List<string> files = new List<string>();

            foreach (TreeNode tn in treeView1.Nodes)
            {
                foreach (TreeNode stn in tn.Nodes)
                {
                    FileInfo fi = (FileInfo)stn.Tag;
                    totalCount++;
                    size += fi.Length;
                    files.Add(fi.FullName);
                }
            }

            var rs = MessageBox.Show(string.Format("一共有 {0} 部影片, {1} 个文件, 总大小 {2}", treeView1.Nodes.Count, totalCount, FileSize.GetAutoSizeString(size, 1)) + " 确定要删除? ", "警告", MessageBoxButtons.YesNo);

            if (rs == DialogResult.Yes)
            {
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ee)
                    {
                        MessageBox.Show(ee.ToString());
                    }
                }
            }
        }

        private void RealGenerateAutoCombineFile(TreeNode node)
        {
            var file = "c:\\setting\\autocombine\\" + node.Text.Substring(node.Text.LastIndexOf("\\") + 1) + DateTime.Now.ToString("yyyyMMddhhmmss") + ".txt";

            if (File.Exists(file))
            {
                File.Delete(file);
            }

            File.CreateText(file).Close();

            StreamWriter sw = new StreamWriter(file);
            StringBuilder sb = new StringBuilder();

            foreach (TreeNode sn in node.Nodes)
            {
                sb.AppendLine(string.Format("file '{0}'", ((FileInfo)sn.Tag).FullName));
            }

            sw.WriteLine(sb.ToString());
            sw.Close();
        }

        private void RestAutoCombineFile()
        {
            txtLook.Text = "";
            treeView1.Nodes.Clear();
            pb2.Value = 0;
        }

        private void Preview()
        {
            var path = "c:\\setting\\autocombine\\";

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path);

                ShowPreviewTree(files);
            }
        }

        private void ShowPreviewTree(string[] files)
        {
            treeView2.BeginUpdate();
            foreach (var file in files)
            {
                TreeNode tn = new TreeNode(file);
                StreamReader sr = new StreamReader(file);
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        TreeNode stn = new TreeNode(line);
                        tn.Nodes.Add(stn);
                    }
                }
                sr.Close();

                treeView2.Nodes.Add(tn);
            }
            treeView2.ExpandAll();
            treeView2.EndUpdate();
        }

        private async void AutoCombie()
        {
            if (treeView2.Nodes.Count <= 0)
            {
                MessageBox.Show("没有需要合并的文件");
                return;
            }

            if (string.IsNullOrEmpty(txtAutoSave.Text))
            {
                MessageBox.Show("没有选择保存地址");
                return;
            }

            pbTotal.Maximum = treeView2.Nodes.Count;
            pbTotal.Minimum = 0;
            int index = 0;

            Dictionary<string, TreeNode> fileList = new Dictionary<string, TreeNode>();

            foreach (TreeNode tn in treeView2.Nodes)
            {
                OkToStart = false;

                await CombineEach(tn.Text, tn);

                index++;

                if (OkToStart)
                {
                    tn.BackColor = Color.Green;
                }
                else
                {
                    tn.BackColor = Color.Red;
                }

                JDuBar(pbTotal, index);
                tn.EnsureVisible();
            }
        }

        private async Task CombineEach(string file, TreeNode node)
        {
            var current = CalculateTotalTimeForAuto(file);
            pbCurrent.Maximum = current;
            pbCurrent.Minimum = 0;
            pbCurrent.Value = 0;

            List<string> sourceFiles = new List<string>();

            foreach (TreeNode stn in node.Nodes)
            {
                sourceFiles.Add(stn.Text.Replace("file '", "").Replace("'", ""));
            }

            FileInfo first = new FileInfo(sourceFiles.FirstOrDefault());
            var targetFile = first.Name.Substring(0, first.Name.LastIndexOf("-")) + ".mp4";

            var fileName = txtAutoSave.Text + targetFile;

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            await StartCombineAuto(node.Text, fileName);

            if (OkToStart)
            {
                File.Delete(file);
            }
            else
            {
                foreach (var dFile in sourceFiles)
                {
                    FileInfo fi = new FileInfo(dFile);
                    var reName = fi.FullName.Replace(fi.Extension, "-NoMerge" + fi.Extension);

                    File.Move(dFile, reName);

                    Thread.Sleep(1000);

                    File.Delete(file);
                }
            }
        }

        private void OutputAuto(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                if (output.Data.StartsWith("frame"))
                {
                    var time = output.Data.Substring(output.Data.IndexOf("time="), 16).Replace("time=", "");
                    var process = FileUtility.ConvertDurationToInt(time);

                    JDuBar(pbCurrent, process);
                }
                else if (output.Data.StartsWith("video:"))
                {
                    OkToStart = true;
                }
            }
        }

        private async Task StartCombineAuto(string combineFile, string fileName)
        {
            p = new Process();//建立外部调用线程
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = ffmpeg;//要调用外部程序的绝对路径
            //TODO
            p.StartInfo.Arguments = string.Format("-f concat -safe 0 -i \"{0}\" -c:v hevc_nvenc -preset:v fast \"{1}\"", combineFile, fileName);
            p.StartInfo.UseShellExecute = false;//不使用操作系统外壳程序启动线程(一定为FALSE,详细的请看MSDN)
            p.StartInfo.RedirectStandardError = true;//把外部程序错误输出写到StandardError流中(这个一定要注意,FFMPEG的所有输出信息,都为错误输出流,用StandardOutput是捕获不到任何消息的...这是我耗费了2个多月得出来的经验...mencoder就是用standardOutput来捕获的)
            p.ErrorDataReceived += OutputAuto;//外部程序(这里是FFMPEG)输出流时候产生的事件,这里是把流的处理过程转移到下面的方法中,详细请查阅MSDN
            p.Start();//启动线程
            p.BeginErrorReadLine();//开始异步读取
            await p.WaitForExitAsync();
            p.Close();
            p.Dispose();
            p = null;
        }

        private void AutoSave()
        {
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;

            var rs = folderBrowserDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                txtAutoSave.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private int CalculateTotalTimeForAuto(string file)
        {
            int ret = 0;

            StreamReader sr = new StreamReader(file);

            var content = sr.ReadToEnd();
            var contentArray = content.Split(new char[] { '\r', '\n' });

            foreach (var c in contentArray.Where(x => !string.IsNullOrEmpty(x)))
            {
                var line = c;
                line = line.Replace("file '", "").Replace("'", "");
                var fi = new FileInfo(line);
                var duration = FileUtility.GetDuration(fi.FullName, ffmpeg);

                ret += FileUtility.ConvertDurationToInt(duration);
            }
            sr.Close();

            return ret;
        }

        private void GetFilesForAutoConvert()
        {
            openFileDialog1.Multiselect = true;

            var rs = openFileDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                var files = openFileDialog1.FileNames;

                ShowConvertFiles(files);

                string convert = "G:\\convert\\";

                if (!Directory.Exists(convert))
                {
                    Directory.CreateDirectory(convert);
                }

                txtConvertSave.Text = convert;
            }
        }

        private void GetFilesForAutoConvertSave()
        {
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;

            var rs = folderBrowserDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                txtConvertSave.Text = folderBrowserDialog1.SelectedPath + "\\convert\\";
            }
        }

        private void GenerateSaveConvertFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void ShowConvertFiles(string[] files)
        {
            listView2.BeginUpdate();

            foreach (var file in files)
            {
                if (!file.Contains("[Code="))
                {
                    FileInfo fi = new FileInfo(file);

                    ListViewItem lvi = new ListViewItem(fi.FullName);
                    lvi.SubItems.Add("--");
                    lvi.SubItems.Add(FileSize.GetAutoSizeString(fi.Length, 1));

                    listView2.Items.Add(lvi);
                }
            }

            listView2.EndUpdate();
        }

        private async Task<TimeSpan> Convert()
        {
            DateTime t1 = DateTime.Now;

            if (string.IsNullOrEmpty(txtConvertSave.Text))
            {
                MessageBox.Show("没有保存地址");
                return new TimeSpan(0);
            }

            GenerateSaveConvertFolder(txtConvertSave.Text);

            if (listView2.Items.Count > 0)
            {
                pbConvertTotal.Maximum = listView2.Items.Count;
                pbConvertTotal.Minimum = 0;

                int index = 0;

                foreach (ListViewItem item in listView2.Items)
                {
                    await GetDurationAndCheck(item, txtConvertSave.Text, cbDeleteConvert.Checked);

                    index++;

                    JDuBar(pbConvertTotal, index);
                }
            }

            DateTime t2 = DateTime.Now;

            return (t2 - t1);
        }

        private async Task GetDurationAndCheck(ListViewItem lvi, string folder, bool isCheck)
        {
            string info = await FileUtility.GetFfmpegInfo(lvi.Text, ffmpeg);

            var duration = info;
            duration = duration.Substring(duration.IndexOf("Duration") + 10);
            duration = duration.Substring(0, duration.IndexOf(","));

            listView2.BeginUpdate();
            lvi.SubItems[1].Text = duration;
            listView2.EndUpdate();

            if (!info.Contains("Video: hevc"))
            {
                pb.Value = 0;
                pbConvertCurrent.Maximum = FileUtility.ConvertDurationToInt(duration);
                pbConvertCurrent.Minimum = 0;

                FileInfo fi = new FileInfo(lvi.Text);
                var targetFile = folder + fi.Name;

                await StartConvert(lvi.Text, targetFile);

                if (isCheck)
                {
                    try
                    {
                        File.Delete(lvi.Text);
                    }
                    catch (Exception ee)
                    {

                    }
                }
            }
            else
            {
                //FileInfo fi = new FileInfo(lvi.Text);
                //var newFi = fi.FullName.Replace(fi.Extension, "") + "[]Format=H265[]" + fi.Extension;
                //try
                //{
                //    File.Move(fi.FullName, newFi);
                //}
                //catch (Exception ee)
                //{

                //}
            }

            listView2.BeginUpdate();
            lvi.BackColor = Color.Green;
            lvi.EnsureVisible();
            listView2.EndUpdate();
        }

        private void OutputConvert(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                if (output.Data.StartsWith("frame"))
                {
                    var time = output.Data.Substring(output.Data.IndexOf("time="), 16).Replace("time=", "");
                    var process = FileUtility.ConvertDurationToInt(time);

                    JDuBar(pbConvertCurrent, process);
                }
                else if (output.Data.StartsWith("video:"))
                {

                }
            }
        }

        private async Task StartConvert(string from, string to)
        {
            p = new Process();//建立外部调用线程
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = ffmpeg;//要调用外部程序的绝对路径
            p.StartInfo.Arguments = string.Format("-i \"{0}\" -c:v hevc_nvenc -preset:v fast \"{1}\"", from, to);
            p.StartInfo.UseShellExecute = false;//不使用操作系统外壳程序启动线程(一定为FALSE,详细的请看MSDN)
            p.StartInfo.RedirectStandardError = true;//把外部程序错误输出写到StandardError流中(这个一定要注意,FFMPEG的所有输出信息,都为错误输出流,用StandardOutput是捕获不到任何消息的...这是我耗费了2个多月得出来的经验...mencoder就是用standardOutput来捕获的)
            p.ErrorDataReceived += OutputConvert;//外部程序(这里是FFMPEG)输出流时候产生的事件,这里是把流的处理过程转移到下面的方法中,详细请查阅MSDN
            p.Start();//启动线程
            p.BeginErrorReadLine();//开始异步读取
            await p.WaitForExitAsync();
            p.Close();
            p.Dispose();
            p = null;
        }

        private void ImportISO()
        {
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;

            var rs = folderBrowserDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                var path = folderBrowserDialog1.SelectedPath;

                List<FileInfo> fis = new List<FileInfo>();
                var files = Directory.GetFiles(path);

                foreach (var file in files)
                {
                    FileInfo fi = new FileInfo(file);

                    if (fi.Extension == ".iso")
                    {
                        fis.Add(fi);
                    }
                }

                ShowIsoTree(fis);
            }
        }

        private void ShowIsoTree(List<FileInfo> files)
        {
            treeView3.BeginUpdate();

            foreach (var fi in files)
            {
                var nameArr = fi.Name.Split('-');
                var searchContent = nameArr[0] + "-" + nameArr[1];

                var rets = MagService.SearchSukebei(searchContent);

                TreeNode tn = new TreeNode(fi.FullName);
                foreach (var ret in rets)
                {
                    TreeNode stn = new TreeNode(FileSize.GetAutoSizeString(ret.Size, 2) + " -> " + ret.MagUrl);

                    tn.Nodes.Add(stn);
                }

                treeView3.Nodes.Add(tn);
            }

            treeView3.ExpandAll();
            treeView3.EndUpdate();
        }

        private void ClickTree(TreeNode tn)
        {
            if (tn.Parent != null)
            {
                var str = tn.Text.Split(' ')[2];

                Clipboard.SetDataObject(str, true);
            }
        }

        private void ScanRedundantClick()
        {
            treeView4.Nodes.Clear();
            var dic = FileUtility.GetAllPossibleRedundant();

            treeView4.BeginUpdate();

            foreach (var d in dic)
            {
                TreeNode tn = new TreeNode(d.Key);
                double largest = d.Value.Max(x => x.Length);

                foreach (var sd in d.Value)
                {
                    var stn = new TreeNode(sd.FullName + " 大小: " + FileSize.GetAutoSizeString(sd.Length, 1));
                    stn.Tag = sd;

                    if (sd.Extension.ToLower() == ".iso")
                    {
                        stn.BackColor = Color.Yellow;
                        stn.Checked = true;
                    }
                    else if (sd.Name.Contains("-C") || sd.Name.Contains("-c"))
                    {
                        stn.BackColor = Color.Green;
                    }
                    else
                    {
                        if (sd.Length < largest)
                        {
                            stn.Checked = true;
                            stn.BackColor = Color.Red;
                        }
                    }

                    tn.Nodes.Add(stn);
                }

                treeView4.Nodes.Add(tn);
            }

            treeView4.ExpandAll();
            treeView4.EndUpdate();
        }

        private void ScanDeleteClick()
        {
            int count = 0;
            double totalSize = 0;
            List<string> files = new List<string>();

            foreach (TreeNode tn in treeView4.Nodes)
            {
                foreach (TreeNode stn in tn.Nodes)
                {
                    if (stn.Checked)
                    {
                        FileInfo fi = ((FileInfo)stn.Tag);
                        count++;
                        totalSize += fi.Length;
                        files.Add(fi.FullName);
                    }
                }
            }

            var rs = MessageBox.Show(string.Format("确定要删除 {0} 个文件, 总大小 {1} ?", count, FileSize.GetAutoSizeString(totalSize, 1)), "警告", MessageBoxButtons.YesNo);

            if (rs == DialogResult.Yes)
            {
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ee)
                    {

                    }
                }

                ScanRedundantClick();
            }
        }

        private void ClearCheck()
        {
            foreach (TreeNode tn in treeView4.Nodes)
            {
                foreach (TreeNode stn in tn.Nodes)
                {
                    stn.Checked = false;
                }
            }
        }

        private async void ScanUnmatchedClick()
        {
            Dictionary<string, List<SeedMagnetSearchModel>> ret = new Dictionary<string, List<SeedMagnetSearchModel>>();
            List<string> unmatchedList = new List<string>();
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            var rs = folderBrowserDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                txtUnmatched.Text = folderBrowserDialog1.SelectedPath;

                var res = FileUtility.GetMagUrlOfUnmatchedFile(txtUnmatched.Text);
                var isos = res.Item1;
                var multi = res.Item2;

                treeView5.BeginUpdate();

                await Task.Run(() =>
                {
                    Parallel.ForEach(isos, new ParallelOptions { MaxDegreeOfParallelism = 5 }, key =>
                    {
                        var result = MagService.SearchSukebei(key.Value);

                        if (result != null && result.Count > 0)
                        {
                            ret.Add(key.Key, result);
                        }
                        else
                        {
                            ret.Add(key.Key, new List<SeedMagnetSearchModel>());
                        }
                    });
                });

                await Task.Run(() =>
                {
                    Parallel.ForEach(multi, new ParallelOptions { MaxDegreeOfParallelism = 5 }, file =>
                    {
                        var result = MagService.SearchSukebei(file.Key.Substring(file.Key.LastIndexOf("\\") + 1));

                        if (result != null && result.Count > 0)
                        {
                            ret.Add(file.Value.FirstOrDefault(), result);
                        }
                        else
                        {
                            ret.Add(file.Value.FirstOrDefault(), new List<SeedMagnetSearchModel>());
                        }
                    });
                });

                foreach (var d in ret)
                {
                    TreeNode tn = new TreeNode(d.Key);
                    tn.BackColor = Color.Green;

                    int index = 1;

                    foreach (var sd in d.Value)
                    {
                        TreeNode stn = new TreeNode(sd.Title + " 大小: " + FileSize.GetAutoSizeString(sd.Size, 1));
                        stn.Tag = sd.MagUrl;

                        if (index % 2 == 0)
                        {
                            stn.BackColor = Color.Gray;
                        }

                        tn.Nodes.Add(stn);
                        index++;
                    }

                    unmatchedList.Add(d.Key);

                    treeView5.Nodes.Add(tn);
                }

                treeView5.ExpandAll();
                treeView5.EndUpdate();

                string uFile = "G:\\" + FileUtility.ReplaceInvalidChar(txtUnmatched.Text) + "-unmatched.json";
                File.Create(uFile).Close();
                StreamWriter sw = new StreamWriter(uFile);
                sw.WriteLine(JsonConvert.SerializeObject(unmatchedList));
                sw.Close();
            }
        }

        private void InitRemoveFolder()
        {
            richTextBox1.Text = "";
            excludes = JavINIClass.IniReadValue("Scan", "Exclude").Split(',').ToList();
        }

        private void RemovceFolderScanClick()
        {
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;

            var rs = folderBrowserDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                txtRemoveFolderTxt.Text = folderBrowserDialog1.SelectedPath;

                richTextBox1.AppendText("设置需要去除子文件夹的目录为 -> " + txtRemoveFolderTxt.Text, Color.Green, font, true);
            }
        }

        private string InitRemove(string folder)
        {
            var moveFolder = folder + "/movefiles/";
            excludes.Add(moveFolder);

            if (!Directory.Exists(moveFolder))
            {
                Directory.CreateDirectory(moveFolder);
            }

            richTextBox1.AppendText("初始化移动到的文件夹为 -> " + moveFolder, Color.Green, font, true);
            richTextBox1.AppendText("把 " + moveFolder + " 添加到忽略列表", Color.Green, font, true);

            return moveFolder;
        }

        private List<FileInfo> GetMoveFiles(string folder)
        {
            int limitSize = 200;
            List<FileInfo> fis = new List<FileInfo>();

            richTextBox1.AppendText("获取所有 >= " + limitSize + "mb 的在子文件夹内的文件", Color.Green, font, true);

            var status = FileUtility.GetFilesRecursive(folder, formats, excludes, fis, limitSize);

            if (string.IsNullOrEmpty(status))
            {
                richTextBox1.AppendText("一共获取了 >= " + fis.Count + " 个文件", Color.Green, font, true);
            }
            else
            {
                richTextBox1.AppendText("异常 >= " + status, Color.Red, font, true);
            }

            return fis;
        }

        private void RemoveFolderStartClick()
        {
            if (!string.IsNullOrEmpty(txtRemoveFolderTxt.Text))
            {
                Dictionary<string, string> remainSize = new Dictionary<string, string>();
                Dictionary<string, int> moveRecord = new Dictionary<string, int>();

                var moveFolder = InitRemove(txtRemoveFolderTxt.Text);
                var fis = GetMoveFiles(txtRemoveFolderTxt.Text);

                foreach (var fi in fis)
                {
                    richTextBox1.AppendText("开始移动 " + fi.FullName, Color.Green, font, true);

                    var n = fi.Name.Replace(fi.Extension, "");
                    var e = fi.Extension;

                    richTextBox1.AppendText("\t文件名 >= " + n + " 扩展名 => " + e, Color.Black, font, true);

                    if (fi.Name.Contains("-5" + fi.Extension) && fi.Length < 1 * 1024 * 1024 * 1024)
                    {
                        richTextBox1.AppendText("删除dummy文件");
                        continue;
                    }

                    if (moveRecord.ContainsKey(fi.Name))
                    {
                        moveRecord[fi.Name]++;
                        richTextBox1.AppendText("\t存在移动记录,添加后缀 >= " + moveRecord[fi.Name], Color.Red, font, true);
                    }
                    else
                    {
                        moveRecord.Add(fi.Name, 1);
                    }

                    if (File.Exists(moveFolder + n + e))
                    {
                        var oldN = n;

                        n += "_" + moveRecord[fi.Name];

                        richTextBox1.AppendText("\t存在重名文件,修改文件名 >= " + (n + "_" + moveRecord[fi.Name]), Color.Red, font, true);

                        if (moveRecord[fi.Name] == 2)
                        {
                            File.Move(moveFolder + oldN + e, moveFolder + oldN + "_1" + e);
                        }
                    }

                    richTextBox1.AppendText("\t移动文件 >= " + fi.FullName + " 到 => " + moveFolder + n + e, Color.Green, font, true);
                    File.Move(fi.FullName, moveFolder + n + e);
                }

                richTextBox1.AppendText("开始计算剩余各子文件夹大小", Color.Green, font, true);

                var subFolders = Directory.GetDirectories(txtRemoveFolderTxt.Text);

                foreach (var sub in subFolders)
                {
                    richTextBox1.AppendText("\t开始计算子文件夹 " + sub + " 的大小", Color.Black, font, true);

                    List<FileInfo> tempFi = new List<FileInfo>();
                    formats.Add(".!qB");
                    string tempStatus = FileUtility.GetFilesRecursive(sub, formats, excludes, tempFi);
                    double tempSize = 0D;

                    if (string.IsNullOrEmpty(tempStatus))
                    {
                        foreach (var fi in tempFi)
                        {
                            tempSize += fi.Length;
                        }

                        remainSize.Add(sub, FileSize.GetAutoSizeString(tempSize, 2));

                        if (tempSize >= 500 * 1024 * 1024)
                        {
                            richTextBox1.AppendText("\t" + sub + "的大小为 = > " + FileSize.GetAutoSizeString(tempSize, 2), Color.Red, font, true);
                        }
                        else
                        {
                            richTextBox1.AppendText("\t" + sub + "的大小为 = > " + FileSize.GetAutoSizeString(tempSize, 2), Color.Black, font, true);
                        }
                    }
                }
            }
        }

        private void RenameScanClick()
        {
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;

            var rs = folderBrowserDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                txtRenameTxt.Text = folderBrowserDialog1.SelectedPath;

                richTextBox2.AppendText("设置需要重命名文件夹的目录为 -> " + txtRenameTxt.Text, Color.Green, font, true);
            }
        }

        private void RenameStartClick()
        {
            if (!string.IsNullOrEmpty(txtRenameTxt.Text))
            {
                InitRenameFolder(txtRenameTxt.Text);
            }
        }

        private void InitRenameFolder(string folder)
        {
            List<string> allPrefix = new List<string>();
            Dictionary<FileInfo, List<AV>> ret = new Dictionary<FileInfo, List<AV>>();
            Dictionary<string, int> moveReocrd = new Dictionary<string, int>();
            var moveFolder = folder + "/tempFin/";
            int found = 0;
            int notFound = 0;

            richTextBox2.AppendText("初始化移动目录 -> " + moveFolder, Color.Green, font, true);

            if (!Directory.Exists(moveFolder))
            {
                Directory.CreateDirectory(moveFolder);
            }

            richTextBox2.AppendText("开始加载缓存", Color.Green, font, true);

            var avs = JavDataBaseManager.GetAllAV();

            richTextBox2.AppendText("共加载 " + avs.Count + " 条缓存", Color.Black, font, true);

            richTextBox2.AppendText("开始处理前缀", Color.Green, font, true);

            foreach (var name in avs.Select(x => x.ID).ToList())
            {
                if (!allPrefix.Contains(name.Split('-')[0]))
                {
                    allPrefix.Add(name.Split('-')[0]);
                }
            }

            allPrefix = allPrefix.OrderByDescending(x => x.Length).ToList();

            richTextBox2.AppendText("共加载 " + allPrefix.Count + " 条前缀", Color.Black, font, true);

            richTextBox2.AppendText("开始获取目录 " + folder + " 下的文件", Color.Green, font, true);

            var files = Directory.GetFiles(folder);

            richTextBox2.AppendText("共获取 " + files.Length + " 个文件", Color.Black, font, true);

            foreach (var f in files)
            {
                richTextBox2.AppendText("开始处理文件 " + f, Color.Green, font, true);

                bool findMatch = false;
                string pi = "";
                FileInfo fi = new FileInfo(f);
                List<AV> fiMatchList = new List<AV>();
                var fiNameUpper = fi.Name.ToUpper();
                var fileNameWithoutFormat = fiNameUpper.Replace(fi.Extension.ToUpper(), "");

                ret.Add(fi, fiMatchList);

                foreach (var prefix in allPrefix)
                {
                    if (fileNameWithoutFormat.Contains(prefix))
                    {
                        richTextBox2.AppendText("\t找到匹配前缀 " + prefix, Color.Black, font, true);

                        var pattern = prefix + "{1}-?\\d{1,7}";
                        var possibleId = Regex.Match(fileNameWithoutFormat, pattern).Value;

                        if (possibleId.Contains("-"))
                        {
                            pi = possibleId;
                        }
                        else
                        {
                            bool isFirst = true;
                            StringBuilder sb = new StringBuilder();

                            foreach (var c in possibleId)
                            {
                                if (c >= '0' && c <= '9')
                                {
                                    if (isFirst)
                                    {
                                        sb.Append("-");
                                        isFirst = false;
                                    }
                                }
                                sb.Append(c);
                            }

                            pi = sb.ToString();
                        }

                        if (!string.IsNullOrEmpty(pi))
                        {
                            richTextBox2.AppendText("\t找到适配的番号 " + pi, Color.Black, font, true);

                            var possibleAv = avs.Where(x => x.ID == pi).ToList();

                            if (possibleAv == null || possibleAv.Count <= 0)
                            {
                                var prefixPart = pi.Split('-')[0];
                                var numberPart = pi.Split('-')[1];

                                if (numberPart.StartsWith("00"))
                                {
                                    numberPart = numberPart.Substring(2);

                                    pi = prefixPart + "-" + numberPart;

                                    possibleAv = avs.Where(x => x.ID == pi).ToList();
                                }
                            }

                            findMatch = true;
                            foreach (var av in possibleAv)
                            {
                                fiMatchList.AddRange(possibleAv);
                            }

                            richTextBox2.AppendText("\t找到适配的AV " + fiMatchList.Count + " 条", Color.Black, font, true);

                            break;
                        }
                    }
                }

                if (findMatch)
                {
                    found++;
                }
                else
                {
                    notFound++;
                }
            }

            foreach (var item in ret)
            {
                if (item.Value.Count == 0)
                {
                    richTextBox2.AppendText("文件 " + item.Key.Name + " 没有找到匹配", Color.Black, font, true);
                }
                else if (item.Value.Count > 1)
                {
                    foreach (var subItem in item.Value)
                    {
                        richTextBox2.AppendText("文件 " + item.Key.Name + " 找到多条匹配,暂不处理", Color.Black, font, true);
                    }
                }
                else if (item.Value.Count == 1)
                {
                    richTextBox2.AppendText("文件 " + item.Key.Name + " 找到1条匹配,开始处理", Color.Green, font, true);

                    var chinese = "";

                    if (item.Key.Name.Replace(item.Value.FirstOrDefault().ID, "").Replace(item.Value.FirstOrDefault().Name, "").Contains("-C") || item.Key.Name.Replace(item.Value.FirstOrDefault().ID, "").Replace(item.Value.FirstOrDefault().Name, "").Contains("-c") || item.Key.Name.Replace(item.Value.FirstOrDefault().ID, "").Replace(item.Value.FirstOrDefault().Name, "").Contains("ch"))
                    {
                        chinese = "-C";
                    }

                    var tempFileName = item.Value.FirstOrDefault().ID + "-" + item.Value.FirstOrDefault().Name + chinese + item.Key.Extension;

                    if (moveReocrd.ContainsKey(tempFileName))
                    {
                        moveReocrd[tempFileName]++;

                        richTextBox2.AppendText("\t存在移动记录,文件名后缀+1 -> " + moveReocrd[tempFileName], Color.Green, font, true);

                        if (moveReocrd[tempFileName] == 2)
                        {
                            var oldFileMove = item.Value.FirstOrDefault().ID + "-" + item.Value.FirstOrDefault().Name + "-1" + item.Key.Extension;
                            File.Move(moveFolder + tempFileName, moveFolder + oldFileMove);
                        }

                        tempFileName = item.Value.FirstOrDefault().ID + "-" + item.Value.FirstOrDefault().Name + "-" + moveReocrd[tempFileName] + item.Key.Extension;
                    }
                    else
                    {
                        moveReocrd.Add(tempFileName, 1);
                    }

                    try
                    {
                        File.Move(item.Key.FullName, moveFolder + tempFileName);

                        richTextBox2.AppendText("\t移动文件 -> " + item.Key.FullName + " 到 -> " + moveFolder + tempFileName, Color.Green, font, true);
                    }
                    catch (Exception ee)
                    {
                        richTextBox2.AppendText(ee.ToString(), Color.Red, font, true);
                    }
                }
            }

            richTextBox2.AppendText("找到匹配 --> " + found + " 未找到匹配 --> " + notFound, Color.Green, font, true);
        }

        private void SearchSeedClick()
        {
            if (!string.IsNullOrEmpty(txtSeedSearchContent.Text))
            {
                List<SearchSeedSiteEnum> sources = new List<SearchSeedSiteEnum>();

                if (cbBtsow.Checked)
                {
                    sources.Add(SearchSeedSiteEnum.Btsow);
                }

                if (cbSukebei.Checked)
                {
                    sources.Add(SearchSeedSiteEnum.Sukebei);
                }

                if (cbJavBus.Checked)
                {
                    sources.Add(SearchSeedSiteEnum.JavBus);
                }

                var resList = SearchSeed(txtSeedSearchContent.Text, sources);
                ListSeedSearch(resList);

                Message ms = new Message();
                ms.ShowDialog();
            }
        }

        private List<SeedMagnetSearchModel> SearchSeed(string content, List<SearchSeedSiteEnum> sources)
        {
            List<SeedMagnetSearchModel> ret = new List<SeedMagnetSearchModel>();

            foreach (var source in sources)
            {
                switch (source)
                {
                    case SearchSeedSiteEnum.Btsow:
                        ret = MagService.SearchBtsow(content);
                        break;
                    case SearchSeedSiteEnum.Sukebei:
                        ret = MagService.SearchSukebei(content);
                        break;
                    case SearchSeedSiteEnum.JavBus:
                        ret = MagService.SearchJavBus(content);
                        break;
                }
            }

            return ret;
        }

        private void ListSeedSearch(List<SeedMagnetSearchModel> data)
        {
            listView3.BeginUpdate();

            int index = 1;
            foreach (var d in data)
            {
                ListViewItem lvi = new ListViewItem(d.Title);
                lvi.SubItems.Add(FileSize.GetAutoSizeString(d.Size, 1));
                lvi.SubItems.Add(d.Date.ToString("yyyy-MM-dd"));
                lvi.SubItems.Add(d.CompleteCount + "");
                lvi.SubItems.Add(d.Source.ToString());
                lvi.Tag = d.MagUrl;

                listView3.Items.Add(lvi);

                if (index % 2 == 0)
                {
                    lvi.BackColor = Color.LightGray;
                }

                index++;
            }

            listView3.EndUpdate();
        }

        private void ShowJavScanPreset(string table)
        {
            listView4.Items.Clear();

            var data = JavDataBaseManager.GetAllValidMap(table);

            listView4.BeginUpdate();

            int index = 1;

            foreach (var d in data)
            {
                ListViewItem lvi = new ListViewItem(d.Name);
                lvi.SubItems.Add(d.URL);

                if (index % 2 == 0)
                {
                    lvi.BackColor = Color.LightGray;
                }

                listView4.Items.Add(lvi);
            }

            listView4.EndUpdate();
        }

        private Dictionary<string, string> GenerateDicToUpdate(bool fromText)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            if (fromText)
            {
                ret.Add(txtJavScanUrl.Text, txtJavScanTitle.Text);
            }
            else
            {
                foreach (ListViewItem lvi in listView4.SelectedItems)
                {
                    ret.Add(lvi.SubItems[1].Text, lvi.SubItems[0].Text);
                }
            }

            return ret;
        }

        private async void JavScanAsync(string command)
        {
            richTextBox3.Text = "";
            LockModel lockModel = new LockModel();
            Dictionary<string, string> dic = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(txtJavScanTitle.Text) && !string.IsNullOrEmpty(txtJavScanUrl.Text))
            {
                dic = GenerateDicToUpdate(true);
            }
            else if (listView4.SelectedItems.Count > 0)
            {
                dic = GenerateDicToUpdate(false);
            }

            var titleStr = string.Join(",", dic.Select(x => x.Value));
            var urlStr = string.Join(",", dic.Select(x => x.Key));

            if (rbPrefix.Checked && !string.IsNullOrEmpty(txtPrefix.Text))
            {
                titleStr = txtPrefix.Text.Trim();
                urlStr = "http://www.javlibrary.com/cn/vl_searchbyid.php?&page=1&keyword=" + txtPrefix.Text.Trim();
            }

            if (command == "daily")
            {
                await StartJavScan("", " " + command, OutputJavScan);
            }
            else
            {
                await StartJavScan("", " " + command + " " + titleStr + " " + urlStr, OutputJavScan);
            }

            MessageBox.Show("操作完毕");
        }

        private async void JavBatchScanAsync(string command)
        {
            foreach (ListViewItem lvi in listView4.SelectedItems)
            {
                var titleStr = lvi.SubItems[0].Text;
                var urlStr = lvi.SubItems[1].Text;

                await StartJavScan("", " " + command + " " + titleStr + " " + urlStr, OutputJavScan);
            }

            MessageBox.Show("操作完毕");
        }

        private async Task StartJavScan(string exe, string arg, DataReceivedEventHandler output)
        {
            exe = "G:\\Github\\AllInOneAV\\AllInOneAV\\BatchJavScanerAndMacthMagUrl\\bin\\Debug\\BatchJavScanerAndMacthMagUrl.exe";

            using (var p = new Process())
            {
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = arg;

                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;

                p.OutputDataReceived += OutputJavScan;

                p.Start();
                p.BeginOutputReadLine();
                await p.WaitForExitAsync();
            }
        }

        private void OutputJavScan(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                richTextBox3.AppendText(output.Data + Environment.NewLine);
            }
        }

        private async void StartScanAndMatch()
        {
            rtbMatch.Text = "";

            await StartScanAndMatchTask(OutputStartScanAndMatch);

            matchesAV = ScanDataBaseManager.GetAllMatch();

            MessageBox.Show("扫描完成");
        }

        private async Task StartScanAndMatchTask(DataReceivedEventHandler output)
        {
            var exe = "G:\\Github\\AllInOneAV\\AllInOneAV\\ScanAllAndMatch\\bin\\Debug\\ScanAllAndMatch.exe";
            var arg = "";

            using (var p = new Process())
            {
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = arg;

                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;

                p.OutputDataReceived += OutputStartScanAndMatch;

                p.Start();
                p.BeginOutputReadLine();
                await p.WaitForExitAsync();
            }
        }

        private void OutputStartScanAndMatch(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                rtbMatch.AppendText(output.Data + Environment.NewLine);
            }
        }

        private void DoFindMovie()
        {
            lvwFind.Items.Clear();

            if (cbFindOnly.Checked)
            {
                if (matchesAV == null || matchesAV.Count <= 0)
                {
                    matchesAV = GetAllMatched();
                }

                ShowMatchedMatch(txtFind.Text.Split(','));
            }
            else
            {
                ShowAllMatch(txtFind.Text.Split(','));
            }
        }

        private List<Model.ScanModels.Match> GetAllMatched()
        {
            return ScanDataBaseManager.GetAllMatch();
        }

        private void ShowMatchedMatch(string[] inputs)
        {
            lvwFind.BeginUpdate();

            foreach (var keyword in inputs)
            {
                var tempKeyword = keyword.Trim().ToUpper();

                foreach (var movie in matchesAV)
                {
                    if (movie.AvID.Trim().ToUpper().Contains(tempKeyword) || movie.AvID.Trim().ToUpper() == tempKeyword)
                    {
                        ListViewItem lvi = new ListViewItem(movie.AvID.Trim().ToUpper());

                        if (movie.Location.Length > 2 && movie.Location[1] != ':')
                        {
                            movie.Location = movie.Location.Substring(0, 1) + ":" + movie.Location.Substring(1);
                        }

                        if (File.Exists(movie.Location.Trim() + "\\" + movie.Name.Trim()))
                        {
                            var tempFi = new FileInfo(movie.Location.Trim() + "\\" + movie.Name.Trim());

                            lvi.SubItems.Add(FileSize.GetAutoSizeString(tempFi.Length, 2));
                            lvi.SubItems.Add(movie.Location.Trim() + "\\" + movie.Name.Trim());

                            lvi.Tag = movie.Location.Trim() + "\\" + movie.Name.Trim();

                            lvwFind.Items.Add(lvi);
                        }
                    }
                }
            }

            lvwFind.EndUpdate();
        }

        private void ShowAllMatch(string[] inputs)
        {
            lvwFind.BeginUpdate();

            var content = string.Join(" | ", inputs);

            var matchesAV = new EverythingHelper().SearchFile(content, EverythingSearchEnum.Video);

            foreach (var movie in matchesAV)
            {
                ListViewItem lvi = new ListViewItem(movie.Name.Replace(movie.Extension, ""));

                lvi.SubItems.Add(FileSize.GetAutoSizeString(movie.Length, 2));
                lvi.SubItems.Add(movie.FullName);

                lvi.Tag = movie.FullName;

                lvwFind.Items.Add(lvi);
            }

            lvwFind.EndUpdate();
        }

        private void Play(ListViewItem current)
        {
            if (current != null)
            {
                Process.Start(@"" + current.Tag);
                current.BackColor = Color.Green;
                ScanDataBaseManager.InsertViewHistory(FileUtility.ReplaceInvalidChar(current.Tag + ""));
            }
        }

        private void PlayPlist(string files)
        {
            Process.Start(@"" + files);
        }

        private void ChooseRecentFolder()
        {
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;

            var rs = folderBrowserDialog1.ShowDialog();

            if (rs == DialogResult.Yes || rs == DialogResult.OK)
            {
                txtRecent.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void ListRecentItems()
        {
            if (!string.IsNullOrEmpty(txtRecent.Text))
            {
                recentFi = new List<FileInfo>();
                lvwRecnet.Items.Clear();

                FileUtility.GetFilesRecursive(txtRecent.Text, formats, excludes, recentFi, 100);

                WhenClickRb();
                ShowContent();
            }
        }

        private void ShowContent()
        {
            lvwRecnet.Items.Clear();

            lvwRecnet.BeginUpdate();

            foreach (var file in recentFi)
            {
                ListViewItem lvi = new ListViewItem(file.DirectoryName);
                lvi.SubItems.Add(file.Name);
                lvi.SubItems.Add(FileSize.GetAutoSizeString(file.Length, 1));
                lvi.Tag = file.FullName;

                if (ScanDataBaseManager.ViewedFile(FileUtility.ReplaceInvalidChar(file.FullName)))
                {
                    lvi.BackColor = Color.Green;
                }

                lvwRecnet.Items.Add(lvi);
            }

            lvwRecnet.EndUpdate();
        }

        private void WhenClickRb()
        {
            if (recentFi != null && recentFi.Count > 0)
            {
                if (rbDateA.Checked)
                {
                    recentFi = recentFi.OrderBy(x => x.LastWriteTime).ToList();
                }

                if (rbDateD.Checked)
                {
                    recentFi = recentFi.OrderByDescending(x => x.LastWriteTime).ToList();
                }

                if (rbSizeA.Checked)
                {
                    recentFi = recentFi.OrderBy(x => x.Length).ToList();
                }

                if (rbSizeD.Checked)
                {
                    recentFi = recentFi.OrderByDescending(x => x.Length).ToList();
                }
            }
        }

        private void Delete(ListViewItem current)
        {
            var res = MessageBox.Show(string.Format("Do you want to delete {0}", current.Tag), "Warrning", MessageBoxButtons.YesNo);

            if (res == DialogResult.Yes)
            {
                File.Delete(current.Tag + "");

                listView1.Items.Remove(current);
            }
        }

        private async void StartReport()
        {
            await StartReportTask(OutputStartReport);
        }

        private async Task StartReportTask(DataReceivedEventHandler output)
        {
            var exe = @"G:\Github\AllInOneAV\AllInOneAV\GenerateReport\bin\Debug\GenerateReport.exe";
            var arg = "report";

            using (var p = new Process())
            {
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = arg;

                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;

                p.OutputDataReceived += OutputStartReport;

                p.Start();
                p.BeginOutputReadLine();
                await p.WaitForExitAsync();
            }
        }

        private void OutputStartReport(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                if (output.Data == "/" || output.Data == "\\" || output.Data == "|" || output.Data == "-")
                {
                    string[] sLines = rtbReport.Lines;
                    string[] sNewLines = new string[sLines.Length - 1];

                    Array.Copy(sLines, 0, sNewLines, 0, sNewLines.Length);

                    rtbReport.Lines = sNewLines;
                    rtbReport.AppendText(Environment.NewLine);

                    rtbReport.AppendText(output.Data);
                }
                else
                {
                    rtbReport.AppendText(output.Data + Environment.NewLine);
                }
            }
        }

        private void SelectAllUnmatchedSubTreeNode()
        {
            StringBuilder sb = new StringBuilder();

            if (treeView5.Nodes.Count > 0)
            {
                foreach (TreeNode tn in treeView5.Nodes)
                {
                    if (tn.Nodes != null && tn.Nodes.Count > 0)
                    {
                        foreach (TreeNode stn in tn.Nodes)
                        {
                            stn.Checked = true;

                            sb.AppendLine((string)stn.Tag);
                        }
                    }
                }
            }

            Clipboard.SetDataObject(sb.ToString());
        }

        private async void BtnMissingClick()
        {
            if (!string.IsNullOrEmpty(txtMissing.Text))
            {
                List<MissingCheckModel> list = new List<MissingCheckModel>();
                ilMissing.Images.Clear();
                lvMissing.LargeImageList = ilMissing;
                Random ran = new Random();

                if (rbMissingFavi.Checked)
                {
                    missingCheckForFavi = new List<MissingCheckModel>();

                    var asc = cbMissingAsc.Checked ? " true " : " false ";
                    int limit = 0;
                    
                    int.TryParse(txtMissingPage.Text, out limit);

                    var arg = " dolist " + txtMissing.Text + asc + limit;

                    await StartJavRefresh("", arg, OutputJavFaviRefresh);

                    list = missingCheckForFavi;
                }
                else
                {
                    var table = "";
                    var content = txtMissing.Text;

                    if (rbMissingActress.Checked)
                    {
                        table = "actress";
                    }
                    else if (rbMissingCate.Checked)
                    {
                        table = "category";
                    }
                    else if(rbMissingPrefix.Checked)
                    {
                        table = "prefix";
                    }

                    list = JavLibraryHelper.GetAllRelatedJav(table, content).Result;
                }

                foreach (var l in list)
                {
                    var pic = imageFolder + l.Av.ID + l.Av.Name + ".jpg";

                    if (File.Exists(pic))
                    {
                        ilMissing.Images.Add(l.Av.Name, Image.FromFile(pic));
                    }
                }

                pbMissing.Maximum = list.Count;

                Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = 5 }, rm =>
                {
                    ListViewItem lvi = new ListViewItem(rm.Av.ID + " " + rm.Av.Name);
                    lvi.ImageIndex = ilMissing.Images.IndexOfKey(rm.Av.Name);
                    lvi.SubItems.Add(rm.Av.ID);

                    var matchFiles = new EverythingHelper().SearchFile(rm.Av.ID + " | " + rm.Av.ID.Replace("-", ""), EverythingSearchEnum.Video);
                    rm.Fi = matchFiles;

                    var seedList = MagService.SearchSukebei(rm.Av.ID);

                    //if (seedList == null || seedList.Count <= 0)
                    //{
                    //    seedList = SearchSeedHelper.SearchBtsow(rm.Av.ID);
                    //}

                    if (seedList != null && seedList.Count > 0)
                    {
                        lvi.Tag = seedList;

                        ScanDataBaseManager.DeleteMagUrlById(rm.Av.ID);

                        foreach (var seed in seedList)
                        {
                            ScanDataBaseManager.InsertMagUrl(rm.Av.ID, seed.MagUrl, seed.Title, 1);
                        }

                        if (matchFiles.Count > 0)
                        {
                            lvi.BackColor = Color.Blue;

                            lvi.SubItems.Add(matchFiles.FirstOrDefault(x => x.Length == matchFiles.Max(y => y.Length)).FullName);

                            lvi.Text = lvi.Text + " " + FileSize.GetAutoSizeString(matchFiles.FirstOrDefault(x => x.Length == matchFiles.Max(y => y.Length)).Length, 1);
                        }
                        else
                        {
                            lvi.BackColor = Color.Green;
                        }
                    }
                    else
                    {
                        if (matchFiles.Count > 0)
                        {
                            lvi.BackColor = Color.Yellow;
                            lvi.SubItems.Add(matchFiles.FirstOrDefault(x => x.Length == matchFiles.Max(y => y.Length)).FullName);

                            lvi.Text = lvi.Text + " " + FileSize.GetAutoSizeString(matchFiles.FirstOrDefault(x => x.Length == matchFiles.Max(y => y.Length)).Length, 1);
                        }
                        else
                        {
                            lvi.BackColor = Color.Gray;
                        }
                    }

                    ListViewItemUpdate2(lvMissing, lvi);

                    JDuBar(pbMissing, lvMissing.Items.Count);

                    Thread.Sleep(100 * ran.Next(5));
                });
            }
        }

        private async void MissingSearch()
        {
            await Task.Run(() => BtnMissingClick());
        }

        private async void DoDailyRefesh(string pageStr)
        {
            lwDaily.Items.Clear();
            ilDaily.Images.Clear();
            refreshModel = new List<RefreshModel>();
            int page = 15;
            int.TryParse(pageStr, out page);
            var arg = " refresh " + page;

            pbDaily.Value = 0;
            pbDaily.Maximum = page * 20;

            await StartJavRefresh("", arg, OutputJavRefresh);

            await Task.Run(() => UpdateRefreshUi());
        }

        private void UpdateRefreshUi()
        {
            Random ran = new Random();

            Parallel.ForEach(refreshModel, new ParallelOptions { MaxDegreeOfParallelism = 10 }, rm =>
            {
                if (File.Exists(imageFolder + rm.Id + rm.Name + ".jpg"))
                {
                    ilDaily.Images.Add(rm.Name, Image.FromFile(imageFolder + rm.Id + rm.Name + ".jpg"));
                }
                else
                {
                    ilDaily.Images.Add(rm.Name, Image.FromFile(imageFolder + "noimage.gif"));
                }
            });

            Parallel.ForEach(refreshModel, new ParallelOptions { MaxDegreeOfParallelism = 5 }, rm =>
            {
                ListViewItem lvi = new ListViewItem(rm.Id + " " + rm.Name);
                lvi.ImageIndex = ilDaily.Images.IndexOfKey(rm.Name);
                lvi.SubItems.Add(rm.Id);

                var matchFiles = new EverythingHelper().SearchFile(rm.Id + " | " + rm.Id.Replace("-", ""), EverythingSearchEnum.Video);

                var list = MagService.SearchSukebei(rm.Id);

                //if (list == null || list.Count <= 0)
                //{
                //    list = SearchSeedHelper.SearchBtsow(rm.Id);
                //}

                if (list != null && list.Count > 0)
                {
                    lvi.Tag = list;

                    ScanDataBaseManager.DeleteMagUrlById(rm.Id);

                    foreach (var seed in list)
                    {
                        ScanDataBaseManager.InsertMagUrl(rm.Id, seed.MagUrl, seed.Title, 1);
                    }

                    if (matchFiles.Count > 0)
                    {
                        lvi.BackColor = Color.Blue;

                        lvi.SubItems.Add(matchFiles.FirstOrDefault(x => x.Length == matchFiles.Max(y => y.Length)).FullName);

                        lvi.Text = lvi.Text + " " + FileSize.GetAutoSizeString(matchFiles.FirstOrDefault(x => x.Length == matchFiles.Max(y => y.Length)).Length, 1);            
                    }
                    else
                    {
                        lvi.BackColor = Color.Green;

                        //if (OneOneFiveService.Get115SearchResult("", rm.Id))
                        //{
                        //    lvi.BackColor = Color.GreenYellow;
                        //}
                    }

                    ListViewItemUpdate2(lwDaily, lvi);
                }
                else
                {
                    //if (matchFiles.Count > 0)
                    //{
                    //    lvi.BackColor = Color.Yellow;
                    //    lvi.SubItems.Add(matchFiles.FirstOrDefault(x => x.Length == matchFiles.Max(y => y.Length)).FullName);

                    //    lvi.Text = lvi.Text + " " + FileSize.GetAutoSizeString(matchFiles.FirstOrDefault(x => x.Length == matchFiles.Max(y => y.Length)).Length, 1);
                    //}
                    //else
                    //{
                    //    lvi.BackColor = Color.Gray;
                    //}

                    pbDaily.Maximum -= 1;
                }

                JDuBar(pbDaily, lwDaily.Items.Count);

                Thread.Sleep(10 * ran.Next(5));
            });
        }

        private async Task StartJavRefresh(string exe, string arg, DataReceivedEventHandler output)
        {
            exe = "G:\\Github\\AllInOneAV\\AllInOneAV\\BatchJavScanerAndMacthMagUrl\\bin\\Debug\\BatchJavScanerAndMacthMagUrl.exe";

            using (var p = new Process())
            {
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = arg;

                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;

                p.OutputDataReceived += output;

                p.Start();
                p.BeginOutputReadLine();

                await p.WaitForExitAsync();
            }
        }

        private void OutputJavRefresh(object sendProcess, DataReceivedEventArgs output)
        {
            if (!string.IsNullOrEmpty(output.Data) && output.Data.StartsWith("AV:"))
            {
                var jsonStr = output.Data.Replace("AV:", "");

                RefreshModel rm = JsonConvert.DeserializeObject<RefreshModel>(jsonStr);

                refreshModel.Add(rm);
            }
        }

        private void OutputJavFaviRefresh(object sendProcess, DataReceivedEventArgs output)
        {
            if (!string.IsNullOrEmpty(output.Data) && output.Data.StartsWith("AV:"))
            {
                var jsonStr = output.Data.Replace("AV:", "");

                RefreshModel rm = JsonConvert.DeserializeObject<RefreshModel>(jsonStr);

                missingCheckForFavi.Add(new MissingCheckModel
                {
                    Av = new AV()
                    {
                        ID = rm.Id,
                        Name = rm.Name,
                        URL = rm.Url
                    },
                    Fi = new List<FileInfo>(),
                    IsMatch = false,
                    Seeds = new List<SeedMagnetSearchModel>()
                });
            }
        }

        private async void JavFaviScan()
        {
            await StartJavFaviScan(null);

            MessageBox.Show("操作完毕");
        }

        private async Task StartJavFaviScan(DataReceivedEventHandler output)
        {
            var exe = "G:\\Github\\AllInOneAV\\AllInOneAV\\BatchJavScanerAndMacthMagUrl\\bin\\Debug\\BatchJavScanerAndMacthMagUrl.exe";

            using (var p = new Process())
            {
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = "faviscan";

                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;

                p.OutputDataReceived += output;

                p.Start();
                p.BeginOutputReadLine();
                await p.WaitForExitAsync();
            }
        }

        private async void RefreshPlayUi()
        {
            var actress = await Task.Run(() => JavDataBaseManager.GetSimilarContent("actress").Select(x => x.Name).ToArray());
            var category = await Task.Run(() =>JavDataBaseManager.GetSimilarContent("category").Select(x => x.Name).ToArray());

            scanResult = await Task.Run(() => ScanDataBaseManager.GetMatchScanResult());
            scanResult = scanResult.Where(x => !string.IsNullOrEmpty(x.Location)).ToList();

            cbPlayActress.DataSource = actress;
            cbPlayCategory.DataSource = category;

            cbPlayActress.Text = "";
            cbPlayCategory.Text = "";
        }

        private async void BtnPlayClick(int page, int pageSize, bool isBack = false)
        {
            bool reOrder = false;
            lvPlay.Items.Clear();

            if (scanResult == null || scanResult.Count <= 0)
            {
                scanResult = await Task.Run(() =>ScanDataBaseManager.GetMatchScanResult());
                scanResult = scanResult.Where(x => !string.IsNullOrEmpty(x.Location)).ToList();

                reOrder = true;
            }

            toBePlay = scanResult;

            List<ScanResult> actressPlay = new List<ScanResult>();
            List<ScanResult> categoryPlay = new List<ScanResult>();
            List<ScanResult> prefixPlay = new List<ScanResult>();

            if (isBack)
            {
                cbPlayActress.Text = "";
                cbPlayCategory.Text = "";
                cbPlayPrefix.Text = "";
            }

            if (!string.IsNullOrEmpty(cbPlayActress.Text))
            {
                foreach (var r in scanResult)
                {
                    foreach (var actress in r.ActressList)
                    {
                        if (actress.Contains(cbPlayActress.Text))
                        {
                            actressPlay.Add(r);
                        }
                    }
                }

                toBePlay = toBePlay.Intersect(actressPlay).ToList();
            }

            if (!string.IsNullOrEmpty(cbPlayCategory.Text))
            {
                foreach (var r in scanResult)
                {
                    foreach (var category in r.CategoryList)
                    {
                        if (category.Contains(cbPlayCategory.Text))
                        {
                            categoryPlay.Add(r);
                        }
                    }
                }

                toBePlay = toBePlay.Intersect(categoryPlay).ToList();
            }

            if (!string.IsNullOrEmpty(cbPlayPrefix.Text))
            {
                var text = cbPlayPrefix.Text.ToUpper();

                foreach (var r in scanResult)
                {
                    if (r.Prefix == text)
                    {
                        prefixPlay.Add(r);
                    }
                }

                toBePlay = toBePlay.Intersect(prefixPlay).OrderBy(x => x.AvId).ToList();
            }

            if (reOrder)
            {
                toBePlay = toBePlay.OrderBy(x => Guid.NewGuid()).ToList();
            }

            lbPlayStatus.Text = "一共有: " + toBePlay.Count + " 条";

            var pageContent = toBePlay.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            lbPlayPage.Text = (toBePlay.Count % pageSize) == 0 ? page + " / " + (toBePlay.Count / pageSize) : page + " / " + ((toBePlay.Count / pageSize) + 1);

            await Task.Run(() => ShowPlayContent(pageContent));
        }

        private async void ShowPlayContent(List<ScanResult> list)
        {
            foreach (var l in list)
            {
                var pic = imageFolder + l.AvId + l.AvName + ".jpg";

                if (File.Exists(pic))
                {
                    ilPlay.Images.Add(l.AvName, Image.FromFile(pic));
                }
            }

            Progress<ListViewItem> progress = new Progress<ListViewItem>();
            progress.ProgressChanged += ReportProgress;

            ShowPlayContent(progress, list);

            //lvPlay.BeginUpdate();

            //foreach (var l in list)
            //{
            //    if (l.FileSize > 0)
            //    { 
            //        ListViewItem lvi = new ListViewItem(FileSize.GetAutoSizeString(new FileInfo(l.AvFilePath).Length, 2) + " " + l.AvId + " " + l.AvName + " " + l.MatchAvId)
            //        {
            //            ImageIndex = ilPlay.Images.IndexOfKey(l.AvName),
            //            Tag = l.AvFilePath
            //        };

            //        ListViewItemUpdate2(lvPlay, lvi);
            //    }
            //}

            //lvPlay.EndUpdate();
        }

        private void ReportProgress(object sender, ListViewItem e)
        {
            lvPlay.Items.Add(e);
        }

        private void ShowPlayContent(IProgress<ListViewItem> progrss, List<ScanResult> list)
        {
            foreach (var l in list)
            {
                if (l.FileSize > 0)
                {
                    ListViewItem lvi = new ListViewItem(FileSize.GetAutoSizeString(new FileInfo(l.AvFilePath).Length, 2) + " " + l.AvId + " " + l.AvName + " " + l.MatchAvId)
                    {
                        ImageIndex = ilPlay.Images.IndexOfKey(l.AvName),
                        Tag = l.AvFilePath
                    };

                    progrss.Report(lvi);
                }
            }
        }

        private void txtPlaySkipClick(object sender, KeyPressEventArgs e)
        {
            if (toBePlay != null && toBePlay.Count > 0 && e.KeyChar == (int)Keys.Enter)
            {
                int pageSize = int.Parse(txtPlayPageSize.Text);
                var pageArray = lbPlayPage.Text.Split('/');

                int current = 1;
                int total = 1;

                int.TryParse(txtPlaySkip.Text, out current);
                int.TryParse(pageArray[1], out total);

                if (current <= total && current >= 1)
                {
                    BtnPlayClick(current, pageSize);
                    lbPlayPage.Text = current + " / " + total;
                }
            }
        }

        private async void FindMove()
        {
            if (lvwRecnet.SelectedItems != null && lvwRecnet.SelectedItems.Count > 0)
            {
                double moveSize = 0;
                List<string> froms = new List<string>();
                foreach (ListViewItem lvi in lvwRecnet.SelectedItems)
                {
                    froms.Add((string)lvi.Tag);
                    moveSize += new FileInfo((string)lvi.Tag).Length;
                }

                var ret = MessageBox.Show($"确定要移动 {froms.Count} 个文件，总大小 {FileSize.GetAutoSizeString(moveSize, 1)} ?", "警告", MessageBoxButtons.YesNo);

                if (ret == DialogResult.Yes)
                {
                    var folder = folderBrowserDialog1.ShowDialog();

                    if (folder == DialogResult.Yes || folder == DialogResult.OK)
                    {
                        var to = folderBrowserDialog1.SelectedPath;

                        if (!string.IsNullOrEmpty(to))
                        {
                            await Task.Run(() =>
                            {
                                FileUtility.TransferFileUsingSystem(froms, to, true);
                            });
                        }
                    }
                }
            }
        }
        #endregion

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {

        }

        private void rtbReport_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            rtbReport.SelectionStart = rtbReport.Text.Length;
            rtbReport.ScrollToCaret();
        }
    }

    #region 扩展方法

    public static class ProcessExtensions
    {
        public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            if (cancellationToken != default(CancellationToken))
            {
                cancellationToken.Register(tcs.SetCanceled);
            }

            return tcs.Task;
        }
    }

    public static class RichTextBoxColorExtensions
    {
        public static void AppendText(this RichTextBox rtb, string text, Color color, Font font, bool isNewLine = false)
        {
            rtb.SuspendLayout();
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;

            rtb.SelectionColor = color;
            rtb.SelectionFont = font;
            rtb.AppendText(isNewLine ? $"{text}{ Environment.NewLine}" : text);
            rtb.SelectionColor = rtb.ForeColor;
            rtb.ScrollToCaret();
            rtb.ResumeLayout();
        }
    }

    #endregion
}