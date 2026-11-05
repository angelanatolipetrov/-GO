namespace BestInTown.Services;

// One photo per place (wwwroot/uploads/places/{id}.jpg|png|webp) and one cover per weekly topic
// (wwwroot/uploads/topics/{id}.*). No database column needed, so existing databases keep working.
public class PhotoStore(IWebHostEnvironment env, ILogger<PhotoStore> log)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] Exts = { ".jpg", ".png", ".webp" };

    private const string Places = "places", Topics = "topics";
    private string Dir(string kind) => Path.Combine(env.WebRootPath, "uploads", kind);

    public string? Url(int placeId) => UrlOf(Places, placeId);
    public bool Has(int placeId) => HasOf(Places, placeId);
    public Task<string?> SaveAsync(int placeId, IFormFile? file) => SaveOf(Places, placeId, file, replace: false);
    public void Delete(int placeId) => DeleteOf(Places, placeId);

    public string? TopicUrl(int topicId) => UrlOf(Topics, topicId);
    public Task<string?> SaveTopicAsync(int topicId, IFormFile? file) => SaveOf(Topics, topicId, file, replace: true);
    public void DeleteTopic(int topicId) => DeleteOf(Topics, topicId);

    private string? UrlOf(string kind, int id)
    {
        foreach (var ext in Exts)
        {
            var path = Path.Combine(Dir(kind), id + ext);
            if (File.Exists(path))
                return $"/uploads/{kind}/{id}{ext}?v={File.GetLastWriteTimeUtc(path).Ticks}";
        }
        return null;
    }

    private bool HasOf(string kind, int id) => Exts.Any(e => File.Exists(Path.Combine(Dir(kind), id + e)));

    // Returns null on success or a Loc key. replace=false keeps an existing photo (first photo wins).
    private async Task<string?> SaveOf(string kind, int id, IFormFile? file, bool replace)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > MaxBytes) return "err.photo.size";
        if (!replace && HasOf(kind, id)) return "err.photo.exists";

        // Trust the file's first bytes, not its name or the browser's content type.
        var head = new byte[12];
        await using (var s = file.OpenReadStream())
        {
            var read = 0;
            while (read < head.Length)
            {
                var n = await s.ReadAsync(head.AsMemory(read));
                if (n == 0) break;
                read += n;
            }
        }
        var ext = head switch
        {
            [0xFF, 0xD8, 0xFF, ..] => ".jpg",
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..] => ".png",
            [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50] => ".webp",
            _ => null
        };
        if (ext == null) return "err.photo.type";

        Directory.CreateDirectory(Dir(kind));
        if (replace) DeleteOf(kind, id);
        var final = Path.Combine(Dir(kind), id + ext);
        var temp = final + ".tmp";
        try
        {
            await using (var output = File.Create(temp))
                await file.CopyToAsync(output);
            File.Move(temp, final, overwrite: false);
        }
        catch (IOException ex)
        {
            log.LogWarning(ex, "Could not save {Kind} photo {Id}", kind, id);
            File.Delete(temp);
            return HasOf(kind, id) ? "err.photo.exists" : "err.generic";
        }
        return null;
    }

    private void DeleteOf(string kind, int id)
    {
        foreach (var ext in Exts) File.Delete(Path.Combine(Dir(kind), id + ext));
    }
}
