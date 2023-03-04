// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapViewer.DynamicConfig
{
	/// <summary>
	/// Implements IGodotConfigFile by Wrapping a Godot.ConfigFile object.
	/// This provides a way to subclass ConfigFile, e.g. add events to it etc.
	/// </summary>
	public partial class GodotConfigFile: GodotObject, IGodotConfigFile
	{
		protected ConfigFile configFile;

		public GodotConfigFile(): this(null) { }
		public GodotConfigFile(ConfigFile configFile) {
			this.configFile = configFile ?? new ConfigFile();
		}

		#region Implement IGodotConfigFile 
		/// <inheritdoc/>
		public virtual void Clear() {
			configFile.Clear();
		}

		/// <inheritdoc/>
		public virtual string EncodeToText() {
			return configFile.EncodeToText();
		}

		/// <inheritdoc/>
		public virtual void EraseSection(string section) {
			configFile.EraseSection(section);
		}

		/// <inheritdoc/>
		public virtual void EraseSectionKey(string section, string key) {
			configFile.EraseSectionKey(section, key);
		}

		/// <inheritdoc/>
		public virtual string[] GetSectionKeys(string section) {
			return configFile.GetSectionKeys(section);
		}

		/// <inheritdoc/>
		public virtual string[] GetSections() {
			return configFile.GetSections();
		}

		/// <inheritdoc/>
		public virtual Variant GetValue(string section, string key, Variant @default = default) {
			return configFile.GetValue(section, key, @default);
		}

		/// <inheritdoc/>
		public virtual bool HasSection(string section) {
			return configFile.HasSection(section);
		}

		/// <inheritdoc/>
		public virtual bool HasSectionKey(string section, string key) {
			return configFile.HasSectionKey(section, key);
		}

		/// <inheritdoc/>
		public virtual Error Load(string path) {
			return configFile.Load(path);
		}

		/// <inheritdoc/>
		public virtual Error LoadEncrypted(string path, byte[] key) {
			return configFile.LoadEncrypted(path, key);
		}

		/// <inheritdoc/>
		public virtual Error LoadEncryptedPass(string path, string password) {
			return configFile.LoadEncryptedPass(path, password);
		}

		/// <inheritdoc/>
		public virtual Error Parse(string data) {
			return configFile.Parse(data);
		}

		/// <inheritdoc/>
		public virtual Error Save(string path) {
			return configFile.Save(path);
		}

		/// <inheritdoc/>
		public virtual Error SaveEncrypted(string path, byte[] key) {
			return configFile.SaveEncrypted(path, key);
		}

		/// <inheritdoc/>
		public virtual Error SaveEncryptedPass(string path, string password) {
			return configFile.SaveEncryptedPass(path, password);
		}

		/// <inheritdoc/>
		public virtual void SetValue(string section, string key, Variant value) {
			configFile.SetValue(section, key, value);
		}
		#endregion Implement IGodotConfigFile 
	}
}
