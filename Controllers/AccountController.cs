using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankApp.Models;
using System.Linq;
using System;
using System.Security.Claims;

namespace BankApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // ใช้สำหรับการตรวจสอบว่า ผู้ใช้ที่เข้ามาเรียก API ต้องผ่านการยืนยันตัวตนด้วย JWT Token ก่อน
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ดึงยอดเงินปัจจุบันของผู้ใช้ที่ยืนยันตัวตนแล้ว
        [HttpGet("balance")]
        public IActionResult GetBalance()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); // ดึง Claim ที่เป็น UserId จาก JWT Token
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token."); // ถ้าไม่เจอ Claim ถือว่า Token ไม่ถูกต้อง
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == int.Parse(userIdClaim.Value)); // ค้นหาผู้ใช้จากฐานข้อมูลด้วย userId ที่ดึงจาก Token
            if (user == null)
            {
                return NotFound("User not found."); // ถ้าไม่เจอผู้ใช้ ให้ส่งข้อความว่าไม่เจอ
            }

            return Ok(new { Balance = user.Balance }); // ส่งยอดเงินของผู้ใช้กลับไป
        }

        // ฟังก์ชันสำหรับฝากเงิน
        [HttpPost("deposit")]
        public IActionResult Deposit([FromBody] TransferModel model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); // ดึง Claim ที่เป็น UserId จาก JWT Token
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token."); // ถ้าไม่เจอ Claim ถือว่า Token ไม่ถูกต้อง
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == int.Parse(userIdClaim.Value)); // ค้นหาผู้ใช้จากฐานข้อมูล
            if (user == null)
            {
                return NotFound("User not found."); // ถ้าไม่เจอผู้ใช้ ให้ส่งข้อความว่าไม่เจอ
            }

            // ตรวจสอบว่าเงินที่ฝากต้องมากกว่า 0
            if (model.Amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }

            user.Balance += model.Amount; // เพิ่มยอดเงินให้กับผู้ใช้
            _context.Transactions.Add(new Transaction
            {
                SenderId = user.Id,
                ReceiverId = null, // ไม่มีผู้รับเพราะเป็นการฝากเงิน
                Amount = model.Amount,
                TransactionType = "Deposit", // ระบุว่าเป็นการฝากเงิน
                Timestamp = DateTime.UtcNow
            });
            _context.SaveChanges(); // บันทึกการเปลี่ยนแปลงลงในฐานข้อมูล

            return Ok(new { Balance = user.Balance }); // ส่งยอดเงินใหม่กลับไป
        }

        // ฟังก์ชันสำหรับถอนเงิน
        [HttpPost("withdraw")]
        public IActionResult Withdraw([FromBody] TransferModel model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); // ดึง Claim ที่เป็น UserId จาก JWT Token
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token."); // ถ้าไม่เจอ Claim ถือว่า Token ไม่ถูกต้อง
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == int.Parse(userIdClaim.Value)); // ค้นหาผู้ใช้จากฐานข้อมูล
            if (user == null)
            {
                return NotFound("User not found."); // ถ้าไม่เจอผู้ใช้ ให้ส่งข้อความว่าไม่เจอ
            }

            // ตรวจสอบว่าเงินที่ถอนต้องมากกว่า 0
            if (model.Amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }

            // ตรวจสอบว่าผู้ใช้มีเงินพอที่จะถอนหรือไม่
            if (user.Balance < model.Amount)
            {
                return BadRequest("Insufficient balance."); // ถ้าเงินไม่พอ ให้ส่งข้อความแจ้ง
            }

            user.Balance -= model.Amount; // หักยอดเงินที่ผู้ใช้ต้องการถอน
            _context.Transactions.Add(new Transaction
            {
                SenderId = user.Id,
                ReceiverId = null, // ไม่มีผู้รับเพราะเป็นการถอนเงิน
                Amount = model.Amount,
                TransactionType = "Withdraw", // ระบุว่าเป็นการถอนเงิน
                Timestamp = DateTime.UtcNow
            });
            _context.SaveChanges(); // บันทึกการเปลี่ยนแปลงลงในฐานข้อมูล

            return Ok(new { Balance = user.Balance }); // ส่งยอดเงินใหม่กลับไป
        }

        // ฟังก์ชันสำหรับโอนเงินจากผู้ใช้คนหนึ่งไปยังผู้ใช้อีกคนหนึ่ง
        [HttpPost("transfer")]
        public IActionResult Transfer([FromBody] TransferModel model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // ดึง UserId ของผู้ส่งจาก JWT Token
            var sender = _context.Users.FirstOrDefault(u => u.Id == int.Parse(userId)); // ค้นหาผู้ส่งจากฐานข้อมูล
            var receiver = _context.Users.FirstOrDefault(u => u.Id == model.ReceiverId); // ค้นหาผู้รับจากฐานข้อมูล

            // ตรวจสอบว่าไม่สามารถโอนเงินให้ตัวเองได้
            if (sender == null || receiver == null || sender.Id == receiver.Id)
            {
                return BadRequest("You cannot transfer money to yourself.");
            }

            // ตรวจสอบว่าผู้ส่งมีเงินพอที่จะโอนหรือไม่
            if (sender.Balance < model.Amount)
            {
                return BadRequest("Insufficient balance.");
            }

            // ตรวจสอบว่า Amount ต้องเป็นจำนวนบวก
            if (model.Amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }

            using (var transaction = _context.Database.BeginTransaction()) // เริ่มต้น Transaction เพื่อให้มั่นใจว่าการโอนสำเร็จสมบูรณ์
            {
                try
                {
                    // หักเงินจากผู้ส่ง
                    sender.Balance -= model.Amount;

                    // เพิ่มเงินให้ผู้รับ
                    receiver.Balance += model.Amount;

                    // บันทึกข้อมูลธุรกรรมการโอนเงิน
                    _context.Transactions.Add(new Transaction
                    {
                        SenderId = sender.Id,
                        ReceiverId = receiver.Id,
                        Amount = model.Amount,
                        TransactionType = "Transfer", // ระบุว่าเป็นการโอนเงิน
                        Timestamp = DateTime.UtcNow
                    });

                    _context.SaveChanges(); // บันทึกการเปลี่ยนแปลงลงในฐานข้อมูล
                    transaction.Commit(); // ยืนยัน Transaction

                    return Ok(new { SenderBalance = sender.Balance, ReceiverBalance = receiver.Balance }); // ส่งยอดเงินใหม่ของผู้ส่งและผู้รับกลับไป
                }
                catch (Exception)
                {
                    transaction.Rollback(); // ยกเลิก Transaction หากเกิดข้อผิดพลาด
                    return StatusCode(500, "An error occurred while processing your request."); // แจ้งข้อผิดพลาด
                }
            }
        }

        // ดึงประวัติการทำธุรกรรมของผู้ใช้ที่ยืนยันตัวตนแล้ว
        [HttpGet("history")]
        public IActionResult GetTransactionHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); // ดึง Claim ที่เป็น UserId จาก JWT Token
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token.");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == int.Parse(userIdClaim.Value)); // ค้นหาผู้ใช้จากฐานข้อมูล
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // ดึงประวัติการทำธุรกรรมของผู้ใช้ทั้งที่เป็นผู้ส่งและผู้รับ
            var transactions = _context.Transactions
                .Where(t => t.SenderId == user.Id || t.ReceiverId == user.Id)
                .OrderByDescending(t => t.Timestamp)
                .ToList();

            return Ok(transactions); // ส่งประวัติการทำธุรกรรมกลับไป
        }

        // ดึงข้อมูลโปรไฟล์ของผู้ใช้ที่ยืนยันตัวตนแล้ว
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); // ดึง Claim ที่เป็น UserId จาก JWT Token
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token.");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == int.Parse(userIdClaim.Value)); // ค้นหาผู้ใช้จากฐานข้อมูล
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // ส่งข้อมูลโปรไฟล์ของผู้ใช้ (Username และ UserId) กลับไป
            return Ok(new { Username = user.Username, UserId = user.Id });
        }

    }
}
