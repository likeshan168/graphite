﻿using System.Reflection;
using Graphite.Reflection;

namespace Graphite.Setup
{
    public partial class ConfigurationDsl
    {
        /// <summary>
        /// Enables the diagnostics page.
        /// </summary>
        public ConfigurationDsl EnableDiagnostics()
        {
            _configuration.Diagnostics = true;
            return this;
        }

        /// <summary>
        /// Enables the diagnostics page when the
        /// calling assembly is in debug mode.
        /// </summary>
        public ConfigurationDsl EnableDiagnosticsInDebugMode()
        {
            _configuration.Diagnostics = Assembly
                .GetCallingAssembly().IsInDebugMode();
            return this;
        }

        /// <summary>
        /// Enables the diagnostics page when the 
        /// type assembly is in debug mode.
        /// </summary>
        public ConfigurationDsl EnableDiagnosticsInDebugMode<T>()
        {
            _configuration.Diagnostics = typeof(T).Assembly.IsInDebugMode();
            return this;
        }

        /// <summary>
        /// Sets the url of the diagnostics page.
        /// </summary>
        public ConfigurationDsl WithDiagnosticsAtUrl(string url)
        {
            _configuration.DiagnosticsUrl = url;
            return this;
        }

        /// <summary>
        /// Excludes the diagnostics pages from authentication.
        /// </summary>
        public ConfigurationDsl ExcludeDiagnosticsFromAuthentication(string statusMessage)
        {
            _configuration.ExcludeDiagnosticsFromAuthentication = true;
            return this;
        }
    }
}
