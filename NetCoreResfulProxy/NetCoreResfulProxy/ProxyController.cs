using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.DTO.Account;
using Domain.DTO.Generics;
using Domain.DTO.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Services;
using Services.Interfaces;

namespace Areas.api
{
    /// <summary>
    /// handle development methods
    /// </summary>
    [Produces("application/json")]
    [Route("api/proxy")]
    [Authorize(Policy = "Bearer")]
    public class ProxyController : AppController
    {
        private readonly IForwardSevice _forwardService;
        private readonly IActivesAccountListService _ActivesAccountList;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public ProxyController(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _forwardService = (IForwardSevice)serviceProvider.GetService(typeof(IForwardSevice));
            _ActivesAccountList = (IActivesAccountListService)serviceProvider.GetService(typeof(IActivesAccountListService));
        }

        /// <summary>
        /// handle and forward request
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Post()
        {
            var server = GetAuthorizedServer();
            var response = _forwardService.RedirectRequest(server, Request);

            return StatusCode(response.StatusCode, response.Body);
        }

        /// <summary>
        ///get list of services by list playerId and tokens
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ListOfAccountsOfUser), 200)]
        [ProducesResponseType(typeof(Error), 400)]
        [ProducesResponseType(typeof(Error), 409)]
        [ProducesResponseType(typeof(Error), 500)]
        [Route("api/listServices")]
        public IActionResult getListServices(string playerId)
        {
            ListOfAccountsOfUser accountList = _ActivesAccountList.GetActivesAccountList(playerId);
            return StatusCode(200, accountList);
        }
    }
}