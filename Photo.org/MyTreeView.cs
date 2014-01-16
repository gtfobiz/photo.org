using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Photo.org
{
    internal class MyTreeView : UserControl
    {
        internal delegate void NodeSelectionChangedDelegate();
        internal event NodeSelectionChangedDelegate NodeSelectionChanged;
        internal delegate void NodeTextChangedDelegate(MyTreeViewNode node);
        internal event NodeTextChangedDelegate NodeTextChanged;
        internal delegate void NodeMouseDownDelegate(MyTreeViewNode node, MouseEventArgs e);
        internal event NodeMouseDownDelegate NodeMouseDown;
        internal delegate void MouseWheelOutsideControlDelegate(object sender, short direction);
        internal event MouseWheelOutsideControlDelegate MouseWheelOutsideControl;
        internal delegate void NodeDragEnterDelegate(MyTreeViewNode sender, DragEventArgs e);
        internal event NodeDragEnterDelegate NodeDragEnter;
        internal delegate void NodeDragDropDelegate(MyTreeViewNode sender, DragEventArgs e);
        internal event NodeDragDropDelegate NodeDragDrop;

        private List<Label> m_NodeControls = new List<Label>();
        private const int c_TopMargin = 2;
        private const int c_LeftMargin = 4;
        private const int c_NodeHeight = 22;
        private const int c_NodeIndent = 16;
        private MyTreeViewNode m_Root = null;
        private Dictionary<object, MyTreeViewNode> m_AllNodes = new Dictionary<object, MyTreeViewNode>();
        private List<MyTreeViewNode> m_Nodes = new List<MyTreeViewNode>();
        private List<MyTreeViewNode> m_SelectedNodes = new List<MyTreeViewNode>();
        private VScrollBar m_ScrollBar = new VScrollBar();
        private MyTreeViewNode m_NodeBeingRenamed = null;
        private TextBox m_NodeRenameBox = new TextBox();

        internal List<MyTreeViewNode> SelectedNodes
        {
            get { return m_SelectedNodes; }
            set { m_SelectedNodes = value; }
        }        

        public MyNodes Nodes
        {
            get { return m_Root.Nodes; }
            //set { m_Nodes = value; }
        }

        internal MyTreeView()
        {
            InitializeComponent();
            m_Root = new MyTreeViewNode(this, null, null);
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.White;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Resize += MyTreeView_Resize;
            this.MouseDown += MyTreeView_MouseDown;            
            this.Paint += MyTreeView_Paint;

            m_ScrollBar.Minimum = 1;
            m_ScrollBar.Maximum = 1;
            m_ScrollBar.Value = 1;
            m_ScrollBar.Dock = DockStyle.Right;
            m_ScrollBar.Visible = false;
            m_ScrollBar.ValueChanged += m_ScrollBar_ValueChanged;
            this.Controls.Add(m_ScrollBar);

            m_NodeRenameBox.Font = new Font("Tahoma", 12);
            m_NodeRenameBox.Visible = false;
            m_NodeRenameBox.KeyDown += m_NodeRenameBox_KeyDown;
            this.Controls.Add(m_NodeRenameBox);
        }

        void MyTreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        void MyTreeView_DragOver(object sender, DragEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void MyTreeView_DragDrop(object sender, DragEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void m_ScrollBar_ValueChanged(object sender, EventArgs e)
        {
            RefreshOrWhatever();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        void MyTreeView_Resize(object sender, EventArgs e)
        {
            RefreshOrWhatever();
        }

        void MyTreeView_Paint(object sender, PaintEventArgs e)
        {
            using (Graphics g = e.Graphics)
            {
                Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                Stream myStream = myAssembly.GetManifestResourceStream("Photo.org.plus.png");
                Bitmap plus = new Bitmap(myStream);
                myStream = myAssembly.GetManifestResourceStream("Photo.org.minus.png");
                Bitmap minus = new Bitmap(myStream);
                Bitmap bitmap = null;
                int xOffset = 0, yOffset = 0;

                xOffset = c_NodeIndent / 2 + (minus.Width / 2) - 2;
                yOffset = c_NodeHeight / 2 - (minus.Height / 2) - 1;

                foreach (Label l in m_NodeControls)
                    if (l.Visible) // && (l.Tag as MyTreeViewNode).Nodes.Count > 0 && (l.Tag as MyTreeViewNode). Parent != m_Root)
                    {
                        MyTreeViewNode node = (MyTreeViewNode)l.Tag;
                        if (node.Nodes.Count > 0 && node.Parent != m_Root)
                        {
                            bitmap = (node.Expanded ? minus : plus);                                        
                            g.DrawImage(bitmap, l.Left - xOffset, l.Top + yOffset);
                        }
                    }
            }
        }

        private uint HiWord(IntPtr ptr)
        {
            return ((uint)ptr >> 16) & 0xffff;
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == 0x020A)
            {
                Point localPosition = this.PointToClient(MousePosition);

                if (localPosition.X < this.Left || localPosition.X > this.Width ||
                    localPosition.Y < this.Top || localPosition.Y > this.Height)
                {
                    if (MouseWheelOutsideControl != null)
                        MouseWheelOutsideControl(this, (short)HiWord(m.WParam));
                    return;
                }
            }

            base.WndProc(ref m);
        }

        internal void RemoveNode(MyTreeViewNode node)
        {
            node.Parent.Nodes.Remove(node);
        }

        internal MyTreeViewNode FindNode(object key)
        {
            if (!m_AllNodes.ContainsKey(key))
                return null;

            return m_AllNodes[key];
        }

        void m_NodeRenameBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    CancelLabelEdit();
                    break;
                case Keys.Return:
                    FinishLabelEdit();
                    break;
            }
        }

        void MyTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                foreach (Label l in m_NodeControls)
                    if (e.Y >= l.Top && e.Y <= l.Top + l.Height && e.X < l.Left && e.X > l.Left - c_NodeIndent && e.X > c_LeftMargin)
                    {
                        (l.Tag as MyTreeViewNode).Expanded = !(l.Tag as MyTreeViewNode).Expanded;                        
                        break;   
                    }
            }

            FinishLabelEdit();
        }

        //!! kuvan raahaus kategoriaan ei lisää tooltippiin nytte!!

        internal void RefreshOrWhatever()
        {
            int y = 0;
            int nodeCount = 0;

            if (m_NodeBeingRenamed == null)
                m_NodeRenameBox.Visible = false;

            DrawNodes(m_Root, 0, ref y, ref nodeCount);

            for (int i = y; i < m_NodeControls.Count; i++)
                m_NodeControls[i].Visible = false;

            m_ScrollBar.Maximum = nodeCount;
            m_ScrollBar.Visible = true;
//            m_ScrollBar.Visible = (y * c_NodeHeight + c_TopMargin >= this.Height);

            Invalidate();
        }

        private void DrawNodes(MyTreeViewNode parent, int x, ref int y, ref int nodeCount)
        {
            Label l = null;

            foreach (MyTreeViewNode node in parent.Nodes)
            {
                nodeCount++;
                if (nodeCount >= m_ScrollBar.Value)
                {
                    if (y * c_NodeHeight + c_TopMargin <= this.Height)
                    {
                        if (y >= m_NodeControls.Count)
                        {
                            l = new Label();
                            l.AutoSize = true;
                            l.Font = new Font("Tahoma", 12);
                            l.MouseDown += l_MouseDown;
                            l.AllowDrop = true;
                            l.DragEnter += l_DragEnter;
                            l.DragDrop += l_DragDrop;
                            l.DragOver += l_DragOver;
                            l.DragLeave += l_DragLeave;

                            m_NodeControls.Add(l);
                            this.Controls.Add(l);
                        }

                        l = m_NodeControls[y];

                        l.Tag = node;
                        l.Text = node.Text;
                        l.BackColor = node.BackColor;

                        l.Top = y * c_NodeHeight + c_TopMargin;
                        l.Left = x * c_NodeIndent + c_LeftMargin;

                        if (node == m_NodeBeingRenamed)
                        {
                            m_NodeRenameBox.Location = l.Location;
                            m_NodeRenameBox.Visible = true;
                            m_NodeRenameBox.Focus();
                            m_NodeRenameBox.SelectAll();
                        }

                        l.Visible = (node != m_NodeBeingRenamed);

                        y++;
                    }
                }

                if (node.Expanded)
                    DrawNodes(node, x + 1, ref y, ref nodeCount);
            }            
        }

        void l_DragLeave(object sender, EventArgs e)
        {
            Label label = (Label)sender;
            label.Font = new Font(label.Font, FontStyle.Regular);
        }

        void l_DragEnter(object sender, DragEventArgs e)
        {
            if (this.AllowDrop && NodeDragEnter != null)
                NodeDragEnter((MyTreeViewNode)((Label)sender).Tag, e);
        }

        void l_DragOver(object sender, DragEventArgs e)
        {
            if (e.Effect == DragDropEffects.None)
                return;

            Label label = (Label)sender;
            label.Font = new Font(label.Font, FontStyle.Bold);
        }

        void l_DragDrop(object sender, DragEventArgs e)
        {
            Label label = (Label)sender;
            label.Font = new Font(label.Font, FontStyle.Regular);

            if (NodeDragDrop != null)
                NodeDragDrop((MyTreeViewNode)label.Tag, e);
        }

        void l_MouseDown(object sender, MouseEventArgs e)
        {
            MyTreeViewNode senderNode = (sender as Label).Tag as MyTreeViewNode;

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            { 
                if (Common.IsCtrlPressed())
                {
                    SetNodeSelectState(senderNode, !m_SelectedNodes.Contains(senderNode));
                }
                else
                {
                    ClearSelections();
                    SetNodeSelectState(senderNode, true);
                }
            }

            FinishLabelEdit();

            if (NodeMouseDown != null)
                NodeMouseDown(senderNode, new MouseEventArgs(e.Button, e.Clicks, (sender as Label).Left + e.X, (sender as Label).Top + e.Y, e.Delta));

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                if (NodeSelectionChanged != null)
                    NodeSelectionChanged();
        }

        internal bool OnAddNode(MyTreeViewNode node)
        {
            if (m_AllNodes.ContainsKey(node.Key))
                return false;

            m_AllNodes.Add(node.Key, node);

            return true;
        }

        internal void OnRemoveNode(MyTreeViewNode node)
        {
            m_AllNodes.Remove(node.Key);
        }

        internal void EditLabel(MyTreeViewNode node)
        {
            m_NodeBeingRenamed = node;
            m_NodeRenameBox.Text = node.Text;
            RefreshOrWhatever();
        }

        private void FinishLabelEdit()
        {
            MyTreeViewNode node = m_NodeBeingRenamed;

            if (node != null)
                m_NodeBeingRenamed.Text = m_NodeRenameBox.Text;

            CancelLabelEdit();

            if (node != null && NodeTextChanged != null)
                NodeTextChanged(node);
        }

        private void CancelLabelEdit()
        {
            m_NodeBeingRenamed = null;
            RefreshOrWhatever();
        }

        internal void SelectNode(MyTreeViewNode node)
        {
            SetNodeSelectState(node, true);
        }

        internal void ClearSelections()
        {
            while (m_SelectedNodes.Count > 0)
                SetNodeSelectState(m_SelectedNodes[0], false);
        }

        private void SetNodeSelectState(MyTreeViewNode node, bool selected)
        {
            if (selected)
            {
                if (!m_SelectedNodes.Contains(node))
                    m_SelectedNodes.Add(node);
                node.BackColor = Color.LightGreen;
            }
            else
            {
                if (m_SelectedNodes.Contains(node))
                    m_SelectedNodes.Remove(node);
                node.BackColor = Color.White;
            }
        }

        internal void CollapseAll()
        {
            foreach (MyTreeViewNode node in m_AllNodes.Values)
                node.Expanded = false;
        }
    }

    internal class MyNodes : List<MyTreeViewNode>
    {
        private MyTreeView m_TreeViewControl = null;
        private MyTreeViewNode m_Parent = null;

        internal MyNodes(MyTreeView treeViewControl, MyTreeViewNode parent)
        {
            m_TreeViewControl = treeViewControl;
            m_Parent = parent;
        }

        internal MyTreeViewNode Add(object key, string text)
        {
            MyTreeViewNode node = new MyTreeViewNode(m_TreeViewControl, key, text);
            if (!m_TreeViewControl.OnAddNode(node))
                return null;

            node.Parent = m_Parent;
            node.TreeViewControl = m_TreeViewControl;

            base.Add(node);
            return node;
        }

        new internal void Clear()
        {
            while (this.Count > 0)
                Remove(this[0]);
        }

        new internal void Remove(MyTreeViewNode node)
        {
            node.Nodes.Clear();

            m_TreeViewControl.OnRemoveNode(node);

            node.TreeViewControl = null;
            node.Parent = null;

            base.Remove(node);
        }

        new internal void Add(MyTreeViewNode node)
        {
            throw new NotSupportedException();
        }
    }

    internal class MyTreeViewNode
    {        
        private MyNodes m_Nodes = null;
        private Category m_Category = null;
        private string m_Text = null;
        private object m_Key = null;
        private bool m_Expanded = false;
        private Color m_BackColor = Color.White;
        private MyTreeView m_TreeViewControl = null;
        private MyTreeViewNode m_Parent = null;

        internal MyTreeView TreeViewControl
        {
            get { return m_TreeViewControl; }
            set { m_TreeViewControl = value; }
        }        

        internal MyTreeViewNode Parent
        {
            get { return m_Parent; }
            set { m_Parent = value; }
        }

        public Color BackColor
        {
            get { return m_BackColor; }
            set { m_BackColor = value; }
        }

        internal Category Category
        {
            get { return m_Category; }
            set { m_Category = value; }
        }        

        public object Key
        {
            get { return m_Key; }
            //set { m_Key = value; }
        }        

        public bool Expanded
        {
            get { return m_Expanded; }
            set {
                if (value)
                    this.Expand();
                else
                    m_Expanded = false;
            }
        }

        public void Expand()
        {
            if (m_Nodes.Count > 0)
                m_Expanded = true;
        }

        public string Text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        internal MyTreeViewNode(MyTreeView treeViewControl, object key, string text)
        {
            m_TreeViewControl = treeViewControl;
            m_Nodes = new MyNodes(treeViewControl, this);
            m_Key = key;
            m_Text = text;           
        }

        internal MyNodes Nodes
        {
            get { return m_Nodes; }
            //set { m_Nodes = value; }
        }
    }
}
