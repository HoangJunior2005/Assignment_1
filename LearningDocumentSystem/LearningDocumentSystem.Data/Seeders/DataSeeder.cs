using LearningDocumentSystem.Common.Helpers;
using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Data.Seeders
{
    public class DataSeeder
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(AppDbContext context, ILogger<DataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Apply pending migrations
                await _context.Database.MigrateAsync();

                await SeedRolesAsync();
                await SeedSchoolsAsync();
                await SeedStudentRegistriesAsync();
                await SeedUsersAsync();
                await SeedSubjectsAsync();
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Database seeded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error seeding database.");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            if (await _context.Roles.AnyAsync()) return;

            _logger.LogInformation("Seeding roles...");
            var roles = new List<Role>
            {
                new() { RoleName = "Admin" },
                new() { RoleName = "Teacher" },
                new() { RoleName = "Student" }
            };
            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync(); // Save để lấy RoleID
        }

        private async Task SeedSchoolsAsync()
        {
            if (await _context.Schools.AnyAsync()) return;

            _logger.LogInformation("Seeding schools...");
            await _context.Schools.AddAsync(new School
            {
                SchoolName = "Trường Đại học Công nghệ",
                SchoolCode = "UNI-TECH"
            });
            await _context.SaveChangesAsync();
        }

        private async Task SeedStudentRegistriesAsync()
        {
            if (await _context.StudentRegistries.AnyAsync()) return;

            _logger.LogInformation("Seeding student registry...");
            var school = await _context.Schools.FirstAsync();

            var registries = new List<StudentRegistry>
            {
                new()
                {
                    StudentCode = "SE123456",
                    FullName    = "Nguyen Van A",
                    SchoolID    = school.SchoolID,
                    IsActivated = false
                },
                new()
                {
                    StudentCode = "SE171001",
                    FullName    = "Tran Manh Sinh Vien",
                    SchoolID    = school.SchoolID,
                    IsActivated = false
                },
                new()
                {
                    StudentCode = "SE654321",
                    FullName    = "Le Thi B",
                    SchoolID    = school.SchoolID,
                    IsActivated = false
                }
            };

            await _context.StudentRegistries.AddRangeAsync(registries);
            await _context.SaveChangesAsync();
        }

        private async Task SeedUsersAsync()
        {
            if (await _context.Users.AnyAsync()) return;

            _logger.LogInformation("Seeding users...");

            var school = await _context.Schools.FirstOrDefaultAsync();

            var adminRole   = await _context.Roles.FirstAsync(r => r.RoleName == "Admin");
            var teacherRole = await _context.Roles.FirstAsync(r => r.RoleName == "Teacher");
            var studentRole = await _context.Roles.FirstAsync(r => r.RoleName == "Student");

            // Password mẫu: Admin@123, Teacher@123, Student@123
            var users = new List<User>
            {
                new()
                {
                    Username     = "admin",
                    PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                    FullName     = "Quản Trị Viên",
                    Email        = "admin@university.edu.vn",
                    IsActive     = true,
                    CreatedAt    = DateTime.UtcNow
                },
                new()
                {
                    // Theo seed data trong file Word
                    Username     = "nguyenvan_gv",
                    PasswordHash = PasswordHelper.HashPassword("Teacher@123"),
                    FullName     = "Nguyễn Văn Giảng Viên",
                    Email        = "teacher@university.edu.vn",
                    IsActive     = true,
                    CreatedAt    = DateTime.UtcNow
                },
                new()
                {
                    Username     = "tranmanh_sv",
                    PasswordHash = PasswordHelper.HashPassword("Student@123"),
                    FullName     = "Trần Mạnh Sinh Viên",
                    Email        = "student@student.edu.vn",
                    SchoolID     = school?.SchoolID,
                    IsActive     = true,
                    CreatedAt    = DateTime.UtcNow
                }
            };

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync(); // Save để lấy UserID

            // Gán quyền
            var admin   = await _context.Users.FirstAsync(u => u.Username == "admin");
            var teacher = await _context.Users.FirstAsync(u => u.Username == "nguyenvan_gv");
            var student = await _context.Users.FirstAsync(u => u.Username == "tranmanh_sv");

            var userRoles = new List<UserRole>
            {
                new() { UserID = admin.UserID,   RoleID = adminRole.RoleID },
                new() { UserID = teacher.UserID, RoleID = teacherRole.RoleID },
                new() { UserID = student.UserID, RoleID = studentRole.RoleID }
            };
            await _context.UserRoles.AddRangeAsync(userRoles);
            await _context.SaveChangesAsync();

            var registry = await _context.StudentRegistries
                .FirstOrDefaultAsync(r => r.StudentCode == "SE171001");
            if (registry != null && !registry.IsActivated)
            {
                registry.IsActivated = true;
                registry.ActivatedAt = DateTime.UtcNow;
                registry.UserID = student.UserID;
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedSubjectsAsync()
        {
            if (await _context.Subjects.AnyAsync()) return;

            _logger.LogInformation("Seeding subjects and chapters...");

            // Môn học từ file Word
            var subject = new Subject
            {
                SubjectName = "Lập trình cấu trúc C#",
                SubjectCode = "INF205",
                CreatedAt   = DateTime.UtcNow
            };
            await _context.Subjects.AddAsync(subject);
            await _context.SaveChangesAsync();

            // Chương từ file Word
            var chapters = new List<Chapter>
            {
                new()
                {
                    SubjectID     = subject.SubjectID,
                    ChapterNumber = 1,
                    ChapterName   = "Tổng quan về Biến cấu trúc trong .NET"
                },
                new()
                {
                    SubjectID     = subject.SubjectID,
                    ChapterNumber = 2,
                    ChapterName   = "Kiểu dữ liệu, Toán tử và Biểu thức"
                },
                new()
                {
                    SubjectID     = subject.SubjectID,
                    ChapterNumber = 3,
                    ChapterName   = "Cấu trúc điều kiện và vòng lặp"
                },
                new()
                {
                    SubjectID     = subject.SubjectID,
                    ChapterNumber = 4,
                    ChapterName   = "Mảng, Chuỗi và Collection"
                }
            };
            await _context.Chapters.AddRangeAsync(chapters);
            await _context.SaveChangesAsync();
        }
    }
}
