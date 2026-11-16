using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;

const uint defaultAppId = 4348910;
const ulong defaultItemId = 3838981739;

string[] languages =
[
    "arabic", "bulgarian", "schinese", "tchinese", "czech", "danish", "dutch", "english",
    "finnish", "french", "german", "greek", "hungarian", "indonesian", "italian", "japanese",
    "koreana", "malay", "norwegian", "polish", "portuguese", "brazilian", "romanian", "russian",
    "spanish", "latam", "swedish", "thai", "turkish", "ukrainian", "vietnamese"
];

var descriptionPath = GetOption(args, "--description");
if (descriptionPath is null)
{
    Console.Error.WriteLine("Usage: WorkshopDescriptionSync --description <path> [--app-id <id>] [--item-id <id>] [--submit]");
    return 2;
}

descriptionPath = Path.GetFullPath(descriptionPath);
if (!File.Exists(descriptionPath))
{
    Console.Error.WriteLine($"Description not found: {descriptionPath}");
    return 2;
}

var description = File.ReadAllText(descriptionPath);
if (string.IsNullOrWhiteSpace(description))
{
    Console.Error.WriteLine("Description is empty.");
    return 2;
}

var appId = ParseUInt32(GetOption(args, "--app-id"), defaultAppId, "app id");
var itemId = ParseUInt64(GetOption(args, "--item-id"), defaultItemId, "item id");
if (!args.Contains("--submit", StringComparer.Ordinal))
{
    Console.WriteLine($"dry-run app={appId} item={itemId} description={descriptionPath} languages={languages.Length}");
    Console.WriteLine(string.Join(",", languages));
    return 0;
}

var originalDirectory = Environment.CurrentDirectory;
var sessionDirectory = Path.Combine(Path.GetTempPath(), "ProbablyTranslated-WorkshopDescriptionSync", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sessionDirectory);

try
{
    Environment.CurrentDirectory = sessionDirectory;
    File.WriteAllText("steam_appid.txt", appId.ToString());
    SteamClient.Init(appId, asyncCallbacks: true);

    var failures = new List<string>();
    foreach (var language in languages)
    {
        var result = await new Editor(new PublishedFileId { Value = itemId })
            .ForAppId(appId)
            .InLanguage(language)
            .WithDescription(description)
            .SubmitAsync();

        Console.WriteLine($"{language}: {result.Result}");
        if (!result.Success) failures.Add(language);
    }

    if (failures.Count != 0)
    {
        Console.Error.WriteLine("Failed languages: " + string.Join(", ", failures));
        return 1;
    }

    Console.WriteLine($"description-sync=ok languages={languages.Length}");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
finally
{
    Environment.CurrentDirectory = originalDirectory;
    try { Directory.Delete(sessionDirectory, recursive: true); }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
    // Facepunch's asynchronous callback loop can race SteamClient.Shutdown during process exit.
    // This short-lived tool intentionally lets the operating system release the Steam pipe.
}

static string? GetOption(string[] arguments, string name)
{
    var index = Array.IndexOf(arguments, name);
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}

static uint ParseUInt32(string? value, uint fallback, string label)
{
    if (value is null) return fallback;
    if (uint.TryParse(value, out var parsed) && parsed != 0) return parsed;
    throw new ArgumentException($"Invalid {label}: {value}");
}

static ulong ParseUInt64(string? value, ulong fallback, string label)
{
    if (value is null) return fallback;
    if (ulong.TryParse(value, out var parsed) && parsed != 0) return parsed;
    throw new ArgumentException($"Invalid {label}: {value}");
}
