using Downer.Services;

namespace Downer.Tests;

public class SpellCheckerTests
{
    [Fact]
    public async Task Loads_embedded_dictionary_and_checks_words()
    {
        var checker = new SpellChecker();
        Assert.False(checker.IsLoaded);

        await checker.LoadAsync();

        Assert.True(checker.IsLoaded);
        Assert.True(checker.IsCorrect("hello"));
        Assert.True(checker.IsCorrect("Hello"));      // sentence-case of a known word
        Assert.True(checker.IsCorrect("don't"));
        Assert.False(checker.IsCorrect("helloo"));
        Assert.False(checker.IsCorrect("recieve"));
    }

    [Fact]
    public void Everything_passes_before_the_dictionary_loads()
    {
        var checker = new SpellChecker();

        Assert.True(checker.IsCorrect("zzzzqqq"));
    }
}
