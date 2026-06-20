using System;

namespace BuffBar.Services;

/// <summary>
/// FFT radix-2 itérative (Cooley-Tukey), en place, sans dépendance.
/// La longueur des tableaux doit être une puissance de deux.
/// </summary>
internal static class Fft
{
    /// <summary>Transformée en place. re/im de même longueur (puissance de 2).</summary>
    public static void Transform(float[] re, float[] im)
    {
        int n = re.Length;
        if (n <= 1) return;

        // Permutation par inversion de bits.
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;

            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
        }

        // Papillons.
        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = -2.0 * Math.PI / len;
            float wRealStep = (float)Math.Cos(ang);
            float wImagStep = (float)Math.Sin(ang);

            for (int i = 0; i < n; i += len)
            {
                float wReal = 1f, wImag = 0f;
                int half = len >> 1;

                for (int k = 0; k < half; k++)
                {
                    int a = i + k;
                    int b = i + k + half;

                    float tReal = wReal * re[b] - wImag * im[b];
                    float tImag = wReal * im[b] + wImag * re[b];

                    re[b] = re[a] - tReal;
                    im[b] = im[a] - tImag;
                    re[a] += tReal;
                    im[a] += tImag;

                    float nextReal = wReal * wRealStep - wImag * wImagStep;
                    wImag = wReal * wImagStep + wImag * wRealStep;
                    wReal = nextReal;
                }
            }
        }
    }
}
