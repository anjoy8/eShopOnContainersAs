using Autofac;
using Autofac.Extensions.DependencyInjection;
using HealthChecks.UI.Client;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.eShopOnContainers.Services.Identity.API.Certificates;
using Microsoft.eShopOnContainers.Services.Identity.API.Data;
using Microsoft.eShopOnContainers.Services.Identity.API.Devspaces;
using Microsoft.eShopOnContainers.Services.Identity.API.Models;
using Microsoft.eShopOnContainers.Services.Identity.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Reflection;

namespace Microsoft.eShopOnContainers.Services.Identity.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // 注册App监控(搁置)
            RegisterAppInsights(services);

            #region 微软 Identity 类库部分
            // 添加 Microsoft.AspNetCore.Identity.EntityFrameworkCore 服务
            // ApplicationDbContext 上下文
            services.AddDbContext<ApplicationDbContext>(options =>
                    // 配置地址
                    options.UseSqlServer(Configuration["ConnectionString"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        // 迁移的应用
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        // 失败规则
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            // 添加用户角色配置
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();


            // 用户登录服务：Microsoft.Extensions.Identity.Core包
            services.AddTransient<ILoginService<ApplicationUser>, EFLoginService>();
            // 跳转服务
            services.AddTransient<IRedirectService, RedirectService>();
            #endregion

            #region 配置数据及保护数据部分
            // 获取配置信息：对象方法
            services.Configure<AppSettings>(Configuration);

            // 获取配置信息：字节方法
            if (Configuration.GetValue<string>("IsClusterEnv") == bool.TrueString)
            {
                // 数据保护
                services.AddDataProtection(opts =>
                {
                    opts.ApplicationDiscriminator = "eshop.identity";
                })
                //Data Protection（数据安全）机制：为了确保Web应用敏感数据的安全存储，该机制提供了一个简单、基于非对称加密改进的、性能良好的、开箱即用的加密API用于数据保护。它不需要开发人员自行生成密钥，它会根据当前应用的运行环境，生成该应用独有的一个私钥。这在单一部署的情况下没有问题。所以在集群情况下，为了确保加密数据的互通，应用必须共享私钥。
                //Microsoft.AspNetCore.DataProtection.StackExchangeRedis
                .PersistKeysToRedis(ConnectionMultiplexer.Connect(Configuration["DPConnectionString"]), "DataProtection-Keys");
            }
            #endregion

            #region 健康检查 服务部分
            // 健康检查 Microsoft.Extensions.Diagnostics.HealthChecks
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                // HealthChecks.SqlServer包
                .AddSqlServer(Configuration["ConnectionString"],
                    name: "IdentityDB-check",
                    tags: new string[] { "IdentityDB" }); 
            #endregion

            #region Ids4 服务部分

            var connectionString = Configuration["ConnectionString"];
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            // 添加 IdentityServer4 服务，3.x版本
            services.AddIdentityServer(x =>
            {
                x.IssuerUri = "null";
                x.Authentication.CookieLifetime = TimeSpan.FromHours(2);
            })
            // 本地开发环境新增功能
            .AddDevspacesIfNeeded(Configuration.GetValue("EnableDevspaces", false))
            // 添加证书 idsrv3test.pfx 本地文件
            .AddSigningCredential(Certificate.Get())
            // 用户数据
            .AddAspNetIdentity<ApplicationUser>()
            // 配置数据
            // ConfigurationDbContext 上下文
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = builder =>
                // sqlserver
                builder.UseSqlServer(connectionString,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);

                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
            })
            // 持久化授权数据
            // PersistedGrantDbContext 上下文
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);

                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
            })
            // 配置数据服务，获取请求上下文的数据
            .Services.AddTransient<IProfileService, ProfileService>();

            #endregion

            // 添加mvc服务的三个子服务
            services.AddControllers();
            services.AddControllersWithViews();
            services.AddRazorPages();

            var container = new ContainerBuilder();
            container.Populate(services);

            // autofac 容器化
            return new AutofacServiceProvider(container.Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {

            if (env.IsDevelopment())
            {
                // 错误页面
                app.UseDeveloperExceptionPage();
                // efcore的输出页面
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // 配置基础路径
            var pathBase = Configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                loggerFactory.CreateLogger<Startup>().LogDebug("Using PATH BASE '{pathBase}'", pathBase);
                app.UsePathBase(pathBase);
            }

            app.UseStaticFiles();

            // 使工作身份服务器重定向在edge和最新版本的浏览器。警告:在生产环境中无效。
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Content-Security-Policy", "script-src 'unsafe-inline'");
                await next();
            });

            // 转发标头
            app.UseForwardedHeaders();

            // 添加 IdentityServer4 服务中间件
            app.UseIdentityServer();

            //修复一个问题与chrome。Chrome启用了一个新功能“没有相同的cookie必须是安全的”，cookie应该从https中删除，但在eShop中，aks和docker的内部共通之处是http。为了避免这个问题，cookies的策略应该是宽松的。
            app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = AspNetCore.Http.SameSiteMode.Lax });

            // 路由
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
                {
                    Predicate = r => r.Name.Contains("self")
                });
            });
        }

        /// <summary>
        /// 性能监控管理服务
        /// </summary>
        /// <param name="services"></param>
        private void RegisterAppInsights(IServiceCollection services)
        {
            //Application Insights 是 Azure Monitor 的一项功能，是面向开发人员和 DevOps 专业人员的可扩展应用程序性能管理 (APM) 服务。 使用它可以监视实时应用程序。 它将自动检测性能异常，并且包含了强大的分析工具来帮助诊断问题，了解用户在应用中实际执行了哪些操作。
            services.AddApplicationInsightsTelemetry(Configuration);
            //K8s，同理
            services.AddApplicationInsightsKubernetesEnricher();
        }
    }
}
