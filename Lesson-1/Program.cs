namespace Lesson_1;

internal class Program
{
    private static readonly (string Key, string Label, string Description, Func<string[], Task> Run)[] Lessons =
    [
        ("01_01_grounding",   "01_01 Grounding",   "Markdown → extract → dedupe → web search → grounded HTML",
            args => Lesson_01_01_grounding.Grounding.Run(args)),
        ("01_01_interaction", "01_01 Interaction", "Multi-turn conversation with reasoning model",
            _ => Lesson_01_01_interaction.MultiTurnConversation.Run()),
        ("01_01_structured",  "01_01 Structured",  "Extract typed JSON from free text using a schema",
            _ => Lesson_01_01_structured.StructuredOutput.Run()),
        ("01_task",           "01 Task — People",  "Filter CSV + LLM tagging + submit to hub.ag3nts.org",
            _ => Lesson_01_Task.TaskPeople.Run()),
    ];

    static async Task Main(string[] args)
    {
        if (args.Length > 0)
        {
            var key = args[0];
            var lesson = Lessons.FirstOrDefault(l => l.Key == key);
            if (lesson.Run != null)
            {
                await lesson.Run(args[1..]);
                return;
            }
        }

        await RunMenu();
    }

    private static async Task RunMenu()
    {
        if (Console.IsInputRedirected)
        {
            await RunTextMenu();
            return;
        }

        await RunArrowMenu();
    }

    private static async Task RunArrowMenu()
    {
        var selected = 0;
        Console.CursorVisible = false;

        try
        {
            while (true)
            {
                RenderMenu(selected);

                if (!TryReadKey(out var key))
                {
                    await RunTextMenu();
                    return;
                }

                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        selected = (selected - 1 + Lessons.Length) % Lessons.Length;
                        break;
                    case ConsoleKey.DownArrow:
                        selected = (selected + 1) % Lessons.Length;
                        break;
                    case ConsoleKey.Enter:
                        Console.Clear();
                        Console.CursorVisible = true;
                        Console.WriteLine($"Running: {Lessons[selected].Label}\n");
                        await Lessons[selected].Run([]);
                        Console.CursorVisible = false;
                        FlushInputBuffer();
                        Console.WriteLine("\nPress any key to return to menu...");
                        TryReadKey(out _);
                        break;
                    case ConsoleKey.Escape:
                    case ConsoleKey.Q:
                        Console.Clear();
                        return;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
            Console.ResetColor();
        }
    }

    private static async Task RunTextMenu()
    {
        while (true)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  bravecourses — Lesson 1");
            Console.ResetColor();
            Console.WriteLine();
            for (var i = 0; i < Lessons.Length; i++)
                Console.WriteLine($"  [{i + 1}] {Lessons[i].Label,-24}  {Lessons[i].Description}");
            Console.WriteLine("  [0] Exit");
            Console.WriteLine();
            Console.Write("  Choose: ");

            var input = Console.ReadLine()?.Trim();
            if (input == "0" || input == null) return;

            if (int.TryParse(input, out var n) && n >= 1 && n <= Lessons.Length)
            {
                Console.WriteLine($"\nRunning: {Lessons[n - 1].Label}\n");
                await Lessons[n - 1].Run([]);
                Console.WriteLine("\nDone. Press Enter to return to menu...");
                Console.ReadLine();
            }
        }
    }

    private static bool TryReadKey(out ConsoleKey key)
    {
        key = default;
        try
        {
            ConsoleKeyInfo info;
            do { info = Console.ReadKey(intercept: true); }
            while (info.Key == ConsoleKey.NoName);
            key = info.Key;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static void FlushInputBuffer()
    {
        try { while (Console.KeyAvailable) Console.ReadKey(intercept: true); }
        catch (InvalidOperationException) { }
    }

    private static void RenderMenu(int selected)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ╔══════════════════════════════════════════╗");
        Console.WriteLine("  ║           bravecourses — Lesson 1        ║");
        Console.WriteLine("  ╚══════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        for (var i = 0; i < Lessons.Length; i++)
        {
            var (_, label, description, _) = Lessons[i];
            if (i == selected)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.Write($"  ▶  {label,-24}");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  {description}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("     ");
                Console.ResetColor();
                Console.Write($"{label,-24}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  {description}");
            }
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑↓ navigate   Enter run   Esc/Q quit");
        Console.ResetColor();
    }
}
