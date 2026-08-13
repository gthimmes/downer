using Downer.Core;

namespace Downer.Tests;

public class TaskCheckboxesTests
{
    [Fact]
    public void Click_on_unchecked_box_checks_it()
    {
        var hit = TaskCheckboxes.HitTest("- [ ] milk", 3);

        Assert.NotNull(hit);
        Assert.Equal(3, hit!.StateCharOffset);
        Assert.Equal('x', hit.NewState);
    }

    [Fact]
    public void Click_on_checked_box_unchecks_it()
    {
        var hit = TaskCheckboxes.HitTest("- [x] milk", 3);

        Assert.NotNull(hit);
        Assert.Equal(' ', hit!.NewState);
    }

    [Fact]
    public void Uppercase_X_unchecks_too()
    {
        var hit = TaskCheckboxes.HitTest("- [X] milk", 3);

        Assert.Equal(' ', hit!.NewState);
    }

    [Fact]
    public void The_whole_marker_region_is_clickable()
    {
        // "- [ ] milk": marker starts at 0, checkbox ends after "] " (offset 6, exclusive).
        Assert.NotNull(TaskCheckboxes.HitTest("- [ ] milk", 0));
        Assert.NotNull(TaskCheckboxes.HitTest("- [ ] milk", 5));
        Assert.Null(TaskCheckboxes.HitTest("- [ ] milk", 7));
    }

    [Fact]
    public void Item_text_is_not_clickable()
    {
        Assert.Null(TaskCheckboxes.HitTest("- [ ] milk", 8));
    }

    [Fact]
    public void Non_task_lines_return_null()
    {
        Assert.Null(TaskCheckboxes.HitTest("- plain bullet", 1));
        Assert.Null(TaskCheckboxes.HitTest("plain text", 2));
        Assert.Null(TaskCheckboxes.HitTest("# heading", 0));
    }

    [Fact]
    public void Works_on_later_lines()
    {
        var text = "# Todo\n- [ ] first\n- [x] second";
        var hit = TaskCheckboxes.HitTest(text, text.IndexOf("[x]") + 1);

        Assert.NotNull(hit);
        Assert.Equal(text.IndexOf("[x]") + 1, hit!.StateCharOffset);
        Assert.Equal(' ', hit.NewState);
    }

    [Fact]
    public void Indented_task_items_toggle()
    {
        var hit = TaskCheckboxes.HitTest("  - [ ] nested", 4);

        Assert.NotNull(hit);
        Assert.Equal(5, hit!.StateCharOffset);
        Assert.Equal('x', hit.NewState);
    }

    [Fact]
    public void Lines_inside_code_fences_do_not_toggle()
    {
        Assert.Null(TaskCheckboxes.HitTest("- [ ] milk", 3, FenceLineState.Inside));
    }

    [Fact]
    public void Out_of_range_offsets_return_null()
    {
        Assert.Null(TaskCheckboxes.HitTest("- [ ] milk", -1));
        Assert.Null(TaskCheckboxes.HitTest("- [ ] milk", 99));
        Assert.Null(TaskCheckboxes.HitTest("", 0));
    }

    [Fact]
    public void Crlf_documents_hit_the_right_char()
    {
        var text = "intro\r\n- [ ] item";
        var hit = TaskCheckboxes.HitTest(text, text.IndexOf('['));

        Assert.NotNull(hit);
        Assert.Equal('x', hit!.NewState);
        Assert.Equal(text.IndexOf('[') + 1, hit.StateCharOffset);
    }
}
