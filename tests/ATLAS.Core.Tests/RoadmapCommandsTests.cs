using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using Xunit;

namespace ATLAS.Core.Tests;

public class RoadmapCommandsTests
{
    private class InMemoryGoalRepository : IGoalRepository
    {
        public readonly Dictionary<string, Goal> Goals = new();

        public Task<Goal> CreateAsync(Goal goal, CancellationToken cancellationToken = default)
        {
            Goals[goal.Id] = goal;
            return Task.FromResult(goal);
        }

        public Task<Goal?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            Goals.TryGetValue(id, out var g);
            return Task.FromResult(g);
        }

        public Task<IReadOnlyList<Goal>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
        {
            var list = Goals.Values.AsEnumerable();
            if (!string.IsNullOrEmpty(status))
                list = list.Where(g => g.Status == status);
            return Task.FromResult<IReadOnlyList<Goal>>(list.ToList());
        }

        public Task<Goal> UpdateAsync(Goal goal, CancellationToken cancellationToken = default)
        {
            Goals[goal.Id] = goal;
            return Task.FromResult(goal);
        }
    }

    private class InMemoryRoadmapRepository : IRoadmapRepository
    {
        public readonly Dictionary<string, Roadmap> Roadmaps = new();
        public readonly Dictionary<string, RoadmapMilestone> Milestones = new();

        public Task CreateAsync(Roadmap roadmap, IEnumerable<RoadmapMilestone>? milestones = null, CancellationToken cancellationToken = default)
        {
            Roadmaps[roadmap.Id] = roadmap;
            if (milestones != null)
            {
                var list = milestones.ToList();
                foreach (var m in list)
                {
                    Milestones[m.Id] = m;
                }
                roadmap.Milestones = list;
            }
            return Task.CompletedTask;
        }

        public Task<Roadmap?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (Roadmaps.TryGetValue(id, out var r))
            {
                r.Milestones = Milestones.Values.Where(m => m.RoadmapId == id).OrderBy(m => m.OrderIndex).ToList();
                return Task.FromResult<Roadmap?>(r);
            }
            return Task.FromResult<Roadmap?>(null);
        }

        public Task<Roadmap?> GetByGoalIdAsync(string goalId, CancellationToken cancellationToken = default)
        {
            var r = Roadmaps.Values.FirstOrDefault(rm => rm.GoalId == goalId);
            if (r != null)
            {
                r.Milestones = Milestones.Values.Where(m => m.RoadmapId == r.Id).OrderBy(m => m.OrderIndex).ToList();
            }
            return Task.FromResult(r);
        }

        public Task<IReadOnlyList<Roadmap>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
        {
            var list = Roadmaps.Values.AsEnumerable();
            if (!string.IsNullOrEmpty(status))
                list = list.Where(r => r.Status == status);

            foreach (var r in list)
            {
                r.Milestones = Milestones.Values.Where(m => m.RoadmapId == r.Id).OrderBy(m => m.OrderIndex).ToList();
            }
            return Task.FromResult<IReadOnlyList<Roadmap>>(list.ToList());
        }

        public Task AddMilestoneAsync(RoadmapMilestone milestone, CancellationToken cancellationToken = default)
        {
            Milestones[milestone.Id] = milestone;
            if (Roadmaps.TryGetValue(milestone.RoadmapId, out var r))
            {
                r.Milestones.Add(milestone);
            }
            return Task.CompletedTask;
        }

        public Task UpdateMilestoneStatusAsync(string milestoneId, string status, DateTimeOffset? completedAt, CancellationToken cancellationToken = default)
        {
            if (Milestones.TryGetValue(milestoneId, out var m))
            {
                m.Status = status;
                m.CompletedAt = completedAt;
            }
            return Task.CompletedTask;
        }

        public Task<RoadmapMilestone?> GetMilestoneByIdAsync(string milestoneId, CancellationToken cancellationToken = default)
        {
            Milestones.TryGetValue(milestoneId, out var m);
            return Task.FromResult(m);
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            Roadmaps.Remove(id);
            var toRemove = Milestones.Values.Where(m => m.RoadmapId == id).Select(m => m.Id).ToList();
            foreach (var mid in toRemove) Milestones.Remove(mid);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task RoadmapCreateCommand_WithInitialMilestones_CreatesSuccessfully()
    {
        var roadmapRepo = new InMemoryRoadmapRepository();
        var goalRepo = new InMemoryGoalRepository();
        var command = new RoadmapCreateCommand(roadmapRepo, goalRepo);

        var parameters = new Dictionary<string, object?>
        {
            ["title"] = "Aprender Ciberseguridad",
            ["description"] = "Ruta desde fundamentos hasta pentesting",
            ["milestones"] = new List<string> { "Redes y Protocolos", "Linux Básico", "Web Exploitation" }
        };

        var result = await command.ExecuteAsync(parameters);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var roadmap = Assert.IsType<Roadmap>(result.Data);
        Assert.Equal("Aprender Ciberseguridad", roadmap.Title);
        Assert.Equal(3, roadmap.Milestones.Count);
        Assert.Equal(0, roadmap.ProgressPercentage);
    }

    [Fact]
    public async Task RoadmapCreateCommand_WithoutTitle_ReturnsFailure()
    {
        var roadmapRepo = new InMemoryRoadmapRepository();
        var goalRepo = new InMemoryGoalRepository();
        var command = new RoadmapCreateCommand(roadmapRepo, goalRepo);

        var result = await command.ExecuteAsync(new Dictionary<string, object?> { ["title"] = "" });

        Assert.False(result.IsSuccess);
        Assert.Contains("título", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RoadmapAddMilestoneCommand_AddsMilestoneWithOrder()
    {
        var roadmapRepo = new InMemoryRoadmapRepository();
        var goalRepo = new InMemoryGoalRepository();

        var roadmap = new Roadmap
        {
            Id = "rm_1",
            Title = "Backend Master",
            Status = "active"
        };
        await roadmapRepo.CreateAsync(roadmap);

        var command = new RoadmapAddMilestoneCommand(roadmapRepo);
        var result = await command.ExecuteAsync(new Dictionary<string, object?>
        {
            ["roadmap_id"] = "rm_1",
            ["title"] = "Aprender SQLite y DDL"
        });

        Assert.True(result.IsSuccess);
        var milestone = Assert.IsType<RoadmapMilestone>(result.Data);
        Assert.Equal("Aprender SQLite y DDL", milestone.Title);
        Assert.Equal(0, milestone.OrderIndex);
        Assert.Equal("pending", milestone.Status);
    }

    [Fact]
    public async Task RoadmapCompleteMilestoneCommand_UpdatesMilestoneAndCascadesGoalProgress()
    {
        var roadmapRepo = new InMemoryRoadmapRepository();
        var goalRepo = new InMemoryGoalRepository();

        // 1. Create Goal
        var goal = new Goal
        {
            Id = "goal_cyber",
            Title = "Certificación de Seguridad",
            Status = "active",
            Progress = 0
        };
        await goalRepo.CreateAsync(goal);

        // 2. Create Roadmap linked to Goal with 2 milestones
        var m1 = new RoadmapMilestone { Id = "m_1", RoadmapId = "rm_cyber", Title = "Paso 1", OrderIndex = 0, Status = "pending" };
        var m2 = new RoadmapMilestone { Id = "m_2", RoadmapId = "rm_cyber", Title = "Paso 2", OrderIndex = 1, Status = "pending" };

        var roadmap = new Roadmap
        {
            Id = "rm_cyber",
            GoalId = "goal_cyber",
            Title = "Roadmap Seguridad",
            Status = "active"
        };
        await roadmapRepo.CreateAsync(roadmap, new[] { m1, m2 });

        var completeCommand = new RoadmapCompleteMilestoneCommand(roadmapRepo, goalRepo);

        // 3. Complete Step 1 -> 50%
        var result1 = await completeCommand.ExecuteAsync(new Dictionary<string, object?>
        {
            ["milestone_id"] = "m_1",
            ["completed"] = true
        });

        Assert.True(result1.IsSuccess);
        var updatedGoal = await goalRepo.GetByIdAsync("goal_cyber");
        Assert.NotNull(updatedGoal);
        Assert.Equal(50, updatedGoal.Progress);
        Assert.Equal("active", updatedGoal.Status);

        // 4. Complete Step 2 -> 100% and goal auto-completed
        var result2 = await completeCommand.ExecuteAsync(new Dictionary<string, object?>
        {
            ["milestone_id"] = "m_2",
            ["completed"] = true
        });

        Assert.True(result2.IsSuccess);
        updatedGoal = await goalRepo.GetByIdAsync("goal_cyber");
        Assert.NotNull(updatedGoal);
        Assert.Equal(100, updatedGoal.Progress);
        Assert.Equal("completed", updatedGoal.Status);

        // 5. Uncomplete Step 2 -> back to 50%
        var result3 = await completeCommand.ExecuteAsync(new Dictionary<string, object?>
        {
            ["milestone_id"] = "m_2",
            ["completed"] = false
        });

        Assert.True(result3.IsSuccess);
        updatedGoal = await goalRepo.GetByIdAsync("goal_cyber");
        Assert.NotNull(updatedGoal);
        Assert.Equal(50, updatedGoal.Progress);
    }
}
