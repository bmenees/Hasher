namespace Hasher;

#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Menees;
using Menees.Shell;

#endregion

internal class Arguments
{
	#region Private Data Members

	private static readonly string SpecialCompares = string.Join("|", Enum.GetNames(typeof(CompareToSpecial)).OrderBy(n => n));

	#endregion

	#region Constructors

	public Arguments(string[] args)
	{
		CommandLine commandLine = new CommandLine(false);
		commandLine.AddHeader($"Usage: Hasher.exe [fileName] [/algorithm typeName] [/compareTo hashValue|{SpecialCompares}] [/start]");

		commandLine.AddValueHandler(
			(file, errors) =>
			{
				if (this.File.IsEmpty() && System.IO.File.Exists(file))
				{
					this.File = file;
				}
				else if (this.File.IsNotEmpty())
				{
					errors.Add($"A file argument has already been specified ({this.File}), so \"{file}\" can't be used.");
				}
				else
				{
					errors.Add($"File \"{file}\" does not exist.");
				}
			},
			new KeyValuePair<string, string>(nameof(this.File), "The full path to a file to hash."));

		// We can't use typeof(HashAlgorithm) here because in .NET "Core" it's in a .Primitives assembly unlike the derived S.S.C types.
		Type knownType = typeof(SHA512);
		commandLine.AddSwitch(nameof(this.Algorithm), $"The name of a {knownType.Namespace} hash algorithm to use.", (algorithm, errors) =>
		{
			this.Algorithm = knownType.Assembly.GetType(knownType.Namespace + "." + algorithm, throwOnError: false);
			if (this.Algorithm is null)
			{
				errors.Add($"Unable to find hash algorithm {algorithm}.");
			}

			// We can't ignore abstract types because all main types are abstract (e.g., MD5, SHA*).
		});

		commandLine.AddSwitch(
			nameof(this.CompareTo),
			$"A hash value to compare to or one of: {SpecialCompares}",
			(compareTo, errors) =>
			{
				// When using Send To shortcuts with /start, we can pass some special non-hash values:
				//   "Empty":  don't compare to any previous value that was saved to the .stgx file.
				//   "Clipboard": compare to the hex value currently on the clipboard.
				// Enum.TryParse would allow integers like 0 or 1, but we'll ignore them since they're ambigous with hex values.
				if (!Enum.TryParse(compareTo, out CompareToSpecial special) || int.TryParse(compareTo, out _))
				{
					this.CompareTo = compareTo;
				}
				else
				{
					this.CompareTo = special switch
					{
						CompareToSpecial.Clipboard => TryGetClipboardHashValue(),
						_ => string.Empty,
					};
				}
			});

		commandLine.AddSwitch(nameof(this.Start), "Whether to begin hashing the file automatically at startup.", start => this.Start = start);

		CommandLineParseResult parseResult = commandLine.Parse(args);
		if (parseResult != CommandLineParseResult.Valid)
		{
			this.ParseMessage = commandLine.CreateMessage();
			this.IsHelpMessage = parseResult == CommandLineParseResult.HelpRequested;
		}
	}

	#endregion

	#region Private Enums

	private enum CompareToSpecial
	{
		// Don't use a "None" member since all these names are shown in the help.
		Empty,
		Clipboard,
	}

	#endregion

	#region Public Properties

	public string? File { get; private set; }

	public Type? Algorithm { get; private set; }

	public string? CompareTo { get; private set; }

	public bool Start { get; private set; }

	public string? ParseMessage { get; }

	public bool IsHelpMessage { get; }

	#endregion

	#region Private Methods

	private static string TryGetClipboardHashValue()
	{
		string result = Clipboard.GetText();

		byte[]? bytes = ConvertUtility.FromHex(result, false);
		if (bytes == null)
		{
			result = string.Empty;
		}

		return result;
	}

	#endregion
}
