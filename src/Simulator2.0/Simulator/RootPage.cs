using DotNetForHtml5;
using DotNetForHtml5.EmulatorWithoutJavascript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Linq;

namespace OpenSilver.Simulator
{
    internal class RootPage
    {
        private string _outputRootPath;
        private string _outputResourcesPath;
        private string _clientAppAssemblyPath;
        private Assembly _clientAppAssembly;
        private string _absolutePath;
        private string _rootPageHtml;
        public RootPage(Assembly clientAppAssembly)
        {
            _clientAppAssembly = clientAppAssembly;
            _clientAppAssemblyPath = _clientAppAssembly.Location;
        }

        public void Create(SimulatorLaunchParameters simulatorLaunchParameters)
        {
            _absolutePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "simulator_root.html");

            _rootPageHtml = File.ReadAllText(_absolutePath);

            string outputPathAbsolute = GetOutputPathAbsoluteAndReadAssemblyAttributes();

            //string outputPathAbsolute = PathsHelper.GetOutputPathAbsolute(pathOfAssemblyThatContainsEntryPoint, outputRootPath);

            // Read the "App.Config" file for future use by the ClientBase.
            string relativePathToAppConfigFolder = PathsHelper.CombinePathsWhileEnsuringEndingBackslashAndMore(_outputResourcesPath, _clientAppAssembly.GetName().Name);
            string relativePathToAppConfig = Path.Combine(relativePathToAppConfigFolder, "app.config.g.js");
            if (File.Exists(Path.Combine(outputPathAbsolute, relativePathToAppConfig)))
            {
                string scriptToReadAppConfig = "<script type=\"application/javascript\" src=\"" + Path.Combine(outputPathAbsolute, relativePathToAppConfig) + "\"></script>";
                _rootPageHtml = _rootPageHtml.Replace("[SCRIPT_TO_READ_APPCONFIG_GOES_HERE]", scriptToReadAppConfig);
            }
            else
            {
                _rootPageHtml = _rootPageHtml.Replace("[SCRIPT_TO_READ_APPCONFIG_GOES_HERE]", string.Empty);
            }

            // Read the "ServiceReferences.ClientConfig" file for future use by the ClientBase:
            string relativePathToServiceReferencesClientConfig = Path.Combine(relativePathToAppConfigFolder, "servicereferences.clientconfig.g.js");
            if (File.Exists(Path.Combine(outputPathAbsolute, relativePathToServiceReferencesClientConfig)))
            {
                string scriptToReadServiceReferencesClientConfig = "<script type=\"application/javascript\" src=\"" + Path.Combine(outputPathAbsolute, relativePathToServiceReferencesClientConfig) + "\"></script>";
                _rootPageHtml = _rootPageHtml.Replace("[SCRIPT_TO_READ_SERVICEREFERENCESCLIENTCONFIG_GOES_HERE]", scriptToReadServiceReferencesClientConfig);

            }
            else
            {
                _rootPageHtml = _rootPageHtml.Replace("[SCRIPT_TO_READ_SERVICEREFERENCESCLIENTCONFIG_GOES_HERE]", string.Empty);
            }

            _rootPageHtml = _rootPageHtml.Replace("..", "[PARENT]");

            // Set the base URL (it defaults to the Simulator exe location, but it can be specified in the command line arguments):
            string baseURL;
            string customBaseUrl;
            if (ReflectionInUserAssembliesHelper.TryGetCustomBaseUrl(out customBaseUrl))
            {
                baseURL = customBaseUrl;
            }
            else
            {
                baseURL = "file:///" + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace('\\', '/');
            }

#if OPENSILVER
            if (simulatorLaunchParameters?.InitParams != null)
            {
                _rootPageHtml = _rootPageHtml.Replace(
                    "[PARAM_INITPARAMS_GOES_HERE]",
                    $"<param name=\"InitParams\" value=\"{simulatorLaunchParameters.InitParams}\"");
            }
            else
            {
                _rootPageHtml = _rootPageHtml.Replace("[PARAM_INITPARAMS_GOES_HERE]", string.Empty);
            }
#endif
            CleanResourcesAndComments();
        }

        public string ToHtmlString()
        {
            return _rootPageHtml;
        }

        public string ToHtmlPath()
        {
            var tmpPath = _absolutePath.Replace(".html", ".tmp.html");
            File.WriteAllText(tmpPath, _rootPageHtml);
            return tmpPath;
        }

        public string GetOutputPathAbsoluteAndReadAssemblyAttributes()
        {
            //--------------------------
            // Note: this method is similar to the one in the Compiler (PathsHelper).
            // IMPORTANT: If you update this method, make sure to update the other one as well.
            //--------------------------

            // Determine the output path by reading the "OutputRootPath" attribute that the compiler has injected into the entry assembly:
            if (_outputRootPath == null)
            {
                string outputRootPath, outputAppFilesPath, outputLibrariesPath, outputResourcesPath, intermediateOutputAbsolutePath;
                ReflectionInUserAssembliesHelper.GetOutputPathsByReadingAssemblyAttributes(_clientAppAssembly, out outputRootPath, out outputAppFilesPath, out outputLibrariesPath, out _outputResourcesPath, out intermediateOutputAbsolutePath);
                _outputRootPath = outputRootPath;
            }

            string outputRootPathFixed = _outputRootPath.Replace('/', '\\');
            if (!outputRootPathFixed.EndsWith("\\") && outputRootPathFixed != "")
                outputRootPathFixed = outputRootPathFixed + '\\';

            // If the path is already ABSOLUTE, we return it directly, otherwise we concatenate it to the path of the assembly:
            string outputPathAbsolute;
            if (Path.IsPathRooted(outputRootPathFixed))
            {
                outputPathAbsolute = outputRootPathFixed;
            }
            else
            {
                outputPathAbsolute = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_clientAppAssemblyPath)), outputRootPathFixed);

                outputPathAbsolute = outputPathAbsolute.Replace('/', '\\');

                if (!outputPathAbsolute.EndsWith("\\") && outputPathAbsolute != "")
                    outputPathAbsolute = outputPathAbsolute + '\\';
            }

            return outputPathAbsolute;
        }

        public string GetOutputIndexPath()
        {
            string absoluteOutputPath = GetOutputPathAbsoluteAndReadAssemblyAttributes();
            string result = Path.Combine(absoluteOutputPath, "index.html");
            result = result.Replace('/', '\\');
            return result;
        }

        private void CleanResourcesAndComments()
        {
            var xDoc = XDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(_rootPageHtml)));
            xDoc.DocumentType.InternalSubset = null;
            var head = xDoc.Element("html").Element("head");
            var asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            head.Elements("link").Where(link =>
            {
                var path = Path.Combine(asmPath, link.Attribute("href").Value.Replace("[PARENT]", "..").Replace("%20", " ").Replace('/', '\\'));
                return !File.Exists(path);
            }).Remove();

            head.Elements("script").Where(script =>
            {
                var path = Path.Combine(asmPath, script.Attribute("src").Value.Replace("[PARENT]", "..").Replace("%20", " ").Replace('/', '\\'));
                return !File.Exists(path);
            }).Remove();

            head.Nodes().Where(node => node is XComment).Remove();

            _rootPageHtml = xDoc.ToString();
        }
    }
}
