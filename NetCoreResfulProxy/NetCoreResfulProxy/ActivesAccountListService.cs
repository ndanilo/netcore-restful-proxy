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
using Domain.DTO.Account;
using Domain.DTO.Generics;
using Domain.DTO.User;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Services.Interfaces;

namespace Services
{
    public class ActivesAccountListService : IActivesAccountListService
    {
        private readonly IAuthService _authService;

        private readonly IList<ServerInfo> validServers = new List<ServerInfo>();

        public ActivesAccountListService(IAuthService authService, IConfiguration config)
        {
            _authService = authService;
            var serverCount = Int32.Parse(config.GetSection("Servers:Count").Value);

            for (int i = 0; i < serverCount; i++)
            {
                var server = config.GetSection($"Servers:Server{i + 1}");

                var id = server.GetSection("Id").Value;
                var key = server.GetSection("Key").Value;
                var baseUrl = server.GetSection("BaseUrl").Value;
                var hashKey = server.GetSection("HashKey").Value;
                var email = server.GetSection("Email").Value;
                var name = server.GetSection("Name").Value;

                validServers.Add(new ServerInfo
                {
                    Id = Int32.Parse(id),
                    Key = key,
                    BaseUrl = baseUrl,
                    HashKey = hashKey,
                    Email = email,
                    Name = name
                });
            }

        }

        public ListOfAccountsOfUser GetActivesAccountList(string playerId)
        {
            try
            {
                ListOfAccountsOfUser listOfAccountsOfUser = new ListOfAccountsOfUser();

                for (var i = 0; i < validServers.Count; i++)
                {
                    var account = verifyUsertRequest(validServers[i], playerId);
                    if (account.status == 200)
                    {
                        listOfAccountsOfUser.accounts.Add(new UserVerificationHasAccountNamed
                        {
                            RegisterAt = account.RegisterAt,
                            name = validServers[i].Name
                        });
                    }
                }
                return listOfAccountsOfUser;
            }
            catch (Exception e)
            {
                throw new InternalServerError("error while establishing connection with remote server");
            }
        }

        public UserVerificationHasAccount verifyUsertRequest(ServerInfo server, String playerId)
        {
            try
            {


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
                var requestMessage = new HttpRequestMessage();
                requestMessage.RequestUri = new Uri($"{server.BaseUrl}admin/user/verifyUserHasCount/{playerId}");
                requestMessage.Method = new HttpMethod("GET");

                var resultRequest = httpClient.SendAsync(requestMessage);

                response = resultRequest.Result;

                var returnedObjectContent = response.Content.ReadAsStringAsync().Result;



                var deserializedObject = JsonConvert.DeserializeObject<UserVerificationHasAccount>(returnedObjectContent);

                var status = deserializedObject.status;
                var hasAccount = deserializedObject.hasAccount;
                var RegisterAt = deserializedObject.RegisterAt;

                return new UserVerificationHasAccount
                {
                    hasAccount = deserializedObject.hasAccount,
                    RegisterAt = deserializedObject.RegisterAt,
                    status = deserializedObject.status
                };


            }
            catch (Exception e)
            {
                return new UserVerificationHasAccount
                {
                    hasAccount = false,
                    RegisterAt = new DateTime(),
                    status = 400
                };
            }
        }

    }
}
