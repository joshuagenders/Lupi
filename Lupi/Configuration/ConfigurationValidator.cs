using System.Linq;
using FluentValidation;
using Lupi.Listeners;

namespace Lupi.Configuration
{

    public class ConfigurationValidator : AbstractValidator<Config>
    {
        public ConfigurationValidator()
        {
            RuleFor(c => c.Concurrency.Threads).GreaterThanOrEqualTo(0)
                .WithMessage("Threads must be positive");
            RuleFor(c => c.Throughput.Phases.Any() || c.Concurrency.Phases.Any()).Equal(true)
                .WithMessage("Must provide at least one throughput or concurrency and duration values");
            RuleFor(c => c.Throughput.Tps).GreaterThanOrEqualTo(0)
                .WithMessage("Tps (Tests per second) must be positive");
            RuleFor(c => c.Throughput.Iterations).GreaterThanOrEqualTo(0)
                .WithMessage("Iterations must be positive");
            RuleFor(c => c.Throughput.Phases.All(p => p.FromTps >= 0)).Equal(true)
                .WithMessage("From (Throughput) must be positive");
            RuleFor(c => c.Throughput.Phases.All(p => p.ToTps >= 0)).Equal(true)
                .WithMessage("To (Throughput) must be positive");
            RuleFor(c => c.Throughput.Phases.All(p => p.Tps >= 0)).Equal(true)
                .WithMessage("Tps must be positive");
            RuleFor(c => c.Concurrency.Phases.All(p => p.FromThreads >= 0)).Equal(true)
                .WithMessage("From (Concurrency) must be positive");
            RuleFor(c => c.Concurrency.Phases.All(p => p.ToThreads >= 0)).Equal(true)
                .WithMessage("To (Concurrency) must be positive");
            RuleFor(c => c.Concurrency.Phases.All(p => p.Threads >= 0)).Equal(true)
                .WithMessage("Threads must be positive");
            RuleFor(c => c.Concurrency.Phases.All(p => !(p.Threads > 0 && (p.FromThreads > 0 || p.ToThreads > 0)))).Equal(true)
                .WithMessage("Concurrency phases must not contain both Threads and From/To");
            RuleFor(c => c.Throughput.Phases.All(p => !(p.Tps > 0 && (p.FromTps > 0 || p.ToTps > 0)))).Equal(true)
                .WithMessage("Throughput phases must not contain both Tps and From/To");
            var aggregatedResultProperties = typeof(AggregatedResult).GetProperties().Select(p => p.Name);
            RuleFor(c => c.ExitConditions.All(e => aggregatedResultProperties.Contains(e.Property))).Equal(true)
                .WithMessage("Exit condition properties must exist on the AggregatedResult type");
        }
    }
}
