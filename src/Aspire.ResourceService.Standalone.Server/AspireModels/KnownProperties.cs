// Copied from and inspired by .NET Aspire's KnownProperties.cs
// https://github.com/dotnet/aspire/blob/main/src/Shared/Model/KnownProperties.cs

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Models some known property names for known types of resources.
/// </summary>
/// <remarks>
/// Used as keys in the "properties" dictionary on resource snapshots and view models.
/// Should be compared using <see cref="StringComparer.Ordinal"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class KnownProperties
{
    public static class Resource
    {
        public const string Uid = "resource.uid";
        public const string Name = "resource.name";
        public const string Type = "resource.type";
        public const string DisplayName = "resource.displayName";
        public const string State = "resource.state";
        public const string ExitCode = "resource.exitCode";
        public const string CreateTime = "resource.createTime";
        public const string StartTime = "resource.startTime";
        public const string StopTime = "resource.stopTime";
        public const string Source = "resource.source";
        public const string HealthState = "resource.healthState";
        public const string ConnectionString = "resource.connectionString";
    }

    public static class Container
    {
        public const string Id = "container.id";
        public const string Image = "container.image";
        public const string Ports = "container.ports";
        public const string Command = "container.command";
        public const string Args = "container.args";
        public const string Lifetime = "container.lifetime";
    }
}
