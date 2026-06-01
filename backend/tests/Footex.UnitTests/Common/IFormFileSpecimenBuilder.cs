using System.Reflection;
using AutoFixture.Kernel;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Footex.UnitTests.Common;

public class IFormFileSpecimenBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        var propertyInfo = request as PropertyInfo;
        if (propertyInfo != null && propertyInfo.PropertyType == typeof(IFormFile))
        {
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

        return new NoSpecimen();
    }
}
