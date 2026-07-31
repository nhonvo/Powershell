using System.Text.Json;
using AgyTui.Domain.LearnContext;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Persistence.DbContext;

public static class LearnDataPaths
{
    public static string? OverrideBaseDirectory { get; set; }

    public static string BaseDirectory
    {
        get
        {
            if (!string.IsNullOrEmpty(OverrideBaseDirectory)) return OverrideBaseDirectory;
            var envDir = Environment.GetEnvironmentVariable("AGY_TEST_LEARN_DIR");
            if (!string.IsNullOrEmpty(envDir)) return envDir;

            var dataLearn = AppPaths.LearnDir;
            if (Directory.Exists(dataLearn)) return AppPaths.DataDir;

            var pwd = Directory.GetCurrentDirectory();
            var localLearn = System.IO.Path.Combine(pwd, "learn");
            if (Directory.Exists(localLearn)) return pwd;

            var store = Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>();
            return store.GetAccountDirectory(store.GetActiveAccount());
        }
    }

    public static string LearnRoot => System.IO.Path.Combine(BaseDirectory, "learn");

    // Suite Domain Directories
    public static string JapaneseDir => System.IO.Path.Combine(LearnRoot, "japanese");
    public static string EnglishDir => System.IO.Path.Combine(LearnRoot, "english");
    public static string CsharpDir => System.IO.Path.Combine(LearnRoot, "csharp");
    public static string DsaDir => System.IO.Path.Combine(LearnRoot, "dsa");
    public static string CareerDir => System.IO.Path.Combine(LearnRoot, "career");
    public static string CertificationsDir => System.IO.Path.Combine(LearnRoot, "certifications");
    public static string StatsDir => System.IO.Path.Combine(LearnRoot, "stats");
    public static string GrammarDir => System.IO.Path.Combine(LearnRoot, "grammar");

    // Sub-directories
    public static string DecksDir => System.IO.Path.Combine(CertificationsDir, "decks");

    public static string VocabDir => System.IO.Path.Combine(EnglishDir, "vocab");

    public static string JlptDir => JapaneseDir;

    public static string SnippetsDir => System.IO.Path.Combine(CsharpDir, "snippets");

    public static string SheetsDir => System.IO.Path.Combine(LearnRoot, "cheatsheets");

    // Domain Files
    public static string KanaFile => System.IO.Path.Combine(JapaneseDir, "kana.json");

    public static string KanjiFile => System.IO.Path.Combine(JapaneseDir, "kanji.json");

    public static string WordBankFile => System.IO.Path.Combine(EnglishDir, "word_bank.json");

    public static string QuizFile => System.IO.Path.Combine(CsharpDir, "csharp_quiz.json");

    public static string ComplexityFile => System.IO.Path.Combine(DsaDir, "complexity.json");

    public static string ProblemsFile => System.IO.Path.Combine(DsaDir, "problems.json");

    public static string InterviewFile => System.IO.Path.Combine(CareerDir, "interview_questions.json");

    public static string StarFile => System.IO.Path.Combine(CareerDir, "star_answers.json");

    public static string StudyLogFile => System.IO.Path.Combine(StatsDir, "study_log.json");

    public static string ResourcesIndex => System.IO.Path.Combine(BaseDirectory, "resources", "index.json");

    public static void EnsureDirectories()
    {
        Bootstrapper.ServiceProvider.GetRequiredService<IStudyRepository>().EnsureDirectories();
        SeedDefaultData();
    }

    public static void SeedDefaultData()
    {
        // 1. C# Quiz questions
        if (!File.Exists(QuizFile))
        {
            var defaultQuestions = new[]
            {
                new QuizQuestion("cs-1", "C# Basics", 1, "What is the size of an int in C#?", ["2 bytes", "4 bytes", "8 bytes", "Depends on platform"], 1, "In C#, 'int' maps directly to System.Int32, which is always 32-bit (4 bytes) regardless of platform or architecture.", null, ["basics", "types"]),
                new QuizQuestion("cs-2", "C# Basics", 1, "Which keyword is used to declare a constant in C#?", ["const", "readonly", "static", "let"], 0, "The 'const' keyword declares a compile-time constant, whereas 'readonly' declares a run-time constant.", null, ["basics", "keywords"]),
                new QuizQuestion("cs-3", "OOP", 2, "Which access modifier allows access within the same assembly or subclass?", ["private", "protected", "internal", "protected internal"], 3, "The 'protected internal' modifier allows access within the defining assembly, or from derived classes in any assembly.", null, ["oop", "access-modifiers"])
            };
            SaveJson(QuizFile, new QuizFile(defaultQuestions));
        }

        // 2. Flashcard deck
        var defaultDeckFile = System.IO.Path.Combine(DecksDir, "general.json");
        if (!File.Exists(defaultDeckFile))
        {
            var defaultMeta = new DeckMeta("deck-1", "General Developer Deck", "English", "General Dev", "Beginner", ["internal"], DateTime.UtcNow.ToString("o"), 1);
            var defaultCards = new[]
            {
                new FlashCard("card-1", "What is SOLID?", "SOLID is an acronym for five design principles: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion.", "Software design acronym", "S-O-L-I-D principles", "Use SOLID principles to write clean code.", ["oop", "architecture"], 2, NewCardState()),
                new FlashCard("card-2", "Explain Git merge vs. rebase.", "Merge keeps full commit history with merge commits. Rebase rewrites commit history on top of the target branch for a linear project history.", "Git workflow strategy", "Rebase = rewrite history, Merge = preserve history", "Rebasing keeps the commit tree clean.", ["git"], 2, NewCardState())
            };
            SaveJson(defaultDeckFile, new DeckFile(defaultMeta, defaultCards));
        }

        // 3. JLPT N5 words
        var defaultJlptFile = System.IO.Path.Combine(JlptDir, "N5.json");
        var defaultJlptLower = System.IO.Path.Combine(JlptDir, "n5.json");
        if (!File.Exists(defaultJlptFile) && !File.Exists(defaultJlptLower))
        {
            var defaultWords = new[]
            {
                new JlptWord("jlpt-1", "日本語", "にほんご", "nihongo", "Japanese language", "Noun", "N5", "日本語を勉強します。", "I study Japanese.", ["language"], NewCardState()),
                new JlptWord("jlpt-2", "食べる", "たべる", "taberu", "To eat", "Verb", "N5", "リンゴを食べます。", "I eat an apple.", ["verbs"], NewCardState()),
                new JlptWord("jlpt-3", "猫", "ねこ", "neko", "Cat", "Noun", "N5", "可愛い猫がいます。", "There is a cute cat.", ["animals"], NewCardState()),
                new JlptWord("jlpt-4", "本", "ほん", "hon", "Book", "Noun", "N5", "本を読みます。", "I read a book.", ["vocab"], NewCardState()),
                new JlptWord("jlpt-5", "水", "みず", "mizu", "Water", "Noun", "N5", "冷たい水を飲みます。", "I drink cold water.", ["vocab"], NewCardState())
            };
            var jlptObj = new JlptFile("N5", defaultWords);
            SaveJson(defaultJlptFile, jlptObj);
            SaveJson(defaultJlptLower, jlptObj);
        }

        // 4. Kana File (Hiragana & Katakana)
        if (!File.Exists(KanaFile))
        {
            var hiragana = new[]
            {
                new KanaEntry("あ", "a", "a", "hiragana", NewCardState()),
                new KanaEntry("い", "i", "a", "hiragana", NewCardState()),
                new KanaEntry("う", "u", "a", "hiragana", NewCardState()),
                new KanaEntry("え", "e", "a", "hiragana", NewCardState()),
                new KanaEntry("お", "o", "a", "hiragana", NewCardState()),
                new KanaEntry("か", "ka", "k", "hiragana", NewCardState()),
                new KanaEntry("き", "ki", "k", "hiragana", NewCardState()),
                new KanaEntry("く", "ku", "k", "hiragana", NewCardState()),
                new KanaEntry("け", "ke", "k", "hiragana", NewCardState()),
                new KanaEntry("こ", "ko", "k", "hiragana", NewCardState()),
                new KanaEntry("さ", "sa", "s", "hiragana", NewCardState()),
                new KanaEntry("し", "shi", "s", "hiragana", NewCardState()),
                new KanaEntry("す", "su", "s", "hiragana", NewCardState()),
                new KanaEntry("せ", "se", "s", "hiragana", NewCardState()),
                new KanaEntry("そ", "so", "s", "hiragana", NewCardState()),
                new KanaEntry("た", "ta", "t", "hiragana", NewCardState()),
                new KanaEntry("ち", "chi", "t", "hiragana", NewCardState()),
                new KanaEntry("つ", "tsu", "t", "hiragana", NewCardState()),
                new KanaEntry("て", "te", "t", "hiragana", NewCardState()),
                new KanaEntry("と", "to", "t", "hiragana", NewCardState()),
                new KanaEntry("な", "na", "n", "hiragana", NewCardState()),
                new KanaEntry("に", "ni", "n", "hiragana", NewCardState()),
                new KanaEntry("ぬ", "nu", "n", "hiragana", NewCardState()),
                new KanaEntry("ね", "ne", "n", "hiragana", NewCardState()),
                new KanaEntry("の", "no", "n", "hiragana", NewCardState())
            };
            var katakana = new[]
            {
                new KanaEntry("ア", "a", "a", "katakana", NewCardState()),
                new KanaEntry("イ", "i", "a", "katakana", NewCardState()),
                new KanaEntry("ウ", "u", "a", "katakana", NewCardState()),
                new KanaEntry("エ", "e", "a", "katakana", NewCardState()),
                new KanaEntry("オ", "o", "a", "katakana", NewCardState()),
                new KanaEntry("カ", "ka", "k", "katakana", NewCardState()),
                new KanaEntry("キ", "ki", "k", "katakana", NewCardState()),
                new KanaEntry("ク", "ku", "k", "katakana", NewCardState()),
                new KanaEntry("ケ", "ke", "k", "katakana", NewCardState()),
                new KanaEntry("コ", "ko", "k", "katakana", NewCardState())
            };
            SaveJson(KanaFile, new KanaFile(hiragana, katakana));
        }

        // 5. Kanji File
        if (!File.Exists(KanjiFile))
        {
            var defaultKanji = new[]
            {
                new KanjiEntry("日", ["ニチ", "ジツ"], ["ひ", "か"], "Sun / Day", "N5", 4, ["日"], [new ExampleWord("日本", "にほん", "Japan"), new ExampleWord("今日", "きょう", "Today")], "Sun in the sky", ["kanji", "n5"], NewCardState()),
                new KanjiEntry("月", ["ゲツ", "ガツ"], ["つき"], "Moon / Month", "N5", 4, ["月"], [new ExampleWord("月曜日", "げつようび", "Monday"), new ExampleWord("今月", "こんげつ", "This month")], "Crescent moon", ["kanji", "n5"], NewCardState()),
                new KanjiEntry("水", ["スイ"], ["みず"], "Water", "N5", 4, ["水"], [new ExampleWord("水曜日", "すいようび", "Wednesday"), new ExampleWord("飲み水", "のみみず", "Drinking water")], "Flowing stream", ["kanji", "n5"], NewCardState()),
                new KanjiEntry("火", ["カ"], ["ひ"], "Fire", "N5", 4, ["火"], [new ExampleWord("火曜日", "かようび", "Tuesday"), new ExampleWord("花火", "はなび", "Fireworks")], "Flickering flames", ["kanji", "n5"], NewCardState())
            };
            SaveJson(KanjiFile, new KanjiFile(defaultKanji));
        }

        // 6. Vocab Words (Intermediate)
        var defaultVocabFile = System.IO.Path.Combine(VocabDir, "intermediate.json");
        if (!File.Exists(defaultVocabFile))
        {
            var defaultWords = new[]
            {
                new VocabWord("vocab-1", "ubiquitous", "yoo-bik-wi-tuhs", "Adjective", "Existing or being everywhere at the same time; constantly encountered.", "Mobile phones are ubiquitous today.", ["omnipresent", "pervasive"], ["rare", "scarce"], 3, ["adjectives"], NewCardState()),
                new VocabWord("vocab-2", "pragmatic", "prag-mat-ik", "Adjective", "Dealing with things sensibly and realistically in a way that is based on practical rather than theoretical considerations.", "We need to take a pragmatic approach to software development.", ["practical", "realistic"], ["idealistic", "impractical"], 2, ["adjectives"], NewCardState()),
                new VocabWord("vocab-3", "resilient", "ri-zil-yuhnt", "Adjective", "Able to withstand or recover quickly from difficult conditions.", "Our microservices are resilient against network partitioning.", ["robust", "tough"], ["fragile"], 2, ["adjectives"], NewCardState()),
                new VocabWord("vocab-4", "idempotent", "eye-dem-poh-tuhnt", "Adjective", "An operation that produces the same result no matter how many times executed.", "HTTP PUT and DELETE APIs must be idempotent.", ["repeatable", "invariant"], ["stateful"], 4, ["tech"], NewCardState())
            };
            SaveJson(defaultVocabFile, new VocabFile("Intermediate", defaultWords));
        }

        // 7. Word of the Day Bank
        if (!File.Exists(WordBankFile))
        {
            var wordBank = new[]
            {
                new WordEntry(DateTime.Today.ToString("yyyy-MM-dd"), "resilient", "ri-zil-yuhnt", "Adjective", "Able to withstand or recover quickly from difficult conditions.", "Our distributed system is resilient to node failures.", ["architecture", "vocab"]),
                new WordEntry(DateTime.Today.ToString("yyyy-MM-dd"), "idempotent", "eye-dem-poh-tuhnt", "Adjective", "Denoting an operation which can be applied multiple times without changing the result.", "Ensure API retry calls are strictly idempotent.", ["api", "tech"]),
                new WordEntry(DateTime.Today.ToString("yyyy-MM-dd"), "ephemeral", "ih-fem-er-uhl", "Adjective", "Lasting for a very short time.", "Container local storage is ephemeral unless backed by volumes.", ["docker", "devops"]),
                new WordEntry(DateTime.Today.ToString("yyyy-MM-dd"), "clandestine", "klan-des-tin", "Adjective", "Kept secret or done secretively.", "Secrets vault prevents clandestine access to tokens.", ["security"]),
                new WordEntry(DateTime.Today.ToString("yyyy-MM-dd"), "perspicacious", "pur-spih-kay-shuhs", "Adjective", "Having a ready insight into and understanding of things; shrewd.", "The architect gave a perspicacious solution to the bottleneck.", ["vocab"])
            };
            SaveJson(WordBankFile, new WordBankFile(wordBank));
        }

        // 8. Interview Question Bank
        if (!File.Exists(InterviewFile))
        {
            var questions = new[]
            {
                new InterviewQuestion("int-1", "Technical", "System Design", "Hard", "How would you design a distributed Rate Limiter for an API gateway?", "System Design", ["Token Bucket", "Redis Sliding Window"], ["Google", "Amazon", "Uber"], ["system-design", "api"]),
                new InterviewQuestion("int-2", "Technical", "C# / .NET", "Medium", "What is the difference between Task, Thread, and ValueTask in .NET?", "Technical Q&A", ["Task allocates heap object", "ValueTask is a struct avoiding allocation on sync path"], ["Microsoft", "Meta"], ["dotnet", "async"]),
                new InterviewQuestion("int-3", "Technical", "Databases", "Medium", "Explain Clustered vs. Non-Clustered Indexes in SQL databases.", "Technical Q&A", ["Clustered sort physical data rows", "Non-clustered is a separate B-tree pointer index"], ["AWS", "Microsoft"], ["sql", "db"]),
                new InterviewQuestion("int-4", "Behavioral", "Conflict Resolution", "Medium", "Describe a situation where you had a technical disagreement with a teammate.", "STAR Method", ["Focus on objective benchmarks", "Show empathy and compromise"], ["Google", "Apple"], ["star", "behavioral"]),
                new InterviewQuestion("int-5", "Behavioral", "Incident Response", "Hard", "Tell me about a critical production outage you handled.", "STAR Method", ["Triage first", "Blameless post-mortem"], ["Amazon", "Netflix"], ["star", "sre"])
            };
            SaveJson(InterviewFile, new InterviewFile(questions));
        }

        // 9. STAR Answers Sample
        if (!File.Exists(StarFile))
        {
            var stars = new[]
            {
                new StarAnswer("star-1", "int-4", "Disagreement on API Architecture", "Team was split between REST and gRPC for high-speed microservices.", "Reach consensus and unblock sprint deadline.", "Ran benchmark POC proving gRPC reduced latency by 45%, presented metrics neutrally.", "Team adopted gRPC for core services, keeping REST for external clients.", "45% latency reduction, zero sprint delay", DateTime.UtcNow.ToString("o"), DateTime.UtcNow.ToString("o"), ["leadership", "architecture"], 5)
            };
            SaveJson(StarFile, new StarFile(stars));
        }

        // 10. Complexity File
        if (!File.Exists(ComplexityFile))
        {
            var structures = new[]
            {
                new ComplexityEntry("Array", "O(1)", "O(n)", "O(n)", "O(n)", "O(n)", "Random access O(1)", ["basics"]),
                new ComplexityEntry("Hash Table", "N/A", "O(1)", "O(1)", "O(1)", "O(n)", "Average O(1), worst O(n)", ["hash"]),
                new ComplexityEntry("Balanced BST (AVL/Red-Black)", "O(log n)", "O(log n)", "O(log n)", "O(log n)", "O(n)", "Self-balancing tree", ["trees"])
            };
            var algos = new[]
            {
                new AlgoEntry("Quick Sort", "O(n log n)", "O(n log n)", "O(n²)", "O(log n)", "sort", "In-place divide & conquer", ["sorting"]),
                new AlgoEntry("Merge Sort", "O(n log n)", "O(n log n)", "O(n log n)", "O(n)", "sort", "Stable divide & conquer", ["sorting"]),
                new AlgoEntry("Binary Search", "O(1)", "O(log n)", "O(log n)", "O(1)", "search", "Requires sorted input", ["search"])
            };
            SaveJson(ComplexityFile, new ComplexityFile(structures, algos));
        }

        // 11. Coding Problems File
        if (!File.Exists(ProblemsFile))
        {
            var problems = new[]
            {
                new Problem("prob-1", "Two Sum", "LeetCode #1", "https://leetcode.com/problems/two-sum/", "easy", ["Hash Table", "Array"], "solved", "O(n)", "O(n)", "Use HashMap to track complement value", 1, DateTime.UtcNow.ToString("o"), DateTime.UtcNow.ToString("o"), ["arrays"]),
                new Problem("prob-2", "Valid Parentheses", "LeetCode #20", "https://leetcode.com/problems/valid-parentheses/", "easy", ["Stack", "String"], "solved", "O(n)", "O(n)", "Push open brackets onto stack", 1, DateTime.UtcNow.ToString("o"), DateTime.UtcNow.ToString("o"), ["stack"]),
                new Problem("prob-3", "LRU Cache", "LeetCode #146", "https://leetcode.com/problems/lru-cache/", "medium", ["Hash Table", "Doubly Linked List"], "todo", "O(1)", "O(n)", "Combine HashMap with Doubly LinkedList", 0, null, null, ["design"])
            };
            SaveJson(ProblemsFile, new ProblemsFile(problems));
        }

        // 12. Snippets
        var defaultSnipFile = System.IO.Path.Combine(SnippetsDir, "csharp.json");
        if (!File.Exists(defaultSnipFile))
        {
            var snippets = new[]
            {
                new CodeSnippet("snip-1", "Async IAsyncEnumerable Streaming", "Async/Await", "async IAsyncEnumerable<int> FetchDataAsync()\n{\n    for (int i = 0; i < 5; i++)\n    {\n        await Task.Delay(100);\n        yield return i;\n    }\n}", "Yields items asynchronously without allocating a full list in memory.", "High-throughput API streaming", ["csharp", "async"], 2),
                new CodeSnippet("snip-2", "LINQ Chunking (.NET 6+)", "LINQ", "var numbers = Enumerable.Range(1, 100);\nforeach (var chunk in numbers.Chunk(10))\n{\n    Console.WriteLine($\"Batch of {chunk.Length}\");\n}", "Splits collections into fixed-size batches for parallel processing.", "Batch DB writes / background queues", ["csharp", "linq"], 1)
            };
            SaveJson(defaultSnipFile, new SnippetsFile("csharp", snippets));
        }

        // 13. Cheat Sheets
        if (!Directory.Exists(SheetsDir)) Directory.CreateDirectory(SheetsDir);
        var csSheet = System.IO.Path.Combine(SheetsDir, "csharp.txt");
        if (!File.Exists(csSheet))
        {
            File.WriteAllText(csSheet, "=== C# & .NET CHEAT SHEET ===\n\n1. Value vs Reference Types:\n   - Value: int, bool, double, struct, enum (allocated on Stack/Inline)\n   - Reference: class, interface, delegate, record class, string (allocated on Heap)\n\n2. Async/Await Best Practices:\n   - Avoid 'async void' except for event handlers\n   - Use ConfigureAwait(false) in class libraries\n   - Prefer Task.WhenAll over sequential awaits");
        }
        var gitSheet = System.IO.Path.Combine(SheetsDir, "git.txt");
        if (!File.Exists(gitSheet))
        {
            File.WriteAllText(gitSheet, "=== GIT CHEAT SHEET ===\n\n1. Undo Last Commit (keep changes):\n   git reset --soft HEAD~1\n\n2. Interactive Rebase:\n   git rebase -i HEAD~5\n\n3. Stash with Name:\n   git stash save 'my-wip-feature'");
        }
        var dockerSheet = System.IO.Path.Combine(SheetsDir, "docker.txt");
        if (!File.Exists(dockerSheet))
        {
            File.WriteAllText(dockerSheet, "=== DOCKER CHEAT SHEET ===\n\n1. Cleanup unused resources:\n   docker system prune -af --volumes\n\n2. Follow container logs:\n   docker logs -f --tail 100 <container>");
        }
        // 14. Grammar Files (Japanese N5-N2 & English)
        var grammarDir = Path.Combine(LearnRoot, "grammar");
        Directory.CreateDirectory(grammarDir);

        var n5GrammarFile = Path.Combine(grammarDir, "n5.json");
        if (!File.Exists(n5GrammarFile))
        {
            var n5Cards = new[]
            {
                new GrammarCard("g_n5_01", "N5", "～は～です", "A is B (Topic marker は and copula です)", "N (Topic) + は + N/Adj + です", "わたしは がくせいです。", "I am a student.", ["grammar", "n5", "basics"], NewCardState()),
                new GrammarCard("g_n5_02", "N5", "～があります／います", "There is / are (Existence marker)", "Inanimate: があります / Animate: がいます", "あそこに ねこが います。", "There is a cat over there.", ["grammar", "n5"], NewCardState()),
                new GrammarCard("g_n5_03", "N5", "～へ行きます／来ます", "Go / Come to (Direction marker へ)", "Place + へ + 行きます / 来ます", "あした とうきょうへ いきます。", "I will go to Tokyo tomorrow.", ["grammar", "n5"], NewCardState()),
                new GrammarCard("g_n5_04", "N5", "～てください", "Please do (Te-form requests)", "Verb (Te-form) + ください", "ここに なまえを かいてください。", "Please write your name here.", ["grammar", "n5"], NewCardState()),
                new GrammarCard("g_n5_05", "N5", "～てもいいです", "May do / Permitted to do", "Verb (Te-form) + もいいです", "しゃしんを とってもいいです。", "You may take photos.", ["grammar", "n5"], NewCardState())
            };
            SaveJson(n5GrammarFile, new GrammarFile("N5", n5Cards));
        }

        var n4GrammarFile = Path.Combine(grammarDir, "n4.json");
        if (!File.Exists(n4GrammarFile))
        {
            var n4Cards = new[]
            {
                new GrammarCard("g_n4_01", "N4", "～すぎる", "Too much / Excessively", "Verb (Masu-stem) / Adj + すぎる", "この ほんは むずかしすぎます。", "This book is too difficult.", ["grammar", "n4"], NewCardState()),
                new GrammarCard("g_n4_02", "N4", "～ために", "In order to / For the sake of", "Verb (Dictionary form) / Noun + の + ために", "にほんごを べんきょうするために にほんへ いきます。", "I am going to Japan in order to study Japanese.", ["grammar", "n4"], NewCardState()),
                new GrammarCard("g_n4_03", "N4", "～たら", "If / When (Conditional)", "Verb (Ta-form) + ら", "あめが ふったら いきません。", "If it rains, I won't go.", ["grammar", "n4"], NewCardState()),
                new GrammarCard("g_n4_04", "N4", "～ようにする", "Try to / Make an effort to", "Verb (Dictionary / Nai-form) + ようにする", "まいあさ ろくじに おきるようにしています。", "I make it a habit to wake up at 6:00 every morning.", ["grammar", "n4"], NewCardState())
            };
            SaveJson(n4GrammarFile, new GrammarFile("N4", n4Cards));
        }

        var n3GrammarFile = Path.Combine(grammarDir, "n3.json");
        if (!File.Exists(n3GrammarFile))
        {
            var n3Cards = new[]
            {
                new GrammarCard("g_n3_01", "N3", "～に関して", "Regarding / Concerning", "Noun + に関して / に関する + Noun", "この もんだいに関して いけんを のべてください。", "Please express your opinion regarding this problem.", ["grammar", "n3"], NewCardState()),
                new GrammarCard("g_n3_02", "N3", "～のおかげで", "Thanks to / Owing to (Positive outcome)", "Noun + の / Verb (Plain) + のおかげで", "せんせいのおかげで しけんに ごうかくできました。", "Thanks to the teacher, I was able to pass the exam.", ["grammar", "n3"], NewCardState()),
                new GrammarCard("g_n3_03", "N3", "～に違いない", "Must be / No doubt that", "Plain Form / Noun + に違いない", "かれは はんにんに ちがいない。", "He must be the culprit.", ["grammar", "n3"], NewCardState())
            };
            SaveJson(n3GrammarFile, new GrammarFile("N3", n3Cards));
        }

        var enGrammarFile = Path.Combine(grammarDir, "english.json");
        if (!File.Exists(enGrammarFile))
        {
            var enCards = new[]
            {
                new GrammarCard("g_en_01", "English", "Present Perfect vs Past Simple", "Completed action at specific past time vs Unspecified/Ongoing connection", "Past: 'I saw him yesterday' | Perfect: 'I have seen him before'", "I have lived in Vietnam for 3 years.", "Tôi đã sống ở Việt Nam được 3 năm (và vẫn đang sống ở đây).", ["grammar", "english", "tenses"], NewCardState()),
                new GrammarCard("g_en_02", "English", "Second Conditional (Unreal Present)", "Hypothetical or unlikely situations in the present/future", "If + Past Simple, ... would + Verb", "If I had more time, I would learn Rust.", "Nếu tôi có nhiều thời gian hơn, tôi sẽ học Rust.", ["grammar", "english", "conditionals"], NewCardState()),
                new GrammarCard("g_en_03", "English", "Third Conditional (Unreal Past)", "Regrets or hypothetical past outcomes", "If + Past Perfect, ... would have + Past Participle", "If we had tested the code, the outage would not have happened.", "Nếu chúng ta đã test code, sự cố đã không xảy ra.", ["grammar", "english", "conditionals"], NewCardState())
            };
            SaveJson(enGrammarFile, new GrammarFile("English", enCards));
        }
    }

    private static SrState NewCardState() => new(2.5, 0, 0, null, null, "new");

    public static T? LoadJson<T>(string path) where T : class => Bootstrapper.ServiceProvider.GetRequiredService<IStudyRepository>().LoadJson<T>(path);

    public static void SaveJson<T>(string path, T obj) => Bootstrapper.ServiceProvider.GetRequiredService<IStudyRepository>().SaveJson(path, obj);
}
