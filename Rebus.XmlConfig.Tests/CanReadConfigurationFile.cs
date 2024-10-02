using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Swindler;
// ReSharper disable UnusedMember.Global

namespace Rebus.XmlConfig.Tests;

[TestFixture]
public class CanReadConfigurationFile : FixtureBase
{
    [Test]
    public void ItWorks()
    {
        using (AppConfig.Use("Examples/App-01.config"))
        {
            using (var activator = new BuiltinHandlerActivator())
            {
                Configure.With(activator)
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
                    .Routing(r => r.TypeBasedRoutingFromAppConfig())
                    .Start();
            }
        }
    }

    [Test]
    public void ItWorksWithComplementaryConfigAsWell()
    {
        using (AppConfig.Use("Examples/App-01.config"))
        {
            using (var activator = new BuiltinHandlerActivator())
            {
                Configure.With(activator)
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
                    .Routing(r => r.TypeBased().AddEndpointMappingsFromAppConfig())
                    .Start();
            }
        }
    }
}

public class SomeExistingType;