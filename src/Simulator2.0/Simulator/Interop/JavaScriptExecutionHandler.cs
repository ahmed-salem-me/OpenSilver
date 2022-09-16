

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




using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;
using System.Collections.Concurrent;
using System;
using OpenSilver.Simulator;

namespace DotNetForHtml5.EmulatorWithoutJavascript
{
    public class JavaScriptExecutionHandler
    {
        private bool _webControlDisposed = false;
        private SimBrowser _webControl;
        private string _lastExecutedJavaScriptCode;
        private ConcurrentQueue<string> _fullLogOfExecutedJavaScriptCode = new ConcurrentQueue<string>();
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
                _fullLogOfExecutedJavaScriptCode.Enqueue(javaScriptToExecute + ";");
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
                _fullLogOfExecutedJavaScriptCode.Enqueue(javaScriptToExecute + ";");
            return _webControl.ExecuteScriptWithResult(javaScriptToExecute);
        }

        internal string GetLastExecutedJavaScriptCode()
        {
            return _lastExecutedJavaScriptCode;
        }

        public string FullLogOfExecutedJavaScriptCode
        {
            get
            {
                return
@"window.onCallBack = {
    OnCallbackFromJavaScript: function(callbackId, idWhereCallbackArgsAreStored, callbackArgsObject)
    {
        // dummy function
    },
    OnCallbackFromJavaScriptError: function(idWhereCallbackArgsAreStored)
    {
        // dummy function
    }
};
"
                + string.Join("\n\n", _fullLogOfExecutedJavaScriptCode);
            }
        }

        public void ClearJSCallsLog()
        {
            _fullLogOfExecutedJavaScriptCode = new ConcurrentQueue<string>();
        }

        public void StartJSLoggin()
        {
            ClearJSCallsLog();
            IsJSLoggingEnabled = true;
        }

        public void StopJSLoggin()
        {
            IsJSLoggingEnabled = false;
        }
    }
}
