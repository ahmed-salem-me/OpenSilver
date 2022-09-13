using DotNetForHtml5.EmulatorWithoutJavascript;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace OpenSilver.Simulator
{
    internal class OpenSilverRuntime
    {
        private JavaScriptExecutionHandler _javaScriptExecutionHandler;
        private SimBrowser _simBrowser;
        private readonly MainWindow _simMainWindow;
        private readonly SynchronizationContext _uiSynchronizationContext;
        private Assembly _OSRuntimeAssembly;

        public Action OnInitialized { get; set; }


        public OpenSilverRuntime(SimBrowser simBrowser, MainWindow simMainWindow, SynchronizationContext uiSynchronizationContext)
        {
            _simBrowser = simBrowser;
            _simMainWindow = simMainWindow;
            _uiSynchronizationContext = uiSynchronizationContext;
            ReflectionInUserAssembliesHelper.TryGetCoreAssembly(out _OSRuntimeAssembly);
        }

        public bool Start(Action clientAppStartup)
        {
            try
            {
                //var worker = new BackgroundWorker();
                //worker.DoWork += (s, e) =>
                //{
                    if (!Initialize())
                        return false;

                    clientAppStartup();
                //};
                //worker.RunWorkerAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to start the application.\r\n\r\n" + ex.ToString());
                //_simMainWindow.HideLoadingMessage();
                return false;
            }

        }

        public bool Initialize()
        {
            // In OpenSilver we already have the user application type passed to the constructor, so we do not need to retrieve it here
            try
            {
                // Create the JavaScriptExecutionHandler that will be called by the "Core" project to interact with the Emulator:
                _javaScriptExecutionHandler = new JavaScriptExecutionHandler(_simBrowser);

                // Create the HTML DOM MANAGER proxy and pass it to the "Core" project:
                //JSValue htmlDocument = (JSObject)_simBrowser.Browser.ExecuteJavaScriptAndReturnValue("document");

                //InteropHelpers.InjectDOMDocument(_simBrowser.Browser.GetDocument(), _OSRuntimeAssembly);
                //InteropHelpers.InjectHtmlDocument(htmlDocument, _OSRuntimeAssembly);//no need for this line right ?
                InteropHelpers.InjectWebControlDispatcherBeginInvoke(_simBrowser, _OSRuntimeAssembly);
                InteropHelpers.InjectWebControlDispatcherInvoke(_simBrowser, _OSRuntimeAssembly);
                InteropHelpers.InjectWebControlDispatcherCheckAccess(_simBrowser, _OSRuntimeAssembly);
                InteropHelpers.InjectConvertBrowserResult(BrowserResultConverter.CastFromJsValue, _OSRuntimeAssembly);
                InteropHelpers.InjectJavaScriptExecutionHandler(_javaScriptExecutionHandler, _OSRuntimeAssembly);
                InteropHelpers.InjectWpfMediaElementFactory(_OSRuntimeAssembly);
                InteropHelpers.InjectWebClientFactory(_OSRuntimeAssembly);
                InteropHelpers.InjectClipboardHandler(_OSRuntimeAssembly);
                InteropHelpers.InjectSimulatorProxy(new SimulatorProxy(_simBrowser, _simMainWindow.Console, _uiSynchronizationContext), _OSRuntimeAssembly);

                // In the OpenSilver Version, we use this work-around to know if we're in the simulator
                InteropHelpers.InjectIsRunningInTheSimulator_WorkAround(_OSRuntimeAssembly);

                WpfMediaElementFactory._gridWhereToPlaceMediaElements = _simMainWindow.GridForAudioMediaElements;

                // Inject the code to display the message box in the simulator:
                InteropHelpers.InjectCodeToDisplayTheMessageBox(
                    (message, title, showCancelButton) => { return MessageBox.Show(message, title, showCancelButton ? MessageBoxButton.OKCancel : MessageBoxButton.OK) == MessageBoxResult.OK; },
                    _OSRuntimeAssembly);

                // Ensure the static constructor of all common types is called so that the type converters are initialized:
                StaticConstructorsCaller.EnsureStaticConstructorOfCommonTypesIsCalled(_OSRuntimeAssembly);
                //OnInitialized();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while loading the application: " + Environment.NewLine + Environment.NewLine + ex.Message);
                //_simMainWindow.HideLoadingMessage();
                return false;
            }
        }

    }
}
