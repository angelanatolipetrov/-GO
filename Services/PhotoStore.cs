namespace BestInTown.Services;

// One photo per place, saved as wwwroot/uploads/places/{placeId}.jpg|png|webp.
// No database column needed, so existing databases keep working.
public class PhotoStore(IWebHostEnvironment env, ILogger<PhotoStore> log)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] Exts = { ".jpg", ".png", ".webp" };

    private string Dir => Path.Combine(env.WebRootPath, "uploads", "places");

    public string? Url(int placeId)
    {
        foreach (var ext in Exts)
        {
            var path = Path.Combine(Dir, placeId + ext);
            if (File.Exists(path))
                return $"/uploads/places/{placeId}{ext}?v={File.GetLastWriteTimeUtc(path).Ticks}";
        }
        return null;
    }

    public bool Has(int placeId) => Exts.Any(e => File.Exists(Path.Combine(Dir, placeId + e)));

    // Returns null on success or a Loc key. Keeps an existing photo (first photo wins).
    public async Task<string?> SaveAsync(int placeId, IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > MaxBytes) return "err.photo.size";
        if (Has(placeId)) return "err.photo.exists";

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

        Directory.CreateDirectory(Dir);
        var final = Path.Combine(Dir, placeId + ext);
        var temp = final + ".tmp";
        try
        {
            await using (var output = File.Create(temp))
                await file.CopyToAsync(output);
            File.Move(temp, final, overwrite: false);
        }
        catch (IOException ex)
        {
            log.LogWarning(ex, "Could not save photo for place {PlaceId}", placeId);
            File.Delete(temp);
            return Has(placeId) ? "err.photo.exists" : "err.generic";
        }
        return null;
    }

    public void Delete(int placeId)
    {
        foreach (var ext in Exts) File.Delete(Path.Combine(Dir, placeId + ext));
    }
}
