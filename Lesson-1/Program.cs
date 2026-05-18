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
        var selected = 0;

        while (true)
        {
            RenderMenu(selected);
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selected = (selected - 1 + Lessons.Length) % Lessons.Length;
                    break;
                case ConsoleKey.DownArrow:
                    selected = (selected + 1) % Lessons.Length;
                    break;
                case ConsoleKey.Enter:
                    Console.Clear();
                    Console.WriteLine($"Running: {Lessons[selected].Label}\n");
                    await Lessons[selected].Run([]);
                    Console.WriteLine("\nPress any key to return to menu...");
                    Console.ReadKey(intercept: true);
                    break;
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    Console.Clear();
                    return;
            }
        }
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
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("     ");
                Console.ResetColor();
                Console.Write($"{label,-24}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  {description}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑↓ navigate   Enter run   Esc/Q quit");
        Console.ResetColor();
    }
}
