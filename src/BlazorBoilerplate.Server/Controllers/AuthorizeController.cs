﻿using BlazorBoilerplate.Server.Models;
using BlazorBoilerplate.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlazorBoilerplate.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthorizeController : ControllerBase
    {
        // Logger instance
        ILogger<AuthorizeController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthorizeController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AuthorizeController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginParameters parameters)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState.Values.SelectMany(state => state.Errors)
                                                                        .Select(error => error.ErrorMessage)
                                                                        .FirstOrDefault());

            var user = await _userManager.FindByNameAsync(parameters.UserName);
            if (user == null)
            {
                _logger.LogInformation("User does not exist: {0}", parameters.UserName);
                return BadRequest("User does not exist");
            }

            var singInResult = await _signInManager.CheckPasswordSignInAsync(user, parameters.Password, false);

            if (!singInResult.Succeeded)
            {
                _logger.LogInformation("Invalid password: {0}, {1}", parameters.UserName, parameters.Password);
                return BadRequest("Invalid password");
            }

            _logger.LogInformation("Logged In: {0}, {1}", parameters.UserName, parameters.Password);
            await _signInManager.SignInAsync(user, parameters.RememberMe);
            return Ok(BuildUserInfo(user));
        }


        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterParameters parameters)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState.Values.SelectMany(state => state.Errors)
                                                                        .Select(error => error.ErrorMessage)
                                                                        .FirstOrDefault());

            var user = new ApplicationUser();
            user.UserName = parameters.UserName;
            var result = await _userManager.CreateAsync(user, parameters.Password);
            if (!result.Succeeded) return BadRequest(result.Errors.FirstOrDefault()?.Description);

            _logger.LogInformation("New user registered: {0}", user);

            return await Login(new LoginParameters
            {
                UserName = parameters.UserName,
                Password = parameters.Password
            });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SendPasswordResetEmail(string emailAddress)
        {
            var user = await _userManager.FindByEmailAsync(emailAddress);
            if (user == null)
            {
                return Ok();
            }

            return Ok();

            // Todo Complete Email Service / Email Templates / Password reset

            //user.SetNewPasswordResetCode();
            //var passwordResetCode = user.PasswordResetCode;

            //var email = this.L(
            //    "PasswordResetEmailBody",
            //    _configuration.GetSection("App:ClientRootAddress").Value.TrimEnd('/'),
            //    user.TenantId,
            //    user.Id,
            //    WebUtility.UrlEncode(passwordResetCode));

            //_emailSender.Send(
            //    from: (await SettingManager.GetSettingValueAsync(EmailSettingNames.DefaultFromAddress)),
            //        to: user.EmailAddress,
            //        subject: this.L("PasswordResetEmailSubject"),
            //        body: email,
            //        isBodyHtml: true
            //    );

        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User Logged out");
            await _signInManager.SignOutAsync();           
            return Ok();
        }

        [Authorize]
        [HttpGet]
        public async Task<UserInfo> UserInfo()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            return BuildUserInfo(user);
        }
        
        private UserInfo BuildUserInfo(ApplicationUser user)
        {
            return new UserInfo
            {
                Username = user.UserName
            };
        }
    }
}