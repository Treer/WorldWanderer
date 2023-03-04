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
    /// A copy of the methods exposed by Godot.ConfigFile
    /// </summary>
    public interface IGodotConfigFile
    {
        /// <summary>
        /// Assigns a value to the specified key of the specified section. If either the
        /// section or the key do not exist, they are created. Passing a null value deletes
        /// the specified key if it exists, and deletes the section if it ends up empty once
        /// the key has been removed.
        /// </summary>
        void SetValue(string section, string key, Variant value);

        /// <summary>
        /// Returns the current value for the specified section and key. If either the section
        /// or the key do not exist, the method returns the fallback default value. If default
        /// is not specified or set to null, an error is also raised.
        /// </summary>
        Variant GetValue(string section, string key, Variant @default = default(Variant));

        /// <summary>
        /// Returns true if the specified section exists.
        /// </summary>
        bool HasSection(string section);

        /// <summary>
        /// Returns true if the specified section-key pair exists.
        /// </summary>
        bool HasSectionKey(string section, string key);

        /// <summary>
        /// Returns an array of all defined section identifiers.
        /// </summary>
        string[] GetSections();

        /// <summary>
        /// Returns an array of all defined key identifiers in the specified section. Raises
        /// an error and returns an empty array if the section does not exist.
        /// </summary>
        string[] GetSectionKeys(string section);

        /// <summary>
        /// Deletes the specified section along with all the key-value pairs inside. Raises
        /// an error if the section does not exist.
        /// </summary>
        void EraseSection(string section);

        /// <summary>
        /// Deletes the specified key in a section. Raises an error if either the section
        /// or the key do not exist.
        /// </summary>
        void EraseSectionKey(string section, string key);

        /// <summary>
        /// Loads the config file specified as a parameter. The file's contents are parsed
        /// and loaded in the Godot.ConfigFile object which the method was called on.
        /// Returns one of the Godot.Error code constants (Godot.Error.Ok on success).
        /// </summary>
        Error Load(string path);

        /// <summary>
        /// Parses the passed string as the contents of a config file. The string is parsed
        /// and loaded in the ConfigFile object which the method was called on.
        /// Returns one of the Godot.Error code constants (Godot.Error.Ok on success).
        /// </summary>
        Error Parse(string data);

        /// <summary>
        /// Saves the contents of the Godot.ConfigFile object to the file specified as a
        /// parameter. The output file uses an INI-style structure.
        /// Returns one of the Godot.Error code constants (Godot.Error.Ok on success).
        /// </summary>
        Error Save(string path);

        /// <summary>
        /// Obtain the text version of this config file (the same text that would be written
        /// to a file).
        /// </summary>
        string EncodeToText();

        /// <summary>
        /// Loads the encrypted config file specified as a parameter, using the provided
        /// key to decrypt it. The file's contents are parsed and loaded in the Godot.ConfigFile
        /// object which the method was called on.
        /// Returns one of the Godot.Error code constants (Godot.Error.Ok on success).
        /// </summary>
        Error LoadEncrypted(string path, byte[] key);

        /// <summary>
        /// Loads the encrypted config file specified as a parameter, using the provided
        /// password to decrypt it. The file's contents are parsed and loaded in the Godot.ConfigFile
        /// object which the method was called on.
        /// Returns one of the Godot.Error code constants (Godot.Error.Ok on success).
        /// </summary>
        Error LoadEncryptedPass(string path, string password);

        /// <summary>
        /// Saves the contents of the Godot.ConfigFile object to the AES-256 encrypted file
        /// specified as a parameter, using the provided key to encrypt it. The output file
        /// uses an INI-style structure.
        /// Returns one of the Godot.Error code constants (Godot.Error.Ok on success).
        /// </summary>
        Error SaveEncrypted(string path, byte[] key);

        /// <summary>
        /// Saves the contents of the Godot.ConfigFile object to the AES-256 encrypted file
        /// specified as a parameter, using the provided password to encrypt it. The output
        /// file uses an INI-style structure.
        /// Returns one of the Godot.Error code constants (Godot.Error.Ok on success).
        /// </summary>
        Error SaveEncryptedPass(string path, string password);

        /// <summary>
        /// Removes the entire contents of the config.
        /// </summary>
        void Clear();
    }
}
