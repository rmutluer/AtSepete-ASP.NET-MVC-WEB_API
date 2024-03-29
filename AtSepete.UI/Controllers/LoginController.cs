﻿using AtSepete.Dtos.Dto.Categories;
using AtSepete.Dtos.Dto.Users;
using AtSepete.Entities.Data;
using AtSepete.UI.ApiResponses.LoginApiResponse;
using AtSepete.UI.ApiResponses.UserApiResponse;
using AtSepete.UI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NToastNotify;
using SendGrid;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;

namespace AtSepete.UI.Controllers
{
    public class LoginController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public LoginController(IMapper mapper, IToastNotification toastNotification,IConfiguration configuration) : base(toastNotification,configuration)
        {
            _mapper = mapper;
            _configuration = configuration;
        }
        [HttpGet]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> ChangePassword()
        {
            ChangePasswordVM changePasswordVM = new();
            changePasswordVM.Email = UserEmail;
            return View(changePasswordVM);
            //< partial name = "~/Views/Partials/LoginPartials/_ChangePassword.cshtml" > 

        }
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM changePasswordVM)
        {
            using (var httpClient = new HttpClient())
            {
                ChangePasswordDto changePasswordDto = _mapper.Map<ChangePasswordVM, ChangePasswordDto>(changePasswordVM);

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", UserToken);
                StringContent content = new StringContent(JsonConvert.SerializeObject(changePasswordDto), Encoding.UTF8, "application/Json");
                using (var response = await httpClient.PostAsync($"{ApiBaseUrl}/Auth/ChangePassword", content))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("RefreshTokenLogin", "Login", new { returnUrl = HttpContext.Request.Path });//notify ile tekrardan email al demeliyiz
                    }
                    string apiAnswer = await response.Content.ReadAsStringAsync();
                    ChangePasswordResponse changePasswordResponse = JsonConvert.DeserializeObject<ChangePasswordResponse>(apiAnswer);
                    if (!changePasswordResponse.IsSuccess)
                    {
                        NotifyErrorLocalized(changePasswordResponse.Message);
                        return View(changePasswordVM);
                    }
                    NotifySuccessLocalized(changePasswordResponse.Message);
                    return RedirectToAction("Index", UserRole, new {area=UserRole});
                }
            }

        }
        [HttpGet]
        public async Task<IActionResult> ForgetPassword()
        {

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM forgetPasswordVM)
        {
            var forgetPasswordEmailDto = _mapper.Map<ForgetPasswordVM, ForgetPasswordEmailDto>(forgetPasswordVM);

            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(forgetPasswordEmailDto), Encoding.UTF8, "application/Json");
                using (var answer = await httpClient.PostAsync($"{ApiBaseUrl}/Auth/ForgetPasswordEmailSender", content))
                {
                    string apiAnswer = await answer.Content.ReadAsStringAsync();
                    ForgetPasswordResponse forgetPasswordResponse = JsonConvert.DeserializeObject<ForgetPasswordResponse>(apiAnswer);
                    if (!forgetPasswordResponse.IsSuccess)
                    {
                        NotifyErrorLocalized(forgetPasswordResponse.Message);
                        return View(forgetPasswordVM);
                    }
                    NotifySuccessLocalized(forgetPasswordResponse.Message);
                    return RedirectToAction("Login", "Login");
                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> NewPassword(string token)
        {
            if (token is null)
            {
                return RedirectToAction("ForgetPassword", "Login");
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var decodeToken = tokenHandler.ReadJwtToken(token);
            NewPasswordVM newPassword = new();
            newPassword.Token = token;
            newPassword.Email = decodeToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            return View(newPassword);
        }
        [HttpPost]
        public async Task<IActionResult> NewPassword(NewPasswordVM newPasswordVM)
        {
            using (var httpClient = new HttpClient())
            {
                var newPasswordDto = _mapper.Map<NewPasswordVM, NewPasswordDto>(newPasswordVM);

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newPasswordDto.Token);
                StringContent content = new StringContent(JsonConvert.SerializeObject(newPasswordDto), Encoding.UTF8, "application/Json");
                using (var response = await httpClient.PostAsync($"{ApiBaseUrl}/Auth/ResetPassword", content))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("ForgetPassword", "Login");//notify ile tekrardan email al demeliyiz
                    }
                    string apiAnswer = await response.Content.ReadAsStringAsync();
                    NewPasswordResponse newPasswordResponse = JsonConvert.DeserializeObject<NewPasswordResponse>(apiAnswer);
                    if (!newPasswordResponse.IsSuccess)
                    {
                        NotifyErrorLocalized(newPasswordResponse.Message);
                        return RedirectToAction("ForgetPassword", "Login");
                    }
                    NotifySuccessLocalized(newPasswordResponse.Message);
                    return RedirectToAction("Login", "Login");
                }
            }

        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            
           
            var createUserDto = _mapper.Map<RegisterVM, CreateUserDto>(registerVM);

            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(createUserDto), Encoding.UTF8, "application/Json");
                using (var answer = await httpClient.PostAsync($"{ApiBaseUrl}/User/AddUser", content))
                {
                    string apiAnswer = await answer.Content.ReadAsStringAsync();
                    CreateUserResponse createUserResponse = JsonConvert.DeserializeObject<CreateUserResponse>(apiAnswer);
                    if (!createUserResponse.IsSuccess)
                    {
                        NotifyErrorLocalized(createUserResponse.Message);
                        return View(registerVM);
                    }
                    NotifySuccessLocalized(createUserResponse.Message);
                    return RedirectToAction("Login", "Login");
                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> RefreshTokenLogin(string returnUrl)
        {

            using (var httpClient = new HttpClient())
            {
                RefreshTokenLoginDto refreshTokenLoginDto = new();
                refreshTokenLoginDto.RefreshTokenLogin = UserRefreshToken;
                StringContent content = new StringContent(JsonConvert.SerializeObject(refreshTokenLoginDto), Encoding.UTF8, "application/Json");
                using (HttpResponseMessage response = await httpClient.PostAsync($"{ApiBaseUrl}/Auth/RefreshTokenLoginSignIn", content))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    LoginUserResponse loginUser = JsonConvert.DeserializeObject<LoginUserResponse>(apiResponse);
                    if (loginUser.IsSuccess)
                    {
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var decodeToken = tokenHandler.ReadJwtToken(loginUser.Data.AccessToken);
                        var userRole = decodeToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                        var userEmail = decodeToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                        var userName = decodeToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                        var userId = decodeToken.Claims.FirstOrDefault(c => c.Type == "ID")?.Value;

                        var claims = new List<Claim>
                        {
                                new Claim("Token", loginUser.Data.AccessToken), // Token burada eklenir
                                new Claim("RefreshToken", loginUser.Data.RefreshToken),
                                new Claim(ClaimTypes.Role, userRole),
                                new Claim(ClaimTypes.Email, userEmail),
                                new Claim("UserId", userId),
                                new Claim(ClaimTypes.Name, userName)
                        };
                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        HttpContext.Response.Cookies.Delete("AtSepeteCookie");

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Login", "Login");// eğer böyle bir kullanıcı yoksa(loginvm de gönderilecek)
                    }

                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(loginVM), Encoding.UTF8, "application/Json");
                using (HttpResponseMessage response = await httpClient.PostAsync($"{ApiBaseUrl}/Auth/LoginSignIn", content))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    LoginUserResponse loginUser = JsonConvert.DeserializeObject<LoginUserResponse>(apiResponse);
                    if (loginUser.IsSuccess)
                    {
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var decodeToken = tokenHandler.ReadJwtToken(loginUser.Data.AccessToken);
                        var userRole = decodeToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                        var userEmail = decodeToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                        var userName = decodeToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                        var userId = decodeToken.Claims.FirstOrDefault(c => c.Type == "ID")?.Value;

                        var claims = new List<Claim>
                        {
                                new Claim("Token", loginUser.Data.AccessToken), // Token burada eklenir
                                new Claim("RefreshToken", loginUser.Data.RefreshToken), // Token burada eklenir
                                new Claim(ClaimTypes.Role, userRole),
                                new Claim(ClaimTypes.Email, userEmail),
                                new Claim("UserId", userId),
                                new Claim(ClaimTypes.Name, userName)
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                        NotifySuccessLocalized(loginUser.Message);
                        return RedirectToAction("Index", userRole, new {area=userRole});//login olunca yönleneceği sayfa areasına göre yöneleceği ilk sayfa!

                    }
                    else
                    {
                        NotifyErrorLocalized(loginUser.Message);
                        return View();//giriş yapılamazsa!!
                    }


                };
            };
        }

        [HttpGet]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Response.Cookies.Delete("AtSepeteCookie");// var olan cookie tarayıcıdan temizlenir!!
            NotifySuccess("Çıkış Başarılı");
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public async Task<IActionResult> AccessDeniedPage()
        {

            return View();
        }
    }
}
