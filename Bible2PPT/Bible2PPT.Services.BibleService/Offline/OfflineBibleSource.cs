using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bible2PPT.Bibles;
using Bible2PPT.Sources;

namespace Bible2PPT.Services.BibleService.Offline;

internal sealed class OfflineBibleSource : BibleSource
{
    public const int SourceId = 0;

    private OfflineBibleSource()
    {
        Id = SourceId;
        Name = "내장 성경 (bible.txt)";
    }

    public static OfflineBibleSource Instance { get; } = new();

    public override Task<List<Bible>> GetBiblesOnlineAsync()
    {
        var data = OfflineBibleDataProvider.GetBible();
        var bible = new Bible
        {
            SourceId = SourceId,
            Source = this,
            Name = data.Name,
            LanguageCode = data.LanguageCode,
            OnlineId = data.OnlineId,
        };

        return Task.FromResult(new List<Bible> { bible });
    }

    public override Task<List<Book>> GetBooksOnlineAsync(Bible bible)
    {
        var data = OfflineBibleDataProvider.GetBible();
        var books = data.Books
            .Select(bookData => new Book
            {
                SourceId = SourceId,
                Source = this,
                Bible = bible,
                BibleId = bible.Id,
                Name = bookData.Name,
                Abbreviation = bookData.Abbreviation,
                OnlineId = bookData.OnlineId,
                Key = bookData.Key,
                ChapterCount = bookData.Chapters.Count,
            })
            .ToList();

        return Task.FromResult(books);
    }

    public override Task<List<Chapter>> GetChaptersOnlineAsync(Book book)
    {
        var bookData = GetBookData(book);
        var chapters = bookData.Chapters
            .Select(chapterData => new Chapter
            {
                SourceId = SourceId,
                Source = this,
                Book = book,
                BookId = book.Id,
                Number = chapterData.Number,
                OnlineId = chapterData.OnlineId,
            })
            .ToList();

        return Task.FromResult(chapters);
    }

    public override Task<List<Verse>> GetVersesOnlineAsync(Chapter chapter)
    {
        var chapterData = GetChapterData(chapter);
        var verses = chapterData.Verses
            .Select(verseData => new Verse
            {
                SourceId = SourceId,
                Source = this,
                Chapter = chapter,
                ChapterId = chapter.Id,
                Number = verseData.Number,
                Text = verseData.Text,
            })
            .ToList();

        return Task.FromResult(verses);
    }

    private static OfflineBookData GetBookData(Book book)
    {
        var books = OfflineBibleDataProvider.GetBible().Books;
        return books.First(data => data.Key == book.Key);
    }

    private static OfflineChapterData GetChapterData(Chapter chapter)
    {
        var bookData = GetBookData(chapter.Book);
        return bookData.Chapters.First(data => data.Number == chapter.Number);
    }
}
