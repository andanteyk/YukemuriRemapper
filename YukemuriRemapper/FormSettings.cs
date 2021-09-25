using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using YukemuriRemapper.Model;

namespace YukemuriRemapper
{
    public partial class FormSettings : Form
    {
        readonly Configuration Configuration;
        readonly int ConfigurationIndex;
        public Configuration Result { get; private set; }

        private Configuration CurrentConfiguration => Configuration.Configurations[ConfigurationIndex];

        public FormSettings()
        {
            InitializeComponent();
        }

        public FormSettings(Configuration configuration, int index) : this()
        {
            Configuration = configuration.Clone();
            ConfigurationIndex = index;
        }

        private void FormSettings_Load(object sender, EventArgs e)
        {
            if (ConfigurationIndex >= Configuration.Configurations.Count)
                Configuration.Configurations.Add(new Configuration());

            var proc = Configuration.Configurations[ConfigurationIndex];

            ConfigurationName.Text = proc.Name;
            ProcessName.Text = proc.ProcessName;

            foreach (var bind in proc.KeyBinds)
            {
                var row = new DataGridViewRow();
                row.CreateCells(dataGridView1);

                row.SetValues(bind.From, bind.To);

                dataGridView1.Rows.Add(row);
            }
        }


        private void ProcessName_DropDown(object sender, EventArgs e)
        {
            ProcessName.Items.Clear();
            ProcessName.Items.AddRange(Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                .Select(p => p.ProcessName)
                .OrderBy(s => s)
                .ToArray());
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProcessName.Text))
            {
                MessageBox.Show("`Process Name` must not be empty.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CurrentConfiguration.Name = ConfigurationName.Text;
            CurrentConfiguration.ProcessName = ProcessName.Text;

            Result = Configuration;
            DialogResult = DialogResult.OK;
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Result = null;
            DialogResult = DialogResult.Cancel;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (e.ColumnIndex == Column_From.Index || e.ColumnIndex == Column_To.Index)
            {
                var proc = CurrentConfiguration;

                using var dialog = new FormKeyReader();
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (e.RowIndex >= proc.KeyBinds.Count)
                    {
                        proc.KeyBinds.Add(new KeyBind());
                        dataGridView1.Rows.Add();
                    }

                    if (e.ColumnIndex == Column_From.Index)
                    {
                        proc.KeyBinds[e.RowIndex].From = dialog.Result;
                    }
                    else
                    {
                        proc.KeyBinds[e.RowIndex].To = dialog.Result;
                    }
                    dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = dialog.Result;
                }
            }
            if (e.ColumnIndex == Column_Delete.Index && e.RowIndex < CurrentConfiguration.KeyBinds.Count)
            {
                CurrentConfiguration.KeyBinds.RemoveAt(e.RowIndex);
                dataGridView1.Rows.RemoveAt(e.RowIndex);
            }
        }

        private void dataGridView1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            Debug.WriteLine(e.KeyCode);
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (e.ColumnIndex == Column_From.Index || e.ColumnIndex == Column_To.Index)
            {
                if (e.RowIndex >= CurrentConfiguration.KeyBinds.Count)
                {
                    e.Value = "";
                }
                else
                {
                    if (e.Value is int key)
                    {
                        e.Value = $"{GetKeyCodeString(key)} (0x{key:x2})";
                    }
                    else
                    {
                        e.Value = "";
                    }
                }
                e.FormattingApplied = true;
            }
        }


        public static string GetKeyCodeString(int keycode)
        {
            // based on JIS (JP) keyboard
            return keycode switch
            {
                0xf3 => "全角/半角",
                0xbd => "-",
                0xde => "^",
                0xdc => "￥",
                0xc0 => "@",
                0xdb => "[",
                0xf0 => "CapsLock",
                0xbb => ";",
                0xba => ":",
                0xdd => "]",
                0xbc => ",",
                0xbe => ".",
                0xbf => "/",
                0xe2 => "＼",
                0xa4 => "LAlt",
                0x1d => "無変換",
                0x1c => "変換",
                0xf2 => "かな",
                0xa5 => "RAlt",
                0x22 => "PageDown",
                _ => ((Keys)keycode).ToString(),
            };
        }
    }
}
