using Xunit;

namespace Fritz.WebFormsTest.Test
{
    [Collection("Precompiler collection")]
    public class PageExtensionsFixture
    {
        [Fact]
        public void IsValidShouldBeFalseWhenMockedAsInvalid()
        {
            var page = WebApplicationProxy.GetPageByLocation<Web.Scenarios.Default>("/Scenarios/Default.aspx");

            page.MockIsValid(false);

            Assert.Equal(page.IsValid, false);
        }

        [Fact]
        public void IsValidShouldBeTrueWhenMockedAsValid()
        {
            var page = WebApplicationProxy.GetPageByLocation<Web.Scenarios.Default>("/Scenarios/Default.aspx");

            page.MockIsValid(true);

            Assert.Equal(page.IsValid, true);
        }
    }
}
