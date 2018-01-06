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
    public partial class InputBoxForm : Form
    {
        public InputBoxForm()
        {
            InitializeComponent();
        }

        public bool IsPassword {
            set
            {
                f_InputText.PasswordChar = (value ? '*': ' ');
            }
        }

        public string InputText {
            get
            {
                return f_InputText.Text;
            }
            //set; 
        }

        public string Prompt { 
            //get;            
            set
            {
                l_Prompt.Text = value;
            }
        }
    }
}
