using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Bible2PPT.Bibles;
using Bible2PPT.Services.BibleIndexService;

namespace Bible2PPT.Services.BibleService.Offline;

internal static class OfflineBibleDataProvider
{
    public const string BibleName = "개역개정 (오프라인)";
    public const string BibleLanguageCode = "ko";
    public const string BibleOnlineId = "offline:ko:gaejong";

    private static readonly Lazy<OfflineBibleData> Data = new(LoadBibleData);

    private static readonly Regex VerseLineRegex = new(
        @"^(?<abbr>[^\d\s]+?)(?<chapter>\d+):(?<verse>\d+)\s*(?<text>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly BookMetadata[] BookOrder =
    {
        new(BookKey.Genesis, "창세기", "창"),
        new(BookKey.Exodus, "출애굽기", "출"),
        new(BookKey.Leviticus, "레위기", "레"),
        new(BookKey.Numbers, "민수기", "민"),
        new(BookKey.Deuteronomy, "신명기", "신"),
        new(BookKey.Joshua, "여호수아", "수"),
        new(BookKey.Judges, "사사기", "삿"),
        new(BookKey.Ruth, "룻기", "룻"),
        new(BookKey.ISamuel, "사무엘상", "삼상"),
        new(BookKey.IISamuel, "사무엘하", "삼하"),
        new(BookKey.IKings, "열왕기상", "왕상"),
        new(BookKey.IIKings, "열왕기하", "왕하"),
        new(BookKey.IChronicles, "역대상", "대상"),
        new(BookKey.IIChronicles, "역대하", "대하"),
        new(BookKey.Ezra, "에스라", "라"),
        new(BookKey.Nehemiah, "느헤미야", "느"),
        new(BookKey.Esther, "에스더", "더"),
        new(BookKey.Job, "욥기", "욥"),
        new(BookKey.Psalms, "시편", "시"),
        new(BookKey.Proverbs, "잠언", "잠"),
        new(BookKey.Ecclesiastes, "전도서", "전"),
        new(BookKey.SongOfSolomon, "아가", "아"),
        new(BookKey.Isaiah, "이사야", "사"),
        new(BookKey.Jeremiah, "예레미야", "렘"),
        new(BookKey.Lamentations, "예레미야애가", "애"),
        new(BookKey.Ezekiel, "에스겔", "겔"),
        new(BookKey.Daniel, "다니엘", "단"),
        new(BookKey.Hosea, "호세아", "호"),
        new(BookKey.Joel, "요엘", "욜"),
        new(BookKey.Amos, "아모스", "암"),
        new(BookKey.Obadiah, "오바댜", "옵"),
        new(BookKey.Jonah, "요나", "욘"),
        new(BookKey.Micah, "미가", "미"),
        new(BookKey.Nahum, "나홈", "나"),
        new(BookKey.Habakkuk, "하박국", "합"),
        new(BookKey.Zephaniah, "스바냐", "습"),
        new(BookKey.Haggai, "학개", "학"),
        new(BookKey.Zechariah, "스가랴", "슥"),
        new(BookKey.Malachi, "말라기", "말"),
        new(BookKey.Matthew, "마태복음", "마"),
        new(BookKey.Mark, "마가복음", "막"),
        new(BookKey.Luke, "누가복음", "눅"),
        new(BookKey.John, "요한복음", "요"),
        new(BookKey.Acts, "사도행전", "행"),
        new(BookKey.Romans, "로마서", "롬"),
        new(BookKey.ICorinthians, "고린도전서", "고전"),
        new(BookKey.IICorinthians, "고린도후서", "고후"),
        new(BookKey.Galatians, "갈라디아서", "갈"),
        new(BookKey.Ephesians, "에베소서", "엡"),
        new(BookKey.Philippians, "빌립보서", "빌"),
        new(BookKey.Colossians, "골로새서", "골"),
        new(BookKey.IThessalonians, "데살로니가전서", "살전"),
        new(BookKey.IIThessalonians, "데살로니가후서", "살후"),
        new(BookKey.ITimothy, "디모데전서", "딤전"),
        new(BookKey.IITimothy, "디모데후서", "딤후"),
        new(BookKey.Titus, "디도서", "딛"),
        new(BookKey.Philemon, "빌레몬서", "몬"),
        new(BookKey.Hebrews, "히브리서", "히"),
        new(BookKey.James, "야고보서", "약"),
        new(BookKey.IPeter, "베드로전서", "벧전"),
        new(BookKey.IIPeter, "베드로후서", "벧후"),
        new(BookKey.IJohn, "요한일서", "요일"),
        new(BookKey.IIJohn, "요한이서", "요이"),
        new(BookKey.IIIJohn, "요한삼서", "요삼"),
        new(BookKey.Jude, "유다서", "유"),
        new(BookKey.Revelation, "요한계시록", "계"),
    };

    private static readonly Dictionary<string, BookMetadata> AbbreviationMap = BookOrder.ToDictionary(x => x.Abbreviation);

    public static OfflineBibleData GetBible() => Data.Value;

    private static OfflineBibleData LoadBibleData()
    {
        var path = ResolveBibleTextPath();
        using var reader = new StreamReader(path, Encoding.UTF8, true);

        var books = new Dictionary<BookKey, BookBuilder>();
        VerseBuilder? currentVerse = null;
        BookBuilder? currentBook = null;
        ChapterBuilder? currentChapter = null;

        while (reader.ReadLine() is { } rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var line = rawLine.Trim();
            var match = VerseLineRegex.Match(line);
            if (match.Success)
            {
                var abbr = match.Groups["abbr"].Value;
                if (!AbbreviationMap.TryGetValue(abbr, out var metadata))
                {
                    throw new InvalidOperationException($"Unknown book abbreviation '{abbr}' in bible.txt.");
                }

                var chapterNumber = int.Parse(match.Groups["chapter"].Value, CultureInfo.InvariantCulture);
                var verseNumber = int.Parse(match.Groups["verse"].Value, CultureInfo.InvariantCulture);
                var verseText = match.Groups["text"].Value.Trim();

                if (!books.TryGetValue(metadata.Key, out currentBook))
                {
                    currentBook = new BookBuilder(metadata);
                    books.Add(metadata.Key, currentBook);
                }

                currentChapter = currentBook.GetOrAddChapter(chapterNumber);
                currentVerse = currentChapter.AddVerse(verseNumber, verseText);
                continue;
            }

            if (currentVerse is not null && line.Length > 0)
            {
                currentVerse.Append(line);
            }
        }

        var orderedBooks = books.Values
            .OrderBy(builder => builder.Metadata.Order)
            .Select(builder => builder.ToBookData())
            .ToList();

        return new OfflineBibleData(BibleName, BibleLanguageCode, BibleOnlineId, orderedBooks);
    }

    private static string ResolveBibleTextPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDirectory, "bible.txt");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        var assemblyDirectory = Path.GetDirectoryName(typeof(OfflineBibleDataProvider).Assembly.Location);
        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            candidate = Path.Combine(assemblyDirectory, "bible.txt");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Unable to locate bible.txt. Ensure the file is copied to the output directory.");
    }

    private sealed record BookMetadata(BookKey Key, string Name, string Abbreviation)
    {
        private static int s_counter;
        public int Order { get; } = Interlocked.Increment(ref s_counter);
    }

    private sealed class BookBuilder
    {
        private readonly SortedDictionary<int, ChapterBuilder> _chapters = new();

        public BookBuilder(BookMetadata metadata)
        {
            Metadata = metadata;
        }

        public BookMetadata Metadata { get; }

        public ChapterBuilder GetOrAddChapter(int number)
        {
            if (_chapters.TryGetValue(number, out var chapter))
            {
                return chapter;
            }

            chapter = new ChapterBuilder(number, Metadata);
            _chapters.Add(number, chapter);
            return chapter;
        }

        public OfflineBookData ToBookData()
        {
            var chapters = _chapters.Values
                .OrderBy(chapter => chapter.Number)
                .Select(chapter => chapter.ToChapterData())
                .ToList();

            return new OfflineBookData(
                Metadata.Key,
                Metadata.Name,
                Metadata.Abbreviation,
                $"{OfflineBibleDataProvider.BibleOnlineId}:{Metadata.Key}",
                chapters);
        }
    }

    private sealed class ChapterBuilder
    {
        private readonly SortedDictionary<int, VerseBuilder> _verses = new();
        private readonly BookMetadata _metadata;

        public ChapterBuilder(int number, BookMetadata metadata)
        {
            Number = number;
            _metadata = metadata;
        }

        public int Number { get; }

        public VerseBuilder AddVerse(int number, string text)
        {
            var verse = new VerseBuilder(number, text);
            _verses[number] = verse;
            return verse;
        }

        public OfflineChapterData ToChapterData()
        {
            var verses = _verses.Values
                .OrderBy(verse => verse.Number)
                .Select(verse => verse.ToVerseData())
                .ToList();

            return new OfflineChapterData(
                Number,
                $"{OfflineBibleDataProvider.BibleOnlineId}:{_metadata.Key}:{Number}",
                verses);
        }
    }

    private sealed class VerseBuilder
    {
        private readonly StringBuilder _text;

        public VerseBuilder(int number, string text)
        {
            Number = number;
            _text = new StringBuilder(text, text.Length + 32);
        }

        public int Number { get; }

        public void Append(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.Length == 0)
            {
                return;
            }

            if (_text.Length > 0)
            {
                _text.Append(' ');
            }

            _text.Append(trimmed);
        }

        public OfflineVerseData ToVerseData() => new(Number, _text.ToString());
    }
}

internal sealed record OfflineBibleData(string Name, string LanguageCode, string OnlineId, IReadOnlyList<OfflineBookData> Books);

internal sealed record OfflineBookData(BookKey Key, string Name, string Abbreviation, string OnlineId, IReadOnlyList<OfflineChapterData> Chapters);

internal sealed record OfflineChapterData(int Number, string OnlineId, IReadOnlyList<OfflineVerseData> Verses);

internal sealed record OfflineVerseData(int Number, string Text);
