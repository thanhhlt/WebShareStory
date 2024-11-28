using App.Models;
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
        // Edit table name
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName) && tableName.StartsWith("AspNet"))
            {
                entityType.SetTableName(tableName.Substring(6));
            }
        }

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
            .OnDelete(DeleteBehavior.Restrict);
        // modelBuilder.Entity<PostsModel>()
        //     .HasIndex(p => p.Content)
        //     .IsUnique();
        modelBuilder.Entity<PostsModel>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Posts);

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

        modelBuilder.Entity<CategoriesModel>()
            .HasOne(c => c.ParrentCate)
            .WithMany(c => c.ChildCates);
        modelBuilder.Entity<CategoriesModel>()
            .HasIndex(c => c.Name)
            .IsUnique();
    }

    public DbSet<UserRelationModel>? UserRelations { get; set; }
    public DbSet<PostsModel>? Posts { get; set; }
    public DbSet<CategoriesModel>? Categories { get; set; }
    public DbSet<CommentsModel>? Comments { get; set; }
    public DbSet<LikesModel>? Likes { get; set; }
    public DbSet<SupportRequestsModel>? SupportRequests { get; set; }
}