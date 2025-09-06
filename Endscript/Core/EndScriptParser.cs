﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Endscript.Enums;
using Endscript.Commands;
using Endscript.Profiles;
using Endscript.Exceptions;
using Endscript.Interfaces;
using CoreExtensions.Text;



namespace Endscript.Core
{
	public class EndScriptParser
	{
		private readonly string _filename;
		private string _xml_description = String.Empty;
		private const string VERSN2 = "[VERSN2]";
		private const string VERSN3 = "[VERSN3]";

		public string CurrentFile { get; private set; } = String.Empty;
		public string CurrentLine { get; private set; } = String.Empty;
		public int CurrentIndex { get; private set; } = -1;

		/// <summary>
		/// Directory of the launcher endscript passed.
		/// </summary>
		public string Directory => Path.GetDirectoryName(this._filename);

		/// <summary>
		/// XML description menu as a string value.
		/// </summary>
		public string XMLDescription => this._xml_description;
		
		public EndScriptParser(string filename)
		{
			this._filename = filename;
		}

		public BaseCommand[] Read()
		{
			return this.RecursiveRead(this._filename).ToArray();
		}

		private List<BaseCommand> RecursiveRead(string filename)
		{
            // Always expect Version 2 endscript to be passed
            var ext = Path.GetExtension(filename).ToLower();
			int ver = 0;

			if (ext == ".endxml") // Assume the file is version 3 and skip first line version check
                ver = 3;
            else if (ext == ".endscript") // assume is version 2
                ver = 2;

			if (!File.Exists(filename))
			{

				throw new FileNotFoundException($"File with path {filename} does not exist");

			}

			using (var sr = new StreamReader(filename))
			{

				switch (ver)
				{
					case 3: // .endxml
                        this._xml_description = sr.ReadToEnd();
                        return null;
					case 2: // .endscript
                        // No need to do anything, we continue to read below
                        break;
					case 0:
					default:
                        var read = sr.ReadLine();
                        if (read == VERSN3)
                        {

                            this._xml_description = sr.ReadToEnd();
                            return null;

                        }
                        else if (read != VERSN2)
                        {

                            throw new InvalidVersionException(2);

                        }
						break;
                }
				

			}

			var relative = filename.Substring(this.Directory.Length + 1);
			this.CurrentFile = relative;
			var lines = File.ReadAllLines(filename);
			var list = new List<BaseCommand>(lines.Length);

            // Start with line 1 b/c line 0 is VERSN line (if not .endscript)
            for (int i = (ver == 2 ? 0 : 1); i < lines.Length; ++i)
			{

				var line = lines[i];
				line = line.Trim();

				if (String.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith('#')) continue;
				if (line.StartsWith('{') || line.StartsWith('}')) continue;

				this.CurrentLine = line;
				this.CurrentIndex = i + 1;

				var splits = line.SmartSplitString().ToArray();
				if (!Enum.TryParse(splits[0], out eCommandType type)) type = eCommandType.invalid;

				// Flatten all endscripts into one by merging them together via append commands
				if (type == eCommandType.append)
				{

					if (splits.Length != 2)
					{

						throw new InvalidArgsNumberException(splits.Length, 2);

					}

					var path = Path.Combine(this.Directory, splits[1]);
					var addon = this.RecursiveRead(path);
					if (addon != null) list.AddRange(addon);
					continue;

				}

				// Get command type, try preparing
				var command = GetCommandFromType(type);
				command.Prepare(splits);

				// If command is correct, add it to list
				command.Filename = relative;
				command.Line = line;
				command.Index = i + 1;
				list.Add(command);

			}

			return list;
		}
	
		public static eCommandType ExecuteSingleCommand(string line, BaseProfile profile)
		{
			if (String.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith('#'))
			{

				return eCommandType.empty;

			}

			var splits = line.SmartSplitString().ToArray();

			if (!Enum.TryParse(splits[0], out eCommandType type))
			{

				throw new Exception($"Unrecognizable command named {splits[0]}");

			}

			var command = GetCommandFromType(type);
			
			if (command is ISingleParsable single)
			{

				command.Line = line;
				command.Prepare(splits);
				single.SingleExecution(profile);
				return type;

			}
			else
			{

				throw new Exception($"Command of type {type} cannot be executed in a single-command mode");

			}

		}

		private static BaseCommand GetCommandFromType(eCommandType type)
		{
			return type switch
			{
				eCommandType.add_collection => new AddCollectionCommand(),
				eCommandType.add_incareer => new AddInCareerCommand(),
				eCommandType.add_or_replace_texture => new AddOrReplaceTextureCommand(),
				eCommandType.add_or_update_string => new AddOrUpdateStringCommand(),
				eCommandType.add_string => new AddStringCommand(),
				eCommandType.add_texture => new AddTextureCommand(),
				eCommandType.bind_textures => new BindTexturesCommand(),
				eCommandType.checkbox => new CheckboxCommand(),
				eCommandType.combobox => new ComboboxCommand(),
				eCommandType.copy_collection => new CopyCollectionCommand(),
				eCommandType.copy_incareer => new CopyInCareerCommand(),
				eCommandType.copy_texture => new CopyTextureCommand(),
				eCommandType.create_file => new CreateFileCommand(),
				eCommandType.create_folder => new CreateFolderCommand(),
				eCommandType.delete => new DeleteCommand(),
				eCommandType.end => new EndCommand(),
				eCommandType.erase_file => new EraseFileCommand(),
				eCommandType.erase_folder => new EraseFolderCommand(),
				eCommandType.@if => new IfStatementCommand(),
				eCommandType.import => new ImportCommand(),
				eCommandType.import_all => new ImportAllCommand(),
				eCommandType.infobox => new InfoboxCommand(),
				eCommandType.move_file => new MoveFileCommand(),
				eCommandType.@new => new NewCommand(),
				eCommandType.pack_stream => new PackStreamCommand(),
				eCommandType.remove_collection => new RemoveCollectionCommand(),
				eCommandType.remove_incareer => new RemoveInCareerCommand(),
				eCommandType.remove_string => new RemoveStringCommand(),
				eCommandType.remove_texture => new RemoveTextureCommand(),
				eCommandType.replace_texture => new ReplaceTextureCommand(),
				eCommandType.speedreflect => new SpeedReflectCommand(),
				eCommandType.@static => new StaticCommand(),
				eCommandType.stop_errors => new StopErrorsCommand(),
				eCommandType.unlock_memory => new UnlockMemoryCommand(),
				eCommandType.unpack_stream => new UnpackStreamCommand(),
				eCommandType.update_collection => new UpdateCollectionCommand(),
				eCommandType.update_incareer => new UpdateInCareerCommand(),
				eCommandType.update_string => new UpdateStringCommand(),
				eCommandType.update_texture => new UpdateTextureCommand(),
				eCommandType.version => new VersionCommand(),
				eCommandType.watermark => new WatermarkCommand(),
				_ => new OptionalCommand()
			};
		}
	}
}
