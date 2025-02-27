// Copied from and inspired by .NET Aspire's ResourceSnapshot.cs
// https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting/Dashboard/GenericResourceSnapshot.cs

using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

internal sealed class GenericResourceSnapshot(CustomResourceSnapshot state) : ResourceSnapshot
{
    public override string ResourceType => state.ResourceType;

    protected override IEnumerable<(string Key, Value Value, bool IsSensitive)> GetProperties()
    {
        foreach (var (key, value, isSensitive) in state.Properties)
        {
            var result = ConvertToValue(value);

            yield return (key, result, isSensitive);
        }
    }
}
