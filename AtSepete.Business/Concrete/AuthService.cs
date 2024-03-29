﻿using AtSepete.Business.Constants;
using AtSepete.Dtos.Dto.Users;
using AtSepete.Entities.Data;
using AtSepete.Results.Concrete;
using AtSepete.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AtSepete.Business.Abstract;
using AtSepete.Business.JWT;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Web;
using AtSepete.Business.Logger;
using AtSepete.Repositories.Abstract;
using Microsoft.AspNetCore.Http;
using IResult = AtSepete.Results.IResult;

namespace AtSepete.Business.Concrete
{
    public class AuthService:IAuthService
    {
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILoggerService _loggerService;
        private readonly ITokenHandler _tokenHandler;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IUserService userService, IUserRepository userRepository, IMapper mapper, ILoggerService loggerService, ITokenHandler tokenHandler, IEmailSender emailSender, IHttpContextAccessor httpContextAccessor = null)
        {
            _userService = userService;
            _userRepository = userRepository;
            _mapper = mapper;
            _loggerService = loggerService;
            _tokenHandler = tokenHandler;
            _emailSender = emailSender;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<IDataResult<ChangePasswordDto>> ChangePasswordAsync(ChangePasswordDto changePasswordDto)//kullanıcı şifresini değiştirir
        {
            try
            {
                if (changePasswordDto == null)
                {
                    _loggerService.LogWarning(LogMessages.User_Object_Not_Valid);
                    return new ErrorDataResult<ChangePasswordDto>(Messages.ObjectNotValid);
                }
                var currentUser = await _userRepository.GetByDefaultAsync(x => x.Email == changePasswordDto.Email);
                if (currentUser is null)
                {
                    _loggerService.LogWarning(LogMessages.User_Object_Not_Found);
                    return new ErrorDataResult<ChangePasswordDto>(Messages.UserNotFound);
                }
                //changePasswordDto.CurrentPassword = await PasswordHashAsync(changePasswordDto.CurrentPassword);

                var passhased = currentUser.Password;
                byte[] hashBytes = Convert.FromBase64String(passhased);
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);
                var pbkdf2 = new Rfc2898DeriveBytes(changePasswordDto.CurrentPassword, salt, 100000);
                byte[] hash = pbkdf2.GetBytes(20);

                for (int i = 0; i < 20; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                    {
                        _loggerService.LogWarning(LogMessages.User_Password_Not_Match);
                        return new ErrorDataResult<ChangePasswordDto>(Messages.PasswordNotMatch);
                    }
                }

                changePasswordDto.NewPassword = await PasswordHashAsync(changePasswordDto.NewPassword);
                var userMap = _mapper.Map(changePasswordDto, currentUser);
                await _userRepository.UpdateAsync(userMap);
                await _userRepository.SaveChangesAsync();
                _loggerService.LogInfo(LogMessages.User_ChangePassword_Success);
                return new SuccessDataResult<ChangePasswordDto>(_mapper.Map<User, ChangePasswordDto>(userMap), Messages.ChangePasswordSuccess);
            }
            catch (Exception)
            {
                _loggerService.LogError(LogMessages.User_ChangePassword_Fail);
                return new ErrorDataResult<ChangePasswordDto>(Messages.ChangePasswordFail);
            }

        }


        public async Task<IResult> CheckPasswordAsync(CheckPasswordDto checkPasswordDto)//login olurken şifre kontrolü
        {
            try
            {
                if (checkPasswordDto == null)
                {
                    _loggerService.LogWarning(LogMessages.User_Object_Not_Valid);
                    return new ErrorResult(Messages.ObjectNotFound);
                }
                var currentUser = await _userRepository.GetByDefaultAsync(x => x.Email == checkPasswordDto.Email);
                if (currentUser is null)
                {
                    _loggerService.LogWarning(LogMessages.User_Object_Not_Found);
                    return new ErrorResult(Messages.UserNotFound);
                }
                var passhased = currentUser.Password;
                byte[] hashBytes = Convert.FromBase64String(passhased);
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);
                var pbkdf2 = new Rfc2898DeriveBytes(checkPasswordDto.Password, salt, 100000);
                byte[] hash = pbkdf2.GetBytes(20);

                for (int i = 0; i < 20; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                    {
                        _loggerService.LogWarning(LogMessages.User_CheckPassword_Not_Valid);
                        return new ErrorResult(Messages.CheckPasswordNotValid);
                    }
                }
                _loggerService.LogInfo(LogMessages.User_CheckPassword_Valid);
                return new SuccessResult(Messages.CheckPasswordValid);
            }
            catch (Exception)
            {
                _loggerService.LogError(LogMessages.User_CheckPassword_Fail);
                return new ErrorDataResult<CheckPasswordDto>(Messages.CheckPasswordFail);
            }

        }

        public async Task<IResult> UpdateRefreshToken(string refreshToken, UserDto userDto, DateTime accessTokenDate, int AddOnAccessTokenDate)//token üretildiğinde refresh token değerini oluşturup veritabanına kaydeder
        {
            if (userDto is not null)
            {
                var user = await _userRepository.GetByIdAsync(userDto.Id);
                user.RefreshToken = refreshToken;
                user.RefreshTokenEndDate = accessTokenDate.AddHours(AddOnAccessTokenDate);
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();
                _loggerService.LogInfo($"Updated refresh token");
                return new SuccessResult();
            }
            _loggerService.LogError("update refresh token is fail");
            return new ErrorResult();
        }

        protected async Task<string> PasswordHashAsync(string password)//şifre hashlemek için kullanırız sadece burada yazdık o yüzden protected
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000);//salt, her kullanıcı için farklıdır ve hash'in sonucunu değiştirir. Bu nedenle, aynı şifreyi kullanan iki kullanıcının hash'leri farklı olacaktır.
            byte[] hash = pbkdf2.GetBytes(20);
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            string savedPasswordHash = Convert.ToBase64String(hashBytes);
            _loggerService.LogInfo(LogMessages.User_PasswordHash_Success);
            return savedPasswordHash;
        }

        public async Task<IDataResult<UserDto>> CheckUserSignAsync(CheckPasswordDto checkPasswordDto, bool lockoutOnFailure)//giriş yapan kullanıcının mail ve şifresini kontrol eder ve user'ı geriye döndürür.Yani Login olan kullancının bilgilerini kontrol eder doğrularsa ona göre login işlemleri yapılabilir
        {
            if (checkPasswordDto is null)
            {
                _loggerService.LogWarning(LogMessages.User_Object_Not_Valid);
                return new ErrorDataResult<UserDto>(Messages.ObjectNotValid);
            }
            var userDto = await _userService.FindUserByEmailAsync(checkPasswordDto.Email);
            if (userDto.Data is null)
            {
                _loggerService.LogWarning(LogMessages.User_Object_Not_Found);
                return new ErrorDataResult<UserDto>(Messages.ObjectNotFound);
            }
            var userCheck = await CheckPasswordAsync(checkPasswordDto);
            if (!lockoutOnFailure)//hesap kitleme aktif değilse giriş denemesi burada yapılır
            {
                if (userDto.IsSuccess)
                {
                    if (userCheck.IsSuccess)
                    {
                        //kullanıcının lockOnEnable özelliği false çekilmesi gerekebilir!
                        var user = await _userRepository.GetByIdAsync(userDto.Data.Id);
                        user.LockoutEnabled = false;
                        await _userRepository.UpdateAsync(user);
                        await _userRepository.SaveChangesAsync();
                        _loggerService.LogInfo(LogMessages.User_Login_Success);//login başarılı
                        return new SuccessDataResult<UserDto>(userDto.Data, Messages.LoginSuccess);
                    }
                    else
                    {
                        _loggerService.LogWarning(LogMessages.User_Password_Fail);//hatalı şifre mesajı dön
                        return new ErrorDataResult<UserDto>(userDto.Data, Messages.PasswordFail);
                    }
                }

                _loggerService.LogWarning(LogMessages.User_Email_Fail);//hatalı email girişi
                return new ErrorDataResult<UserDto>(Messages.EmailOrPasswordInvalid);//şifre veya mail hatalı
            }
            if (userDto.Data.LockoutEnd > DateTime.Now)
            {
                _loggerService.LogWarning($"Hesabınız {userDto.Data.LockoutEnd}'e Kadar Kilitlendi");
                return new ErrorDataResult<UserDto>($"Hesabınız {userDto.Data.LockoutEnd}'e Kadar Kilitlendi");//hesap kilitli ise buraya uğrayacak ve bunu dönecek!
            }
            _loggerService.LogInfo(LogMessages.User_Password_Lock_Enabled); //şifre kilitleme aktif                                 
            return await PasswordSignInAsync(checkPasswordDto);
        }

        protected async Task<IDataResult<UserDto>> PasswordSignInAsync(CheckPasswordDto checkPasswordDto)
        {
            //kullanıcı şifreyi hatalı girdiği anda buraya uğrar
            var userDto = await _userService.FindUserByEmailAsync(checkPasswordDto.Email);
            var userCheck = await CheckPasswordAsync(checkPasswordDto);
            if (userDto.Data is null)
            {
                _loggerService.LogWarning(LogMessages.User_Email_Fail);//bu mailde kullanıcı bullanılamadı
                return new ErrorDataResult<UserDto>(Messages.EmailFailed);//kullanıcı mail'i hatalı
            }
            var currentUser = await _userRepository.GetByIdAsync(userDto.Data.Id);
            if (currentUser.AccessFailedDate is not null)//daha önce hatalı giriş yapmış mı yoksa ilk girişi mi kontrol ederiz
            {
                TimeSpan ts = DateTime.Now - currentUser.AccessFailedDate.Value;
                if (ts.TotalMinutes > 15)//son hatalı girişten sonra 15 dakika geçmiş mi ya da şifre kilitlenmişse de süresini doldurmuş mu diye bakılır!
                {
                    currentUser.AccessFailedCount = 0;
                    //currentUser.AccessFailedDate=null gerekli olursa bakılacak!!
                    _loggerService.LogInfo(LogMessages.User_AccessFailedCount_Has_Been_Reset_To_Zero);// AccessFailedCount sıfırlandı
                }
            }
            if (userDto.IsSuccess)//doğru email ile kullanıcı gelmiş mi kontrol ederiz ve gelmiş ise giriş işlemi denenir
            {
                if (userCheck.IsSuccess && (currentUser.LockoutEnd <= DateTime.Now || currentUser.LockoutEnd is null))//şifre başarılı ve daha önceden hesap kilitlenmişse kontrol yapar kilitleme süresi dolmuşşa girebilir
                {
                    currentUser.AccessFailedCount = 0;
                    await _userRepository.UpdateAsync(currentUser);
                    await _userRepository.SaveChangesAsync();
                    _loggerService.LogInfo(LogMessages.User_Login_Success);
                    return new SuccessDataResult<UserDto>(userDto.Data, Messages.LoginSuccess);//buradan alınan değer login controllerda yakalanıcak
                }
                else
                {
                    currentUser.AccessFailedCount++;
                    currentUser.AccessFailedDate = DateTime.Now;
                    if (currentUser.AccessFailedCount >= 5)
                    {
                        currentUser.LockoutEnd = DateTime.Now.AddMinutes(30);//kaç dakika kilitlemek istiyorsak o süre kadar hesabı kilitler , 30 dakika olarak belirlendi.
                        _loggerService.LogInfo(LogMessages.User_Add_30_Minutes_To_AccessFailedDate);//AccessFailedDate'e 30 dakika eklendi
                    }
                    await _userRepository.UpdateAsync(currentUser);
                    await _userRepository.SaveChangesAsync();
                    _loggerService.LogWarning(LogMessages.User_Login_Fail);
                    return new ErrorDataResult<UserDto>(Messages.LoginFailed);
                }
            }
            _loggerService.LogWarning(LogMessages.User_Email_Fail);//hatalı email 
            return new ErrorDataResult<UserDto>(userDto.Data, Messages.EmailOrPasswordInvalid);//hatalı şifre veya email döneriz
        }

        public async Task<IDataResult<string>> ForgetPasswordEmailSenderAsync(ForgetPasswordEmailDto emailDto)
        {
            try
            {
                var user = await _userService.FindUserByEmailAsync(emailDto.Email);
                if (user.Data == null)
                {
                    _loggerService.LogWarning(LogMessages.User_Email_Fail);//bu mailde kullanıcı bullanılamadı
                    return new ErrorDataResult<string>(Messages.EmailFailed);//kullanıcı mail'i hatalı
                }

                Token token = _tokenHandler.ResetPasswordToken(20, emailDto);

                string url = $"https://localhost:7290/Login/NewPassword?token={token.AccessToken}";
                var content = $"Merhaba, <br />" +
                    $"Şifreni yenilemek için linke tıklayabilirsin: " +
                    $"<a href='{url}'> Şifre Yenile </a>" +
                    $"İyi alışverişler dileriz.. <br /> <br />" +
                    $"AtSepete";
                await _emailSender.SendEmailAsync(emailDto.Email, "Şifre Yenile", content); //Mail gönderiliyor
                _loggerService.LogInfo(LogMessages.User_ResetPasswordEmailSender_Success);
                return new SuccessDataResult<string>(content, Messages.ResetPasswordEmailSender_Success);
            }
            catch (Exception)
            {

                _loggerService.LogError(LogMessages.User_ResetPasswordEmailSender_Fail);
                return new ErrorDataResult<string>(Messages.ResetPasswordEmailSender_Fail);
            }

        }

        public async Task<IResult> ResetPasswordAsync(NewPasswordDto newPasswordDto)
        {
            try
            {
                if (newPasswordDto is null)
                {
                    _loggerService.LogWarning(LogMessages.User_Object_Not_Valid);
                    return new ErrorResult(Messages.ObjectNotValid);
                }
                var userDto = await _userService.FindUserByEmailAsync(newPasswordDto.Email);
                if (userDto.Data is null)
                {
                    _loggerService.LogWarning(LogMessages.User_Object_Not_Found);
                    return new ErrorResult(Messages.ObjectNotFound);
                }
                if (string.IsNullOrEmpty(newPasswordDto.Token))
                {
                    _loggerService.LogWarning(LogMessages.User_Token_Not_Found);
                    return new ErrorResult(Messages.UserTokenNotFound);
                }
                var user = await _userRepository.GetByIdAsync(userDto.Data.Id);

                newPasswordDto.Password = await PasswordHashAsync(newPasswordDto.Password);
                var updateUser = _mapper.Map(newPasswordDto, user);
                var result = await _userRepository.UpdateAsync(updateUser);
                await _userRepository.SaveChangesAsync();

                _loggerService.LogInfo(LogMessages.User_ResetPassword_Success);
                return new SuccessResult(Messages.ResetPasswordSuccess);
            }
            catch (Exception)
            {
                _loggerService.LogError(LogMessages.User_ResetPassword_Fail);
                return new ErrorResult(Messages.ResetPasswordFail);
            }
        }

        public async Task<IDataResult<Token>> RefreshTokenSignInAsync(RefreshTokenLoginDto refreshTokenLoginDto)//refresh token geçerliyse token süresini uzatır
        {
            try
            {
                User? user = await _userRepository.GetByDefaultAsync(x => x.RefreshToken == refreshTokenLoginDto.RefreshTokenLogin);
                var userDto = _mapper.Map<User, UserDto>(user);
                if (user != null && user?.RefreshTokenEndDate > DateTime.UtcNow)
                {
                    var claims = new List<Claim>()
                    {
                        new Claim("ID", userDto.Id.ToString()),
                        new Claim(ClaimTypes.Name, userDto.FirstName),
                        new Claim(ClaimTypes.Surname, userDto.LastName),
                        new Claim(ClaimTypes.Email, userDto.Email),
                        new Claim(ClaimTypes.Role, userDto.Role.ToString())
                    };
                    var identity = new ClaimsIdentity(claims);
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                    Token token = _tokenHandler.CreateAccessToken(24, principal);
                    await UpdateRefreshToken(token.RefreshToken, userDto, token.Expirition, 6);

                    _loggerService.LogInfo(LogMessages.User_Login_Success);
                    return new SuccessDataResult<Token>(token, Messages.LoginSuccess);
                }
                else
                {
                    _loggerService.LogWarning(LogMessages.User_Login_Fail);
                    return new ErrorDataResult<Token>(Messages.LoginFailed);
                }
            }
            catch (Exception)
            {
                _loggerService.LogError(LogMessages.User_Login_Fail);
                return new ErrorDataResult<Token>(Messages.LoginFailed);
            }

        }

        public async Task<IDataResult<Token>> SignInAsync(UserDto userDto, bool IsSuccess)//buradan claimsPrincipal tipinde gönderilen veri login controllerda yakalanacak ve //await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal); metot ile login işlemi başarılı olacak!!!
        {
            try
            {
                if (IsSuccess && userDto is not null)
                {

                    var claims = new List<Claim>()
                    {
                        new Claim("ID", userDto.Id.ToString()),
                        new Claim(ClaimTypes.Name, userDto.FirstName),
                        new Claim(ClaimTypes.Surname, userDto.LastName),
                        new Claim(ClaimTypes.Email, userDto.Email),
                        new Claim(ClaimTypes.Role, userDto.Role.ToString())
                    };
                    var identity = new ClaimsIdentity(claims);
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                    Token token = _tokenHandler.CreateAccessToken(24, principal);
                    await UpdateRefreshToken(token.RefreshToken, userDto, token.Expirition, 6);


                    _loggerService.LogInfo(LogMessages.User_Login_Success);
                    return new SuccessDataResult<Token>(token, Messages.LoginSuccess);

                }
                else
                {
                    _loggerService.LogWarning(LogMessages.User_Login_Fail);
                    return new ErrorDataResult<Token>(Messages.LoginFailed);
                }
            }
            catch (Exception)
            {
                _loggerService.LogError(LogMessages.User_Login_Fail);
                return new ErrorDataResult<Token>(Messages.LoginFailed);
            }

        }

        protected async Task<string> ToEncodedString(Token token)// bu metot sadece token'ı encoded etmek için buraya özel yazılmıştır
        {
            // Token'ın byte dizisi olarak temsilini al
            byte[] tokenBytes = Encoding.UTF8.GetBytes(token.ToString());

            // Base64 kodlaması yaparak byte dizisini string'e dönüştür
            string encodedToken = Convert.ToBase64String(tokenBytes);

            // URL-encoding yap ve sonucu döndür
            return HttpUtility.UrlEncode(encodedToken);
        }
        protected async Task<string> FromEncodedString(string encodedToken)//bu metot sadece encode olan token'ı decoded etmek için buraya özel yazılmıştır...
        {
            // URL-decoding yap
            string decodedToken = HttpUtility.UrlDecode(encodedToken);

            // Base64 kodlamasını çözerek byte dizisine dönüştür
            byte[] tokenBytes = Convert.FromBase64String(decodedToken);

            // Byte dizisini string'e çevirerek sonucu döndür
            return Encoding.UTF8.GetString(tokenBytes);
        }

        public async Task<IResult> SignOutAsync()
        {
            try
            {
                _loggerService.LogInfo(LogMessages.User_LogOut_Success);
                return new SuccessResult(Messages.LogOutSuccess);
            }
            catch (Exception)
            {
                _loggerService.LogError(LogMessages.User_LogOut_Fail);
                return new ErrorResult(Messages.LogOutFailed);
            }

        }

    }
}
