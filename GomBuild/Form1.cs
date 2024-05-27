using GomBuild.Model;
using System.Configuration;
using Newtonsoft.Json;
using System.Data.OleDb;
using System.Data;
using System.Text;
using System.IO;
using System.Linq.Expressions;
using System.Xml.Linq;
using System.Net.WebSockets;
using System.Diagnostics;
using System.Windows.Forms;

namespace GomBuild
{
    public partial class Form1 : Form
    {
        public List<JsonString> JsonString = new List<JsonString>();
        public List<FileInfo> lstCurrent = new List<FileInfo>();
        public List<DupFile> lstFileTrung = new List<DupFile>();
        public Form1()
        {
            InitializeComponent();
            List<TypeFile> typeFiles = new List<TypeFile>()
            {
                new TypeFile { ID = (int)_TypeFile.Stored, NAME = "Stored" }, new TypeFile { ID = (int)_TypeFile.StoredNonReport, NAME = "StoredNonReport" },
                new TypeFile { ID = (int)_TypeFile.Report, NAME = "Report" }, new TypeFile { ID = (int)_TypeFile.Form, NAME = "Form" },
                new TypeFile { ID = (int)_TypeFile.Script, NAME = "Script" }, new TypeFile { ID = (int)_TypeFile.Template, NAME = "Template" }, new TypeFile { ID = (int)_TypeFile.Other, NAME = "Other" }
            };
            List<TypeFile> List = typeFiles;

            (dataGridView1.Columns["Type"] as DataGridViewComboBoxColumn).DataSource = List;
            (dataGridView1.Columns["Type"] as DataGridViewComboBoxColumn).DisplayMember = "NAME";
            (dataGridView1.Columns["Type"] as DataGridViewComboBoxColumn).ValueMember = "ID";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(comboBox1.Text))
            {
                MessageBox.Show("Chọn build ver trước. không biết build nào thì hỏi OanhDH2 hộ em");
                return;
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "files (*.gz;*.rdl;*.rdlx;*.txt;*.sql)|*.gz;*.rdl;*.rdlx;*.txt;*.sql|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (String file in openFileDialog.FileNames)
                    {
                        System.IO.FileInfo oFileInfo = new System.IO.FileInfo(file);

                        // kiểm tra đã tồn tại trong list hiện tại chưa
                        if (lstCurrent.Any(x => x.Name == oFileInfo.Name))
                            continue;

                        // Kiểm tra có trùng trong folder build chưa

                        var duplicate = CheckTrungfile(oFileInfo, oFileInfo.Extension);
                        string status = "OK";
                        bool ISDUP = false;
                        bool Override = false;
                        if (!string.IsNullOrEmpty(duplicate.NAME))
                        {
                            status = "Duplicate";
                            ISDUP = true;
                            Override= true;
                        }
                        lstCurrent.Add(oFileInfo);
                        this.dataGridView1.Rows.Add(oFileInfo.FullName, oFileInfo.Extension, GetTypeFile(oFileInfo.Extension), oFileInfo.Name, status, duplicate.BUILDPATH, ISDUP, Override, false);
                    }
                }

            }
        }


        private int? GetTypeFile(string extend)
        {
            int? res = null;
            switch (extend)
            {
                case ".rdl":
                case ".rdlx":
                    res = (int)_TypeFile.Report;
                    break;
                case ".gz":
                    res = (int)_TypeFile.Form;
                    break;
                case ".xls":
                case ".xlsx":
                    res = (int)_TypeFile.Template;
                    break;
            }
            return res;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("Nhập mã jira vô");
                return;
            }
            if (string.IsNullOrEmpty(comboBox1.Text))
            {
                MessageBox.Show("Chọn build ver. không biết build nào thì hỏi OanhDH2 hộ em");
                return;
            }
            if(dataGridView1.RowCount <= 0)
            {
                MessageBox.Show("Có file nào đâu mà commit");
                return;
            }


            var dev = comboBox2.Text;
            string path = Path.Combine(textBox1.Text, comboBox1.Text, "4.FOLDER_BACKUP");
            bool exists = System.IO.Directory.Exists(Path.Combine(path, dev, textBox2.Text));
            if (!exists) { System.IO.Directory.CreateDirectory(Path.Combine(path, dev, textBox2.Text)); }


            string LOG_PATH = ConfigurationManager.AppSettings["LOG_PATH"].ToString();
            
            OleDbConnection conn = new OleDbConnection
            {
                ConnectionString = @"Provider=Microsoft.ACE.OLEDB.16.0;" + @"Data source= "+ LOG_PATH + ""
            };
            OleDbCommand add = new OleDbCommand();
            add.Connection = conn;
            try
            {
                add.Connection.Open();
                foreach (DataGridViewRow item in dataGridView1.Rows)
                {
                    string name = item.Cells[0].Value.ToString();
                    string pathdes = Path.Combine(path, dev, textBox2.Text) + @"\" + System.IO.Path.GetFileName(name);
                    var Filename = item.Cells[3].Value.ToString();
                    string type = item.Cells[2].FormattedValue.ToString();

                    bool isDuplicate = (bool)(item.Cells[6] as DataGridViewCheckBoxCell).Value;
                    bool isOverride = (bool)(item.Cells[7] as DataGridViewCheckBoxCell).Value;
                    bool isAddNew = (bool)(item.Cells[8] as DataGridViewCheckBoxCell).Value;

                    var ssss = (item.Cells[2].Value is null) ? _TypeFile.Other : (_TypeFile)item.Cells[2].Value;

                    //Move file to detail folder
                    CopyFileByExtend(name, ssss);

                    //Move file to folder backup
                    System.IO.File.Copy(name, pathdes, true);

                    //Hotfix
                    if (checkBox1.Checked)
                    {
                        string pathHothix = Path.Combine(textBox1.Text, comboBox1.Text, "6.HOTFIX");
                        if (!System.IO.Directory.Exists(pathHothix)) { System.IO.Directory.CreateDirectory(pathHothix); }
                        System.IO.File.Copy(name, pathHothix + @"\" + System.IO.Path.GetFileName(name), true);
                    }


                    if (!string.IsNullOrEmpty(textBox3.Text))
                    {
                        System.IO.File.WriteAllText(Path.Combine(path, dev, textBox2.Text, "01 - README.txt"), textBox3.Text, Encoding.UTF8);
                    }
                    using (StreamWriter writer = new StreamWriter(Path.Combine(path, dev, textBox2.Text, "02 - URL.url")))
                    {
                        writer.WriteLine("[{000214A0-0000-0000-C000-000000000046}]");
                        writer.WriteLine("Prop3=19,11");
                        writer.WriteLine("[InternetShortcut]");
                        writer.WriteLine("IDList=");
                        writer.WriteLine("URL=" + string.Format("https://jira.fis.com.vn/browse/{0}", textBox2.Text));
                        writer.Flush();
                    }

                    DateTime a = DateTime.Now;
                    string sql = "INSERT INTO LOG (DEV,FILENAME,BUILD_VER,TYPE,JIRA,HOTFIX,SITE,CONTENT,[TIME]) VALUES (@a,@b,@bb,@c,@e,@f,@g,@h,'" +a+ "')"; 
                    add.Parameters.AddWithValue("@a", comboBox2.Text);
                    add.Parameters.AddWithValue("@b", Filename);
                    add.Parameters.AddWithValue("@bb", comboBox1.Text);
                    add.Parameters.AddWithValue("@c", type);
                    add.Parameters.AddWithValue("@e", "https://jira.fis.com.vn/browse/" + textBox2.Text);
                    add.Parameters.AddWithValue("@f", checkBox1.Checked);
                    add.Parameters.AddWithValue("@g", comboBox3.Text);
                    add.Parameters.AddWithValue("@h", textBox3.Text);
                    //add.Parameters.Add("@i", OleDbType.Date);

                    add.CommandText = sql;
                    add.ExecuteNonQuery();
                    add.Parameters.Clear();
                }
                conn.Close();
                //clear
                ClearForm();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();

            }
        }
        private void CopyFileByExtend(string pathfile, _TypeFile type , bool o = true)
        {
            string FormFolder = Path.Combine(textBox1.Text, comboBox1.Text, "1.FORM");
            string ReportFolder = Path.Combine(textBox1.Text, comboBox1.Text, "2.REPORT");
            string TempFolder = Path.Combine(textBox1.Text, comboBox1.Text, "5.TEMPLATE");
            string ScriptFolder = Path.Combine(textBox1.Text, comboBox1.Text, "3.SQL", "SCRIPT");
            string StoredFolder = Path.Combine(textBox1.Text, comboBox1.Text, "3.SQL", "STORED");
            string StoredNonreportFolder = Path.Combine(textBox1.Text, comboBox1.Text, "3.SQL", "STOREDNonREPORT");
            string OtherFolder = Path.Combine(textBox1.Text, comboBox1.Text, "3.SQL");

            switch (type)
            {
                case _TypeFile.Report:
                    if (!System.IO.Directory.Exists(ReportFolder)) { System.IO.Directory.CreateDirectory(ReportFolder); }
                    System.IO.File.Copy(pathfile, ReportFolder + @"\" + System.IO.Path.GetFileName(pathfile), o);
                    break;
                case _TypeFile.Form:
                    if (!System.IO.Directory.Exists(FormFolder)) { System.IO.Directory.CreateDirectory(FormFolder); }
                    System.IO.File.Copy(pathfile, FormFolder + @"\" + System.IO.Path.GetFileName(pathfile), o);
                    break;
                case _TypeFile.Template:
                    if (!System.IO.Directory.Exists(TempFolder)) { System.IO.Directory.CreateDirectory(TempFolder); }
                    System.IO.File.Copy(pathfile, TempFolder + @"\" + System.IO.Path.GetFileName(pathfile), o);
                    break;
                case _TypeFile.Script:
                    if (!System.IO.Directory.Exists(ScriptFolder)) { System.IO.Directory.CreateDirectory(ScriptFolder); }
                    System.IO.File.Copy(pathfile, ScriptFolder + @"\" + System.IO.Path.GetFileName(pathfile), o);
                    break;
                case _TypeFile.Stored:
                    if (!System.IO.Directory.Exists(StoredFolder)) { System.IO.Directory.CreateDirectory(StoredFolder); }
                    System.IO.File.Copy(pathfile, StoredFolder + @"\" + System.IO.Path.GetFileName(pathfile), o);
                    break;
                case _TypeFile.StoredNonReport:
                    if (!System.IO.Directory.Exists(StoredNonreportFolder)) { System.IO.Directory.CreateDirectory(StoredNonreportFolder); }
                    System.IO.File.Copy(pathfile, StoredNonreportFolder + @"\" + System.IO.Path.GetFileName(pathfile), o);
                    break;
                case _TypeFile.Other:
                    if (!System.IO.Directory.Exists(OtherFolder)) { System.IO.Directory.CreateDirectory(OtherFolder); }
                    System.IO.File.Copy(pathfile, OtherFolder + @"\" + System.IO.Path.GetFileName(pathfile), o);
                    break;
                default:
                    if (!System.IO.Directory.Exists(OtherFolder)) { System.IO.Directory.CreateDirectory(OtherFolder); }
                    System.IO.File.Copy(pathfile, OtherFolder + @"\" + System.IO.Path.GetFileName(pathfile), o);
                    break;
            }
        }
        private void ClearForm()
        {
            lstCurrent.Clear();
            dataGridView1.Rows.Clear();
            lstFileTrung.Clear();
            textBox3.Text = string.Empty;
            textBox4.Text = string.Empty;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            string apiUrl = ConfigurationManager.AppSettings["API_SQLJSON"].ToString();
            string Master = ConfigurationManager.AppSettings["API_MASTER"].ToString();
            string Access = ConfigurationManager.AppSettings["API_ACCSESS"].ToString();
            string json = await FetchDataFromAPIWithHeaders(apiUrl, Master, Access);
            JsonString = JsonConvert.DeserializeObject<List<JsonString>>(json);

            string PathCommit = ConfigurationManager.AppSettings["PathCommit"];


            JsonString dev = JsonString.Where(x => x.code.Equals("DEV")).First();
            foreach (var item in dev.value.Split(","))
            {
                comboBox2.Items.Add(item);
            }
            JsonString buildver = JsonString.Where(x => x.code.Equals("BUILDVER")).First();
            foreach (var item2 in buildver.value.Split(","))
            {
                comboBox1.Items.Add(item2);
            }
            JsonString site = JsonString.Where(x => x.code.Equals("SITE")).First();
            foreach (var item3 in site.value.Split(","))
            {
                comboBox3.Items.Add(item3);
            }

            string DEV = ConfigurationManager.AppSettings["DEV"];
            textBox1.Text = PathCommit;
            comboBox2.SelectedIndex = comboBox2.Items.IndexOf(DEV);
        }
        static async Task<string> FetchDataFromAPIWithHeaders(string apiUrl, string Master, string Access)
        {
            string responseData = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("X-Master-Key", Master);
                    client.DefaultRequestHeaders.Add("X-Access-Key", Access);
                    client.DefaultRequestHeaders.Add("X-BIN-META", "false");
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        responseData = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                }
            }
            return responseData;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //lstCurrent.Clear();
            //dataGridView1.Rows.Clear();
            //lstFileTrung.Clear();
            ClearForm();
        }

        private DupFile CheckTrungfile(FileInfo oFileInfo, string extend)
        {
            DupFile dupf = new DupFile();
            string FormFolder = Path.Combine(textBox1.Text, comboBox1.Text, "1.FORM");
            string ReportFolder = Path.Combine(textBox1.Text, comboBox1.Text, "2.REPORT");
            string StoredFolder = Path.Combine(textBox1.Text, comboBox1.Text, "3.SQL", "STORED");
            string StoredNonreportFolder = Path.Combine(textBox1.Text, comboBox1.Text, "3.SQL", "STOREDNonREPORT");
            string OtherFolder = Path.Combine(textBox1.Text, comboBox1.Text, "3.SQL");

            if (!System.IO.Directory.Exists(ReportFolder))
                return dupf;

            string[] usePaths = new string[3];
            switch (extend)
            {
                case ".gz":
                    usePaths = new string[] { FormFolder };
                    break;
                case ".rdl":
                case ".rdlx":
                    usePaths = new string[] { ReportFolder };
                    break;
                case ".sql":
                case ".txt":
                    usePaths = new string[] { StoredFolder, StoredNonreportFolder, OtherFolder };
                    break;
            }
            foreach (string usePath in usePaths)
            {
                var dir = new DirectoryInfo(usePath);
                FileInfo[] files = dir.GetFiles();
                if (files.Any(x => x.Name.Equals(oFileInfo.Name)))
                {
                    dupf.NAME = oFileInfo.Name;
                    dupf.EXTEND = extend;
                    dupf.SOURCEPATH = oFileInfo.FullName;
                    dupf.BUILDPATH = Path.Combine(ReportFolder, oFileInfo.Name);
                    break;
                }
            }

            return dupf;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Column1")
            {
                

                
            }
            else if (dataGridView1.Columns[e.ColumnIndex].Name == "Column3") // override
            {
                dataGridView1.Rows[e.RowIndex].Cells[8].Value = false;

            }
            else if (dataGridView1.Columns[e.ColumnIndex].Name == "Column4") // newver
            {
                dataGridView1.Rows[e.RowIndex].Cells[7].Value = false;
            }
        }
    }
}