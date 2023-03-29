using System.Configuration;

namespace Rebus.XmlConfig;

/// <summary>
/// Represents a mapping from an assembly of types or one specifiec type to an endpoint
/// </summary>
public class EndpointMapping : ConfigurationElement
{
    const string MessagesPropertyName = "messages";
    const string EndpointPropertyName = "endpoint";

    /// <summary>
    /// Gets/sets the value of the messages attribute on the add element
    /// </summary>
    [ConfigurationProperty(MessagesPropertyName)]
    public string Messages
    {
        get => (string)this[MessagesPropertyName];
        set => this[MessagesPropertyName] = value;
    }

    /// <summary>
    /// Gets/sets the value of the endpoint attribute on the add element
    /// </summary>
    [ConfigurationProperty(EndpointPropertyName)]
    public string Endpoint
    {
        get => (string)this[EndpointPropertyName];
        set => this[EndpointPropertyName] = value;
    }

    internal bool IsAssemblyName => !string.IsNullOrWhiteSpace(Endpoint)
                                    && !Messages.Contains(",");
}