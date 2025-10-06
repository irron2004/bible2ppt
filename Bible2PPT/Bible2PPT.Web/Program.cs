using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Bible2PPT.Data;
using Bible2PPT.Services.BibleIndexService;
using Bible2PPT.Services.BibleService;
using Bible2PPT.Sources;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDirectory);

var bibleDbPath = Path.Combine(dataDirectory, "bible-v3.db");
var bibleIndexDbPath = Path.Combine(dataDirectory, "bindex-v3.db");

builder.Services.AddBibleIndexService(options => options.UseSqlite($"Data Source={bibleIndexDbPath}"));
builder.Services.AddBibleService(options => options.UseSqlite($"Data Source={bibleDbPath}"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Services.UseBibleIndexService();
app.Services.UseBibleService();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

var offlineSource = BibleSource.AvailableSources.First();
var offlineSourceId = offlineSource.Id;

app.MapGet("/api/status", () => Results.Ok(new { message = "Bible2PPT service is up" }));

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/bibles", async (BibleService bibleService) =>
{
    var bibles = await bibleService.GetBiblesAsync(offlineSource).ConfigureAwait(false);
    return Results.Ok(bibles.Select(b => new { b.Id, b.Name, b.LanguageCode }));
});

app.MapGet("/api/bibles/{bibleId:int}/books", async (int bibleId, BibleService bibleService) =>
{
    var bible = bibleService.FindBible(bibleId);
    if (bible is null)
    {
        return Results.NotFound();
    }

    var books = await bibleService.GetBooksAsync(bible).ConfigureAwait(false);
    return Results.Ok(books.Select(book => new
    {
        book.Id,
        book.Name,
        book.Abbreviation,
        book.Key,
        book.ChapterCount,
    }));
});

app.MapGet("/api/books/{bookId:int}/chapters", async (int bookId, IDbContextFactory<BibleContext> dbFactory, CancellationToken cancellationToken) =>
{
    await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
    var book = await db.Books.AsNoTracking()
        .FirstOrDefaultAsync(b => b.Id == bookId && b.SourceId == offlineSourceId, cancellationToken)
        .ConfigureAwait(false);

    if (book is null)
    {
        return Results.NotFound();
    }

    var chapters = await db.Chapters.AsNoTracking()
        .Where(c => c.BookId == book.Id && c.SourceId == offlineSourceId)
        .OrderBy(c => c.Number)
        .Select(c => new { c.Id, c.Number })
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

    return Results.Ok(new
    {
        book = new { book.Id, book.Name, book.Abbreviation, book.Key, book.ChapterCount },
        chapters,
    });
});

app.MapGet("/api/chapters/{chapterId:int}/verses", async (int chapterId, IDbContextFactory<BibleContext> dbFactory, CancellationToken cancellationToken) =>
{
    await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
    var chapter = await db.Chapters.AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == chapterId && c.SourceId == offlineSourceId, cancellationToken)
        .ConfigureAwait(false);

    if (chapter is null)
    {
        return Results.NotFound();
    }

    var verses = await db.Verses.AsNoTracking()
        .Where(v => v.ChapterId == chapter.Id && v.SourceId == offlineSourceId)
        .OrderBy(v => v.Number)
        .Select(v => new { v.Id, v.Number, v.Text })
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

    return Results.Ok(new
    {
        chapter = new { chapter.Id, chapter.Number, chapter.BookId },
        verses,
    });
});

app.MapGet("/api/passages", async (string reference, VerseQueryParser parser, IDbContextFactory<BibleContext> dbFactory, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(reference))
    {
        return Results.BadRequest(new { message = "Query parameter 'reference' is required." });
    }

    var ranges = parser.ParseVerseQueries(reference).ToList();
    if (!ranges.Any())
    {
        return Results.BadRequest(new { message = "Unable to parse reference." });
    }

    await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
    var bible = await db.Bibles.AsNoTracking()
        .FirstOrDefaultAsync(b => b.SourceId == offlineSourceId, cancellationToken)
        .ConfigureAwait(false);

    if (bible is null)
    {
        return Results.NotFound(new { message = "Offline bible is not available." });
    }

    var payload = new List<object>();

    foreach (var range in ranges)
    {
        var book = await db.Books.AsNoTracking()
            .FirstOrDefaultAsync(b => b.BibleId == bible.Id && b.Key == range.BookKey && b.SourceId == offlineSourceId, cancellationToken)
            .ConfigureAwait(false);

        if (book is null)
        {
            continue;
        }

        var startChapter = range.StartChapterNumber;
        var endChapter = range.EndChapterNumber ?? book.ChapterCount;

        var chapters = await db.Chapters.AsNoTracking()
            .Where(c => c.BookId == book.Id && c.Number >= startChapter && c.Number <= endChapter && c.SourceId == offlineSourceId)
            .OrderBy(c => c.Number)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var chapter in chapters)
        {
            var startVerse = chapter.Number == startChapter ? range.StartVerseNumber : 1;
            var endVerse = chapter.Number == endChapter && range.EndVerseNumber.HasValue
                ? range.EndVerseNumber.Value
                : int.MaxValue;

            var verses = await db.Verses.AsNoTracking()
                .Where(v => v.ChapterId == chapter.Id && v.SourceId == offlineSourceId && v.Number >= startVerse && v.Number <= endVerse)
                .OrderBy(v => v.Number)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var verse in verses)
            {
                payload.Add(new
                {
                    bookId = book.Id,
                    bookName = book.Name,
                    bookAbbreviation = book.Abbreviation,
                    bookKey = book.Key.ToString(),
                    chapter = chapter.Number,
                    verse = verse.Number,
                    text = verse.Text,
                });
            }
        }
    }

    if (!payload.Any())
    {
        return Results.NotFound(new { message = "Reference did not match any verses." });
    }

    return Results.Ok(payload);
});

app.Run();
