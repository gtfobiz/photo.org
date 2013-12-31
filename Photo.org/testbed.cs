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
    public partial class testbed : Form
    {
        private MyTreeView tv = new MyTreeView();

        public testbed()
        {
            InitializeComponent();
            this.Click += testbed_Click;
     
            Controls.Add(tv);

            tv.NodeSelectionChanged += tv_NodeSelectionChanged;

            //tv.AddNode(Guids.Unassigned, "Unassigned");
            //MyTreeViewNode n = tv.Nodes.Add(Guids.AllFiles, "All items");

            //n.Add(1, "testi1");
            //n.AddNode(2, "testi2").AddNode(3, "testi3");
            //n.AddNode(4, "testi4");
            //n.AddNode(5, "testi5").AddNode(5.1, "testi5.1").AddNode("##", "testi5.1.1");
            //n.AddNode(6, "testi6");
            //n.AddNode(7, "testi7");

            tv.RefreshOrWhatever();
        }

        void testbed_Click(object sender, EventArgs e)
        {
            if (tv.SelectedNodes.Count != 1)
                return;

            tv.EditLabel(tv.SelectedNodes[0]);
        }

        void tv_NodeSelectionChanged()
        {
 
        }
    }
}
