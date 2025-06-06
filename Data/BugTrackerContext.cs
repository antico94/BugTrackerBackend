// Data/BugTrackerContext.cs
using Microsoft.EntityFrameworkCore;
using BugTracker.Models;
using BugTracker.Models.Enums;
using BugTracker.Models.Workflow;

namespace BugTracker.Data;

public class BugTrackerContext : DbContext
{
    public BugTrackerContext(DbContextOptions<BugTrackerContext> options) : base(options) { }

    // DbSets
    public DbSet<Client> Clients { get; set; }
    public DbSet<Study> Studies { get; set; }
    public DbSet<TrialManager> TrialManagers { get; set; }
    public DbSet<InteractiveResponseTechnology> InteractiveResponseTechnologies { get; set; }
    public DbSet<ExternalModule> ExternalModules { get; set; }
    public DbSet<CoreBug> CoreBugs { get; set; }
    public DbSet<CustomTask> CustomTasks { get; set; }
    public DbSet<TaskStep> TaskSteps { get; set; }
    public DbSet<TaskNote> TaskNotes { get; set; }
    public DbSet<WeeklyCoreBugs> WeeklyCoreBugs { get; set; }
    public DbSet<WeeklyCoreBugEntry> WeeklyCoreBugEntries { get; set; }
    
    // Workflow entities
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
    public DbSet<WorkflowExecution> WorkflowExecutions { get; set; }
    public DbSet<WorkflowAuditLog> WorkflowAuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Client Configuration
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            
            // Client -> TrialManager (1:1)
            entity.HasOne(c => c.TrialManager)
                  .WithOne(tm => tm.Client)
                  .HasForeignKey<TrialManager>(tm => tm.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TrialManager Configuration
        modelBuilder.Entity<TrialManager>(entity =>
        {
            entity.HasKey(e => e.TrialManagerId);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            entity.Property(e => e.JiraKey).HasMaxLength(50);
            
            // TrialManager -> Client (1:1 - configured above)
            
            // TrialManager -> Studies (1:Many)
            entity.HasMany(tm => tm.Studies)
                  .WithOne(s => s.TrialManager)
                  .HasForeignKey(s => s.TrialManagerId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            // Ignore the IProduct Study navigation to avoid circular reference
            entity.Ignore(tm => tm.Study);
            entity.Ignore(tm => tm.StudyId);
        });

        // Study Configuration
        modelBuilder.Entity<Study>(entity =>
        {
            entity.HasKey(e => e.StudyId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Protocol).HasMaxLength(100);
            
            // Study -> Client (Many:1)
            entity.HasOne(s => s.Client)
                  .WithMany(c => c.Studies)
                  .HasForeignKey(s => s.ClientId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts
            
            // Study -> TrialManager (Many:1 - configured above)
            
            // Study -> IRTs (1:Many)
            entity.HasMany(s => s.InteractiveResponseTechnologies)
                  .WithOne(irt => irt.Study)
                  .HasForeignKey(irt => irt.StudyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // InteractiveResponseTechnology Configuration
        modelBuilder.Entity<InteractiveResponseTechnology>(entity =>
        {
            entity.HasKey(e => e.InteractiveResponseTechnologyId);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            entity.Property(e => e.JiraKey).HasMaxLength(50);
            
            // IRT -> Study (Many:1 - configured above)
            
            // IRT -> TrialManager (Many:1)
            entity.HasOne(irt => irt.TrialManager)
                  .WithMany() // No navigation property back to IRTs from TrialManager
                  .HasForeignKey(irt => irt.TrialManagerId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // IRT -> ExternalModules (1:Many)
            entity.HasMany(irt => irt.ExternalModules)
                  .WithOne(em => em.InteractiveResponseTechnology)
                  .HasForeignKey(em => em.InteractiveResponseTechnologyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ExternalModule Configuration
        modelBuilder.Entity<ExternalModule>(entity =>
        {
            entity.HasKey(e => e.ExternalModuleId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            
            // ExternalModule -> IRT (Many:1 - configured above)
        });

        // CoreBug Configuration
        modelBuilder.Entity<CoreBug>(entity =>
        {
            entity.HasKey(e => e.BugId);
            entity.Property(e => e.JiraKey).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BugTitle).IsRequired().HasMaxLength(500);
            entity.Property(e => e.BugDescription).HasColumnType("ntext");
            entity.Property(e => e.AffectedVersions).HasColumnType("ntext");
            entity.Property(e => e.AssessedImpactedVersions).HasColumnType("ntext");
            
            // Index on JiraKey for performance
            entity.HasIndex(e => e.JiraKey).IsUnique();
            
            // CoreBug -> Tasks (1:Many)
            entity.HasMany(cb => cb.Tasks)
                  .WithOne(t => t.CoreBug)
                  .HasForeignKey(t => t.BugId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CustomTask Configuration
        modelBuilder.Entity<CustomTask>(entity =>
        {
            entity.HasKey(e => e.TaskId);
            entity.Property(e => e.TaskTitle).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TaskDescription).HasColumnType("ntext");
            entity.Property(e => e.JiraTaskKey).HasMaxLength(50);
            
            // CustomTask -> CoreBug (Many:1 - configured above)
            
            // CustomTask -> Study (Many:1)
            entity.HasOne(t => t.Study)
                  .WithMany(s => s.Tasks)
                  .HasForeignKey(t => t.StudyId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // CustomTask -> TrialManager (Many:1, Optional)
            entity.HasOne(t => t.TrialManager)
                  .WithMany(tm => tm.Tasks)
                  .HasForeignKey(t => t.TrialManagerId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // CustomTask -> IRT (Many:1, Optional)
            entity.HasOne(t => t.InteractiveResponseTechnology)
                  .WithMany(irt => irt.Tasks)
                  .HasForeignKey(t => t.InteractiveResponseTechnologyId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Ensure task is linked to either TM or IRT, not both
            entity.HasCheckConstraint("CK_Task_Product", 
                "(TrialManagerId IS NOT NULL AND InteractiveResponseTechnologyId IS NULL) OR " +
                "(TrialManagerId IS NULL AND InteractiveResponseTechnologyId IS NOT NULL)");
        });

        // TaskStep Configuration
        modelBuilder.Entity<TaskStep>(entity =>
        {
            entity.HasKey(e => e.TaskStepId);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnType("ntext");
            entity.Property(e => e.DecisionAnswer).HasMaxLength(10);
            entity.Property(e => e.Notes).HasColumnType("ntext");
            
            // TaskStep -> CustomTask (Many:1)
            entity.HasOne(ts => ts.Task)
                  .WithMany(t => t.TaskSteps)
                  .HasForeignKey(ts => ts.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskNote Configuration
        modelBuilder.Entity<TaskNote>(entity =>
        {
            entity.HasKey(e => e.TaskNoteId);
            entity.Property(e => e.Content).IsRequired().HasColumnType("ntext");
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            
            // TaskNote -> CustomTask (Many:1)
            entity.HasOne(tn => tn.Task)
                  .WithMany(t => t.TaskNotes)
                  .HasForeignKey(tn => tn.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // WeeklyCoreBugs Configuration
        modelBuilder.Entity<WeeklyCoreBugs>(entity =>
        {
            entity.HasKey(e => e.WeeklyCoreBugsId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        // WeeklyCoreBugEntry Configuration (Junction Table)
        modelBuilder.Entity<WeeklyCoreBugEntry>(entity =>
        {
            entity.HasKey(e => e.WeeklyCoreBugEntryId);
            
            // Junction table relationships
            entity.HasOne(wce => wce.WeeklyCoreBugs)
                  .WithMany(wcb => wcb.WeeklyCoreBugEntries)
                  .HasForeignKey(wce => wce.WeeklyCoreBugsId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(wce => wce.CoreBug)
                  .WithMany(cb => cb.WeeklyCoreBugEntries)
                  .HasForeignKey(wce => wce.BugId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Prevent duplicate entries
            entity.HasIndex(e => new { e.WeeklyCoreBugsId, e.BugId }).IsUnique();
        });

        // Configure Enums
        modelBuilder.Entity<CoreBug>()
            .Property(e => e.Status)
            .HasConversion<string>();
            
        modelBuilder.Entity<CoreBug>()
            .Property(e => e.Severity)
            .HasConversion<string>();
            
        modelBuilder.Entity<CoreBug>()
            .Property(e => e.AssessedProductType)
            .HasConversion<string>();

        modelBuilder.Entity<CustomTask>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<TaskStep>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<WeeklyCoreBugs>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ExternalModule>()
            .Property(e => e.ExternalModuleType)
            .HasConversion<string>();

        // Workflow Configuration
        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.HasKey(e => e.WorkflowDefinitionId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DefinitionJson).IsRequired().HasColumnType("ntext");
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            
            entity.HasIndex(e => new { e.Name, e.Version }).IsUnique();
        });

        modelBuilder.Entity<WorkflowExecution>(entity =>
        {
            entity.HasKey(e => e.WorkflowExecutionId);
            entity.Property(e => e.CurrentStepId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContextJson).HasColumnType("ntext");
            entity.Property(e => e.StartedBy).IsRequired().HasMaxLength(100);
            
            // WorkflowExecution -> CustomTask (1:1)
            entity.HasOne(we => we.Task)
                  .WithOne()
                  .HasForeignKey<WorkflowExecution>(we => we.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // WorkflowExecution -> WorkflowDefinition (Many:1)
            entity.HasOne(we => we.WorkflowDefinition)
                  .WithMany()
                  .HasForeignKey(we => we.WorkflowDefinitionId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // WorkflowExecution -> AuditLogs (1:Many)
            entity.HasMany(we => we.AuditLogs)
                  .WithOne(al => al.WorkflowExecution)
                  .HasForeignKey(al => al.WorkflowExecutionId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.TaskId).IsUnique();
        });

        modelBuilder.Entity<WorkflowAuditLog>(entity =>
        {
            entity.HasKey(e => e.WorkflowAuditLogId);
            entity.Property(e => e.StepId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Result).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PreviousStepId).HasMaxLength(100);
            entity.Property(e => e.NextStepId).HasMaxLength(100);
            entity.Property(e => e.Decision).HasMaxLength(50);
            entity.Property(e => e.Notes).HasColumnType("ntext");
            entity.Property(e => e.ConditionsEvaluated).HasColumnType("ntext");
            entity.Property(e => e.ContextSnapshot).HasColumnType("ntext");
            entity.Property(e => e.PerformedBy).IsRequired().HasMaxLength(100);
            
            entity.HasIndex(e => new { e.WorkflowExecutionId, e.Timestamp });
        });

        // Workflow enum conversions
        modelBuilder.Entity<WorkflowExecution>()
            .Property(e => e.Status)
            .HasConversion<string>();
    }
}