using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Newtonsoft.Json;

namespace Middlewares
{
    /// <summary>
    /// Middleware forward request to valid servers
    /// </summary>
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="next"></param>
        public ProxyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// business role is done here
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext httpContext)
        {
            var headers = httpContext.Request.Headers;

            if (headers.ContainsKey("Transfer-Action") && headers["Transfer-Action"].Equals("Forward"))
            {
                httpContext.Request.Headers.Add("Original-Path", httpContext.Request.Path.ToString());
                httpContext.Request.Headers.Add("Original-QueryString", httpContext.Request.QueryString.ToString());
                httpContext.Request.Headers.Add("Original-Method", httpContext.Request.Method);
                httpContext.Request.Headers.Add("Original-ContentType", httpContext.Request.ContentType);

                httpContext.Request.Path = "/api/proxy";
                httpContext.Request.ContentType = "application/json";
                httpContext.Request.Method = "POST";
            }

            await _next(httpContext);
        }
    }

    /// <summary>
    /// Extension method used to add Middleware to http request pipeline
    /// </summary>
    public static class ProxyMiddlewareExtensions
    {
        /// <summary>
        /// extension method declaration
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseProxyMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProxyMiddleware>();
        }
    }
}
