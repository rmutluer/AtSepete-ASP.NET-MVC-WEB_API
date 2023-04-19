using AtSepete.Business.Abstract;
using AtSepete.Business.Concrete;
using AtSepete.Repositories.Abstract;
using AtSepete.Repositories.Concrete;
using Microsoft.EntityFrameworkCore;
using AtSepete.DataAccess.Extensions;
using AtSepete.Business.Mapper.Profiles;
using System.Reflection;
using AtSepete.UI.Extensions;
using AtSepete.UI.MapperUI.Profiles;
using AtSepete.Business.Extensions;
using AtSepete.Repositories.Extensions;
using AspNetCoreHero.ToastNotification.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services
    .AddDataAccessServices(builder.Configuration)
    .AddBusinessServices(builder.Configuration)
    .AddRepositoriesServices()
    .AddMvcServices();



var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();//Bu metot, geli�tirme ortam�nda �al���rken herhangi bir hata meydana geldi�inde, kullan�c�ya ayr�nt�l� bir hata sayfas� g�sterir.
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseNotyf();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
