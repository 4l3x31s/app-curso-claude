using app_curso_claude.Services;
using app_curso_claude.Tests.Fakes;

namespace app_curso_claude.Tests.Services
{
    public class ContactRequestServiceTests
    {
        private const string ExistingSku = "SKU-00001";
        private const string ValidEmail = "ana@example.com";
        private const string ValidMessage = "I would like to know more.";

        private readonly FakeContactRequestRepository _requests = new();
        private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 3, 15, 10, 30, 0, TimeSpan.Zero));
        private readonly RecordingLogger<ContactRequestService> _logger = new();
        private readonly ContactRequestService _service;

        public ContactRequestServiceTests()
        {
            _service = new ContactRequestService(_requests, FakeProductRepository.WithSkus(ExistingSku), _clock, _logger);
        }

        [Fact]
        public async Task SendAsync_FirstRequestOfTheYear_GetsTheFirstFolio()
        {
            var result = await _service.SendAsync("Consulta", null, ValidEmail, ValidMessage);

            Assert.True(result.Success);
            Assert.Empty(result.Errors);
            Assert.Equal("SOL-2026-0001", result.Folio);
        }

        [Fact]
        public async Task SendAsync_ConsecutiveRequests_GetCorrelativeFolios()
        {
            var first = await _service.SendAsync("Consulta", null, ValidEmail, ValidMessage);
            var second = await _service.SendAsync("Reclamo", null, ValidEmail, ValidMessage);
            var third = await _service.SendAsync("Solicitud", null, ValidEmail, ValidMessage);

            Assert.Equal("SOL-2026-0001", first.Folio);
            Assert.Equal("SOL-2026-0002", second.Folio);
            Assert.Equal("SOL-2026-0003", third.Folio);
        }

        [Fact]
        public async Task SendAsync_FirstRequestOfANewYear_RestartsTheSequence()
        {
            await _service.SendAsync("Consulta", null, ValidEmail, ValidMessage);
            await _service.SendAsync("Consulta", null, ValidEmail, ValidMessage);
            _clock.Now = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = await _service.SendAsync("Consulta", null, ValidEmail, ValidMessage);

            Assert.Equal("SOL-2027-0001", result.Folio);
        }

        [Fact]
        public async Task SendAsync_RejectedRequest_DoesNotConsumeAFolio()
        {
            await _service.SendAsync("Queja", null, ValidEmail, ValidMessage);

            var result = await _service.SendAsync("Consulta", null, ValidEmail, ValidMessage);

            Assert.Equal("SOL-2026-0001", result.Folio);
        }

        [Fact]
        public async Task SendAsync_ValidRequest_StoresItWithItsFolioAndTheCurrentTime()
        {
            var result = await _service.SendAsync("Reclamo", ExistingSku, ValidEmail, ValidMessage);

            var stored = Assert.Single(_requests.Requests);
            Assert.Equal(result.Folio, stored.Folio);
            Assert.Equal("Reclamo", stored.Type);
            Assert.Equal(ExistingSku, stored.Sku);
            Assert.Equal(ValidEmail, stored.Email);
            Assert.Equal(ValidMessage, stored.Message);
            Assert.Equal(_clock.Now.UtcDateTime, stored.CreatedAt);
        }

        [Theory]
        [InlineData("Consulta")]
        [InlineData("Reclamo")]
        [InlineData("Solicitud")]
        public async Task SendAsync_AllowedType_Succeeds(string type)
        {
            var result = await _service.SendAsync(type, null, ValidEmail, ValidMessage);

            Assert.True(result.Success);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Queja")]
        [InlineData("consulta")]
        public async Task SendAsync_UnknownType_FailsWithTheTypeError(string? type)
        {
            var result = await _service.SendAsync(type, null, ValidEmail, ValidMessage);

            AssertRejected(result, "Type must be Consulta, Reclamo or Solicitud.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ana")]
        [InlineData("ana@")]
        [InlineData("@example.com")]
        [InlineData("ana@example")]
        [InlineData("ana example@example.com")]
        [InlineData("Ana <ana@example.com>")]
        public async Task SendAsync_InvalidEmail_FailsWithTheEmailError(string? email)
        {
            var result = await _service.SendAsync("Consulta", null, email, ValidMessage);

            AssertRejected(result, "Email is not valid.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(9)]
        [InlineData(501)]
        public async Task SendAsync_MessageLengthOutOfRange_FailsWithTheMessageError(int length)
        {
            var result = await _service.SendAsync("Consulta", null, ValidEmail, new string('a', length));

            AssertRejected(result, "Message must be between 10 and 500 characters.");
        }

        [Theory]
        [InlineData(10)]
        [InlineData(500)]
        public async Task SendAsync_MessageLengthOnTheLimit_Succeeds(int length)
        {
            var result = await _service.SendAsync("Consulta", null, ValidEmail, new string('a', length));

            Assert.True(result.Success);
        }

        [Fact]
        public async Task SendAsync_NullMessage_FailsWithTheMessageError()
        {
            var result = await _service.SendAsync("Consulta", null, ValidEmail, null);

            AssertRejected(result, "Message must be between 10 and 500 characters.");
        }

        [Fact]
        public async Task SendAsync_UnknownSku_FailsWithTheSkuError()
        {
            var result = await _service.SendAsync("Consulta", "SKU-DOES-NOT-EXIST", ValidEmail, ValidMessage);

            AssertRejected(result, "No product found with SKU SKU-DOES-NOT-EXIST.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAsync_NoSku_SucceedsAndStoresNoSku(string? sku)
        {
            var result = await _service.SendAsync("Consulta", sku, ValidEmail, ValidMessage);

            Assert.True(result.Success);
            Assert.Null(Assert.Single(_requests.Requests).Sku);
        }

        [Fact]
        public async Task SendAsync_SeveralInvalidValues_ReportsEveryErrorAndStoresNothing()
        {
            var result = await _service.SendAsync("Queja", "SKU-DOES-NOT-EXIST", "ana", "short");

            Assert.False(result.Success);
            Assert.Null(result.Folio);
            Assert.Equal(4, result.Errors.Count);
            Assert.Empty(_requests.Requests);
        }

        [Fact]
        public async Task SendAsync_ValidRequest_LogsTheFolioButNeverTheEmail()
        {
            var result = await _service.SendAsync("Consulta", null, ValidEmail, ValidMessage);

            Assert.Contains(_logger.Entries, entry => entry.Contains(result.Folio!));
            Assert.DoesNotContain(_logger.Entries, entry => entry.Contains(ValidEmail));
        }

        private void AssertRejected(ContactRequestResult result, string expectedError)
        {
            Assert.False(result.Success);
            Assert.Null(result.Folio);
            Assert.Equal([expectedError], result.Errors);
            Assert.Empty(_requests.Requests);
        }
    }
}
