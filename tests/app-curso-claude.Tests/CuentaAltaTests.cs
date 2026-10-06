using System.Net;
using System.Net.Http.Json;
using app_curso_claude.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace app_curso_claude.Tests
{
    public class CuentaAltaTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public CuentaAltaTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Alta_ConDatosValidos_DevuelveCreatedConLaCuenta()
        {
            var response = await _client.PostAsJsonAsync("/Cuenta/Alta", new { titular = "Ana Pérez", saldoInicial = 150.50m });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var cuenta = await response.Content.ReadFromJsonAsync<Cuenta>();
            Assert.NotNull(cuenta);
            Assert.True(cuenta.Id > 0);
            Assert.Equal("Ana Pérez", cuenta.Titular);
            Assert.Equal(150.50m, cuenta.Saldo);
        }

        [Fact]
        public async Task Alta_DosCuentas_AsignaIdsDistintos()
        {
            var primera = await (await _client.PostAsJsonAsync("/Cuenta/Alta", new { titular = "Uno", saldoInicial = 0m }))
                .Content.ReadFromJsonAsync<Cuenta>();
            var segunda = await (await _client.PostAsJsonAsync("/Cuenta/Alta", new { titular = "Dos", saldoInicial = 0m }))
                .Content.ReadFromJsonAsync<Cuenta>();

            Assert.NotEqual(primera!.Id, segunda!.Id);
        }

        [Theory]
        [InlineData("", 100)]
        [InlineData("Ana", -1)]
        public async Task Alta_ConDatosInvalidos_DevuelveBadRequest(string titular, double saldoInicial)
        {
            var response = await _client.PostAsJsonAsync("/Cuenta/Alta", new { titular, saldoInicial });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
