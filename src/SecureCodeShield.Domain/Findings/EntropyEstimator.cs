namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// Shannon entropy over a candidate literal, in bits per character.
/// Random credentials sit high; English words, paths and repeated characters sit low.
/// </summary>
public static class EntropyEstimator
{
    public static double BitsPerCharacter(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            return 0d;
        }

        Span<int> counts = stackalloc int[128];
        var wide = 0;

        foreach (var character in value)
        {
            if (character < 128)
            {
                counts[character]++;
            }
            else
            {
                wide++;
            }
        }

        var entropy = 0d;
        double length = value.Length;

        foreach (var count in counts)
        {
            if (count == 0)
            {
                continue;
            }

            var probability = count / length;
            entropy -= probability * Math.Log2(probability);
        }

        if (wide > 0)
        {
            var probability = wide / length;
            entropy -= probability * Math.Log2(probability);
        }

        return entropy;
    }
}
