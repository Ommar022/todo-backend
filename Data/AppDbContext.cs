﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TODO.Model;

namespace TODO.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Todo> Todos { get; set; }
        public DbSet<TodoStatus> TodoStatuses { get; set; }
        public DbSet<TodoList> TodoLists { get; set; }
        public DbSet<Avatar> Avatars { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<TodoUserAssignment> TodoUserAssignments { get; set; }
        public DbSet<TodoListUserAssignment> TodoListUserAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customize Identity tables for PostgreSQL
            modelBuilder.Entity<User>().ToTable("AspNetUsers").Property(u => u.Id).HasColumnType("integer");
            modelBuilder.Entity<IdentityRole<int>>().ToTable("AspNetRoles").Property(r => r.Id).HasColumnType("integer");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("AspNetUserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("AspNetUserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("AspNetUserLogins");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("AspNetRoleClaims");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("AspNetUserTokens");

            // Set string columns to varchar for all entities, including Identity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(string));

                foreach (var property in properties)
                {
                    property.SetColumnType("varchar(256)"); // Use varchar(256) for consistency
                }
            }

            // User -> Avatar (1:1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Avatar)
                .WithOne(a => a.User)
                .HasForeignKey<Avatar>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Log -> User (1:Many, no collection in User)
            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comment -> Todo (1:Many)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Todo)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TodoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comment -> User (1:Many, no collection in User)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // TodoUserAssignment (Many:Many between Todo and User)
            modelBuilder.Entity<TodoUserAssignment>()
                .HasKey(tua => new { tua.TodoId, tua.UserId });

            modelBuilder.Entity<TodoUserAssignment>()
                .HasOne(tua => tua.Todo)
                .WithMany(t => t.TodoUserAssignments)
                .HasForeignKey(tua => tua.TodoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TodoUserAssignment>()
                .HasOne(tua => tua.User)
                .WithMany(u => u.TodoAssignments)
                .HasForeignKey(tua => tua.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // TodoList -> User (1:Many)
            modelBuilder.Entity<TodoList>()
                .HasOne(tl => tl.User)
                .WithMany(u => u.TodoLists)
                .HasForeignKey(tl => tl.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // TodoList -> Todos (1:Many)
            modelBuilder.Entity<TodoList>()
                .HasMany(tl => tl.Todos)
                .WithOne(t => t.TodoList)
                .HasForeignKey(t => t.TodoListId)
                .OnDelete(DeleteBehavior.Restrict);

            // Todo -> User (Assignee, 1:Many)
            modelBuilder.Entity<Todo>()
                .HasOne(t => t.User)
                .WithMany(u => u.AssignedTodos)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Todo -> Creator (1:Many)
            modelBuilder.Entity<Todo>()
                .HasOne(t => t.Creator)
                .WithMany(u => u.CreatedTodos)
                .HasForeignKey(t => t.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Todo -> Status (1:Many)
            modelBuilder.Entity<Todo>()
                .HasOne(t => t.Status)
                .WithMany(s => s.Todos)
                .HasForeignKey(t => t.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed TodoStatus
            modelBuilder.Entity<TodoStatus>().HasData(
                new TodoStatus { Id = 1, StatusName = "Pending" },
                new TodoStatus { Id = 2, StatusName = "Done" },
                new TodoStatus { Id = 3, StatusName = "Canceled" }
            );

            // TodoListUserAssignment (Many:Many between TodoList and User)
            modelBuilder.Entity<TodoListUserAssignment>()
                .HasKey(tlua => new { tlua.TodoListId, tlua.UserId });

            modelBuilder.Entity<TodoListUserAssignment>()
                .HasOne(tlua => tlua.TodoList)
                .WithMany(tl => tl.UserAssignments)
                .HasForeignKey(tlua => tlua.TodoListId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TodoListUserAssignment>()
                .HasOne(tlua => tlua.User)
                .WithMany(u => u.AssignedTodoLists)
                .HasForeignKey(tlua => tlua.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}