

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



extern alias OS;
using System.Collections.Concurrent;
using OpenSilver.Simulator;
using OS::DotNetForHtml5;

namespace DotNetForHtml5.EmulatorWithoutJavascript
{
    public class JavaScriptExecutionHandler : IJavaScriptExecutionHandler
    {
        private bool _webControlDisposed = false;
        private SimBrowser _webControl;
        private string _lastExecutedJavaScriptCode;
        private ConcurrentQueue<string> _InteropLog = new ConcurrentQueue<string>();
        public bool IsJSLoggingEnabled { get; set; }

        public JavaScriptExecutionHandler(SimBrowser webControl)
        {
            _webControl = webControl; 
            //ams>could/should be replaced
            //webControl.DisposeEvent += WebControl_DisposeEvent;
            //webControl.Browser.DisposeEvent += WebControl_DisposeEvent;
        }

        //private void WebControl_DisposeEvent(object sender, DotNetBrowser.Events.DisposeEventArgs e)
        //{
        //    _webControlDisposed = true;
        //}

        // Called via reflection by the "INTERNAL_HtmlDomManager" class of the "Core" project.
        public void ExecuteJavaScript(string javaScriptToExecute)
        {
            // This prevents interop calls from throwing an exception if they are called after the simulator started closing
            if (_webControlDisposed)
                return;
            _lastExecutedJavaScriptCode = javaScriptToExecute;
            if (IsJSLoggingEnabled)
                _InteropLog.Enqueue(javaScriptToExecute + ";");
            _webControl.ExecuteScriptAsync(javaScriptToExecute);
        }

        // Called via reflection by the "INTERNAL_HtmlDomManager" class of the "Core" project.
        public object ExecuteJavaScriptWithResult(string javaScriptToExecute)
        {
            // This prevents interop calls from throwing an exception if they are called after the simulator started closing
            if (_webControlDisposed)
                return null;
            _lastExecutedJavaScriptCode = javaScriptToExecute;
            if (IsJSLoggingEnabled)
                _InteropLog.Enqueue(javaScriptToExecute + ";");
            return _webControl.ExecuteScriptWithResult(javaScriptToExecute);
        }

        internal string GetLastExecutedJavaScriptCode()
        {
            return _lastExecutedJavaScriptCode;
        }

        public string InteropLog
        {
            get
            {
                return string.Join("\n\n", _InteropLog);
            }
        }

        public void ClearInteropLog()
        {
            _InteropLog = new ConcurrentQueue<string>();
        }

        public void StartInteropLogging()
        {
            ClearInteropLog();
            IsJSLoggingEnabled = true;
        }

        public void StopInteropLoggin()
        {
            IsJSLoggingEnabled = false;
        }
    }
}
