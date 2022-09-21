using System;
using System.Windows.Threading;

namespace OpenSilver.Internal
{
    //ams>make internal?
    internal interface ISimulatorProxy
    {
        //ams> remove
        bool UseSimBrowser { get; set; }
        /// <summary>
        /// Invokes the action on the Dispatcher of OpenSilver thread
        /// </summary>
        /// <param name="action"></param>
        void OSInvokeAsync(Action action);

        /// <summary>
        /// Invokes the action on the Dispatcher of Simulator thread
        /// </summary>
        /// <param name="action"></param>
        void SimInvokeAsync(Action action);
        void AddHostObject(string objectName, object objectInstance);
        void ReportJavaScriptError(string error, string where);
        string PathCombine(params string[] paths);
        bool IsOSRuntimeRunning { get; set; }
        dynamic CreateOSDispatcherTimer(Action tickAction);

    }
}
