using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Jarvis.DocumentStore.Host.Support.Filters
{
    /// <summary>
    /// Simple class to allow only certain IP to call document store.
    /// This allows to open port in firewall to allow READ-ONLY access.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SecurityFilterAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// This is the list of the Ip that can call EVERYTHING on this computer
        /// </summary>
        private readonly HashSet<string> _allowedIpList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "localhost",
            "127.0.0.1",
            "::1"
        };

        /// <summary>
        /// Key is the  name of the controller, the list of string are all actions of that controller that
        /// are assocaited to Get permission
        /// </summary>
        private readonly Dictionary<String, HashSet<String>> _getControllerInfo;

        /// <summary>
        /// This is the list of the Ip that can only get blob, these are the machine that uses this
        /// documentstore as secondary instance
        /// </summary>
        private readonly HashSet<String> _getOnlyAllowedIpList;

        public SecurityFilterAttribute(IEnumerable<String> allowedIpList, IEnumerable<String> getOnlyIpList)
        {
            //machine name is always allowed.
            _allowedIpList.Add(Environment.MachineName);
            foreach (var ip in allowedIpList)
            {
                _allowedIpList.Add(ip);
            }

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork || ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _allowedIpList.Add(ip.ToString());
                }
            }
            _getOnlyAllowedIpList = new HashSet<string>(getOnlyIpList);

            _getControllerInfo = new Dictionary<string, HashSet<string>>()
            {
                ["Documents"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "GetFormat",
                    "GetCustomData"
                }
            };
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var callingIp = GetClientIpAddress(actionContext.Request);
            Boolean isOk = _allowedIpList.Contains(callingIp);
            if (!isOk)
            {
                //maybe this ip can only access in get
                if (_getOnlyAllowedIpList.Contains(callingIp))
                {
                    if (_getControllerInfo.TryGetValue(actionContext.ControllerContext.ControllerDescriptor.ControllerName, out var allowedAction))
                    {
                        if (allowedAction.Contains(actionContext.ActionDescriptor.ActionName))
                        {
                            isOk = true;
                        }
                    }
                }
            }
            if (!isOk)
            {
                throw new SecurityException($"IP {callingIp} is not in the list of allowed hosts");
            }
            base.OnActionExecuting(actionContext);
        }

        private static string GetClientIpAddress(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return IPAddress.Parse(((HttpContextBase)request.Properties["MS_HttpContext"]).Request.UserHostAddress).ToString();
            }
            if (request.Properties.ContainsKey("MS_OwinContext"))
            {
                return IPAddress.Parse(((OwinContext)request.Properties["MS_OwinContext"]).Request.RemoteIpAddress).ToString();
            }
            return String.Empty;
        }
    }
}
