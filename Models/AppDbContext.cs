using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Models;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Edit table Identity name
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName) && tableName.StartsWith("AspNet"))
            {
                entityType.SetTableName(tableName.Substring(6));
            }
        }

        //Users
        modelBuilder.Entity<AppUser>()
            .Property(u => u.BirthDate)
            .HasColumnType("DATE");

        // UserRelation
        modelBuilder.Entity<UserRelationModel>()
            .HasOne(ur => ur.User)
            .WithMany()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserRelationModel>()
            .HasOne(ur => ur.OtherUser)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<UserRelationModel>()
            .HasKey(ur => new {ur.UserId, ur.OtherUserId});
        
        // Posts
        modelBuilder.Entity<PostsModel>()
            .HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .OnDelete(DeleteBehavior.SetNull);
        // modelBuilder.Entity<PostsModel>()
        //     .HasIndex(p => p.Content)
        //     .IsUnique();
        modelBuilder.Entity<PostsModel>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Posts)
            .OnDelete(DeleteBehavior.SetNull);

        // Comments
        modelBuilder.Entity<CommentsModel>()
            .HasOne(c => c.ParrentComment)
            .WithMany(c => c.ChildComments);
        modelBuilder.Entity<CommentsModel>()
            .HasOne(c => c.Posts)
            .WithMany(p => p.Comments);
        modelBuilder.Entity<CommentsModel>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments);

        // Likes
        modelBuilder.Entity<LikesModel>()
            .HasOne(l => l.User)
            .WithMany();
        modelBuilder.Entity<LikesModel>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes);
        modelBuilder.Entity<LikesModel>()
            .HasOne(l => l.Comment)
            .WithMany();

        // SupportRequest
        modelBuilder.Entity<SupportRequestsModel>()
            .HasOne(r => r.User)
            .WithMany(u => u.SupportRequests);
        
        //Categories
        modelBuilder.Entity<CategoriesModel>()
            .HasOne(c => c.ParrentCate)
            .WithMany(c => c.ChildCates);
        modelBuilder.Entity<CategoriesModel>()
            .HasIndex(c => c.Name)
            .IsUnique();

        //LoggedBrowsers
        modelBuilder.Entity<LoggedBrowsersModel>()
            .HasOne(l => l.User)
            .WithMany(u => u.LoggedBrowsers)
            .OnDelete(DeleteBehavior.Cascade);

        //Images
        modelBuilder.Entity<ImagesModel>()
            .HasOne(i => i.User)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ImagesModel>()
            .HasOne(i => i.Post)
            .WithMany(p => p.Images)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ImagesModel>()
            .HasIndex(i => i.FilePath)
            .IsUnique();
    }

    public DbSet<UserRelationModel>? UserRelations { get; set; }
    public DbSet<PostsModel>? Posts { get; set; }
    public DbSet<CategoriesModel>? Categories { get; set; }
    public DbSet<CommentsModel>? Comments { get; set; }
    public DbSet<LikesModel>? Likes { get; set; }
    public DbSet<SupportRequestsModel>? SupportRequests { get; set; }
    public DbSet<LoggedBrowsersModel>? LoggedBrowsers { get; set; }
    public DbSet<ImagesModel>? Images { get; set; }
}