using System.IO;
using AutoFixture;
using AutoFixture.Kernel;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Footex.UnitTests.Common
{
    public class IFormFileCustomization : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (
                request is not System.Reflection.ParameterInfo pi
                || pi.ParameterType != typeof(IFormFile)
            )
            {
                return new NoSpecimen();
            }

            var mockFile = new Mock<IFormFile>();
            var content = "Hello World from a Fake File";
            var fileName = "test.pdf";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            mockFile.Setup(_ => _.OpenReadStream()).Returns(ms);
            mockFile.Setup(_ => _.FileName).Returns(fileName);
            mockFile.Setup(_ => _.Length).Returns(ms.Length);

            return mockFile.Object;
        }
    }
}
