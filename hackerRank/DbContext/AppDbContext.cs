
using HackerRank.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext:DbContext
{
public AppDbContext(DbContextOptions<AppDbContext> options):base(options){

}

// tables:
    public DbSet<Admin> Admins { get; set; }
    public DbSet<TestModel> Tests { get; set; }

    public DbSet<SectionModel> Sections { get; set; }
    public DbSet<QuestionModel> Questions { get; set; }
    public DbSet<TestQuestionModel> TestQuestions { get; set; }

}
