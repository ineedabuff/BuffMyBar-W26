using System;
using BuffBar.Services;
using Xunit;

namespace BuffBar.Tests;

/// <summary>
/// Tests de la FFT maison (BuffBar.Services.Fft). Code pur et déterministe :
/// une sinusoïde de fréquence connue doit concentrer son énergie dans le bin
/// attendu. C'est exactement le genre de cœur algorithmique qui mérite un filet
/// de sécurité contre les régressions.
/// </summary>
public class FftTests
{
    private const int N = 1024; // puissance de deux

    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(200)]
    public void PureSine_PeaksAtExpectedBin(int bin)
    {
        var re = new float[N];
        var im = new float[N];

        // Sinusoïde réelle à 'bin' cycles sur la fenêtre.
        for (int i = 0; i < N; i++)
            re[i] = (float)Math.Sin(2.0 * Math.PI * bin * i / N);

        Fft.Transform(re, im);

        // Magnitude par bin sur la moitié utile du spectre.
        int peak = 0;
        double max = -1;
        for (int k = 1; k < N / 2; k++)
        {
            double mag = Math.Sqrt(re[k] * re[k] + im[k] * im[k]);
            if (mag > max) { max = mag; peak = k; }
        }

        Assert.Equal(bin, peak);
        Assert.True(max > 1.0, $"énergie trop faible au pic : {max}");
    }

    [Fact]
    public void Silence_ProducesNoEnergy()
    {
        var re = new float[N];
        var im = new float[N];

        Fft.Transform(re, im);

        double total = 0;
        for (int k = 0; k < N; k++)
            total += Math.Sqrt(re[k] * re[k] + im[k] * im[k]);

        Assert.True(total < 1e-3, $"un signal nul devrait rester nul, somme = {total}");
    }

    [Fact]
    public void TrivialLength_IsNoOp()
    {
        var re = new float[] { 42f };
        var im = new float[] { 0f };

        Fft.Transform(re, im); // n <= 1 : retour immédiat, pas d'exception

        Assert.Equal(42f, re[0]);
    }
}
