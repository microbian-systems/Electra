using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Electra.Core.Helpers;

public static class AzureFuncsBindingHelper
{
    private static bool IsStarted = false;
    private static object _syncLock = new();

    ///<summary>
    /// Sets up the app before running any other code
    /// </summary>
    public static void Startup()
    {
        if (!IsStarted)
            lock (_syncLock)
            {
                if (!IsStarted)
                {
                    AssemblyBindingRedirectHelper.ConfigureBindingRedirects();
                    IsStarted = true;
                }
            }
    }
}

public static class AssemblyBindingRedirectHelper
{
    ///<summary>
    /// Reads the "BindingRedirecs" field from the app settings and applies the redirection on the
    /// specified assemblies
    /// </summary>
    public static void ConfigureBindingRedirects()
    {
        var redirects = GetBindingRedirects();
        redirects.ForEach(RedirectAssembly);
    }

    private static List<BindingRedirect> GetBindingRedirects()
    {
        var result = new List<BindingRedirect>();
        var bindingRedirectListJson = GetJson();
        bindingRedirectListJson = Regex.Unescape(bindingRedirectListJson);
        using (var memoryStream = new MemoryStream(Encoding.Unicode.GetBytes(bindingRedirectListJson)))
        {
            var serializer = new DataContractJsonSerializer(typeof(List<BindingRedirect>));
            result = (List<BindingRedirect>)serializer.ReadObject(memoryStream);
        }

        return result;
    }

    private static string GetJson()
    {
        //return Config.GetSetting("BindingRedirects");
        const string json = @"
                    
                [
                    { ""ShortName"": ""Microsoft.ApplicationInsights"", ""RedirectToVersion"" : ""2.6.4"", ""PublicKeyToken"":""31bf3856ad364e35"" },
                    { ""ShortName"": ""System.Text.Json"", ""RedirectToVersion"": ""11.0.0.0"", ""PublicKeyToken"": ""30ad4fe6b2a6aeed"" },
                    { ""ShortName"": ""System.ComponentModel.Annotations"", ""RedirectToVersion"": ""4.2.1.0"", ""PublicKeyToken"": ""b03f5f7f11d50a3a"" }
                ]
            
            ";

        return json;
    }

    private static void RedirectAssembly(BindingRedirect bindingRedirect)
    {
        ResolveEventHandler handler = null;
        handler = (sender, args) =>
        {
            var requestedAssembly = new AssemblyName(args.Name);
            if (requestedAssembly.Name != bindingRedirect.ShortName)
            {
                return null;
            }

            var targetPublicKeyToken = new AssemblyName("x, PublicKeyToken=" + bindingRedirect.PublicKeyToken)
                .GetPublicKeyToken();
            requestedAssembly.SetPublicKeyToken(targetPublicKeyToken);
            requestedAssembly.Version = new Version(bindingRedirect.RedirectToVersion);
            requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;
            AppDomain.CurrentDomain.AssemblyResolve -= handler;
            return Assembly.Load(requestedAssembly);
        };
        AppDomain.CurrentDomain.AssemblyResolve += handler;
    }

    public class BindingRedirect
    {
        public string ShortName { get; set; }
        public string PublicKeyToken { get; set; }
        public string RedirectToVersion { get; set; }
    }
}