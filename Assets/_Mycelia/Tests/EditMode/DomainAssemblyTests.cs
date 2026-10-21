using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Mycelia.Domain.Tests
{
    /// <summary>
    /// Architecture guard for ADR-0003: the domain must stay engine-free.
    /// </summary>
    public class DomainAssemblyTests
    {
        [Test]
        public void DomainAssembly_DoesNotReferenceUnity()
        {
            var references = Assembly.Load("Mycelia.Domain")
                .GetReferencedAssemblies()
                .Select(reference => reference.Name);

            Assert.That(references, Has.None.StartsWith("Unity"));
        }
    }
}
