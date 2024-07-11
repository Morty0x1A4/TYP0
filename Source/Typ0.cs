using Spectre.Console;
using System.Diagnostics;
using System.Text.Json;
namespace Typ0;

// Interface for typos
interface ITypo
{
    int FailureCounter { get; set; }
    string? WrongInput { get; set; }
}

// Base class for typos
class Typo : ITypo
{
    public Typo() { }
    public Typo(int _failureCounter, string? _wrongInput)
    {
        FailureCounter = _failureCounter;
        WrongInput = _wrongInput;
    }
    public int FailureCounter { get; set; }
    public string? WrongInput { get; set; }

}

// Class for letter typos
class LetterTypo : Typo
{
    string? wrongFinger;
    //parameterless constructor needed for generics
    public LetterTypo()
    {
        WrongFingerCounter(WrongInput);
    }
    public LetterTypo(int failureCounter, string? wrongLetter) : base(failureCounter, wrongLetter)
    {
        WrongFingerCounter(wrongLetter);
    }
    readonly Dictionary<string, HashSet<string>> fingers = new()
    {
        { "left Pinky", new HashSet<string> { "Q", "A", "Y", "q", "a", "y" } },
        { "left RingFinger", new HashSet<string> { "W", "S", "X", "w", "s", "x" } },
        { "left MiddleFinger", new HashSet<string> { "E", "D", "C", "e", "d", "c" } },
        { "left IndexFinger", new HashSet<string> { "R", "T", "F", "G", "V", "B", "r", "t", "f", "g", "v", "b" } },
        { "Thumbs", new HashSet<string> { " " } },
        { "right Pinky", new HashSet<string> { "Z", "U", "H", "J", "N", "M", "z", "u", "h", "j", "n", "m" } },
        { "right RingFinger", new HashSet<string> { "I", "K", "i", "k", "," } },
        { "right MiddleFinger", new HashSet<string> { "O", "L", "o", "l", "." } },
        { "right IndexFinger", new HashSet<string> { "ß", "P", "Ü", "Ö", "Ä", "p", "ü", "ö", "ä" } }
    };

    public string? WrongFinger { get => wrongFinger; }
    // Method to determine the wrong finger based on the wrong character
    private void WrongFingerCounter(string? wrongChar)
    {
        foreach (var f in fingers)
        {
            if (f.Value.Contains(wrongChar))
            {
                wrongFinger = f.Key;
                return;
            }
        }
    }
    public override string ToString()
    {
        return "Letter;" + WrongInput + ";Count;" + FailureCounter + ";Finger;" + WrongFinger;
    }
}

class WordTypo : Typo
{
    //parameterless constructor needed for generics
    public WordTypo(int _failureCounter, string _wrongWord) : base(_failureCounter, _wrongWord)
    {
    }
    public WordTypo() { }

    public override string ToString()
    {
        return "Word;" + WrongInput + ";Count;" + FailureCounter;
    }
}
class Program
{

    static async Task Main(string[] args)
    {
        await ShowStartScreen();
        await MainMenu();
    }

    static async Task ShowStartScreen()
    {
        bool showStartScreen = true;
        Console.CursorVisible = false;
        AnsiConsole.Clear();
        var font = FigletFont.Load("Larry 3D.flf");


        AnsiConsole.Write(
            new FigletText(font, "TYP0")
                .Centered()
                .Color(Color.White));

        var stopwatch = Stopwatch.StartNew();

        int currentLine = 0;

        while (showStartScreen && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
        {
            AnsiConsole.Clear();

            // Move the Figlet text one line down
            for (int i = 0; i < currentLine; i++)
            {
                Console.WriteLine();
            }

            AnsiConsole.Write(
                new FigletText(font, "TYP0")
                    .Centered()
                    .Color(Color.White));

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape)
                {
                    showStartScreen = false;
                }
            }
            if (currentLine <= 10)
            {
                currentLine++;
            }
            await Task.Delay(25); // Move down every second
        }

        stopwatch.Stop();
    }
    static async Task MainMenu()
    {
        bool run = true;
        while (run)
        {
            Console.CursorVisible = false;
            Console.SetWindowSize(105, 40);
            List<string> words;
            AnsiConsole.Clear();
            var menu = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .Title("TYP" + $"[red]{0}[/]")
                        .HighlightStyle("red")
                        .AddChoices(["Start", "Settings", "Statistics", "Exit"]
                    ));
            if (menu == "Start")
            {
                words = await ProcessRandomWords();
                StartGame(30000, words);
            }
            else if (menu == "Settings")
            {
                var gamemodeMenu = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("TYP" + $"[red]{0}[/]")
                    .HighlightStyle("red")
                    .AddChoices(["30s", "60s", "120s"])
                    );
                words = await ProcessRandomWords();
                StartGame(Convert.ToInt32(gamemodeMenu.TrimEnd('s')) * 1000, words);
            }
            else if (menu == "Statistics")
            {
                Tuple<List<LetterTypo>, List<WordTypo>, double[]> statistics = LoadStatisticsFromFile();
                await DrawStatistics(statistics, "historic");
            }
            else if (menu == "Exit")
            {
                run = false;
                Environment.Exit(0);
            }
        }
    }
    static void StartGame(int gameTime, List<string> words)
    {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Thread functionThread = new Thread(() => GameLoop(words, cancellationTokenSource.Token, gameTime / 1000));
        functionThread.Start();
        Thread.Sleep(gameTime);
        cancellationTokenSource.Cancel();
        functionThread.Join();
    }
    static async Task<List<string>> ProcessRandomWords(string lang = "&lang=en")
    {
        try
        {
            // Make the API request
            string responseBody = await FetchRandomWords(lang);

            // Parse the JSON response into a list of strings
            List<string>? randomWords = JsonSerializer.Deserialize<List<string>>(responseBody);

            // Create a new list with spaces added between items
            List<string> wordsWithSpaces = new List<string>();
            for (int i = 0; i < randomWords.Count; i++)
            {
                wordsWithSpaces.Add(randomWords[i]);
                if (i < randomWords.Count - 1) // Avoid adding a space after the last item
                {
                    wordsWithSpaces.Add(" ");
                }
            }
            return wordsWithSpaces;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return new List<string>();
        }
    }

    static async Task<string> FetchRandomWords(string lang)
    {
        // Create an HttpClient instance
        using var httpClient = new HttpClient();

        httpClient.BaseAddress = new Uri("https://random-word-api.herokuapp.com/");

        HttpResponseMessage response = await httpClient.GetAsync("word?number=30" + lang);

        // Check if the response is successful
        if (response.IsSuccessStatusCode)
        {
            // Read the content of the response
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            throw new Exception($"Failed to fetch data. Status code: {response.StatusCode}");
        }
    }

    static async Task DrawStatistics(Tuple<List<LetterTypo>, List<WordTypo>, double[]> statistics, string identifier)
    {
        AnsiConsole.Clear();
        Console.CursorVisible = false;
        Spectre.Console.Color[] colorPalette = new Color[]
        {
            new Color(3, 166, 150),   // #03A696
            new Color(242, 146, 29),  // #F2921D
            new Color(217, 208, 197), // #D9D0C5
            new Color(166, 149, 134), // #A69586
            new Color(242, 100, 48)   // #F26430
        };

        var table = new Table()
            .AddColumn(new TableColumn("[#A69586]Wrong Letters[/]"))
            .AddColumn(new TableColumn("[#A69586]Finger failures[/]"));

        int i = 0;
        var barChartWrongLetters = new BarChart()
            .Width(60)
            .CenterLabel();
        foreach (LetterTypo letterTypo in statistics.Item1)
        {
            barChartWrongLetters.AddItem(letterTypo.WrongInput, letterTypo.FailureCounter, colorPalette[i]);
            if (i < colorPalette.Length - 1)
            {
                i++;
            }
            else
            {
                i = 0;
            }
        }
        var barChartFingers = new BarChart()
            .Width(60)
            .CenterLabel();

        Dictionary<string, int> fingerFailureCount = new Dictionary<string, int>();

        foreach (LetterTypo letterTypo in statistics.Item1)
        {
            if (fingerFailureCount.ContainsKey(letterTypo.WrongFinger))
            {
                fingerFailureCount[letterTypo.WrongFinger] += letterTypo.FailureCounter;
            }
            else
            {
                fingerFailureCount[letterTypo.WrongFinger] = letterTypo.FailureCounter;
            }
        }
        i = 0;
        foreach (var item in fingerFailureCount)
        {
            barChartFingers.AddItem(item.Key, item.Value, colorPalette[i]);
            if (i < colorPalette.Length - 1)
            {
                i++;
            }
            else
            {
                i = 0;
            }
        }

        table.AddRow(barChartWrongLetters, barChartFingers);
        AnsiConsole.Write(table);

        var tableWrongWords = new Table()
            .Title("[#A69586]Most failed words[/]")
            .Expand();
        var sortedWordTypo = statistics.Item2.OrderByDescending(w => w.FailureCounter).ToList();
        foreach (WordTypo wordTypo in sortedWordTypo.Take(5))
        {

            tableWrongWords.AddColumn($"{wordTypo.WrongInput}: {wordTypo.FailureCounter}");
        }
        AnsiConsole.Write(tableWrongWords);
        if (identifier == "present")
        {
            var tableCpm = new Table()
            .Title("[#A69586]CPM[/]")
            .Expand();
            foreach (double cpm in statistics.Item3)
            {
                tableCpm.AddColumn(new TableColumn($"Cpm: {cpm}"));
            }
            AnsiConsole.Write(tableCpm);
        }
        var tableBack = new Table()
            .Expand()
            .AddColumn(new TableColumn("[#A69586]Press Tab to go Back[/]").Centered());
        AnsiConsole.Write(tableBack);
        while (Convert.ToString(Console.ReadKey().Key) != "Tab") { }
    }

    static async Task GameLoop(List<string> words, CancellationToken cancellationToken, int timespan)
    {
        Console.CursorVisible = false;
        int wordIndex = 0;
        int charIndex = 0;
        bool hasError = false;
        List<int> wrongCharIndices = new List<int>();
        List<char> wrongChars = new List<char>();
        List<LetterTypo> letterTypoList = new List<LetterTypo>(); // assuming LetterTypo is defined somewhere
        List<WordTypo> wordTypoList = new List<WordTypo>(); // assuming WordTypo is defined somewhere
        int correctLetter = 0, wrongLetter = 0;
        double[] cpm = new double[1], wpm = new double[1];

        var fullText = string.Concat(words);
        var panel = new Panel(new Markup($"[grey50]{fullText}[/]"))
        {
            Header = new PanelHeader("TYP" + $"[red]{0}[/]"),
            Border = BoxBorder.Square,
            Padding = new Padding(2, 2, 2, 2),
        };
        AnsiConsole.Write(panel);

        var tableCounter = new Table()
            .AddColumn(new TableColumn("").Centered())
            .HideHeaders()
            .AddRow($"{timespan}");
        // Start the live display
        await AnsiConsole.Live(tableCounter)
            .StartAsync(async ctx =>
            {
                // Start the countdown in the live context
                var countdownTask = CountdownAsync(timespan, tableCounter, ctx, cancellationToken);
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        int currentWordStartIndex = fullText.IndexOf(words[wordIndex]);
                        int currentWordEndIndex = currentWordStartIndex + words[wordIndex].Length;

                        var key = Console.ReadKey(true); // true to not display the key
                        if (key.Key == ConsoleKey.Backspace)
                        {
                            if (charIndex > 0)
                            {
                                charIndex--;
                                if (wrongCharIndices.Contains(charIndex))
                                {
                                    wrongCharIndices.RemoveAt(wrongCharIndices.Count - 1);
                                    wrongChars.RemoveAt(wrongChars.Count - 1);
                                    if (wrongCharIndices.Count == 0)
                                    {
                                        hasError = false;
                                    }
                                }
                                PrintPanel(fullText, charIndex, "underline grey50", wrongCharIndices, wrongChars);
                                continue; // Skip to the next iteration of the loop
                            }
                        }

                        if (!hasError && key.KeyChar == fullText[charIndex] && charIndex != fullText.Length)
                        {
                            PrintPanel(fullText, charIndex, "underline white", wrongCharIndices, wrongChars);
                            charIndex++;
                            correctLetter++;
                        }
                        else
                        {
                            hasError = true;
                            if (!wrongCharIndices.Contains(charIndex))
                            {
                                wrongCharIndices.Add(charIndex);
                                wrongChars.Add(key.KeyChar);
                            }
                            AddTypoToList(letterTypoList, 1, Convert.ToString(key.KeyChar), words[wordIndex]);
                            if (words[wordIndex] != " ")
                            {
                                AddTypoToList(wordTypoList, 1, Convert.ToString(key.KeyChar), words[wordIndex]);
                            }
                            wrongLetter++;
                            PrintPanel(fullText, charIndex, "underline red", wrongCharIndices, wrongChars);
                            if (charIndex < fullText.Length)
                            {
                                charIndex++;
                            }
                        }

                        // Checking if the current word has been completed
                        if (charIndex >= currentWordEndIndex && wordIndex < words.Count - 1)
                        {
                            wordIndex++;
                        }
                    }
                }
                finally
                {
                    // Stop the countdown if the loop ends
                    cpm[0] = correctLetter / (timespan / 60.0);
                    wpm[0] = Math.Floor((double)correctLetter / 5 / (timespan / 60.0));
                    timespan = 0;
                    AnsiConsole.Clear();
                    tableCounter.NoBorder();
                    tableCounter.UpdateCell(0, 0, $"");
                }


                await DrawStatistics(Tuple.Create(letterTypoList, wordTypoList, cpm), "present");

                SaveStatisticsToFile(letterTypoList, wordTypoList, cpm[0], wpm[0], correctLetter, wrongLetter);

            });
    }
    static async Task CountdownAsync(int remainingTime, Table table, LiveDisplayContext ctx, CancellationToken cancellationToken)
    {
        while (remainingTime > 0 && !cancellationToken.IsCancellationRequested)
        {
            table.UpdateCell(0, 0, $"{remainingTime}");

            // Refresh the live display context
            ctx.Refresh();


            // Delay for one second
            await Task.Delay(1000, cancellationToken);

            // Decrement the remaining time
            remainingTime--;
        }
    }

    static void PrintPanel(string textToDisplay, int charIndex, string textStyling, List<int> wrongCharIndices, List<char> wrongChars)
    {
        var formattedText = new List<string>();

        for (int i = 0; i < textToDisplay.Length; i++)
        {
            if (wrongCharIndices.Contains(i))
            {
                int wrongCharIndex = wrongCharIndices.IndexOf(i);
                char wrongCharacter = wrongChars[wrongCharIndex];
                if (i == charIndex)
                {
                    formattedText.Add($"[underline red]{wrongCharacter}[/]");
                }
                else
                {
                    formattedText.Add($"[red]{wrongCharacter}[/]");
                }
            }
            else if (i < charIndex)
            {
                formattedText.Add($"[white]{textToDisplay[i]}[/]");
            }
            else if (i == charIndex)
            {
                formattedText.Add($"[{textStyling}]{textToDisplay[i]}[/]");
            }
            else
            {
                formattedText.Add($"[grey50]{textToDisplay[i]}[/]");
            }
        }

        var panel = new Panel(new Markup(string.Concat(formattedText)))
        {
            Header = new PanelHeader("TYP" + $"[red]{0}[/]"),
            Border = BoxBorder.Square,
            Padding = new Padding(2, 2, 2, 2),
        };

        AnsiConsole.Clear();
        AnsiConsole.Write(panel);
    }

    // Load statistics from file and return them as a tuple
    // The tuple contains:
    // - List of LetterTypos
    // - List of WordTypos
    // - Array of average statistics (cpm and wpm)
    static Tuple<List<LetterTypo>, List<WordTypo>, double[]> LoadStatisticsFromFile()
    {
        // Path to the statistics file
        string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savegames", "statistics.txt");

        // Lists to store LetterTypos and WordTypos
        List<LetterTypo> letterTypoList = [];
        List<WordTypo> wordTypoList = [];

        // Array to store average statistics (cpm and wpm)
        double[] avgStats = new double[2];

        // Counter to keep track of the number of statistics items
        int itemCnt = 0;
        // Throwh exception when file doesn't exist and create file
        try
        {
            // Read statistics from the file
            using StreamReader reader = new(fileName);
            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                string[] words = line.Split(";");
                for (int i = 0; i < words.Length - 1; i++)
                {
                    switch (words[i])
                    {
                        // If the line contains a LetterTypo
                        case "Letter":
                            AddTypoToList(letterTypoList, Convert.ToInt32(words[i + 3]), words[i + 1], "");
                            break;
                        // If the line contains a WordTypo
                        case "Word":
                            AddTypoToList(wordTypoList, Convert.ToInt32(words[i + 3]), "", words[i + 1]);
                            break;
                        // If the line contains average statistics
                        case "cpm":
                            itemCnt++;
                            avgStats[0] += Convert.ToInt32(words[i + 1]);
                            avgStats[1] += Convert.ToInt32(words[i + 3]);
                            break;
                    }
                }
            }
        }
        catch (Exception)
        {
            using FileStream fs = File.Create(fileName);
        }

        // Calculate the average statistics
        avgStats[0] /= itemCnt;
        avgStats[1] /= itemCnt;

        // Return the loaded statistics as a tuple
        return Tuple.Create(letterTypoList, wordTypoList, avgStats);
    }

    // Save statistics to a file
    // Parameters:
    // - letterTypoList: List of LetterTypos
    // - wordTypoList: List of WordTypos
    // - cpm: Characters per minute
    // - wpm: Words per minute
    // - correctLetter: Number of correct characters
    // - wrongLetter: Number of wrong characters
    static void SaveStatisticsToFile(List<LetterTypo> letterTypoList, List<WordTypo> wordTypoList, double cpm, double wpm, int correctLetter, int wrongLetter)
    {
        // Path to the statistics file
        string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savegames", "statistics.txt");

        // Append statistics to the file
        using (StreamWriter writer = new(fileName, append: true))
        {
            // Write current date
            writer.WriteLine("Played;" + DateTime.Now);

            // Write letter typos
            foreach (LetterTypo w in letterTypoList)
            {
                writer.WriteLine(w);
            }

            // Write word typos
            foreach (WordTypo w in wordTypoList)
            {
                writer.WriteLine(w);
            }

            // Write additional statistics
            writer.WriteLine($"cpm;{cpm};wpm;{wpm};correctChars;{correctLetter};wrongChars;{wrongLetter}");
        }
        //Console.WriteLine($"Statistics saved to {fileName}");
    }

    // Add a typo to the list
    // Parameters:
    // - typoList: List of typos
    // - failureCounter: Number of failures
    // - wrongInput: Wrong input (for LetterTypo)
    // - wrongWord: Wrong word (for WordTypo)
    // Generic method that can add LetterTypo or WordTypo objects to the list
    static void AddTypoToList<T>(List<T> typoList, int failureCounter, string wrongInput, string wrongWord) where T : ITypo, new()
    {
        ITypo typo; // Typo object
        ITypo? existingTypo; // Existing typo object in the list

        // Check the type of T
        if (typeof(T) == typeof(LetterTypo))
        {
            // Create a new LetterTypo object and cast it to ITypo
            typo = new LetterTypo(failureCounter, wrongInput);
            // Find existing LetterTypo object with the same wrong input
            existingTypo = typoList.Find(x => x.WrongInput == wrongInput);
        }
        else if (typeof(T) == typeof(WordTypo))
        {
            // Create a new WordTypo object and cast it to ITypo
            typo = new WordTypo(failureCounter, wrongWord);
            // Find existing WordTypo object with the same wrong input
            existingTypo = typoList.Find(x => x.WrongInput == wrongWord);
        }
        else
        {
            // If T is neither LetterTypo nor WordTypo, throw an exception
            throw new ArgumentException("Invalid type specified");
        }

        // If an existing typo is found, increment its failure counter
        if (existingTypo != null)
        {
            existingTypo.FailureCounter++;
        }
        else
        {
            // If no existing typo is found, add the new typo to the list
            typoList.Add((T)typo);
        }
    }
}