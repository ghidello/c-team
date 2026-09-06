using Xunit;

namespace CTeam.Experiments.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentVariableCollection
{
    public const string Name = "Process environment";
}
