using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using YukemuriRemapper.Controller;
using YukemuriRemapper.Model;
using YukemuriRemapper.Properties;

namespace YukemuriRemapper
{
    public partial class FormMain : Form
    {
        Configuration Configuration;

        readonly List<Hook> Hooks = new();


        public FormMain()
        {
            InitializeComponent();

            Icon = notifyIcon1.Icon = Resources.YukemuriRemapper;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            try
            {
                Configuration = Configuration.Load(Configuration.DefaultPath);
            }
            catch (Exception)
            {
                Configuration = new Configuration();
            }
            finally
            {
                UpdateView();
                UpdateHook();
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Configuration.Save(Configuration.DefaultPath, Configuration);

            if (e.CloseReason == CloseReason.UserClosing)
            {
                Hide();
                e.Cancel = true;
                return;
            }

            // termination
            foreach (var hook in Hooks)
                hook.Dispose();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex == Column_Edit.Index)
            {
                using var dialog = new FormSettings(Configuration, e.RowIndex);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    Configuration = dialog.Result;
                    UpdateView();
                }
                UpdateHook();
            }
            if (e.ColumnIndex == Column_Delete.Index && e.RowIndex < Configuration.Configurations.Count)
            {
                if (MessageBox.Show("Are you sure you want to delete it?", "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    Configuration.Configurations.RemoveAt(e.RowIndex);
                    dataGridView1.Rows.RemoveAt(e.RowIndex);

                    UpdateHook();
                }
            }
        }
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == Column_IsEnabled.Index && 0 <= e.RowIndex && e.RowIndex < Configuration.Configurations.Count)
            {
                Configuration.Configurations[e.RowIndex].IsEnabled = (bool)dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            }
        }

        private void UpdateView()
        {
            dataGridView1.Rows.Clear();

            foreach (var proc in Configuration.Configurations)
            {
                var row = new DataGridViewRow();
                row.CreateCells(dataGridView1);

                row.SetValues(proc.IsEnabled, proc.Name, "");

                dataGridView1.Rows.Add(row);
            }
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateHook();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
        }


        private void UpdateHook()
        {
            foreach (var hook in Hooks)
                hook.Dispose();

            Hooks.Clear();

            foreach (var config in Configuration.Configurations.Where(c => c.IsEnabled && !string.IsNullOrEmpty(c.ProcessName)))
            {
                foreach (var proc in Process.GetProcessesByName(config.ProcessName))
                {
                    Hooks.Add(new Hook(config, proc));
                }
            }
        }

    }
}
