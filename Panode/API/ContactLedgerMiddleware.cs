using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Panode.Core;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Panode.API
{
    public class ContactLedgerMiddleware
    {
        readonly RequestDelegate _next;

        public ContactLedgerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ContactLedger contacts)
        {
            try
            {
                /*context.Request.EnableBuffering();

                context.Request.Body.Position = 0;

                var buffer = new byte[context.Request.Body.Length];
                var count = context.Request.Body.Read(buffer, 0, buffer.Length);
                var json = Encoding.UTF8.GetString(buffer, 0, count);
                var jObject = JObject.Parse(json);

                var version = (string)jObject["version"];
                var isAccessible = (bool)jObject["isAccessible"];
                var hostname = (string)jObject["hostname"];

                if (isAccessible)
                {
                    contacts.AddContact(hostname);
                }

                context.Request.Body.Position = 0;
                */

                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var ret = JsonConvert.SerializeObject(new //dont share exceptions in final version/make optional (off by default)
            {
                exception.Message,
                exception.StackTrace
            });

            return context.Response.WriteAsync(ret);
        }

        private static Task HandleIncorrectVersionAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var ret = JsonConvert.SerializeObject(new
            {
                Error = "Invalid version"
            });

            return context.Response.WriteAsync(ret);
        }
    }

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseContactLedgerMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ContactLedgerMiddleware>();
        }
    }
}
