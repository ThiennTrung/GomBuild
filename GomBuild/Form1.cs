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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
//using EverythingSearchClient;

namespace GomBuild
{
    public partial class Form1 : Form
    {
        public List<JsonString> JsonString = new List<JsonString>();
        public List<FileInfo> lstCurrent = new List<FileInfo>();
        public List<DupFile> lstFileTrung = new List<DupFile>();
        StringBuilder mess = new StringBuilder();
        public Form1()
        {
            InitializeComponent();

            //SearchClient everything = new();
            //Result res = everything.Search( "C:\\Users\\ADMIN\\FPT Corporation\\Tu Vuong Quang - HM_UPBUILD\\  BBM16P00101_TheoDoiTruyenMau",f);
            //foreach (Result.Item item in res.Items)
            //{
            //    textBox3.Text += item.Name + "\n";
            //}

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
                openFileDialog.Filter = "files (*.gz;*.rdl;*.rdlx;*.sql)|*.gz;*.rdl;*.rdlx;*.sql|All files (*.*)|*.*";
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
            bool check = false;
            foreach (DataGridViewRow item in dataGridView1.Rows)
            {
                if (string.IsNullOrEmpty(item.Cells[2].FormattedValue.ToString()))
                {
                    check = true;
                    break;
                }
            }
            if (check)
            {
                MessageBox.Show("Chọn loại file kìa....");
                return;
            }



            var dev = comboBox2.Text;
            string path = Path.Combine(textBox1.Text, comboBox1.Text, "4.FOLDER_BACKUP");
            bool exists = System.IO.Directory.Exists(Path.Combine(path, dev, textBox2.Text));
            if (!exists) { System.IO.Directory.CreateDirectory(Path.Combine(path, dev, textBox2.Text)); }

            if(checkBox1.Checked)
                mess.Append("*HOTFIX ");

            if (!string.IsNullOrEmpty(comboBox3.Text))
                mess.Append(comboBox3.Text + "\n");

            mess.Append("- JIRA: " + textBox2.Text + "\n");
            mess.Append("- BUILD: " + comboBox1.Text + "\n");
           
            if (!string.IsNullOrEmpty(textBox3.Text))
                mess.Append("- NỘI DUNG: " + textBox3.Text + "\n");
            mess.Append("- FILE: " + "\n");

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
                DateTime a = DateTime.Now;
                for (int i = 0; i < textBox4.Lines.Length; i++)
                {
                    if (!string.IsNullOrEmpty(textBox4.Lines[i]))
                    {
                        string sqlinsert = "INSERT INTO APPSETTINGS (DEV,CODE,BUILD,JIRA,[TIME]) VALUES (@a,@b,@c,@d,'" + a + "')";
                        add.Parameters.AddWithValue("@a", comboBox2.Text);
                        add.Parameters.AddWithValue("@b", textBox4.Lines[i]);
                        add.Parameters.AddWithValue("@c", comboBox1.Text);
                        add.Parameters.AddWithValue("@d", textBox2.Text);
                        add.CommandText = sqlinsert;
                        add.ExecuteNonQuery();
                        add.Parameters.Clear();
                    }
                }

                foreach (DataGridViewRow item in dataGridView1.Rows)
                {
                    string pathfile = item.Cells[0].Value.ToString();
                    var Filename = item.Cells[3].Value.ToString();

                    string type = item.Cells[2].FormattedValue.ToString();
                    string Extend = item.Cells[1].FormattedValue.ToString();

                    bool isDuplicate = (bool)(item.Cells[6] as DataGridViewCheckBoxCell).Value;
                    bool isOverride = (bool)(item.Cells[7] as DataGridViewCheckBoxCell).Value && isDuplicate;
                    bool isAddNew = (bool)(item.Cells[8] as DataGridViewCheckBoxCell).Value && isDuplicate;

                    var ssss = (item.Cells[2].Value is null) ? _TypeFile.Other : (_TypeFile)item.Cells[2].Value;

                    mess.Append("    + " + Filename + "\n");

                    //gen new file name
                    if (isDuplicate && isAddNew)
                    {
                        Filename = string.Format("{0}_{1}_{2}{3}", System.IO.Path.GetFileNameWithoutExtension(Filename), comboBox2.Text, DateTime.Now.ToString("ddMMyy"), Extend);
                    }

                    //Move file to detail folder
                    string pathdes = Path.Combine(path, dev, textBox2.Text) + @"\" + Filename;
                    CopyFileByExtend(pathfile, Filename, ssss);

                    //Move file to folder backup
                    System.IO.File.Copy(pathfile, pathdes, true);

                    //Hotfix
                    if (checkBox1.Checked)
                    {
                        string pathHothix = Path.Combine(textBox1.Text, comboBox1.Text, "6.HOTFIX");
                        if (!System.IO.Directory.Exists(pathHothix)) { System.IO.Directory.CreateDirectory(pathHothix); }
                        System.IO.File.Copy(pathfile, pathHothix + @"\" + Filename, true);
                    }


                    if (!string.IsNullOrEmpty(textBox3.Text))
                    {
                        System.IO.File.WriteAllText(Path.Combine(path, dev, textBox2.Text, "01 - README.txt"), textBox3.Text, Encoding.UTF8);
                    }
                    if (textBox2.Text.StartsWith("HM_")) 
                    {
                        using (StreamWriter writer = new StreamWriter(Path.Combine(path, dev, textBox2.Text, "02 - URL.url")))
                        {
                            writer.WriteLine("[{000214A0-0000-0000-C000-000000000046}]");
                            writer.WriteLine("Prop3=19,11");
                            writer.WriteLine("[InternetShortcut]");
                            writer.WriteLine("IDList=");
                            writer.WriteLine("URL=" + string.Format("https://jira.fis.com.vn/browse/{0}", textBox2.Text));
                            writer.Flush();
                        }
                    }
                    string sql = "INSERT INTO LOG (DEV,FILENAME,BUILD_VER,TYPE,JIRA,HOTFIX,SITE,CONTENT,OVERRIDE,NEW_VER,[TIME]) VALUES (@a,@b,@bb,@c,@e,@f,@g,@h,@i,@j,'" + a+ "')"; 
                    add.Parameters.AddWithValue("@a", comboBox2.Text);
                    add.Parameters.AddWithValue("@b", Filename);
                    add.Parameters.AddWithValue("@bb", comboBox1.Text);
                    add.Parameters.AddWithValue("@c", type);
                    add.Parameters.AddWithValue("@e", "https://jira.fis.com.vn/browse/" + textBox2.Text);
                    add.Parameters.AddWithValue("@f", checkBox1.Checked);
                    add.Parameters.AddWithValue("@g", comboBox3.Text);
                    add.Parameters.AddWithValue("@h", textBox3.Text);

                    add.Parameters.AddWithValue("@i", isOverride);
                    add.Parameters.AddWithValue("@j", isAddNew);

                    add.CommandText = sql;
                    add.ExecuteNonQuery();
                    add.Parameters.Clear();
                }
                conn.Close();

                var formRes = MessageBox.Show(mess.ToString(), "Xong rồi đó (OK = copy text)");
                if(formRes == DialogResult.OK)
                {
                    System.Windows.Forms.Clipboard.SetText(mess.ToString());
                    ClearForm();
                }
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
        private void CopyFileByExtend(string pathfile, string filename,_TypeFile type , bool o = true)
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
                    System.IO.File.Copy(pathfile, ReportFolder + @"\" + filename, o);
                    break;
                case _TypeFile.Form:
                    if (!System.IO.Directory.Exists(FormFolder)) { System.IO.Directory.CreateDirectory(FormFolder); }
                    System.IO.File.Copy(pathfile, FormFolder + @"\" + filename, o);
                    break;
                case _TypeFile.Template:
                    if (!System.IO.Directory.Exists(TempFolder)) { System.IO.Directory.CreateDirectory(TempFolder); }
                    System.IO.File.Copy(pathfile, TempFolder + @"\" + filename, o);
                    break;
                case _TypeFile.Script:
                    if (!System.IO.Directory.Exists(ScriptFolder)) { System.IO.Directory.CreateDirectory(ScriptFolder); }
                    System.IO.File.Copy(pathfile, ScriptFolder + @"\" + filename, o);
                    break;
                case _TypeFile.Stored:
                    if (!System.IO.Directory.Exists(StoredFolder)) { System.IO.Directory.CreateDirectory(StoredFolder); }
                    System.IO.File.Copy(pathfile, StoredFolder + @"\" + filename, o);
                    break;
                case _TypeFile.StoredNonReport:
                    if (!System.IO.Directory.Exists(StoredNonreportFolder)) { System.IO.Directory.CreateDirectory(StoredNonreportFolder); }
                    System.IO.File.Copy(pathfile, StoredNonreportFolder + @"\" + filename, o);
                    break;
                case _TypeFile.Other:
                    if (!System.IO.Directory.Exists(OtherFolder)) { System.IO.Directory.CreateDirectory(OtherFolder); }
                    System.IO.File.Copy(pathfile, OtherFolder + @"\" + filename, o);
                    break;
                default:
                    if (!System.IO.Directory.Exists(OtherFolder)) { System.IO.Directory.CreateDirectory(OtherFolder); }
                    System.IO.File.Copy(pathfile, OtherFolder + @"\" + filename, o);
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
            mess.Clear();
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
                    usePaths = new string[] { StoredFolder, StoredNonreportFolder, OtherFolder };
                    break;
            }
            foreach (string usePath in usePaths)
            {
                if (!System.IO.Directory.Exists(usePath))
                    return dupf;
                var dir = new DirectoryInfo(usePath);
                FileInfo[] files = dir.GetFiles();
                if (files.Any(x => x.Name.Equals(oFileInfo.Name)))
                {
                    dupf.NAME = oFileInfo.Name;
                    dupf.EXTEND = extend;
                    dupf.SOURCEPATH = oFileInfo.FullName;
                    dupf.BUILDPATH = Path.Combine(usePath, oFileInfo.Name);
                    break;
                }
            }

            return dupf;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Column1")
            {
                string LOG_PATH = ConfigurationManager.AppSettings["LOG_PATH"].ToString();
                string ConnectionString = @"Provider=Microsoft.ACE.OLEDB.16.0;" + @"Data source= " + LOG_PATH + "";

                using (OleDbConnection connection = new OleDbConnection(ConnectionString))
                {
                    connection.Open();
                    OleDbDataReader reader = null;
                    OleDbCommand command = new OleDbCommand("SELECT TOP 1 * from LOG WHERE FILENAME = @a and BUILD_VER = @b order by [TIME] DESC", connection);
                    command.Parameters.AddWithValue("@a", dataGridView1.Rows[e.RowIndex].Cells[3].Value.ToString());
                    command.Parameters.AddWithValue("@a", comboBox1.Text);
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        StringBuilder mess= new StringBuilder();
                        mess.AppendLine("DEV: " + reader["DEV"].ToString());
                        mess.AppendLine("JIRA: "+ reader["JIRA"].ToString());
                        mess.AppendLine("NỘI DUNG: " + reader["CONTENT"].ToString());
                        mess.AppendLine("TIME: " + reader["TIME"].ToString());
                        
                        if (MessageBox.Show(mess.ToString(), "DEV commit cuối (Yes = copy jira)", MessageBoxButtons.YesNo, MessageBoxIcon.Question
                                ) == DialogResult.Yes)
                        {
                            System.Windows.Forms.Clipboard.SetText(reader["JIRA"].ToString());
                        }
                    }
                }


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