namespace E2e.Bdd.Support;

internal static class DocumentoFactory
{
    public static string NovoCpf()
    {
        var n = new int[9];
        for (var i = 0; i < 9; i++)
        {
            n[i] = Random.Shared.Next(0, 10);
        }

        var d1 = Digito(n, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        var d2 = Digito([.. n, d1], [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);
        return string.Concat(n.Select(x => x.ToString())) + d1 + d2;
    }

    public static string NovaPlaca(string suffix)
    {
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var a = letters[Random.Shared.Next(letters.Length)];
        var b = letters[Random.Shared.Next(letters.Length)];
        var c = letters[Random.Shared.Next(letters.Length)];
        var d = letters[Random.Shared.Next(letters.Length)];
        var digit = suffix.Length > 0 && char.IsDigit(suffix[^1])
            ? suffix[^1]
            : (char)('0' + Random.Shared.Next(0, 10));
        return $"{a}{b}{c}{digit}{d}{Random.Shared.Next(0, 10)}{Random.Shared.Next(0, 10)}";
    }

    private static int Digito(IReadOnlyList<int> nums, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            sum += nums[i] * weights[i];
        }

        var mod = sum % 11;
        return mod < 2 ? 0 : 11 - mod;
    }
}
