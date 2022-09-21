extern alias OS;
using DotNetForHtml5.EmulatorWithoutJavascript;
using System;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace OpenSilver.Simulator
{
    internal class OpenSilverRuntime
    {
        private SimBrowser _simBrowser;
        private readonly MainWindow _simMainWindow;
        private Dispatcher _SimDispatcher;
        private Dispatcher _OSDispatcher;
        private Assembly _OSRuntimeAssembly;
        private Action _ClientAppStartup;

        public JavaScriptExecutionHandler JavaScriptExecutionHandler { get; set; }

        public Action OnInitialized { get; set; }


        public  OpenSilverRuntime(SimBrowser simBrowser, MainWindow simMainWindow, Dispatcher simDispatcher)
        {
            _simBrowser = simBrowser;
            _simMainWindow = simMainWindow;
            _SimDispatcher = simDispatcher;
            ReflectionInUserAssembliesHelper.TryGetCoreAssembly(out _OSRuntimeAssembly);
        }

        //public static void Start(SimBrowser simBrowser, MainWindow simMainWindow, Dispatcher wpfDispatcher)
        //{
        //    AppDomainSetup ads = new AppDomainSetup();
        //    ads.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;

        //    ads.DisallowBindingRedirects = false;
        //    ads.DisallowCodeDownload = true;
        //    ads.ConfigurationFile =
        //        AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

        //    var osAppDomain = AppDomain.CreateDomain("OS App Domain");
        //}

        public bool Start(Action clientAppStartup)
        {
            _ClientAppStartup = clientAppStartup;
            try
            {
                var osThread = new Thread(new ThreadStart(() =>
                {
                    _OSDispatcher = Dispatcher.CurrentDispatcher;

                    if (!Initialize())
                        return;

                    _ClientAppStartup();

                    Dispatcher.Run();
                }));

                osThread.SetApartmentState(ApartmentState.STA);
                osThread.IsBackground = true;
                osThread.Start();

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
            try
            {
                JavaScriptExecutionHandler = new JavaScriptExecutionHandler(_simBrowser);
                OS::DotNetForHtml5.Core.INTERNAL_Simulator.JavaScriptExecutionHandler = JavaScriptExecutionHandler;
                OS::DotNetForHtml5.Core.INTERNAL_Simulator.ClipboardHandler = new ClipboardHandler();
                OS::DotNetForHtml5.Core.INTERNAL_Simulator.IsRunningInTheSimulator_WorkAround = true;
                OS::DotNetForHtml5.Core.INTERNAL_Simulator.SimulatorProxy =
                    new SimulatorProxy(_simBrowser, _simMainWindow.Console, _SimDispatcher, _OSDispatcher);
                OS::DotNetForHtml5.Core.INTERNAL_Simulator.SimulatorProxy.IsOSRuntimeRunning = true;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while loading the application: " + Environment.NewLine + Environment.NewLine + ex.Message);
                //_simMainWindow.HideLoadingMessage();
                return false;
            }
        }

        public bool Initialize0()
        {
            // In OpenSilver we already have the user application type passed to the constructor, so we do not need to retrieve it here
            try
            {
                // Create the JavaScriptExecutionHandler that will be called by the "Core" project to interact with the Emulator:
                JavaScriptExecutionHandler = new JavaScriptExecutionHandler(_simBrowser);

                // Create the HTML DOM MANAGER proxy and pass it to the "Core" project:
                //JSValue htmlDocument = (JSObject)_simBrowser.Browser.ExecuteJavaScriptAndReturnValue("document");

                //InteropHelpers.InjectDOMDocument(_simBrowser.Browser.GetDocument(), _OSRuntimeAssembly);
                //InteropHelpers.InjectHtmlDocument(htmlDocument, _OSRuntimeAssembly);//no need for this line right ?
                //InteropHelpers.InjectWebControlDispatcherBeginInvoke(_simBrowser, _OSRuntimeAssembly);
                //InteropHelpers.InjectWebControlDispatcherInvoke(_simBrowser, _OSRuntimeAssembly);
                //InteropHelpers.InjectWebControlDispatcherCheckAccess(_simBrowser, _OSRuntimeAssembly);
                //InteropHelpers.InjectConvertBrowserResult(BrowserResultConverter.CastFromJsValue, _OSRuntimeAssembly);
                InteropHelpers.InjectJavaScriptExecutionHandler(JavaScriptExecutionHandler, _OSRuntimeAssembly);
                //InteropHelpers.InjectWpfMediaElementFactory(_OSRuntimeAssembly);
                //InteropHelpers.InjectWebClientFactory(_OSRuntimeAssembly);
                InteropHelpers.InjectClipboardHandler(_OSRuntimeAssembly);
                InteropHelpers.InjectSimulatorProxy(new SimulatorProxy(_simBrowser, _simMainWindow.Console, _SimDispatcher, _OSDispatcher), _OSRuntimeAssembly);

                // In the OpenSilver Version, we use this work-around to know if we're in the simulator
                InteropHelpers.InjectIsRunningInTheSimulator_WorkAround(_OSRuntimeAssembly);

                //WpfMediaElementFactory._gridWhereToPlaceMediaElements = _simMainWindow.GridForAudioMediaElements;

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

        public void Stop()
        {
            OS::DotNetForHtml5.Core.INTERNAL_Simulator.SimulatorProxy.IsOSRuntimeRunning = false;
        }
    }
}
