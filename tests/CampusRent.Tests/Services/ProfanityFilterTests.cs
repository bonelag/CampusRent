using System.Net;
using System.Text;
using CampusRent.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusRent.Tests;
public class ProfanityFilterTests
{
    [Theory]
    [InlineData("Phòng sạch sẽ và thoáng", false)]
    [InlineData("ĐM phòng này", true)]
    [InlineData("FUCK", true)]
    [InlineData("admin thân thiện", false)]
    public void ProfanityUsesWordBoundaries(string text, bool expected) => Assert.Equal(expected, new ProfanityFilterService().CheckProfanity(text).IsFlagged);
}
