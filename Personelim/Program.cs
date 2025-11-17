using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Personelim.Data;
using Personelim.Services.Auth;
using Personelim.Services.Business;
using Personelim.Services.Invitation;
using System.Text;
using Personelim.Services.Location;
using Personelim.Validators;
using Personelim.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") 
    ?? builder.Configuration["Jwt:Key"];
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
    ?? builder.Configuration["Jwt:Issuer"];
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
    ?? builder.Configuration["Jwt:Audience"];

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IBusinessValidator, BusinessValidator>();

var key = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Personelim API",
        Version = "v1",
        Description = "Personel yönetim sistemi API"
    });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header kullanarak Bearer şeması. Örnek: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ✅ Migration ve Seed Data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Migration kontrol ve uygulama
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        
        if (pendingMigrations.Any())
        {
            Console.WriteLine($"⏳ {pendingMigrations.Count} migration uygulanıyor:");
            foreach (var migration in pendingMigrations)
            {
                Console.WriteLine($"   - {migration}");
            }
            
            db.Database.Migrate();
            Console.WriteLine("✅ Tüm migration'lar başarıyla uygulandı!");
        }
        else
        {
            Console.WriteLine("✅ Database güncel - uygulanacak migration yok");
        }
        
        // İl-İlçe verilerini kontrol et ve ekle
        if (!db.Provinces.Any())
        {
            Console.WriteLine("⏳ İl-İlçe verileri API'den çekiliyor...");
            
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            
            try
            {
                var response = await httpClient.GetStringAsync("https://turkiyeapi.dev/api/v1/provinces");
                var apiResponse = JsonSerializer.Deserialize<TurkeyApiResponse>(response);
                
                if (apiResponse?.Data != null && apiResponse.Data.Any())
                {
                    var provinceId = 1;
                    var districtId = 1;
                    
                    foreach (var provinceData in apiResponse.Data)
                    {
                        var province = new Province 
                        { 
                            Id = provinceId,
                            Name = provinceData.Name 
                        };
                        db.Provinces.Add(province);
                        
                        if (provinceData.Districts != null)
                        {
                            foreach (var districtData in provinceData.Districts)
                            {
                                db.Districts.Add(new District
                                {
                                    Id = districtId++,
                                    Name = districtData.Name,
                                    ProvinceId = provinceId
                                });
                            }
                        }
                        
                        provinceId++;
                    }
                    
                    await db.SaveChangesAsync();
                    Console.WriteLine($"✅ {apiResponse.Data.Count} il ve {districtId - 1} ilçe eklendi!");
                }
                else
                {
                    Console.WriteLine("⚠️ API'den veri alınamadı");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ İl-İlçe API hatası: {ex.Message}");
                Console.WriteLine("   Uygulama çalışmaya devam edecek ancak il-ilçe verileri eksik olabilir.");
            }
        }
        else
        {
            Console.WriteLine($"✅ İl-İlçe verileri mevcut ({db.Provinces.Count()} il, {db.Districts.Count()} ilçe)");
        }
        
        // Uygulanan migration'ları göster
        var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
        Console.WriteLine($"📊 Toplam {appliedMigrations.Count} migration uygulanmış");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Başlatma hatası: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"❌ Inner Exception: {ex.InnerException.Message}");
        }
        
        throw; // Migration başarısız olursa uygulama başlamasın
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// API Response Models
public class TurkeyApiResponse
{
    public string Status { get; set; }
    public List<TurkeyProvinceData> Data { get; set; }
}

public class TurkeyProvinceData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<TurkeyDistrictData> Districts { get; set; }
}

public class TurkeyDistrictData
{
    public int Id { get; set; }
    public string Name { get; set; }
}