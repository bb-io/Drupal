using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Tests.Drupal.Base;

public sealed class FileManager : IFileManagementClient
{
    private readonly string inputDirectory;
    private readonly string outputDirectory;

    public FileManager(string testRoot, int version, string outputScope)
    {
        inputDirectory = Path.Combine(testRoot, "TestFiles", "Input", $"Drupal{version}");
        outputDirectory = Path.Combine(testRoot, "TestFiles", "Output", $"Drupal{version}", Sanitize(outputScope));
        Directory.CreateDirectory(outputDirectory);
    }

    public Task<Stream> DownloadAsync(FileReference reference)
    {
        var outputPath = Resolve(outputDirectory, reference.Name);
        var inputPath = Resolve(inputDirectory, reference.Name);
        var path = File.Exists(outputPath) ? outputPath : inputPath;
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Test file '{reference.Name}' was not found.", path);
        }

        return Task.FromResult<Stream>(new MemoryStream(File.ReadAllBytes(path), writable: false));
    }

    public async Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName)
    {
        var path = Resolve(outputDirectory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        await stream.CopyToAsync(file);
        await file.FlushAsync();

        return new FileReference
        {
            Name = fileName,
            ContentType = contentType
        };
    }

    public FileReference WriteOutput(string fileName, byte[] bytes, string contentType)
    {
        var path = Resolve(outputDirectory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        return new FileReference { Name = fileName, ContentType = contentType };
    }

    public FileReference WriteOutput(string fileName, string content, string contentType) =>
        WriteOutput(fileName, System.Text.Encoding.UTF8.GetBytes(content), contentType);

    public byte[] ReadOutput(FileReference reference) =>
        File.ReadAllBytes(Resolve(outputDirectory, reference.Name));

    public string ReadOutputText(FileReference reference) =>
        File.ReadAllText(Resolve(outputDirectory, reference.Name));

    public string ReadInputText(string fileName) =>
        File.ReadAllText(Resolve(inputDirectory, fileName));

    private static string Resolve(string root, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, fileName));
        if (!fullPath.StartsWith(fullRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Test file path escapes storage root: {fileName}");
        }

        return fullPath;
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
