namespace LoadTest.Test;

using LoadTest.Helpers;

public class KeyListenerTests
{
    [Theory]
    [InlineData('p')]
    [InlineData('P')]
    public void P_pauses(char key)
    {
        var run = new RunController();

        Assert.NotNull(KeyListener.HandleKey(run, key));
        Assert.True(run.IsPaused);
    }

    [Theory]
    [InlineData('r')]
    [InlineData('R')]
    public void R_resumes(char key)
    {
        var run = new RunController();
        run.Pause();

        Assert.NotNull(KeyListener.HandleKey(run, key));
        Assert.False(run.IsPaused);
    }

    [Theory]
    [InlineData('q')]
    [InlineData('Q')]
    public void Q_quits(char key)
    {
        var run = new RunController();

        Assert.NotNull(KeyListener.HandleKey(run, key));
        Assert.True(run.IsQuitRequested);
    }

    [Fact]
    public void Keys_that_change_nothing_give_no_message()
    {
        var run = new RunController();

        Assert.Null(KeyListener.HandleKey(run, 'r'));

        KeyListener.HandleKey(run, 'p');
        Assert.Null(KeyListener.HandleKey(run, 'p'));

        KeyListener.HandleKey(run, 'q');
        Assert.Null(KeyListener.HandleKey(run, 'q'));
    }

    [Theory]
    [InlineData('x')]
    [InlineData(' ')]
    [InlineData('\r')]
    public void Other_keys_are_ignored(char key)
    {
        var run = new RunController();

        Assert.Null(KeyListener.HandleKey(run, key));
        Assert.False(run.IsPaused);
        Assert.False(run.IsQuitRequested);
    }
}
