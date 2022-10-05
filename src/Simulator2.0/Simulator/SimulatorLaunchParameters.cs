
using OpenSilver.Simulator;
using System;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;

#if OPENSILVER
namespace OpenSilver.Simulator
#else
namespace CSHTML5.Simulator
#endif
{
    public class SimulatorLaunchParameters
    {

        // Add stuff as needed, like cookies, etc.

        public Action<WebView2> BrowserCreatedCallback { get; set; }

        /// <summary>
        /// Action to call when the provided app class is created successfully.
        /// </summary>
        public Action AppStartedCallback { get; set; }

        /// <summary>
        /// Sets or gets custom cookies to the simulator
        /// </summary>
        public IList<CookieData> CookiesData { get; set; }
    
        /// <summary>
        /// Sets the application init parameters
        /// </summary>
        public string InitParams { get; set; }
    }
}
