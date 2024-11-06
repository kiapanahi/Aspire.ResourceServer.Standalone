namespace Aspire.ResourceServer.Standalone.ResourceLocator;

internal sealed partial class DockerResourceProvider
{
    private bool _disposedValue;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _dockerClient?.Dispose();
            }

            _disposedValue = true;
        }
    }
}
