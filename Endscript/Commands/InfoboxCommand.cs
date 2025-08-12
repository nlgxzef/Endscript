using System;
using Endscript.Core;
using Endscript.Enums;
using Endscript.Helpers;
using Endscript.Exceptions;
using Endscript.Interfaces;



namespace Endscript.Commands
{
	/// <summary>
	/// Command of type 'checkbox [description]' with 'enabled/disabled' options.
	/// </summary>
	public class InfoboxCommand : BaseCommand
	{
		private string _description = String.Empty;

		public override eCommandType Type => eCommandType.checkbox;
		public string Description => this._description;
		public int LastCommand { get; set; }

		public InfoboxCommand()
		{
			
		}


		public override void Prepare(string[] splits)
		{
			if (splits.Length != 2) throw new InvalidArgsNumberException(splits.Length, 2);
			this._description = splits[1];
		}

		public override void Execute(CollectionMap map)
		{

		}
	}
}
