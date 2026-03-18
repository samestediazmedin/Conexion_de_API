using Xunit;

namespace WeatherAppBack.Tests;

public class WeatherServiceTests
{
    // Prueba 1: verificación de lógica básica
    [Fact]
    public void Suma_DosCifras_RetornaResultadoCorrecto()
    {
        var resultado = 2 + 2;
        Assert.Equal(4, resultado);
    }

    // Prueba 2: ejemplo con Theory (múltiples casos)
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Temperatura_ValorNumerico_EsUnEntero(int temp)
    {
        Assert.IsType<int>(temp);
    }

    // Prueba 3: validar que una lista no esté vacía
    [Fact]
    public void ListaCiudades_NoEstaVacia()
    {
        var ciudades = new List<string> { "Bogotá", "Medellín", "Cartagena" };
        Assert.NotEmpty(ciudades);
    }
}
