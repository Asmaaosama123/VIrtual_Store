using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using Stripe;
using Vstore.Helpers;
using Vstore.Hubs;
using Vstore.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Vstore.Controllers;

namespace Vstore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

           
            builder.Services.AddDbContext<AppDBContext>(options =>
                options.UseSqlServer(connectionString));
           
           
            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                
                options.User.RequireUniqueEmail = true;

              
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;

                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.SignIn.RequireConfirmedEmail = true;

            })
            .AddEntityFrameworkStores<AppDBContext>() 
            .AddDefaultTokenProviders(); 
            
            // Configure the token lifespan for email confirmations, password resets, etc.
            builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
                options.TokenLifespan = TimeSpan.FromHours(6));

            // Register controllers
            builder.Services.AddControllers();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Set timeout as per your requirement
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true; // Make the session cookie essential
            });

            builder.Services.Configure<JWT>(builder.Configuration.GetSection("JWT"));
            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(o => {
                    o.RequireHttpsMetadata = false;
                    o.SaveToken = true;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidIssuer= builder.Configuration["JWT:Issure"],
                        ValidAudience= builder.Configuration["JWT:Audience"],
                        IssuerSigningKey =new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]))

                    };
                    });


            builder.Services.AddAuthorization();
            
            // Set up Swagger/OpenAPI for API documentation
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider; // Ensure you are using the default provider
            });
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name= "Authorization",
                    Type= SecuritySchemeType.ApiKey,
                    Scheme="Bearer",
                    BearerFormat="JWT",
                    In= ParameterLocation.Header,
                    Description= "Enter the jwt key" 
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                {
                    new OpenApiSecurityScheme()
                    {
                        Reference = new OpenApiReference()
                        {
                            Type= ReferenceType.SecurityScheme,
                            Id= "Bearer"
                        },
                        Name= "Bearer",
                         In= ParameterLocation.Header


                    },
                    new List<string>()
                    }

                });

            });
            // Configure CORS to allow all origins, methods, and headers
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });
            builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.AddSignalR();
            builder.Services.AddScoped<NotificationService>();
            StripeConfiguration.SetApiKey(builder.Configuration.GetSection("Stripe")["Secretkey"]);
            // ›Ì `Startup.cs` √Ê `Program.cs`:
            //builder.Services.Configure<KestrelServerOptions>(options =>
            //{
            //    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
            //    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
            //});
            builder.Services.AddHttpClient<Hunyuan3DController>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5); // √Ê √Ì Êﬁ  √ÿÊ· „‰ 120 À«‰Ì…
            });

            builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
            builder.Services.AddHttpClient<SegmindService>();
            builder.Services.Configure<SegmindSettings>(builder.Configuration.GetSection("Segmind"));
            builder.Services.AddControllers();
            var app = builder.Build();
            app.MapHub<NotificationHub>("/notificationHub");
            // Enable CORS globally with the "AllowAll" policy
            app.UseCors("AllowAll");

            // Enable Swagger in all environments for API testing
            app.UseSwagger();
            app.UseSwaggerUI();

            // Enable HTTPS redirection
            app.UseHttpsRedirection();

            // Make sure authentication comes before authorization
            app.UseAuthentication();
            app.UseAuthorization();
           app.UseRouting();
           app.UseSession();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            app.UseStaticFiles();
            app.UseDefaultFiles(); 
           // app.UseStaticFiles();

            // Map controller routes
            app.MapControllers();

            // Run the application
            app.Run();
        }
       

    }
}
