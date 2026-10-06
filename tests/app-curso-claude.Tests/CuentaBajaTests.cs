using System.Net;
using System.Net.Http.Json;
using app_curso_claude.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace app_curso_claude.Tests
{
    public class CuentaBajaTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public CuentaBajaTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<Cuenta> CrearCuentaAsync()
        {
            var response = await _client.PostAsJsonAsync("/Cuenta/Alta", new { titular = "Para Baja", saldoInicial = 10m });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var cuenta = await response.Content.ReadFromJsonAsync<Cuenta>();
            Assert.NotNull(cuenta);
            return cuenta;
        }

        [Fact]
        public async Task Baja_DeCuentaExistente_DevuelveNoContent()
        {
            var cuenta = await CrearCuentaAsync();

            var response = await _client.PostAsync($"/Cuenta/Baja/{cuenta.Id}", null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Baja_DosVecesLaMismaCuenta_LaSegundaDevuelveNotFound()
        {
            var cuenta = await CrearCuentaAsync();

            var primera = await _client.PostAsync($"/Cuenta/Baja/{cuenta.Id}", null);
            var segunda = await _client.PostAsync($"/Cuenta/Baja/{cuenta.Id}", null);

            Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, segunda.StatusCode);
        }

        [Fact]
        public async Task Baja_DeIdInexistente_DevuelveNotFound()
        {
            var response = await _client.PostAsync("/Cuenta/Baja/9999", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
