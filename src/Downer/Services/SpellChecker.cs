using System.Collections.Concurrent;
using System.Reflection;
using WeCantSpell.Hunspell;

namespace Downer.Services;

/// <summary>
/// Hunspell-backed spell checking against the embedded en_US dictionary.
/// Loading is explicit and off-thread; until it completes every word passes,
/// so the editor never blocks or flashes false squiggles.
/// </summary>
public sealed class SpellChecker
{
    private readonly ConcurrentDictionary<string, bool> _cache = new();
    private volatile WordList? _words;

    public bool IsLoaded => _words is not null;

    public async Task LoadAsync()
    {
        if (_words is not null)
            return;

        _words = await Task.Run(() =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var dic = assembly.GetManifestResourceStream("Downer.Dictionaries.en_US.dic")!;
            using var aff = assembly.GetManifestResourceStream("Downer.Dictionaries.en_US.aff")!;
            return WordList.CreateFromStreams(dic, aff);
        });
    }

    public bool IsCorrect(string word)
    {
        var words = _words;
        if (words is null)
            return true;

        return _cache.GetOrAdd(word, static (w, list) => list.Check(w), words);
    }
}
