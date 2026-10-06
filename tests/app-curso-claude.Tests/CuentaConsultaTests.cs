using System.Net;
using System.Net.Http.Json;
using app_curso_claude.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace app_curso_claude.Tests
{
    public class CuentaConsultaTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public CuentaConsultaTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Consulta_DeCuentaExistente_DevuelveOkConLaCuenta()
        {
            var creada = await (await _client.PostAsJsonAsync("/Cuenta/Alta", new { titular = "Luis Gómez", saldoInicial = 320.75m }))
                .Content.ReadFromJsonAsync<Cuenta>();
            Assert.NotNull(creada);

            var response = await _client.GetAsync($"/Cuenta/Consulta/{creada.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cuenta = await response.Content.ReadFromJsonAsync<Cuenta>();
            Assert.NotNull(cuenta);
            Assert.Equal(creada.Id, cuenta.Id);
            Assert.Equal("Luis Gómez", cuenta.Titular);
            Assert.Equal(320.75m, cuenta.Saldo);
        }

        [Fact]
        public async Task Consulta_DeCuentaInexistente_DevuelveNotFound()
        {
            var response = await _client.GetAsync("/Cuenta/Consulta/9999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
