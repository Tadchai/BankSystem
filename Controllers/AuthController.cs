using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using BankApp.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using System;

namespace BankApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _config = configuration;
        }

        // ฟังก์ชันสำหรับการล็อกอิน
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            // ตรวจสอบว่ามีข้อมูลที่ส่งมาครบถ้วนหรือไม่
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid input."); // แจ้งกลับถ้าข้อมูลไม่ถูกต้อง
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == model.Username); // ค้นหาผู้ใช้ตามชื่อผู้ใช้

            // ตรวจสอบว่าผู้ใช้มีอยู่ในระบบและตรวจสอบรหัสผ่านด้วย BCrypt
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials."); // แจ้งถ้าข้อมูลไม่ถูกต้อง
            }

            // สร้าง JWT Token เพื่อให้ผู้ใช้ยืนยันตัวตนในระบบ
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]); // ดึงค่า Key สำหรับการเข้ารหัสจากไฟล์ config
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Username), // สร้าง Claim สำหรับชื่อผู้ใช้
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) // สร้าง Claim สำหรับ UserId
                }),
                Expires = DateTime.UtcNow.AddHours(1), // กำหนดวันหมดอายุของ Token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature) // ใช้ HMACSHA256 ในการลงนาม
            };

            var token = tokenHandler.CreateToken(tokenDescriptor); // สร้าง JWT Token
            var tokenString = tokenHandler.WriteToken(token); // แปลง Token เป็น String

            return Ok(new { Token = tokenString }); // ส่ง JWT Token กลับไปให้ผู้ใช้
        }

        // ฟังก์ชันสำหรับการลงทะเบียน
        [HttpPost("register")]
        public IActionResult Register([FromBody] LoginModel model)
        {
            // ตรวจสอบว่ามีข้อมูลที่ส่งมาครบถ้วนหรือไม่
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input." }); // แจ้งถ้าข้อมูลไม่ถูกต้อง
            }

            // ตรวจสอบว่ารหัสผ่านยาวอย่างน้อย 8 ตัวอักษรหรือไม่
            if (model.Password.Length < 8)
            {
                return BadRequest(new { success = false, message = "Password must be at least 8 characters long." }); // แจ้งถ้ารหัสผ่านสั้นเกินไป
            }

            // ตรวจสอบว่ามีผู้ใช้ที่ใช้ Username นี้อยู่แล้วหรือไม่
            var existingUser = _context.Users.FirstOrDefault(u => u.Username == model.Username);
            if (existingUser != null)
            {
                return BadRequest(new { success = false, message = "Username is already taken." }); // แจ้งถ้าชื่อผู้ใช้มีคนใช้อยู่แล้ว
            }

            // เข้ารหัสรหัสผ่านด้วย BCrypt
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // สร้างผู้ใช้ใหม่
            var user = new User
            {
                Username = model.Username,
                PasswordHash = hashedPassword, // เก็บรหัสผ่านที่ถูกเข้ารหัส
                Balance = 0 // ยอดเงินเริ่มต้นเป็น 0
            };

            _context.Users.Add(user); // เพิ่มผู้ใช้ในฐานข้อมูล
            _context.SaveChanges(); // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล

            return Ok(new { success = true, message = "Registration successful." }); // ส่งข้อความว่าการลงทะเบียนสำเร็จ
        }

    }
}
