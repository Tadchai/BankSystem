using BankApp.Models; // อ้างอิง ApplicationDbContext
using Microsoft.EntityFrameworkCore; // สำหรับ AddDbContext
using Microsoft.AspNetCore.Authentication.JwtBearer; // สำหรับ AddAuthentication
using Microsoft.AspNetCore.Builder; // สำหรับ WebApplication
using Microsoft.Extensions.DependencyInjection; // สำหรับ IServiceCollection และ AddControllers, AddAuthorization
using Microsoft.IdentityModel.Tokens; // สำหรับ TokenValidationParameters
using System.Text;
using System; // อ้างอิง Version
using DotNetEnv; // สำหรับการโหลด environment variables จากไฟล์ .env
using System.IO;


var builder = WebApplication.CreateBuilder(args);

// โหลด environment variables จากไฟล์ .env ถ้ามี
if (File.Exists(".env"))
{
    DotNetEnv.Env.Load();
}

// อ่านค่า ConnectionStrings จาก environment variables หรือ appsettings.json
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

//Test CONNECTION_STRING JWT_KEY .env
//Console.WriteLine($"Connection String: {Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")}");
//Console.WriteLine($"JWT Key: {Environment.GetEnvironmentVariable("JWT_KEY")}");


builder.Services.AddControllers();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 25))));

// ตั้งค่า Authentication ด้วย JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseStaticFiles(); // สำหรับเสิร์ฟไฟล์สแตติก เช่น HTML, CSS, JavaScript

app.UseRouting();

// เปิดใช้ Authentication และ Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();
app.MapFallbackToFile("index.html"); // ให้แสดง index.html ถ้าไม่มี route ที่ตรงกัน

// เริ่มต้นแอปพลิเคชัน
app.Run();
