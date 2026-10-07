using app_curso_claude.Services;

namespace app_curso_claude.Tests.Services
{
    public class OperationResultTests
    {
        [Fact]
        public void Ok_NoArguments_ReturnsSuccessWithoutErrors()
        {
            var result = OperationResult.Ok();

            Assert.True(result.Success);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Fail_SingleError_ReturnsFailureWithThatError()
        {
            var result = OperationResult.Fail("The SKU is required.");

            Assert.False(result.Success);
            Assert.Equal(["The SKU is required."], result.Errors);
        }

        [Fact]
        public void Fail_SeveralErrors_KeepsThemInOrder()
        {
            var result = OperationResult.Fail("First error.", "Second error.");

            Assert.False(result.Success);
            Assert.Equal(["First error.", "Second error."], result.Errors);
        }

        [Fact]
        public void Fail_ArrayChangedAfterwards_DoesNotChangeTheResult()
        {
            string[] errors = ["Original error."];

            var result = OperationResult.Fail(errors);
            errors[0] = "Changed error.";

            Assert.Equal(["Original error."], result.Errors);
        }
    }
}
