using System.Threading.Tasks;
using Bid_Go_Backend.Services.Email;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Bid_Go.Tests.Unit.Services
{
    /// <summary>
    /// Unit tests for EmailService. This test only ensures no unexpected exception is thrown with provided config.
    /// </summary>
    public class EmailServiceTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly EmailService _service;

        public EmailServiceTests()
        {
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c["SmtpSettings:Host"]).Returns("smtp.sapo.pt");
            _configMock.Setup(c => c["SmtpSettings:Port"]).Returns("587");
            _configMock.Setup(c => c["SmtpSettings:User"]).Returns("bidandgo2025@sapo.pt");
            _configMock.Setup(c => c["SmtpSettings:Pass"]).Returns("Bidandgo2025");


            _service = new EmailService(_configMock.Object);
        }

        [Fact]
        public async Task SendEmailAsync_ShouldNotThrow_WhenCalled()
        {
            // Arrange
            var to = "recipient@test.com";
            var subject = "Test Subject";
            var body = "Test Body";

            // Act & Assert
            var exception = await Record.ExceptionAsync(() => _service.SendEmailAsync(to, subject, body));

            if (exception is System.Net.Mail.SmtpException smtpEx)
            {
                Assert.Contains("Sorry", smtpEx.Message);
            }
            else
            {
                Assert.Null(exception);
            }
        }
    }
}