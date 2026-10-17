namespace Atc.Azure.Messaging.EventHub.Options;

/// <summary>
/// The Blob Storage account where checkpointed Event Hub processors store checkpoints and partition ownership.
/// </summary>
public class CheckpointStorageOptions
{
    /// <summary>
    /// Gets or sets the blob service endpoint, e.g. <c>https://account.blob.core.windows.net</c>.
    /// </summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Gets or sets the credential for <see cref="ServiceUri"/>.
    /// When <see langword="null"/>, the credential of the Event Hubs connection is used.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a connection string for the storage account.
    /// </summary>
    public string? ConnectionString { get; set; }

    public BlobClientOptions? ClientOptions { get; set; }
}