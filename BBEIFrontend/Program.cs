using BBEIDataAccess;
using BBEIDataAccess.Models;
using BBEIFrontend.Areas.Identity;
using BBEILib;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BBEIFrontend
{
    public class Program
    {
        public static string Version { get; set; } = "1.1.4";

        public static CoreIdentity CoreIdentity { get; set; } = new();
        public static ServiceProvider? ServiceProvider { get; set; }
        public static bool SystemUsersCreated { get; internal set; } = false;
        public static bool SystemRolesCreated { get; internal set; } = false;
        public static bool SystemUserRolesCreated { get; internal set; } = false;

        public static DateTime LastRefresh_DisplayGui = DateTime.Now;

        public static Task taskCheckManager = null;
        public static CancellationTokenSource? tokenSourceCheckManager;
        public static CancellationToken tokenCheckManager;
        public static AutoResetEvent? AutoCheckDispatcher { get; set; }
        public static object LockCheck = new object();
        public static object CleanUpCheck = new object();
        public static AutoResetEvent? EventEngine { get; set; }

        public static DateTime FirstRun {  get; set; } = DateTime.Now;
        public static string AntiCache { get; set; } = DateTime.Now.ToString("yyMMddHH");

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("Logs/BBEI-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31)
            .CreateLogger();

            Log.Information("BBEI app started...");

            EventEngine = new AutoResetEvent(false);

            var builder = WebApplication.CreateBuilder(args);

            CoreIdentity = builder.Configuration.GetSection("CoreIdentity").Get<CoreIdentity>();
            if (CoreIdentity == null)
                CoreIdentity = new CoreIdentity();

            // Add services to the container.
            CoreIdentity.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            //Overwrite con le variabili d'ambiente definite
            GetEnvVariables();

            builder.Services.AddDbContext<BBEIContext>(options => options.UseSqlite(CoreIdentity.ConnectionString));

            //builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            //builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<BBEIContext>();

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = false;
                options.User.AllowedUserNameCharacters += "_";
                //TODO quando abiliteremo la gestione utenti sarà da rivedere
                //options.Password.RequireNonAlphanumeric = services.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>().IsDevelopment() ? false : true;
                //options.Password.RequiredLength = services.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>().IsDevelopment() ? 8 : 16;
                //options.Password.RequireLowercase = true;
                //options.Password.RequireUppercase = services.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>().IsDevelopment() ? false : true;
                //options.Password.RequireDigit = services.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>().IsDevelopment() ? false : true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;
                options.Lockout.AllowedForNewUsers = false;
            }).AddEntityFrameworkStores<BBEIContext>().AddDefaultTokenProviders();

            ServiceProvider = builder.Services.BuildServiceProvider();

            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();

            var app = builder.Build();

            //Esecuzione migrations
            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BBEIContext>();
                db.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<BlazorCookieLoginMiddleware>();

            app.MapControllers();
            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Start();

            GestoreDb programDb = new GestoreDb();

            Engine eng = new Engine();
            eng.AvvioMotore();
        }

        private static void GetEnvVariables()
        {
            try
            {
                Log.Information("GetEnvVariables...");
                //Lettura variabili di ambiente, overwrite dei parametri da appSettings, se esistono
                string? envValue = null;
                
                envValue = Environment.GetEnvironmentVariable("ConnectionString");
                if (envValue != null)
                {
                    CoreIdentity.ConnectionString = envValue;
                    Log.Information("GetEnvVariables: ConnectionString acquired from environment variables...");
                }

                envValue = Environment.GetEnvironmentVariable("BBEIAdminUser");
                if (envValue != null)
                {
                    CoreIdentity.BBEIAdminUser = envValue;
                    Log.Information("GetEnvVariables: BBEIAdminUser acquired from environment variables...");
                }

                envValue = Environment.GetEnvironmentVariable("BBEIAdminPassword");
                if (envValue != null)
                {
                    CoreIdentity.BBEIAdminPassword = envValue;
                    Log.Information("GetEnvVariables: BBEIAdminPassword acquired from environment variables...");
                }
            }
            catch (Exception ex)
            {
                Log.Error("GetEnvVariables error: " + ex.Message);
            }
        }
    }
}