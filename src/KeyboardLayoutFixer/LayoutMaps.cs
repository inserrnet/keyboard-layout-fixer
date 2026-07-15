using System.Collections.Frozen;

namespace KeyboardLayoutFixer;

internal static class LayoutMaps
{
    private static readonly FrozenDictionary<char, char> EnToRu = new Dictionary<char, char>
    {
        ['q'] = 'й', ['w'] = 'ц', ['e'] = 'у', ['r'] = 'к', ['t'] = 'е', ['y'] = 'н', ['u'] = 'г', ['i'] = 'ш', ['o'] = 'щ', ['p'] = 'з',
        ['['] = 'х', [']'] = 'ъ', ['a'] = 'ф', ['s'] = 'ы', ['d'] = 'в', ['f'] = 'а', ['g'] = 'п', ['h'] = 'р', ['j'] = 'о',
        ['k'] = 'л', ['l'] = 'д', [';'] = 'ж', ['\''] = 'э', ['z'] = 'я', ['x'] = 'ч', ['c'] = 'с', ['v'] = 'м', ['b'] = 'и',
        ['n'] = 'т', ['m'] = 'ь', [','] = 'б', ['.'] = 'ю', ['`'] = 'ё'
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<char, char> RuToEn =
        EnToRu.ToDictionary(pair => pair.Value, pair => pair.Key).ToFrozenDictionary();

    public static string ConvertToRussian(string text) => Convert(text, EnToRu);

    public static string ConvertToEnglish(string text) => Convert(text, RuToEn);

    private static string Convert(string text, FrozenDictionary<char, char> map)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var lower = char.ToLowerInvariant(chars[i]);
            if (!map.TryGetValue(lower, out var mapped))
            {
                continue;
            }

            chars[i] = char.IsUpper(chars[i]) ? char.ToUpperInvariant(mapped) : mapped;
        }

        return new string(chars);
    }
}
