﻿

/*===================================================================================
* 
*   Copyright (c) Userware (OpenSilver.net, CSHTML5.com)
*      
*   This file is part of both the OpenSilver Simulator (https://opensilver.net), which
*   is licensed under the MIT license (https://opensource.org/licenses/MIT), and the
*   CSHTML5 Simulator (http://cshtml5.com), which is dual-licensed (MIT + commercial).
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/





using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Wpf;


namespace DotNetForHtml5.EmulatorWithoutJavascript.XamlInspection
{
    /// <summary>
    /// Interaction logic for XamlInspectionTreeView.xaml
    /// </summary>
    public partial class XamlInspectionTreeView : UserControl
    {
        XamlPropertiesPane _xamlPropertiesPane;
        bool _hasBeenFullyExpanded;

        public XamlInspectionTreeView()
        {
            InitializeComponent();
        }

        public bool TryRefresh(Assembly entryPointAssembly, XamlPropertiesPane xamlPropertiesPane)
        {
            int nbTreeViewElements;

            _xamlPropertiesPane = xamlPropertiesPane;
            _hasBeenFullyExpanded = false;
            
            var isSuccess = XamlInspectionHelper.TryInitializeTreeView(this.TreeViewForXamlInspection, entryPointAssembly, out nbTreeViewElements);

            if (isSuccess)
                this.NumberTreeViewElement.Text = "Element count: " + nbTreeViewElements;
            else
                this.NumberTreeViewElement.Text = "";
            return isSuccess;
        }

        void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || ((TreeNode)e.NewValue) == null)
            {
                // Clear properties pane:
                _xamlPropertiesPane.Refresh(e.NewValue);

                // Clear highlight:
                XamlInspectionHelper.HighlightElementUsingJS(null, 2);
            }
            else
            {
                // Refresh the properties pane:
                var treeNode = (TreeNode)e.NewValue;
                _xamlPropertiesPane.Refresh(treeNode.Element);

                // Highlight the element in the web browser:
                XamlInspectionHelper.HighlightElementUsingJS(treeNode.Element, 2);
            }
        }

        public void ExpandAllNodes()
        {
            foreach (object item in this.TreeViewForXamlInspection.Items)
            {
                TreeViewItem treeViewItem = this.TreeViewForXamlInspection.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem != null)
                    treeViewItem.ExpandSubtree();
            }
            _hasBeenFullyExpanded = true;
        }

        public bool TrySelectTreeNode(object uiElement)
        {
            bool wasFullyExpanded = _hasBeenFullyExpanded;

            // First, we need to expand all the nodes so that the "item generators" can be called (which creates the TreeViewItems") and so we can select the node:
            ExpandAllNodes();

            // Then, select the item:
            foreach (Tuple<TreeNode, TreeViewItem> treeNodeAndTreeViewItem in TraverseTreeViewItems(TreeViewForXamlInspection))
            {
                TreeNode treeNode = treeNodeAndTreeViewItem.Item1;
                TreeViewItem treeViewItem = treeNodeAndTreeViewItem.Item2;
                if (treeNodeAndTreeViewItem.Item1.Element == uiElement)
                {
                    if (treeViewItem != null)
                    {
                        Dispatcher.BeginInvoke((Action)(async () =>
                            {
                                treeViewItem.IsSelected = true;

                                if (!wasFullyExpanded)
                                    await Task.Delay(3000); // We give the time to the TreeView to expand, in ordero to make it possible to bring the selected item into view.

                                treeViewItem.BringIntoView();
                            }));
                    }
                    else
                        throw new Exception("Unable to get the TreeViewItem from the TreeNode. Please inform the authors at: support@cshtml5.com");
                    return true;
                }
            }
            return false;
        }

        public bool TrySelectTreeNodeX(object uiElement)
        {
            bool wasFullyExpanded = _hasBeenFullyExpanded;

            // First, we need to expand all the nodes so that the "item generators" can be called (which creates the TreeViewItems") and so we can select the node:
            //ExpandAllNodes();

            // Then, select the item:
            foreach (Tuple<TreeNode, TreeViewItem> treeNodeAndTreeViewItem in TraverseTreeViewItems(TreeViewForXamlInspection))
            {
                TreeNode treeNode = treeNodeAndTreeViewItem.Item1;
                TreeViewItem treeViewItem = treeNodeAndTreeViewItem.Item2;
                if (treeNodeAndTreeViewItem.Item1.Element == uiElement)
                {
                    if (treeViewItem != null)
                    {
                        Dispatcher.BeginInvoke((Action)(async () =>
                        {
                            treeViewItem.IsSelected = true;

                            if (!wasFullyExpanded)
                                await Task.Delay(3000); // We give the time to the TreeView to expand, in ordero to make it possible to bring the selected item into view.

                            treeViewItem.BringIntoView();
                        }));
                    }
                    else
                        throw new Exception("Unable to get the TreeViewItem from the TreeNode. Please inform the authors at: support@cshtml5.com");
                    return true;
                }
            }
            return false;
        }


        /*
        static IEnumerable<TreeNode> TraverseTreeView(object treeViewOrTreeNode)
        {
            if (treeViewOrTreeNode != null)
            {
                if (treeViewOrTreeNode is TreeView)
                {
                    foreach (var item in ((TreeView)treeViewOrTreeNode).Items)
                    {
                        TreeNode treeNode = item as TreeNode;
                        if (treeNode != null)
                        {
                            yield return treeNode;

                            foreach (var subChild in TraverseTreeView(treeNode))
                            {
                                yield return subChild;
                            }
                        }
                    }
                }
                else if (treeViewOrTreeNode is TreeNode)
                {
                    foreach (var item in ((TreeNode)treeViewOrTreeNode).Children)
                    {
                        TreeNode treeNode = item as TreeNode;
                        if (treeNode != null)
                        {
                            yield return treeNode;

                            foreach (var subChild in TraverseTreeView(treeNode))
                            {
                                yield return subChild;
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Unexpected type during TreeView traversal: " + (treeViewOrTreeNode.GetType().ToString()));
                }
            }
        }
        */

        static IEnumerable<Tuple<TreeNode, TreeViewItem>> TraverseTreeViewItems(object treeViewOrTreeViewItem)
        {
            if (treeViewOrTreeViewItem != null)
            {
                if (treeViewOrTreeViewItem is TreeView)
                {
                    foreach (var item in ((TreeView)treeViewOrTreeViewItem).Items)
                    {
                        TreeNode treeNode = item as TreeNode;
                        if (treeNode != null)
                        {
                            // Get the TreeViewItem:
                            TreeViewItem treeViewItem = ((TreeView)treeViewOrTreeViewItem).ItemContainerGenerator.ContainerFromItem(treeNode) as TreeViewItem;

                            yield return new Tuple<TreeNode, TreeViewItem>(treeNode, treeViewItem);

                            foreach (var subChild in TraverseTreeViewItems(treeViewItem))
                            {
                                yield return subChild;
                            }
                        }
                    }
                }
                else if (treeViewOrTreeViewItem is TreeViewItem)
                {
                    foreach (var item in ((TreeViewItem)treeViewOrTreeViewItem).Items)
                    {
                        TreeNode treeNode = item as TreeNode;
                        if (treeNode != null)
                        {
                            // Get the TreeViewItem:
                            TreeViewItem treeViewItem = ((TreeViewItem)treeViewOrTreeViewItem).ItemContainerGenerator.ContainerFromItem(treeNode) as TreeViewItem;

                            yield return new Tuple<TreeNode, TreeViewItem>(treeNode, treeViewItem);

                            foreach (var subChild in TraverseTreeViewItems(treeViewItem))
                            {
                                yield return subChild;
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Unexpected type during TreeView traversal: " + (treeViewOrTreeViewItem.GetType().ToString()));
                }
            }
        }
    }
}
