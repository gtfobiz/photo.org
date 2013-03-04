using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Photo.org
{
    internal class CustomTreeView : TreeView 
    {
        internal delegate void NodeSelectionChangedDelegate(object sender, EventArgs e);
        internal event NodeSelectionChangedDelegate NodeSelectionChanged;

        internal delegate void MouseWheelOutsideControlDelegate(object sender, short direction);
        internal event MouseWheelOutsideControlDelegate MouseWheelOutsideControl;

        private List<TreeNode> m_SelectedNodes = new List<TreeNode>();

        internal CustomTreeView() : base()
        {
            base.SelectedNode = null;
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

        internal void RemoveNode(TreeNode tn)
        {
            Nodes.Remove(tn);
            if (m_SelectedNodes.Contains(tn))
                m_SelectedNodes.Remove(tn);
        }

        private void ResetNodeColors(TreeNode tn)
        {
            tn.BackColor = Color.White;
            tn.ForeColor = Color.Black;

            foreach (TreeNode child in tn.Nodes)
                ResetNodeColors(child);
        }

        internal void ClearSelections()
        {
            m_SelectedNodes.Clear();

            foreach (TreeNode tn in Nodes)
                ResetNodeColors(tn);
        }

        internal void ClearNodes()
        {
            Nodes.Clear();            
            m_SelectedNodes.Clear();
        }

        internal new TreeNode SelectedNode
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        internal List<TreeNode> SelectedNodes
        {
            get { return m_SelectedNodes; }
            //set { m_SelectedNodes = value; }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Shift)
                return;

            base.SelectedNode = null;

            if (e.Button == MouseButtons.Left)
            {
                bool controlPressed = (ModifierKeys == Keys.Control);

                TreeNode tn = GetNodeAt(new Point(e.X, e.Y));
                if (tn == null)
                    return;

                int leftBound = tn.Bounds.X;
                int rightBound = tn.Bounds.Right + 10;
                if (e.Location.X <= leftBound || e.Location.X >= rightBound)
                    return;

                if (controlPressed)
                {
                    if (m_SelectedNodes.Contains(tn))
                    {
                        m_SelectedNodes.Remove(tn);
                        RemovePaintFromNode(tn);
                    }
                    else
                    {
                        m_SelectedNodes.Add(tn);
                        PaintSelectedNode(tn);
                    }
                }
                else
                {
                    RemovePaintFromNodes();
                    m_SelectedNodes.Clear();
                    m_SelectedNodes.Add(tn);
                    PaintSelectedNodes();
                }

                if (NodeSelectionChanged != null)
                    NodeSelectionChanged(this, new EventArgs());
            }

            base.OnMouseDown(e);
        }
        
        protected override void OnAfterSelect(TreeViewEventArgs e)
        {
            base.SelectedNode = null;
        }

        protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
        {
            base.SelectedNode = null;
            e.Cancel = true;
        }

        private void RemovePaintFromNode(TreeNode tn)        
        {
            tn.ForeColor = ForeColor;
            tn.BackColor = BackColor;
        }

        private void RemovePaintFromNodes()
        {
            foreach (TreeNode tn in m_SelectedNodes)
            {
                RemovePaintFromNode(tn);
            }
        }

        private void PaintSelectedNode(TreeNode tn)
        {
            tn.BackColor = Color.LightGreen;
            tn.ForeColor = Color.Black;
        }

        private void PaintSelectedNodes()
        {
            foreach (TreeNode tn in m_SelectedNodes)
            {
                PaintSelectedNode(tn);
            }
        }
    }
}
