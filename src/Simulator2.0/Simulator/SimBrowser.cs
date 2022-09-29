using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.DevToolsProtocolExtension;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OpenSilver.Simulator
{
    public class SimBrowser : WebView2
    {
        private bool _ReloadApp;
        public static SimBrowser Instance { get; }
        public Action OnNavigationCompleted { get; set; }
        public Action OnInitialized { get; set; }

        static SimBrowser() { Instance = new SimBrowser(); }

        private SimBrowser()
        {
            Loaded += SimBrowser_Loaded;
            CoreWebView2InitializationCompleted += SimBrowser_CoreWebView2InitializationCompleted;

            //_NavigationCompletedCheckTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(10) };
            //_NavigationCompletedCheckTimer.Tick += (s, e) =>
            //{
            //    if (_IsNavigationCompleted)
            //    {
            //        _NavigationCompletedCheckTimer.Stop();
            //        OnNavigationCompleted();
            //    }
            //};
            //_NavigationCompletedCheckTimer.Start();
        }

        private async void SimBrowser_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await EnsureCoreWebView2Async(CreateDefaultEnvironment());
        }

        private CoreWebView2Environment CreateDefaultEnvironment()
        {
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions();

            options.AdditionalBrowserArguments = "--disable - web - security";
            options.AdditionalBrowserArguments += " --allow-file-access-from-file";
            options.AdditionalBrowserArguments += " --allow-file-access";
            options.AdditionalBrowserArguments += " --remote-debugging-port=9222";

            CoreWebView2Environment environment = CoreWebView2Environment.CreateAsync(null, null, options).Result;
            return environment;
        }

        private async void SimBrowser_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            var coreWebView = CoreWebView2;

            coreWebView.AddWebResourceRequestedFilter("*.js", CoreWebView2WebResourceContext.Script);
            coreWebView.AddWebResourceRequestedFilter("*.css", CoreWebView2WebResourceContext.Stylesheet);

            coreWebView.WebResourceRequested += CoreWebView_WebResourceRequested;
            coreWebView.NavigationStarting += CoreWebView_NavigationStarting;
            coreWebView.NavigationCompleted += CoreWebView_NavigationCompleted;
            coreWebView.ContextMenuRequested += CoreWebView_ContextMenuRequested;

            await coreWebView.CallDevToolsProtocolMethodAsync("Network.clearBrowserCache", "{}");

            coreWebView.SetVirtualHostNameToFolderMapping(RootPage.SimulatorHostName, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), CoreWebView2HostResourceAccessKind.Allow);

            //Attach to browser console logging
            DevToolsProtocolHelper helper = coreWebView.GetDevToolsProtocolHelper();
            await helper.Runtime.EnableAsync();
            helper.Runtime.ConsoleAPICalled += OnConsoleMessage;

            if (DotNetForHtml5.EmulatorWithoutJavascript.Properties.Settings.Default.IsDevToolsOpened)
                coreWebView.OpenDevToolsWindow();

            if (OnInitialized != null)
                OnInitialized();
        }

        private void CoreWebView_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var uri = e.Request.Uri.Replace("file:///", "").Replace("[PARENT]", "..").Replace("%20", " ").Replace('/', '\\');

            if (File.Exists(uri))
            {
                string contentType = null;
                if (uri.EndsWith(".js"))
                    contentType = "application/javascript";
                else if (uri.EndsWith(".css"))
                    contentType = "text/css";

                if (contentType == null)
                    throw new Exception("unexpected resource in simulator-root");

                FileStream fs = File.OpenRead(uri);
                e.Response = CoreWebView2.Environment.CreateWebResourceResponse(fs, 200, "OK", $"Content-Type: {contentType}");
            }
        }

        private void CoreWebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.Contains(@"http://cshtml5-fbc-mm2-preview.azurewebsites.net"))
            {
                e.Cancel = true;
                string urlFragment = "";
                int hashIndex = e.Uri.IndexOf('#');
                if (hashIndex != -1)
                    urlFragment = e.Uri.Substring(hashIndex);

                //ams> rethink-rewrite
                // We use a dispatcher to go back to the main thread so that the CurrentCulture remains the same (otherwise, for example on French systems we get an exception during Double.Parse when processing the <Path Data="..."/> control).
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    _ReloadApp = true;
                }));
            }
        }

        private void CoreWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            OnNavigationCompleted();
            if (_ReloadApp && OnNavigationCompleted != null) ;
            else
            {
                //        Dispatcher.BeginInvoke((Action)(() =>
                //        {
                //            //if (_javaScriptExecutionHandler == null)
                //            //    _javaScriptExecutionHandler = new JavaScriptExecutionHandler(MainWebBrowser);

                //            //dynamic rootElement = _javaScriptExecutionHandler.ExecuteJavaScriptWithResult(@"document.getElementByIdSafe(""cshtml5-root"");");

                //            //MessageBox.Show(rootElement.ToString());

                //            //todo: verify that we are not on an outside page (eg. Azure Active Directory login page)
                //            OnLoaded();
                //        }), DispatcherPriority.ApplicationIdle);
                //    };
                //    //ams>could/should be replaced?
                //    //MainWebBrowser.ConsoleMessageEvent += OnConsoleMessageEvent;

            }
        }

        private void OnConsoleMessage(object sender, Runtime.ConsoleAPICalledEventArgs e)
        {

            //            switch (args.Level)
            //            {
            //#if DEBUG
            //                case ConsoleEventArgs.MessageLevel.DEBUG:
            //#endif
            //                case ConsoleEventArgs.MessageLevel.LOG:
            //                    Console.AddMessage(new ConsoleMessage(args.Message, ConsoleMessage.MessageLevel.Log));
            //                    break;
            //                case ConsoleEventArgs.MessageLevel.WARNING:
            //                    if (!string.IsNullOrEmpty(args.Source))
            //                    {
            //                        Console.AddMessage(new ConsoleMessage(
            //                            args.Message,
            //                            ConsoleMessage.MessageLevel.Warning,
            //                            new FileSource(args.Source, args.LineNumber)
            //                            ));
            //                    }
            //                    else
            //                    {
            //                        Console.AddMessage(new ConsoleMessage(
            //                            args.Message,
            //                            ConsoleMessage.MessageLevel.Warning
            //                            ));
            //                    }
            //                    break;
            //                case ConsoleEventArgs.MessageLevel.ERROR:
            //                    if (!string.IsNullOrEmpty(args.Source))
            //                    {
            //                        Console.AddMessage(new ConsoleMessage(
            //                            args.Message,
            //                            ConsoleMessage.MessageLevel.Error,
            //                            new FileSource(args.Source, args.LineNumber)
            //                            ));
            //                    }
            //                    else
            //                    {
            //                        Console.AddMessage(new ConsoleMessage(
            //                            args.Message,
            //                            ConsoleMessage.MessageLevel.Error
            //                            ));
            //                    }
            //                    break;
            //            }

        }

        private void CoreWebView_ContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            var validItems = new List<string> { "inspectElement" };

            for (int i = e.MenuItems.Count - 1; i > -1; i--)
                if (!validItems.Contains(e.MenuItems[i].Name))
                    e.MenuItems.RemoveAt(i);

            CoreWebView2Deferral deferral = e.GetDeferral();
            e.Handled = true;
            ContextMenu cMenu = new ContextMenu();
            cMenu.Closed += (s, ex) => deferral.Complete();

            var inspectXamlElementItem = new MenuItem() { Header = "Inspect Xaml Element in New Window" };
            inspectXamlElementItem.Click += (ss, ee) => { };//ams> not implemented

            var inspectDomElementItem = new MenuItem() { Header = "Inspect Dom Element" };
            inspectDomElementItem.Click += (ss, ee) =>
            {
                e.SelectedCommandId = e.MenuItems.Single(mi => mi.Name == "inspectElement").CommandId;
            };

            cMenu.Items.Add(inspectXamlElementItem);
            cMenu.Items.Add(new Separator());
            cMenu.Items.Add(inspectDomElementItem);
            cMenu.IsOpen = true;
        }

        void PopulateContextMenu(CoreWebView2ContextMenuRequestedEventArgs args, IList<CoreWebView2ContextMenuItem> menuList, ItemsControl cm)
        {
            for (int i = 0; i < menuList.Count; i++)
            {
                CoreWebView2ContextMenuItem current = menuList[i];
                if (current.Kind == CoreWebView2ContextMenuItemKind.Separator)
                {
                    Separator sep = new Separator();
                    cm.Items.Add(sep);
                    continue;
                }
                MenuItem newItem = new MenuItem();
                // The accessibility key is the key after the & in the label
                // Replace with '_' so it is underlined in the label
                newItem.Header = current.Label.Replace('&', '_');
                newItem.InputGestureText = current.ShortcutKeyDescription;
                newItem.IsEnabled = current.IsEnabled;
                if (current.Kind == CoreWebView2ContextMenuItemKind.Submenu)
                {
                    PopulateContextMenu(args, current.Children, newItem);
                }
                else
                {
                    if (current.Kind == CoreWebView2ContextMenuItemKind.CheckBox
                    || current.Kind == CoreWebView2ContextMenuItemKind.Radio)
                    {
                        newItem.IsCheckable = true;
                        newItem.IsChecked = current.IsChecked;
                    }

                    newItem.Click += (s, ex) =>
                    {
                        args.SelectedCommandId = current.CommandId;
                    };
                }
                cm.Items.Add(newItem);
            }
        }

        public object ExecuteScriptWithResult(string javaScript)
        {
            if (Dispatcher.HasShutdownStarted)
                return null;

            string jsonString = null;

            if ((this as DispatcherObject).CheckAccess())
                throw new NotSupportedException("Should not call ExecuteScript on the WebView2 thread");
            else
                jsonString = Dispatcher.InvokeAsync(async () => await ExecuteScriptAsync(javaScript)).Result.Result;

            var jsonDoc = JsonDocument.Parse(jsonString);

            if (jsonDoc != null)
            {
                switch (jsonDoc.RootElement.ValueKind)
                {
                    case JsonValueKind.String:
                        return jsonDoc.RootElement.GetString();
                    case JsonValueKind.Null:
                        return null;
                }
            }

            return jsonString;
        }
    }
}
