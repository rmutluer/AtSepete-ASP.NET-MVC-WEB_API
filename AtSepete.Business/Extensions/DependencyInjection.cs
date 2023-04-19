﻿using AtSepete.Business.Abstract;
using AtSepete.Business.Concrete;
using AtSepete.Business.JWT;
using AtSepete.Business.Logger;
using AtSepete.Business.Mapper.Profiles;
using AtSepete.Repositories.Abstract;
using AtSepete.Repositories.Concrete;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AtSepete.Business.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();//servislerde httpContext'e ulaşabilmek için
            services.AddScoped<ITokenHandler, JWT.TokenHandler>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options =>
               {
                   options.TokenValidationParameters = new()
                   {
                       ValidateAudience = true,// hangi sitelerin veya kimlerin kullancağını belirleriz
                       ValidateIssuer = true,//oluşturulacak token değerini kimin dağıttığının belirlendiği yerdir
                       ValidateLifetime = true,//oluşturulan token değerinin süresini kontrol edecek olan doğrulama
                       ValidateIssuerSigningKey = true,//üretilecek token değerinin uygulamamıza ait bir değer olduğunu ifade eden security key  verisinin doğrulanmasıdır
                       ValidAudience = configuration["Token:Audience"],
                       ValidIssuer = configuration["Token:Issuer"],
                       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Token:SecurityKey"])),
                       LifetimeValidator = (notBefore, expires, securityToken, validationParameters) => expires != null ? expires > DateTime.UtcNow : false
                       //expires=> gelen jwt=token=accessToken nin ömrüne bakar.eğer ki süresini doldurmuşsa kullanılamaz.

                   };
                   options.Events = new JwtBearerEvents
                   {
                       OnTokenValidated = context =>
                       {
                           var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;

                           // Kontrol etmek istediğiniz roller veya izinler burada belirtilir.
                           if (!claimsIdentity.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == "Admin"||c.Value=="Customer")))
                           {
                               context.Fail("Unauthorized");
                           }

                           return Task.CompletedTask;
                       }
                   };
               });
               //.AddJwtBearer("Customer", options =>
               //{
               //    options.TokenValidationParameters = new()
               //    {
               //        ValidateAudience = true,// hangi sitelerin veya kimlerin kullancağını belirleriz
               //        ValidateIssuer = true,//oluşturulacak token değerini kimin dağıttığının belirlendiği yerdir
               //        ValidateLifetime = true,//oluşturulan token değerinin süresini kontrol edecek olan doğrulama
               //        ValidateIssuerSigningKey = true,//üretilecek token değerinin uygulamamıza ait bir değer olduğunu ifade eden security key  verisinin doğrulanmasıdır
               //        ValidAudience = configuration["Token:Audience"],
               //        ValidIssuer = configuration["Token:Issuer"],
               //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Token:SecurityKey"])),
               //        LifetimeValidator = (notBefore, expires, securityToken, validationParameters) => expires != null ? expires > DateTime.UtcNow : false
               //        //expires=> gelen jwt=token=accessToken nin ömrüne bakar.eğer ki süresini doldurmuşsa kullanılamaz.

               //    };
               //    options.Events = new JwtBearerEvents
               //    {
               //        OnTokenValidated = context =>
               //        {
               //            var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;

               //            // Kontrol etmek istediğiniz roller veya izinler burada belirtilir.
               //            if (!claimsIdentity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Customer"))
               //            {
               //                context.Fail("Unauthorized");
               //            }

               //            return Task.CompletedTask;
               //        }
               //    };
               //});

            services.AddAutoMapper(
            Assembly.GetExecutingAssembly(),
            typeof(CategoryProfile).Assembly,
            typeof(MarketProfile).Assembly,
            typeof(OrderProfile).Assembly,
            typeof(OrderDetailProfile).Assembly,
            typeof(ProductMarketProfile).Assembly,
            typeof(ProductProfile).Assembly,
            typeof(UserProfile).Assembly
            );
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IMarketService, MarketService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IProductMarketService, ProductMarketService>();
            services.AddScoped<IOrderDetailService, OrderDetailService>();
            services.AddScoped<IUserService, UserService>();
            services.AddSingleton<ILoggerService, LoggerService>();
            return services;
        }
    }
}
