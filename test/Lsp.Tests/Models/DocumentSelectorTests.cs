using System;
using FluentAssertions;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using Xunit;

namespace Lsp.Tests.Models
{
    public class DocumentSelectorTests
    {
        [Theory, JsonFixture]
        public void SimpleTest(string expected)
        {
            var model = new DocumentSelector(new DocumentFilter[]
            {
                new DocumentFilter()
                {
                    Language = "csharp",
                },
                new DocumentFilter()
                {
                    Pattern = "**/*.vb"
                },
                new DocumentFilter()
                {
                    Scheme = "visualbasic"
                },
            });
            var result = Fixture.SerializeObject(model);

            result.Should().Be(expected);

            var deresult = new Serializer(ClientVersion.Lsp3).DeserializeObject<DocumentSelector>(expected);
            deresult.ShouldBeEquivalentTo(model);
        }
    }
}
