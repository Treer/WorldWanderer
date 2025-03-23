// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapViewer.DynamicConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapViewer.DynamicConfig
{
	public partial class ObservableConfigFile: GodotConfigFile, IGodotConfigFile
	{
		public ObservableConfigFile() { }
		public ObservableConfigFile(ConfigFile configFile) : base(configFile) { }

		/// <inheritdoc/>
		public override void SetValue(string section, string key, Variant value) {
			base.SetValue(section, key, value);
			ValueChanged?.Invoke(this, (section, key, value));
		}

		public event EventHandler<(string, string, Variant)> ValueChanged;
	}
}
