namespace ReleaseSymbols;

using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;


static class Program
{
    static readonly Guid SourceLink = new("cc110556-a091-4d38-9fec-25ab9a351a6a");
    static readonly Guid EmbeddedSource = new("0e8a571b-6926-466e-b4ad-8ab04611f5fe");
    static readonly Guid Sha256 = new("8829d00f-11b8-4213-878b-770e8597ac16");
    static readonly Guid Sha1 = new("ff1816ec-aa5e-4d10-87f7-6f4963833460");

    static int Main(string[] args)
    {
        string? root = null, commit = null;
        var assemblies = new List<string>();
        for (var i = 0; i < args.Length; i += 2)
        {
            if (i + 1 == args.Length)
                throw new ArgumentException("Every option needs a value");
            switch (args[i])
            {
                case "--root": root = Path.GetFullPath(args[i + 1]); break;
                case "--commit": commit = args[i + 1]; break;
                case "--assembly": assemblies.Add(args[i + 1]); break;
                default: throw new ArgumentException($"Unknown option {args[i]}");
            }
        }
        if (root == null || string.IsNullOrWhiteSpace(commit) || assemblies.Count == 0)
            throw new ArgumentException("Use --root, --commit and one or more --assembly options");

        var results = assemblies.Select(path => Inspect(path, root, commit)).ToArray();
        Console.WriteLine(JsonSerializer.Serialize(results));
        return results.Any(result => result.errors.Count > 0) ? 1 : 0;
    }

    static SymbolResult Inspect(string path, string root, string commit)
    {
        var result = new SymbolResult { path = path };
        try
        {
            var identity = AssemblyName.GetAssemblyName(path);
            result.name = identity.Name ?? "";
            result.token = Convert.ToHexString(identity.GetPublicKeyToken() ?? []).ToLowerInvariant();
            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            var embedded = pe.ReadDebugDirectory().Where(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb).ToArray();
            if (embedded.Length != 1)
                throw new InvalidDataException("Expected one embedded portable PDB");
            using var provider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embedded[0]);
            var reader = provider.GetMetadataReader();
            var links = reader.GetCustomDebugInformation(MetadataTokens.EntityHandle(1))
                .Select(reader.GetCustomDebugInformation).Where(info => reader.GetGuid(info.Kind) == SourceLink).ToArray();
            if (links.Length != 1)
                throw new InvalidDataException("Expected one Source Link document map");
            using var json = JsonDocument.Parse(reader.GetBlobBytes(links[0].Value));
            var maps = json.RootElement.GetProperty("documents").EnumerateObject()
                .ToDictionary(property => property.Name.Replace('\\', '/'), property => property.Value.GetString()!);
            if (maps.Count == 0)
                throw new InvalidDataException("Source Link document map is empty");
            foreach (var handle in reader.Documents)
            {
                result.documents++;
                var document = reader.GetDocument(handle);
                var name = reader.GetString(document.Name).Replace('\\', '/');
                try
                {
                    VerifyDocument(reader, handle, document, name, maps, root, commit, result);
                }
                catch (Exception exception)
                {
                    result.errors.Add($"{name}: {exception.Message}");
                }
            }
            if (result.documents == 0)
                result.errors.Add("PDB contains no source documents");
            if (result.trackedDocuments == 0)
                result.errors.Add("Expected at least one validated checkout source document");
        }
        catch (Exception exception)
        {
            result.errors.Add(exception.Message);
        }
        return result;
    }

    static void VerifyDocument(MetadataReader reader, DocumentHandle handle, Document document, string name,
        Dictionary<string, string> maps, string root, string commit, SymbolResult result)
    {
        var checksum = reader.GetBlobBytes(document.Hash);
        var algorithm = reader.GetGuid(document.HashAlgorithm);
        if (checksum.Length == 0 || (algorithm != Sha256 && algorithm != Sha1))
            throw new InvalidDataException("Missing or unsupported source checksum");
        var embedded = reader.GetCustomDebugInformation(handle).Select(reader.GetCustomDebugInformation)
            .Where(info => reader.GetGuid(info.Kind) == EmbeddedSource).ToArray();
        byte[]? embeddedBytes = null;
        if (embedded.Length > 1)
            throw new InvalidDataException("Duplicate embedded source");
        if (embedded.Length == 1)
        {
            var blob = reader.GetBlobBytes(embedded[0].Value);
            if (blob.Length < 4)
                throw new InvalidDataException("Invalid embedded source header");
            var size = BitConverter.ToInt32(blob, 0);
            if (size < 0 || size > 100_000_000)
                throw new InvalidDataException("Invalid embedded source size");
            if (size == 0)
                embeddedBytes = blob[4..];
            else
            {
                using var compressed = new MemoryStream(blob, 4, blob.Length - 4);
                using var deflate = new DeflateStream(compressed, CompressionMode.Decompress);
                embeddedBytes = new byte[size];
                deflate.ReadExactly(embeddedBytes);
                if (deflate.ReadByte() != -1)
                    throw new InvalidDataException("Embedded source size mismatch");
            }
            VerifyChecksum(embeddedBytes, algorithm, checksum);
        }

        // Compiler-generated documents are not repository sources. Only accept their verified embedded bytes.
        if (name.Contains("/obj/", StringComparison.Ordinal) && embeddedBytes != null)
        {
            result.embeddedDocuments++;
            return;
        }

        var matches = maps.Select(pair => (pair, url: Map(name, pair.Key, pair.Value)))
            .Where(match => match.url != null).OrderByDescending(match => match.pair.Key.Length).ToArray();
        if (matches.Length == 0)
            throw new InvalidDataException("Document has no Source Link mapping");
        var url = matches[0].url!;
        var prefix = $"https://raw.githubusercontent.com/JacobChwastek/MessageTransit/{commit}/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Source Link URL must identify the candidate repository and commit: {url}");
        var relative = Uri.UnescapeDataString(url[prefix.Length..]);
        if (relative.Contains('\\') || relative.Contains('?') || relative.Contains('#'))
            throw new InvalidDataException("Invalid repository source path");
        var source = Path.GetFullPath(Path.Combine(root, relative));
        if (!source.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException("Source Link path escapes checkout");
        if (!File.Exists(source))
            throw new InvalidDataException($"Mapped source is absent from checkout: {relative}");
        VerifyChecksum(File.ReadAllBytes(source), algorithm, checksum);
        result.trackedDocuments++;
    }

    static string? Map(string document, string pattern, string url)
    {
        if (!pattern.Contains('*'))
            return document == pattern ? url : null;
        if (!pattern.EndsWith('*') || pattern.Count(character => character == '*') != 1
            || !url.EndsWith('*') || url.Count(character => character == '*') != 1)
            throw new InvalidDataException("Unsupported Source Link wildcard pattern");
        var prefix = pattern[..^1];
        return document.StartsWith(prefix, StringComparison.Ordinal) ? url[..^1] + document[prefix.Length..] : null;
    }

    static void VerifyChecksum(byte[] contents, Guid algorithm, byte[] expected)
    {
        var actual = algorithm == Sha256 ? SHA256.HashData(contents) : SHA1.HashData(contents);
        if (!CryptographicOperations.FixedTimeEquals(actual, expected))
            throw new InvalidDataException("Source checksum mismatch");
    }
}

sealed class SymbolResult
{
    public string path { get; set; } = "";
    public string name { get; set; } = "";
    public string token { get; set; } = "";
    public int documents { get; set; }
    public int trackedDocuments { get; set; }
    public int embeddedDocuments { get; set; }
    public List<string> errors { get; } = [];
}
