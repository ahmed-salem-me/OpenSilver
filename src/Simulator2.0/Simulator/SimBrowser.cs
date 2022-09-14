using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.DevToolsProtocolExtension;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OpenSilver.Simulator
{
    public class SimBrowser : WebView2
    {
        private bool _ReloadApp;
        private bool _IsNavigationCompleted;
        private DispatcherTimer _NavigationCompletedCheckTimer;
        public Action OnNavigationCompleted { get; set; }
        public Action OnInitialized { get; set; }

        public SimBrowser()
        {
            Loaded += SimBrowser_Loaded;
            CoreWebView2InitializationCompleted += SimBrowser_CoreWebView2InitializationCompleted;

            _NavigationCompletedCheckTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(10) };
            _NavigationCompletedCheckTimer.Tick += (s, e) =>
            {
                if (_IsNavigationCompleted)
                {
                    _NavigationCompletedCheckTimer.Stop();
                    OnNavigationCompleted();
                }
            };
            _NavigationCompletedCheckTimer.Start();
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

        private async void SimBrowser_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            var coreWebView = CoreWebView2;

            coreWebView.AddWebResourceRequestedFilter("*.js", CoreWebView2WebResourceContext.Script);
            coreWebView.AddWebResourceRequestedFilter("*.css", CoreWebView2WebResourceContext.Stylesheet);

            coreWebView.WebResourceRequested += CoreWebView_WebResourceRequested;
            coreWebView.NavigationStarting += CoreWebView_NavigationStarting;
            coreWebView.NavigationCompleted += CoreWebView_NavigationCompleted;
            coreWebView.DOMContentLoaded += CoreWebView_DOMContentLoaded;

            await coreWebView.CallDevToolsProtocolMethodAsync("Network.clearBrowserCache", "{}");

            //the following 2 lines don't work
            //await coreWebView.CallDevToolsProtocolMethodAsync("Log.enable", "{}");
            //coreWebView.GetDevToolsProtocolEventReceiver("Log.entryAdded").DevToolsProtocolEventReceived += OnConsoleMessage;

            //Attach to browser console logging
            DevToolsProtocolHelper helper = coreWebView.GetDevToolsProtocolHelper();
            await helper.Runtime.EnableAsync();
            helper.Runtime.ConsoleAPICalled += OnConsoleMessage;

            //coreWebView.OpenDevToolsWindow();

            if (OnInitialized != null)
                OnInitialized();
        }

        private void CoreWebView_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
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

        private void CoreWebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.Contains(@"http://cshtml5-fbc-mm2-preview.azurewebsites.net"))
            {
                e.Cancel = true;
                string urlFragment = "";
                int hashIndex = e.Uri.IndexOf('#');
                if (hashIndex != -1)
                    urlFragment = e.Uri.Substring(hashIndex);

                // We use a dispatcher to go back to the main thread so that the CurrentCulture remains the same (otherwise, for example on French systems we get an exception during Double.Parse when processing the <Path Data="..."/> control).
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    _ReloadApp = true;
                }));
            }
        }

        private void CoreWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _IsNavigationCompleted = true;
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

        private void CoreWebView_DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
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

        public object ExecuteScriptWithResult(string javaScript)
        {
            string jsonResult = null;

            if ((this as DispatcherObject).CheckAccess())
                jsonResult = Await(ExecuteScriptAsync(javaScript));
            else
                jsonResult = Dispatcher.InvokeAsync(async () => await ExecuteScriptAsync(javaScript)).Result.Result;

            var result = JsonDocument.Parse(jsonResult);

            if (result != null)
            {
                switch (result.RootElement.ValueKind)
                {
                    case JsonValueKind.String:
                        return result.RootElement.GetString();
                    case JsonValueKind.Null:
                        return null;
                }
            }
            return jsonResult;
        }

        private  string Await(Task<string> task)
        {
            //We use this hack to be able to await an async call from a non async method, has to be called from non webview2 event hence the class timer

            //DispatcherTimer timeOutTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(55000) };
            // Timeout if task takes too long
            //timeOutTimer.Tick += (s, e) => throw new TimeoutException(javaScript);

            DispatcherFrame frame = new DispatcherFrame();


            string result = null;

            task.ContinueWith(_ =>
            {
                result = !task.IsFaulted ? task.Result : task.Exception.Message;
                frame.Continue = false;
            });

            Dispatcher.PushFrame(frame);
            //timeOutTimer.Start();
            //timeOutTimer.Stop();

            return result;

        }
    }
}
