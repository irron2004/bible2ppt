using Bible2PPT.Bibles;
using Bible2PPT.Services.BibleService.Offline;

namespace Bible2PPT.Sources;

public abstract class BibleSource
{
    public static BibleSource[] AvailableSources = new BibleSource[]
    {
        OfflineBibleSource.Instance,
        new GodpeopleBible { Id = 1 },
        new GodpiaBible { Id = 2 },
        new GoodtvBible { Id = 3 },
    };

    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public abstract Task<List<Bible>> GetBiblesOnlineAsync();
    public abstract Task<List<Book>> GetBooksOnlineAsync(Bible bible);
    public abstract Task<List<Chapter>> GetChaptersOnlineAsync(Book book);
    public abstract Task<List<Verse>> GetVersesOnlineAsync(Chapter chapter);

    public override string ToString() => Name;
}
