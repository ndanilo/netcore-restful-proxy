using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Domain.AuthService;
using Domain.DTO.Generics;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Services.Interfaces;

namespace Services
{
    public class ForwardService : IForwardSevice
    {
        private readonly IAuthService _authService;
        private ILogService _logService;

        public ForwardService(IAuthService authService, ILogService logService)
        {
            _authService = authService;
            _logService = logService;
        }

        public DefaultResponse RedirectRequest(ServerInfo server, HttpRequest request)
        {
            try
            {
                var badRequestErrorMessage = "can't forward request. invalid parameters";
                var headers = request.Headers;

                var contentType = "application/json";

                var endPoint = string.Empty;
                var method = string.Empty;
                var queryString = string.Empty;

                if (!headers.ContainsKey("Original-Path"))
                    throw new BadRequestException(badRequestErrorMessage);

                if (!headers.ContainsKey("Original-Method"))
                    throw new BadRequestException(badRequestErrorMessage);

                if (headers.ContainsKey("Original-ContentType"))
                    contentType = headers["Original-ContentType"];

                if (headers.ContainsKey("Original-QueryString"))
                    queryString = headers["Original-QueryString"];

                endPoint = headers["Original-Path"];
                method = headers["Original-Method"];

                var handler = new HttpClientHandler()
                {
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                var requestToken = _authService.GenerateTokenForServerRequest(server);
                var httpClient = new HttpClient(handler);

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", requestToken);

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = null;

                var contentTypeParameters = contentType.Split(";");
                contentType = contentTypeParameters[0];

                var mediaTypeHeaderValue = new MediaTypeHeaderValue(contentType);

                for (var i = 1; i < contentTypeParameters.Length; i++)
                    mediaTypeHeaderValue.Parameters.Add(NameValueHeaderValue.Parse(contentTypeParameters[i]));

                var requestMessage = new HttpRequestMessage();
                requestMessage.RequestUri = new Uri($"{server.BaseUrl}/{endPoint}{queryString}");

                switch (method)
                {
                    case "GET":
                        requestMessage.Method = new HttpMethod("GET");
                        response = httpClient.SendAsync(requestMessage).Result;
                        break;
                    case "POST":
                        using (var scontent = new StreamContent(request.Body)
                        {
                            Headers = { ContentType = mediaTypeHeaderValue }
                        })
                        {
                            requestMessage.Method = new HttpMethod("POST");
                            requestMessage.Content = scontent;
                            response = httpClient.SendAsync(requestMessage).Result;
                        }
                        break;
                    case "PUT":
                        using (var scontent = new StreamContent(request.Body)
                        {
                            Headers = { ContentType = mediaTypeHeaderValue }
                        })
                        {
                            requestMessage.Method = new HttpMethod("PUT");
                            requestMessage.Content = scontent;
                            response = httpClient.SendAsync(requestMessage).Result;
                        }
                        break;
                    case "DELETE":
                        requestMessage.Method = new HttpMethod("DELETE");
                        response = httpClient.SendAsync(requestMessage).Result;
                        break;
                    default:
                        throw new BadRequestException(badRequestErrorMessage);
                }

                var returnedObjectContent = response.Content.ReadAsStringAsync().Result;
                var deserializedObject = JsonConvert.DeserializeObject(returnedObjectContent);

                var forwardResponse = new DefaultResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Body = deserializedObject
                };

                return forwardResponse;
            }
            catch (Exception e)
            {
                _logService.logExceptionOnDatabase(e);

                throw new InternalServerError("error while establishing connection with remote server");
            }
        }
    }
}
