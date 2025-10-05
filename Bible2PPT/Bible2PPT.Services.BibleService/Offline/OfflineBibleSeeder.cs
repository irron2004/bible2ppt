using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bible2PPT.Bibles;
using Bible2PPT.Data;
using Microsoft.EntityFrameworkCore;

namespace Bible2PPT.Services.BibleService.Offline;

internal static class OfflineBibleSeeder
{
    public static async Task SeedAsync(IDbContextFactory<BibleContext> dbFactory, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        if (await db.Bibles.AsNoTracking().AnyAsync(b => b.SourceId == OfflineBibleSource.SourceId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var data = OfflineBibleDataProvider.GetBible();

        var bible = new Bible
        {
            SourceId = OfflineBibleSource.SourceId,
            Source = OfflineBibleSource.Instance,
            Name = data.Name,
            LanguageCode = data.LanguageCode,
            OnlineId = data.OnlineId,
        };

        var books = new List<Book>();
        var chapters = new List<Chapter>();
        var verses = new List<Verse>();

        foreach (var bookData in data.Books)
        {
            var book = new Book
            {
                SourceId = OfflineBibleSource.SourceId,
                Bible = bible,
                Name = bookData.Name,
                Abbreviation = bookData.Abbreviation,
                OnlineId = bookData.OnlineId,
                Key = bookData.Key,
                ChapterCount = bookData.Chapters.Count,
            };
            books.Add(book);

            foreach (var chapterData in bookData.Chapters)
            {
                var chapter = new Chapter
                {
                    SourceId = OfflineBibleSource.SourceId,
                    Book = book,
                    Number = chapterData.Number,
                    OnlineId = chapterData.OnlineId,
                };
                chapters.Add(chapter);

                foreach (var verseData in chapterData.Verses)
                {
                    verses.Add(new Verse
                    {
                        SourceId = OfflineBibleSource.SourceId,
                        Chapter = chapter,
                        Number = verseData.Number,
                        Text = verseData.Text,
                    });
                }
            }
        }

        db.Bibles.Add(bible);
        db.Books.AddRange(books);
        db.Chapters.AddRange(chapters);
        db.Verses.AddRange(verses);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
