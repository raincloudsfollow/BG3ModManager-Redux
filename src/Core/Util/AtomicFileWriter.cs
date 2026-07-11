using System.Text;

namespace DivinityModManager.Util;

/// <summary>
/// Writes a complete file beside its destination, validates it, and only then
/// replaces the live file. Keeping the temporary file on the same volume lets
/// Windows perform the final replacement atomically.
/// </summary>
public static class AtomicFileWriter
{
	public static void WriteAllText(string destinationPath, string contents, string backupPath = null,
		Func<string, bool> validateTemporaryFile = null)
	{
		var bytes = new UTF8Encoding(false).GetBytes(contents ?? String.Empty);
		WriteAllBytes(destinationPath, bytes, backupPath, validateTemporaryFile);
	}

	public static void WriteAllBytes(string destinationPath, byte[] contents, string backupPath = null,
		Func<string, bool> validateTemporaryFile = null)
	{
		var temporaryPath = destinationPath + ".tmp";
		PrepareDirectory(destinationPath);
		DeleteTemporaryFile(temporaryPath);
		try
		{
			using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
				Math.Max(4096, contents.Length), FileOptions.WriteThrough))
			{
				stream.Write(contents, 0, contents.Length);
				stream.Flush(true);
			}

			Validate(temporaryPath, validateTemporaryFile);
			Commit(temporaryPath, destinationPath, backupPath);
		}
		catch
		{
			DeleteTemporaryFile(temporaryPath);
			throw;
		}
	}

	public static async Task WriteAllBytesAsync(string destinationPath, byte[] contents, string backupPath = null,
		Func<string, bool> validateTemporaryFile = null, CancellationToken cancellationToken = default)
	{
		var temporaryPath = destinationPath + ".tmp";
		PrepareDirectory(destinationPath);
		DeleteTemporaryFile(temporaryPath);
		try
		{
			using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
				Math.Max(4096, contents.Length), FileOptions.Asynchronous | FileOptions.WriteThrough))
			{
				await stream.WriteAsync(contents, cancellationToken);
				await stream.FlushAsync(cancellationToken);
				stream.Flush(true);
			}

			cancellationToken.ThrowIfCancellationRequested();
			Validate(temporaryPath, validateTemporaryFile);
			Commit(temporaryPath, destinationPath, backupPath);
		}
		catch
		{
			DeleteTemporaryFile(temporaryPath);
			throw;
		}
	}

	private static void PrepareDirectory(string destinationPath)
	{
		var directory = Path.GetDirectoryName(destinationPath);
		if (String.IsNullOrWhiteSpace(directory))
			throw new ArgumentException("The destination must include a parent directory.", nameof(destinationPath));
		Directory.CreateDirectory(directory);
	}

	private static void Validate(string temporaryPath, Func<string, bool> validator)
	{
		if (new FileInfo(temporaryPath).Length <= 0)
			throw new InvalidDataException($"Temporary file '{temporaryPath}' is empty.");
		if (validator != null && !validator(temporaryPath))
			throw new InvalidDataException($"Temporary file '{temporaryPath}' did not pass validation.");
	}

	private static void Commit(string temporaryPath, string destinationPath, string backupPath)
	{
		if (File.Exists(destinationPath))
		{
			File.Replace(temporaryPath, destinationPath, backupPath, true);
		}
		else
		{
			File.Move(temporaryPath, destinationPath);
		}
	}

	private static void DeleteTemporaryFile(string temporaryPath)
	{
		try
		{
			if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Could not remove temporary file '{temporaryPath}': {ex}");
		}
	}
}
