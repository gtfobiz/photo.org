using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Photo.org
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Common.Initialize(this);          
        }

        public bool IsCtrlPressed()
        {
            return ((ModifierKeys & Keys.Control) == Keys.Control);
        }

        public bool IsShiftPressed()
        {
            return ((ModifierKeys & Keys.Shift) == Keys.Shift);
        }

        public bool IsAltPressed()
        {
            return ((ModifierKeys & Keys.Alt) == Keys.Alt);
        }
    }
}
