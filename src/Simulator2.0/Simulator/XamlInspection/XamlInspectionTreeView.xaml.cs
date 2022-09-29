

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
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DotNetForHtml5.EmulatorWithoutJavascript.XamlInspection
{
    /// <summary>
    /// Interaction logic for XamlInspectionTreeView.xaml
    /// </summary>
    public partial class XamlInspectionTreeView : UserControl
    {
        XamlPropertiesPane _xamlPropertiesPane;
        bool _hasBeenFullyExpanded;
        TreeViewItem _selectedTreeItem;

        public XamlInspectionTreeView()
        {
            InitializeComponent();
            MouseRightButtonDown += (s, e) =>
            {
                UIElement ClickedItem = VisualTreeHelper.GetParent(e.OriginalSource as UIElement) as UIElement;
                while ((ClickedItem != null) && !(ClickedItem is TreeViewItem))
                {
                    ClickedItem = VisualTreeHelper.GetParent(ClickedItem) as UIElement;
                }
            };
        }

        public bool TryRefresh(Assembly entryPointAssembly, XamlPropertiesPane xamlPropertiesPane)
        {
            _xamlPropertiesPane = xamlPropertiesPane;
            _hasBeenFullyExpanded = false;

            var isSuccess = XamlInspectionHelper.TryInitializeTreeView(XamlTree);

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
            foreach (object item in this.XamlTree.Items)
            {
                TreeViewItem treeViewItem = this.XamlTree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem != null)
                    treeViewItem.ExpandSubtree();
            }
            _hasBeenFullyExpanded = true;
        }

        public void ExpandToNode(TreeNode treeNode)
        {
            var ancestors = new Stack<TreeNode>();
            var node = treeNode;
            ancestors.Push(node);

            while (node.Parent != null)
            {
                ancestors.Push(node.Parent);
                node = node.Parent;
            }

            TreeViewItem treeItem = null;
            while (ancestors.Count > 0)
            {
                var item = ancestors.Pop();
                treeItem = FindTreeViewItem(XamlTree, item);
                if (treeItem != null)
                    treeItem.IsExpanded = true;
            }
            treeItem.IsSelected = true;
        }

        private void SelectTreeItem(TreeViewItem treeItem)
        {
            if (_selectedTreeItem != null)
                _selectedTreeItem.Background = Brushes.Black;

            treeItem.Background = Brushes.AntiqueWhite;
            _selectedTreeItem = treeItem;
        }

        public bool TrySelectTreeNode(object uiElement)
        {
            bool wasFullyExpanded = _hasBeenFullyExpanded;

            // First, we need to expand all the nodes so that the "item generators" can be called (which creates the TreeViewItems") and so we can select the node:
            ExpandAllNodes();

            // Then, select the item:
            foreach (Tuple<TreeNode, TreeViewItem> treeNodeAndTreeViewItem in TraverseTreeViewItems(XamlTree))
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
            foreach (Tuple<TreeNode, TreeViewItem> treeNodeAndTreeViewItem in TraverseTreeViewItems(XamlTree))
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

        public TreeNode FindElementNode(object uiElement, TreeNode node)
        {
            if (node.Element == uiElement)
                return node;
            foreach (var chidlNode in node.Children)
            {
                var targetNode = FindElementNode(uiElement, chidlNode);
                if (targetNode != null)
                    return targetNode;
            }
            return null;
        }


        public TreeViewItem FindNodeTItem(TreeNode treeNode)
        {
            return null;
        }

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

        public TreeViewItem FindTreeViewItem(ItemsControl container, object item)
        {
            if (container != null)
            {
                if (container.DataContext == item)
                {
                    return container as TreeViewItem;
                }

                // Expand the current container
                if (container is TreeViewItem && !((TreeViewItem)container).IsExpanded)
                {
                    container.SetValue(TreeViewItem.IsExpandedProperty, true);
                }

                // Try to generate the ItemsPresenter and the ItemsPanel.
                // by calling ApplyTemplate.  Note that in the
                // virtualizing case even if the item is marked
                // expanded we still need to do this step in order to
                // regenerate the visuals because they may have been virtualized away.

                container.ApplyTemplate();
                ItemsPresenter itemsPresenter =
                    (ItemsPresenter)container.Template.FindName("ItemsHost", container);
                if (itemsPresenter != null)
                {
                    itemsPresenter.ApplyTemplate();
                }
                else
                {
                    // The Tree template has not named the ItemsPresenter,
                    // so walk the descendents and find the child.
                    itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                    if (itemsPresenter == null)
                    {
                        container.UpdateLayout();

                        itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                    }
                }

                Panel itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);

                // Ensure that the generator for this panel has been created.
                UIElementCollection children = itemsHostPanel.Children;

                for (int i = 0, count = container.Items.Count; i < count; i++)
                {
                    TreeViewItem subContainer;
                    subContainer =
                        (TreeViewItem)container.ItemContainerGenerator.
                        ContainerFromIndex(i);

                    // Bring the item into view to maintain the
                    // same behavior as with a virtualizing panel.
                    subContainer.BringIntoView();

                    if (subContainer != null)
                    {
                        // Search the next level for the object.
                        TreeViewItem resultContainer = FindTreeViewItem(subContainer, item);
                        if (resultContainer != null)
                        {
                            return resultContainer;
                        }
                        else
                        {
                            // The object is not under this TreeViewItem
                            // so collapse it.
                            subContainer.IsExpanded = false;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Search for an element of a certain type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of element to find.</typeparam>
        /// <param name="visual">The parent element.</param>
        /// <returns></returns>
        private T FindVisualChild<T>(Visual visual) where T : Visual
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                Visual child = (Visual)VisualTreeHelper.GetChild(visual, i);
                if (child != null)
                {
                    T correctlyTyped = child as T;
                    if (correctlyTyped != null)
                    {
                        return correctlyTyped;
                    }

                    T descendent = FindVisualChild<T>(child);
                    if (descendent != null)
                    {
                        return descendent;
                    }
                }
            }

            return null;
        }

        private void SubtreeLoader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton== MouseButtonState.Pressed && e.ClickCount == 2)
            {
                var treeNode = XamlTree.SelectedItem as TreeNode; //ams> u're assuming it clicked the icon on the selected item : wrong
                XamlInspectionHelper.RecursivelyAddElementsToTree(treeNode.Element, false, treeNode, 5, false);
            }
        }

        private void SubtreeLoader_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Released && e.ClickCount == 1)
            {
                var cMenu = new ContextMenu();
                var miExpandRecursivly = new MenuItem() { Header = "Expand Recursivly" };
                miExpandRecursivly.Click += (ss, ee) =>
                {
                    var treeNode = XamlTree.SelectedItem as TreeNode; //ams> u're assuming it clicked the icon on the selected item : wrong
                    XamlInspectionHelper.RecursivelyAddElementsToTree(treeNode.Element, false, treeNode, -1, false);
                };
                cMenu.Items.Add(miExpandRecursivly);
                cMenu.IsOpen = true;
            }
        }
    }
}
