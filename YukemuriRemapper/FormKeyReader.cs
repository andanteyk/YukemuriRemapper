using System;
using System.Windows.Forms;

namespace YukemuriRemapper
{
    public partial class FormKeyReader : Form
    {
        public int Result { get; private set; }

        public FormKeyReader()
        {
            InitializeComponent();
        }

        private void FormKeyReader_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void FormKeyReader_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            Result = e.KeyValue;
            DialogResult = DialogResult.OK;
        }
    }
}
