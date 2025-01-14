#nullable disable

using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Bogus;
using OfficeOpenXml;
using System.Data;
using Microsoft.AspNetCore.Authorization;

namespace App.Areas.Database.Controllers;

[Authorize(Policy = "CanManageDb")]
[Area("Database")]
[Route("/db-dashboard/[action]")]
public class DbManageController : Controller
{
    private readonly ILogger<DbManageController> _logger;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IDeleteUserService _deleteUser;

    public DbManageController(
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        ILogger<DbManageController> logger,
        IDeleteUserService deleteUser)
    {
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
        _deleteUser = deleteUser;
    }

    public struct TableInfo
    {
        public string Name { get; set; }
        public string Columns { get; set; }
        public long Rows { get; set; }
        public decimal TotalSpaceMB { get; set; }
    }

    [TempData]
    public string StatusMessage { get; set; } = "";

    [Route("/db-dashboard")]
    public IActionResult Index()
    {
        var tables = new List<TableInfo>();
        var connectionString = _dbContext.Database.GetConnectionString();

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            string query = @"
                            SELECT 
                                RowCountTable.TABLE_NAME,
                                RowCountTable.TABLE_ROWS,
                                SpaceTable.TOTAL_SPACE_MB
                            FROM 
                                (
                                    SELECT 
                                        T.name AS TABLE_NAME,
                                        SUM(P.rows) AS TABLE_ROWS
                                    FROM 
                                        sys.tables AS T
                                        INNER JOIN sys.schemas AS S ON T.schema_id = S.schema_id
                                        INNER JOIN sys.partitions AS P ON T.object_id = P.object_id
                                    WHERE 
                                        T.is_ms_shipped = 0 
                                        AND P.index_id IN (0, 1)
                                    GROUP BY 
                                        S.name, T.name
                                ) AS RowCountTable
                            JOIN 
                                (
                                    SELECT 
                                        T.name AS TABLE_NAME,
                                        CAST(SUM(AU.total_pages) * 8.0 / 1024 AS DECIMAL(18, 2)) AS TOTAL_SPACE_MB
                                    FROM 
                                        sys.tables AS T
                                        INNER JOIN sys.schemas AS S ON T.schema_id = S.schema_id
                                        INNER JOIN sys.partitions AS P ON T.object_id = P.object_id
                                        INNER JOIN sys.allocation_units AS AU ON P.partition_id = AU.container_id
                                    WHERE 
                                        T.is_ms_shipped = 0 
                                        AND P.index_id IN (0, 1)
                                    GROUP BY 
                                        S.name, T.name
                                ) AS SpaceTable
                            ON RowCountTable.TABLE_NAME = SpaceTable.TABLE_NAME";
            using (var command = new SqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string Name = reader["TABLE_NAME"].ToString() ?? "";
                    long Rows = reader["TABLE_ROWS"] != DBNull.Value ? (long)reader["TABLE_ROWS"] : 0;
                    decimal TotalSpaceMB = reader["TOTAL_SPACE_MB"] != DBNull.Value ? (decimal)reader["TOTAL_SPACE_MB"] : 0;

                    string columnsQuery = @"
                        SELECT COLUMN_NAME
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = @TableName";

                    var columns = new List<string>();
                    using (var columnCommand = new SqlCommand(columnsQuery, connection))
                    {
                        columnCommand.Parameters.AddWithValue("@TableName", Name);

                        using (var columnReader = columnCommand.ExecuteReader())
                        {
                            while (columnReader.Read())
                            {
                                columns.Add(columnReader["COLUMN_NAME"].ToString() ?? "");
                            }
                        }
                    }
                    tables.Add(new TableInfo
                    {
                        Name = Name,
                        Columns = string.Join(", ", columns),
                        Rows = Rows,
                        TotalSpaceMB = TotalSpaceMB
                    });
                }
            }
        }
        List<string> blacklist = new List<string> { "__EFMigrationsHistory", "sysdiagrams" };
        tables.RemoveAll(table => blacklist.Contains(table.Name));

        ViewBag.DbInfo = tables;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateMigrationAsync(string migrationName)
    {
        if (string.IsNullOrEmpty(migrationName))
        {
            StatusMessage = "Error Không có tên migration!";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _dbContext.Database.MigrateAsync(migrationName);
            StatusMessage = "Cập nhật Database thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception e)
        {
            StatusMessage = $"Error Lỗi: {e}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public IActionResult ExportToExcel(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            StatusMessage = " Error Không có tên bảng!";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            connection.Open();

            var query = $"SELECT * FROM {tableName}";
            var command = connection.CreateCommand();
            command.CommandText = query;

            var dataTable = new DataTable();
            using (var reader = command.ExecuteReader())
            {
                dataTable.Load(reader);
            }

            if (dataTable.Rows.Count == 0)
            {
                StatusMessage = $"Error Không có dữ liệu trong bảng {tableName}";
                return RedirectToAction(nameof(Index));
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(tableName);

                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    worksheet.Cells[1, col + 1].Value = dataTable.Columns[col].ColumnName;
                }

                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        var cellValue = dataTable.Rows[row][col];
                        if (cellValue is DateTime)
                        {
                            worksheet.Cells[row + 2, col + 1].Value = cellValue;
                            worksheet.Cells[row + 2, col + 1].Style.Numberformat.Format = "yyyy-mm-dd";
                        }
                        else
                        {
                            worksheet.Cells[row + 2, col + 1].Value = cellValue;
                        }
                    }
                }

                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{tableName}_Data.xlsx");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error Lỗi: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDataTable(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            StatusMessage = "Error Không có tên bảng!";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            var deleteQuery = $"DELETE FROM {tableName}";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = deleteQuery;
                await command.ExecuteNonQueryAsync();
            }

            StatusMessage = $"Đã xoá toàn bộ dữ liệu trong bảng {tableName}";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error Lỗi khi xóa dữ liệu: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> ImportFromExcel(IFormFile excelFile, string tableName)
    {
        if (excelFile == null || string.IsNullOrWhiteSpace(tableName))
        {
            StatusMessage = "Error File Excel hoặc tên bảng không đúng!";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.CommandText = $"DELETE FROM {tableName}";
                await deleteCommand.ExecuteNonQueryAsync();
            }

            var errors = new List<string>();
            const int maxDetailedErrors = 5;
            using (var memoryStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using (var package = new ExcelPackage(memoryStream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    if (worksheet == null)
                    {
                        StatusMessage = "Error File Excel không chứa dữ liệu.";
                        return RedirectToAction(nameof(Index));
                    }

                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    var columnNames = new List<string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var columnName = worksheet.Cells[1, col].Text.Trim();
                        columnNames.Add(columnName);
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var values = new List<string>();

                                for (int col = 1; col <= colCount; col++)
                                {
                                    var cellValue = worksheet.Cells[row, col]?.Text ?? string.Empty;
                                    var processedValue = ProcessCellValue(cellValue, worksheet.Cells[1, col].Text.Trim());
                                    values.Add(processedValue);
                                }

                                var insertQuery = "";
                                if (tableName == "Users")
                                {
                                    insertQuery = $@"
                                        INSERT INTO {tableName} ({string.Join(", ", columnNames)}) 
                                        VALUES ({string.Join(", ", values)})";
                                }
                                else
                                {
                                    insertQuery = $@"
                                        SET IDENTITY_INSERT {tableName} ON
                                        INSERT INTO {tableName} ({string.Join(", ", columnNames)}) 
                                        VALUES ({string.Join(", ", values)})
                                        SET IDENTITY_INSERT {tableName} OFF";
                                }

                                using (var insertCommand = connection.CreateCommand())
                                {
                                    insertCommand.Transaction = transaction;
                                    insertCommand.CommandText = insertQuery;
                                    await insertCommand.ExecuteNonQueryAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                if (errors.Count < maxDetailedErrors)
                                {
                                    errors.Add($"<br/>Lỗi dòng {row}: {ex.Message}");
                                }
                                else if (errors.Count == maxDetailedErrors)
                                {
                                    errors.Add($"<br/>Và các dòng khác.");
                                }
                            }
                        }
                        transaction.Commit();
                    }
                }
            }

            if (errors.Count > 0)
            {
                StatusMessage = $"Error Khôi phục dữ liệu bị lỗi ở các dòng: {string.Join("", errors)}";
            }
            else
            {
                StatusMessage = $"Khôi phục dữ liệu bảng {tableName} thành công.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error Lỗi trong quá trình khôi phục: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    private string ProcessCellValue(string cellValue, string columnName)
    {
        // Nếu ô trong Excel trống, trả về "NULL"
        if (string.IsNullOrWhiteSpace(cellValue))
            return "NULL";

        // Kiểm tra và xử lý cho kiểu số
        if (decimal.TryParse(cellValue, out var numericValue))
        {
            return numericValue.ToString();
        }

        // Kiểm tra và xử lý cho kiểu DateTime
        if (DateTime.TryParse(cellValue, out var dateTimeValue))
        {
            return $"'{dateTimeValue:yyyy-MM-dd HH:mm:ss.fff}'";
        }

        // Kiểm tra và xử lý cho kiểu boolean (true/false)
        if (bool.TryParse(cellValue, out var boolValue))
        {
            return boolValue ? "1" : "0"; // Chuyển thành 1 hoặc 0
        }

        // Kiểm tra kiểu chuỗi (cần bao quanh bằng dấu nháy đơn)
        return $"'{cellValue.Replace("'", "''")}'";
    }

    [HttpPost]
    public async Task<IActionResult> SeedDataAsync()
    {
        try
        {
            await SeedUsersAsync();
            // await SeedCatesAsync();
            await SeedPostsAsync();
            await SeedLikesAsync();
            await SeedContactsAsync();
            await SeedCommentsAsync();
            StatusMessage = "Seed Data thành công!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            StatusMessage = $"Error Seed Data thất bại!" + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task SeedUsersAsync()
    {
        var usersDel = _userManager.Users.Where(u => u.Email.Contains("fakeData")).ToList();
        foreach (var user in usersDel)
        {
            // await _userManager.DeleteAsync(user);
            await _deleteUser.DeleteUserAsync(user.Id);
        }
        await _dbContext.SaveChangesAsync();

        var fakerUser = new Faker<AppUser>()
            // .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
            .RuleFor(u => u.BirthDate, f => f.Date.Between(new DateTime(1980, 1, 1), new DateTime(2012, 12, 31)))
            .RuleFor(u => u.UserName, (f, u) => f.Internet.UserName() + "{i}")
            .RuleFor(u => u.NormalizedUserName, (f, u) => u.UserName?.ToUpperInvariant())
            .RuleFor(u => u.Email, (f, u) => "fakeData" + f.Lorem.Word() + "{i}@gmail.com")
            .RuleFor(u => u.NormalizedEmail, (f, u) => u.Email?.ToUpperInvariant())
            .RuleFor(u => u.EmailConfirmed, f => f.Random.Bool())
            .RuleFor(u => u.PasswordHash, f => Convert.ToBase64String(Guid.NewGuid().ToByteArray()))
            .RuleFor(u => u.SecurityStamp, f => Guid.NewGuid().ToString())
            .RuleFor(u => u.ConcurrencyStamp, f => Guid.NewGuid().ToString())
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.PhoneNumberConfirmed, f => f.Random.Bool())
            .RuleFor(u => u.TwoFactorEnabled, f => f.Random.Bool())
            .RuleFor(u => u.LockoutEnd, f => f.Random.Bool() ? f.Date.FutureOffset() : null)
            .RuleFor(u => u.LockoutEnabled, f => true)
            .RuleFor(u => u.AccessFailedCount, (f, u) => u.LockoutEnabled ? 0 : f.Random.Int(0, 4))
            .RuleFor(u => u.isActivate, f => f.Random.Bool());

        List<AppUser> fkUsers = new List<AppUser>();
        for (int i = 0; i < 2359; i++)
        {
            var user = fakerUser.Generate();
            user.UserName = user.UserName.Replace("{i}", i.ToString());
            user.NormalizedUserName = user.UserName.ToUpperInvariant();
            user.Email = user.Email.Replace("{i}", i.ToString());
            user.NormalizedEmail = user.Email.ToUpperInvariant();
            fkUsers.Add(user);
        }

        await _dbContext.AddRangeAsync(fkUsers);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedCatesAsync()
    {
        _dbContext.Categories.RemoveRange(_dbContext.Categories.Where(c => c.Description.Contains("fakeData")));
        await _dbContext.SaveChangesAsync();
        await _dbContext.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Categories', RESEED, 0)");

        var usedNames = new HashSet<string>();
        var fakerCategory = new Faker<CategoriesModel>()
            .RuleFor(c => c.Name, f =>
            {
                string uniqueName;
                do
                {
                    uniqueName = f.Commerce.Categories(1)[0];
                } while (usedNames.Contains(uniqueName));
                usedNames.Add(uniqueName);
                return uniqueName;
            })
            .RuleFor(c => c.Description, f => f.Lorem.Paragraph(2) + "fakeData");

        List<CategoriesModel> fkCates = new List<CategoriesModel>();
        for (int i = 0; i < 10; i++)
        {
            var cate = fakerCategory.Generate();
            cate.SetSlug();
            fkCates.Add(cate);
        }

        await _dbContext.AddRangeAsync(fkCates);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedPostsAsync()
    {
        _dbContext.Posts.RemoveRange(_dbContext.Posts.Where(p => p.Content.Contains("fakeData")));
        await _dbContext.SaveChangesAsync();
        await _dbContext.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Posts', RESEED, 0)");

        var userIds = _dbContext.Users.Select(u => u.Id).ToList();
        var cateIds = _dbContext.Categories.Where(c => c.ChildCates.Count == 0).Select(c => c.Id).ToList();

        double percentage = 0.33;
        var random = new Random();

        var selectedUserIds = userIds
            .OrderBy(x => random.Next())
            .Take((int)(userIds.Count * percentage))
            .ToList();

        var fakerPost = new Faker<PostsModel>()
            .RuleFor(p => p.AuthorId, f => f.PickRandom(selectedUserIds))
            // .RuleFor(p => p.AuthorId, "8e339bd2-b7c2-47f7-8e3f-71dad05bacb6")
            .RuleFor(p => p.Title, f => f.Lorem.Sentence(5))
            .RuleFor(p => p.Description, f => f.Lorem.Paragraph(2))
            .RuleFor(p => p.Content, f => f.Lorem.Paragraphs(7) + "fakeData")
            .RuleFor(p => p.Hashtag, f => "fakeData")
            .RuleFor(p => p.DateCreated, f => f.Date.Past(2))
            .RuleFor(p => p.DateUpdated, (f, p) => p.DateCreated)
            .RuleFor(p => p.isPublished, f => f.Random.Bool())
            .RuleFor(p => p.isChildAllowed, f => f.Random.Bool())
            .RuleFor(p => p.isPinned, f => false)
            // .RuleFor(p => p.CategoryId, 9);
            .RuleFor(p => p.CategoryId, f => f.PickRandom(cateIds));

        List<PostsModel> fkPosts = new List<PostsModel>();
        for (int i = 0; i < 1193; i++)
        {
            var post = fakerPost.Generate();
            fkPosts.Add(post);
            post.SetSlug(i);
        }

        await _dbContext.AddRangeAsync(fkPosts);
        await _dbContext.SaveChangesAsync();

        foreach (var post in fkPosts)
        {
            post.SetSlug();
        }
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedLikesAsync()
    {
        _dbContext.Likes.RemoveRange(_dbContext.Likes);
        await _dbContext.SaveChangesAsync();
        await _dbContext.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Likes', RESEED, 0)");

        var userIds = await _dbContext.Users.Select(u => u.Id).ToListAsync();
        var postIds = await _dbContext.Posts.Select(p => p.Id).ToListAsync();
        var commentIds = await _dbContext.Comments.Select(c => c.Id).ToListAsync();

        double percentage = 0.46;
        var random = new Random();

        var selectedPostIds = postIds
            .OrderBy(x => random.Next())
            .Take((int)(postIds.Count * percentage))
            .ToList();

        var fakerLike = new Faker<LikesModel>()
            .RuleFor(l => l.UserId, f => f.PickRandom(userIds))
            // .RuleFor(l => l.LikeType, f => f.PickRandom<LikeTypes>())
            .RuleFor(l => l.LikeType, f => LikeTypes.Post)
            .RuleFor(l => l.DateLiked, f => f.Date.Recent(30));

        var fkLikes = new List<LikesModel>();
        var uniqueLikes = new HashSet<(string, int?, int?)>();

        for (int i = 0; i < 5390; i++)
        {
            var like = fakerLike.Generate();

            if (like.LikeType == LikeTypes.Post && postIds.Any())
            {
                like.PostId = new Faker().PickRandom(selectedPostIds);
                like.CommentId = null;
            }
            else if (like.LikeType == LikeTypes.Comment && commentIds.Any())
            {
                like.CommentId = new Faker().PickRandom(commentIds);
                like.PostId = null;
            }
            else
            {
                continue;
            }

            var key = (like.UserId, like.PostId, like.CommentId);
            if (!uniqueLikes.Contains(key))
            {
                uniqueLikes.Add(key);
                fkLikes.Add(like);
            }
        }

        await _dbContext.AddRangeAsync(fkLikes);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedContactsAsync()
    {
        _dbContext.Contacts.RemoveRange(_dbContext.Contacts.Where(c => c.Content.Contains("fakeData")));
        await _dbContext.SaveChangesAsync();
        await _dbContext.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Contacts', RESEED, 0)");

        var userIds = _dbContext.Users.Select(u => u.Id).ToList();

        var fakerContact = new Faker<ContactsModel>()
            .RuleFor(c => c.Name, f => f.Name.FullName())
            .RuleFor(c => c.Email, (f, c) => string.IsNullOrEmpty(c.UserId) ? f.Internet.Email() : null)
            .RuleFor(c => c.Title, f => {
                var text = f.Lorem.Text();
                return text.Length > 35 ? text.Substring(0, Math.Min(30, text.Length)) : text;
            })
            .RuleFor(c => c.Content, f => {
                var text = f.Lorem.Text();
                return text.Length > 210 ? text.Substring(0, Math.Min(200, text.Length)) + "fakeData" : text + "fakeData";
            })
            .RuleFor(c => c.Status, f => f.Random.Int(0, 1))
            .RuleFor(c => c.DateSend, f => f.Date.Past(1))
            .RuleFor(c => c.Priority, f => f.Random.Int(1, 3))
            .RuleFor(c => c.UserId, f => f.Random.Bool() ? f.PickRandom(userIds) : null)
            .RuleFor(c => c.Response, (f, c) => c.Status == 1 ? f.Lorem.Paragraph() : null);

        List<ContactsModel> fakeContacts = new List<ContactsModel>();
        for (int i = 0; i < 200; i++)
        {
            var contact = fakerContact.Generate();
            fakeContacts.Add(contact);
        }

        await _dbContext.Contacts.AddRangeAsync(fakeContacts);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedCommentsAsync()
    {
        _dbContext.Comments.RemoveRange(_dbContext.Comments.Where(c => c.Content.Contains("fakeData")));
        await _dbContext.SaveChangesAsync();
        await _dbContext.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Comments', RESEED, 0)");

        var userIds = _dbContext.Users.Select(u => u.Id).ToList();
        var postIds = _dbContext.Posts.Select(p => p.Id).ToList();

        double percentage = 0.38;
        var random = new Random();

        var selectedPostIds = postIds
            .OrderBy(x => random.Next())
            .Take((int)(postIds.Count * percentage))
            .ToList();

        var fakerComment = new Faker<CommentsModel>()
            .RuleFor(c => c.Content, f => {
                var text = f.Lorem.Text();
                return text.Length > 210 ? text.Substring(0, Math.Min(200, text.Length)) + "fakeData" : text + "fakeData";
            })
            .RuleFor(c => c.DateCommented, f => f.Date.Past(1))
            .RuleFor(c => c.PostId, f => f.PickRandom(selectedPostIds))
            // .RuleFor(c => c.PostId, f => 6)
            .RuleFor(c => c.UserId, f => f.PickRandom(userIds));
            // .RuleFor(c => c.ParentCommentId, f => f.Random.Bool(0.2f) ? null : (int?)f.Random.Int(1, 200));

        List<CommentsModel> fakeComments = new List<CommentsModel>();
        for (int i = 0; i < 3978; i++)
        {
            var comment = fakerComment.Generate();
            fakeComments.Add(comment);
        }

        await _dbContext.Comments.AddRangeAsync(fakeComments);
        await _dbContext.SaveChangesAsync();
    }
}