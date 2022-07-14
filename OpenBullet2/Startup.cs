using Blazor.FileReader;
using Blazored.Modal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenBullet2.Auth;
using OpenBullet2.Repositories;
using OpenBullet2.Services;
using RuriLib.Services;
using System.Globalization;
using Blazored.LocalStorage;
using BlazorDownloadFile;
using OpenBullet2.Models.Jobs;
using OpenBullet2.Models.Data;
using OpenBullet2.Models.Proxies;
using OpenBullet2.Helpers;
using System;
using System.Threading;
using System.IO;
using OpenBullet2.Logging;
using RuriLib.Logging;
using System.Net;
using RuriLib.Providers.UserAgents;
using RuriLib.Providers.RandomNumbers;
using System.Threading.Tasks;

namespace OpenBullet2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
            ThreadPool.SetMinThreads(1000, 1000);

            services.AddRazorPages();
            services.AddServerSideBlazor()
                .AddHubOptions(x => x.MaximumReceiveMessageSize = 102400000)
                .AddCircuitOptions(options => { options.DetailedErrors = true; });
            services.AddBlazoredModal();
            services.AddFileReaderService();
            services.AddBlazorDownloadFile();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
            
            services.AddScoped<AuthenticationStateProvider, OBAuthenticationStateProvider>();
            services.AddBlazoredLocalStorage();
            services.AddHttpContextAccessor();

            // Repositories
            services.AddScoped<IProxyRepository, DbProxyRepository>();
            services.AddScoped<IProxyGroupRepository, DbProxyGroupRepository>();
            services.AddScoped<IHitRepository, DbHitRepository>();
            services.AddScoped<IJobRepository, DbJobRepository>();
            services.AddScoped<IGuestRepository, DbGuestRepository>();
            services.AddScoped<IRecordRepository, DbRecordRepository>();
            services.AddScoped<IThemeRepository>(_ => new DiskThemeRepository("wwwroot/css/themes"));
            services.AddScoped<IConfigRepository>(_ => new DiskConfigRepository("UserData/Configs"));
            services.AddScoped<IWordlistRepository>(service => 
                new HybridWordlistRepository(service.GetService<ApplicationDbContext>(),
                "UserData/Wordlists"));

            // Singletons
            services.AddSingleton<UpdateService>();
            services.AddSingleton<AnnouncementService>();
            services.AddSingleton<MetricsService>();
            services.AddSingleton<VolatileSettingsService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<JwtValidationService>();
            services.AddSingleton<ProxyReloadService>();
            services.AddSingleton<JobFactoryService>();
            services.AddSingleton<JobManagerService>();
            services.AddSingleton<JobMonitorService>();
            services.AddSingleton<DataPoolFactoryService>();
            services.AddSingleton<ProxySourceFactoryService>();
            services.AddSingleton(_ => new RuriLibSettingsService("UserData"));
            services.AddSingleton(_ => new PersistentSettingsService("UserData"));
            services.AddSingleton(_ => new PluginRepository("UserData/Plugins"));
            services.AddSingleton<IRandomUAProvider>(_ => new IntoliRandomUAProvider("user-agents.json"));
            services.AddSingleton<IRNGProvider, DefaultRNGProvider>();
            services.AddSingleton<MemoryJobLogger>();
            services.AddSingleton<IJobLogger>(service =>
                new FileJobLogger(service.GetService<RuriLibSettingsService>(),
                "UserData/Logs/Jobs"));

            // Transient
            services.AddTransient<BrowserConsoleLogger>();
            services.AddTransient(service => 
                new ConfigSharingService(service.GetService<IConfigRepository>(),
                "UserData"));

            // Localization
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
                {
                    var settingsService = context.RequestServices.GetService<PersistentSettingsService>();
                    return Task.FromResult(new ProviderCultureResult(settingsService.OpenBulletSettings.GeneralSettings.Culture));
                }));

                var supportedCultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("it"),
                    new CultureInfo("fr"),
                    new CultureInfo("de"),
                    new CultureInfo("es"),
                    new CultureInfo("pt"),
                    new CultureInfo("nl"),
                    new CultureInfo("ru"),
                    new CultureInfo("tr"),
                    new CultureInfo("ro")
                };

                options.DefaultRequestCulture = new RequestCulture("en", "en");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var obSettings = app.ApplicationServices.GetService<PersistentSettingsService>().OpenBulletSettings;

            if (RootChecker.IsRoot())
                Console.WriteLine(RootChecker.RootWarning);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (obSettings.SecuritySettings.HttpsRedirect)
                app.UseHttpsRedirection();

            app.UseRequestLocalization();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action}");

                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            // Apply DB migrations or create a DB if it doesn't exist
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();
            }

            // Load the configs
            var configService = app.ApplicationServices.GetService<ConfigService>();
            var configRepo = app.ApplicationServices.GetService<IConfigRepository>();
            configService.ReloadConfigs().Wait();
        }
    }
}
