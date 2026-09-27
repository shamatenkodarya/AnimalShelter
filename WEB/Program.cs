using BLL.IServices;
using BLL.Services;
using DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DAL.Entities;
using BLL.MappingProfiles;
using Microsoft.Extensions.Logging;    
using System;    

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;      
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();      

builder.Services.AddAutoMapper(typeof(MappingProfile));      

builder.Services.AddScoped<IAnimalRepository, AnimalRepository>();
builder.Services.AddScoped<IAdoptionRepository, AdoptionRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAnimalService, AnimalService>();
builder.Services.AddScoped<IAdoptionService, AdoptionService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

builder.Services.AddControllersWithViews()
    .AddViewComponentsAsServices();     

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);    

    options.LoginPath = "/Account/Login";     
    options.AccessDeniedPath = "/Account/AccessDenied";      
    options.SlidingExpiration = true;      
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Начало инициализации БД и ролей...");
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roleNames = { "Admin", "Manager", "Consultant", "Controller", "Visitor" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation("Роль '{RoleName}' создана.", roleName);
            }
        }
        logger.LogInformation("Проверка ролей завершена.");

        string adminEmail = "admin@animalshelter.local";    
        string adminPass = "AdminPass123!";       

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,      
                Email = adminEmail,
                FullName = "Главный Администратор",
                EmailConfirmed = true,      
                RegistrationDate = DateTime.UtcNow,
                Position = "Administrator"  
            };

            var result = await userManager.CreateAsync(adminUser, adminPass);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");    
                logger.LogInformation("Пользователь администратора '{AdminEmail}' создан и ему присвоена роль Admin.", adminEmail);
            }
            else
            {
                logger.LogError("Ошибка при создании администратора: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Пользователь администратора '{AdminEmail}' уже существует.", adminEmail);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Произошла ошибка при инициализации базы данных или создании ролей/администратора.");
    }
    logger.LogInformation("Инициализация БД и ролей завершена.");
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
}

app.UseHttpsRedirection();     
app.UseStaticFiles();         

app.UseRouting();   

app.UseAuthentication();     
app.UseAuthorization();     

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");   

app.Run();   