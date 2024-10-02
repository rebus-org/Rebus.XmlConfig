using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Rebus.Logging;
using Rebus.Routing;
using Rebus.Routing.TypeBased;
using Rebus.XmlConfig;
// ReSharper disable UnusedMember.Global

namespace Rebus.Config;

/// <summary>
/// Configuration extensions that allow for picking up Rebus configuration from the current app.config/web.config
/// </summary>
public static class RoutingConfigurationExtensions
{
    /// <summary>
    /// Adds mappings
    /// </summary>
    public static TypeBasedRouterConfigurationExtensions.TypeBasedRouterConfigurationBuilder AddEndpointMappingsFromAppConfig(this TypeBasedRouterConfigurationExtensions.TypeBasedRouterConfigurationBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        var rebusRoutingConfigurationSection = GetRebusRoutingConfigurationSection();

        try
        {
            SetUpEndpointMappings(rebusRoutingConfigurationSection.MappingsCollection,
                (type, endpoint) => builder.Map(type, endpoint));

            return builder;
        }
        catch (Exception exception)
        {
            throw GetStandardConfigurationException(exception);
        }
    }

    /// <summary>
    /// Loads an almost-identical-to-Rebus1 configuration section of endpoint mappings from the current app.config/web.config
    /// </summary>
    public static void TypeBasedRoutingFromAppConfig(this StandardConfigurer<IRouter> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        try
        {
            Configure(configurer);
        }
        catch (Exception exception)
        {
            throw GetStandardConfigurationException(exception);
        }
    }

    static ConfigurationErrorsException GetStandardConfigurationException(Exception exception)
    {
        return new ConfigurationErrorsException(@"There was a problem configuring the type-based router. Please ensure that your configuration file has the following configuration section defined:

    <configSections>
        <section name=""rebus"" type=""Rebus.XmlConfig.RebusConfigurationSection, Rebus.XmlConfig"" />
    </configSections>

and then - further down - you can set up the mappings like this:

    <rebus>
        <endpoints>
            <add messages=""AnotherSystem.Messages.SomeParticularMessage, AnotherSystem.Messages"" endpoint=""specialhandling"" />

            <add messages=""SomeSystem.Messages"" endpoint=""somesystem"" />
            <add messages=""AnotherSystem.Messages"" endpoint=""anothersystem"" />
        </endpoints>
    </rebus>

in this case mapping all types from the assemblies SomeSystem.Messages and AnotherSystem.Messages to their respective endpoints, while overriding the mapping for SomeParticularMessage to some special handling somewhere else.

Please note that explicitly mapped types will always take precedence over assembly-mapped types.
", exception);
    }

    static void Configure(StandardConfigurer<IRouter> configurer)
    {
        var rebusRoutingConfigurationSection = GetRebusRoutingConfigurationSection();

        configurer.Register(c =>
        {
            var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
            var typeBasedRouter = new TypeBasedRouter(rebusLoggerFactory);

            SetUpEndpointMappings(rebusRoutingConfigurationSection.MappingsCollection, (type, endpoint) => typeBasedRouter.Map(type, endpoint));

            return typeBasedRouter;
        });
    }

    static RebusConfigurationSection GetRebusRoutingConfigurationSection()
    {
        var section = ConfigurationManager.GetSection("rebus");

        if (section == null)
        {
            throw new ConfigurationErrorsException(
                "Could not find 'rebus' configuration section in the current app.config/web.config");
        }

        if (section is RebusConfigurationSection rebusRoutingConfigurationSection)
        {
            return rebusRoutingConfigurationSection;
        }
        
        throw new ConfigurationErrorsException($"The configuration section 'rebus' is not a {typeof(RebusConfigurationSection)} as expected, it's a {section.GetType()}");
    }

    static void SetUpEndpointMappings(EndpointConfigurationElement mappings, Action<Type, string> mappingFunction)
    {
        var mappingElements = mappings.OrderBy(c => !c.IsAssemblyName).ToList();

        foreach (var element in mappingElements)
        {
            if (element.IsAssemblyName)
            {
                var assemblyName = element.Messages;
                var assembly = LoadAssembly(assemblyName);

                foreach (var type in assembly.GetTypes())
                {
                    mappingFunction(type, element.Endpoint);
                }
            }
            else
            {
                var typeName = element.Messages;
                var messageType = Type.GetType(typeName);

                if (messageType == null)
                {
                    throw new ConfigurationErrorsException($"Could not find the message type {typeName}. If you choose to map a specific message type, please ensure that the type is available for Rebus to load. This requires that the assembly can be found in Rebus' current runtime directory, that the type is available, and that any (of the optional) version and key requirements are matched");
                }

                mappingFunction(messageType, element.Endpoint);
            }
        }
    }

    static Assembly LoadAssembly(string assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (Exception e)
        {
            throw new ConfigurationErrorsException($@"
Something went wrong when trying to load message types from assembly {assemblyName}
{e}
For this to work, Rebus needs access to an assembly with one of the following filenames:
    {assemblyName}.dll
    {assemblyName}.exe
");
        }
    }
}