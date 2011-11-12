using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		Persistent repository folder configuration
	/// </summary>
	/// <remarks>
	///		Folder properties are designed for reliability as the priority. Any changes are immediately flushed to disk. As a result they are not suitable
	///		for large volumes of data or frequent updates.
	/// </remarks>
	public interface IFolderProperties
	{
		/// <summary>
		///		Get or set folder description.
		/// </summary>
		string Description
		{ get; set; }

		/// <summary>
		///		Get or set user-friendly name.
		/// </summary>
		string DisplayName
		{ get; set; }

		/// <summary>
		///		Get or set code of the compressor (<see cref="ICoder.KeyCode"/>) configured for use when writing data into this folder.
		/// </summary>
		/// <remarks>
		///		If the property is not set for the folder, the value will be inherited from the closest ancestor which has this property set.
		///		Must return <code>string.Empty</code> if not set. This property must return valid compressor code to be able to save data into the folder.
		///		The compressor must be registered beforehand with <see cref="IObjectFactory.AddCompressor(ICoder, bool)"/>.
		///		The compressor code is case-insensitive.
		///		Cannot be null.
		/// </remarks>
		string Compressor
		{ get; set; }

		/// <summary>
		///		Get or set optional flag enabling or disabling encryption in this and descendand folders (unless overridden).
		/// </summary>
		/// <remarks>
		///		The property can be inherited or overridden. Set null value to clear explicit setting for this folder and only inherit it from
		///		closest ancestor which has this property set.
		/// </remarks>
		bool? EnableEncryption
		{ get; set; }

		/// <summary>
		///		Get or set code of the encryptor configured for use when writing data into this folder.
		/// </summary>
		/// <remarks>
		///		If the property is not set for the folder, the value will be inherited from the closest ancestor which has this property set.
		///		Must return <code>string.Empty</code> if not set. If <see cref="EnableEncryption"/> is true this property must return valid
		///		encryptor code to be able to save data into the folder.
		/// </remarks>
		string Encryptor
		{ get; set; }

		/// <summary>
		///		Get or set desired number of data items per file when writing data to this or descendant folders.
		/// </summary>
		/// <remarks>
		///		The value will be used as a guide. During normal sequential writing the target size will be observed exactly. But when inserting data not in order
		///		the actual file size may differ.
		///		When the value is not a positive number, it will be inherited from the closest ancestor whith the valid setting. If none has a valid setting a default
		///		will be used. If the value has not been set this property returns 0.
		///		Set 0 value to clear explicit setting for this folder and only inherit it from closest ancestor which has this property set.
		/// </remarks>
		int DesiredItemsPerFile
		{ get; set; }

		/// <summary>
		///		Get custom property value.
		/// </summary>
		/// <param name="name">
		///		Property name.
		/// </param>
		/// <returns>
		///		Custom property value or null if not set.
		/// </returns>
		/// <remarks>
		///		Every repository folder can have an arbitrary set of named string properties. This should not be overused as the storage is not optimised for performance and
		///		large volumes of data.
		/// </remarks>
		/// <seealso cref="SetCustomProperty(string, string)"/>
		string GetCustomProperty(string name);

		/// <summary>
		///		Set custom property value.
		/// </summary>
		/// <param name="name">
		///		Property name.
		/// </param>
		/// <param name="value">
		///		Property value. Specify null to remove property.
		/// </param>
		/// <remarks>
		///		Every repository folder can have an arbitrary set of named string properties. This should not be overused as the storage is not optimised for performance and
		///		large volumes of data.
		///		If property with the specified name is already set it will be overwritten. To remove a propert set its value to null.
		///		The value is immediately saved to disk.
		/// </remarks>
		/// <seealso cref="GetCustomProperty(string)"/>
		void SetCustomProperty(string name, string value);

		/// <summary>
		///		Load properties from disk.
		/// </summary>
		void Load();

		/// <summary>
		///		Force save to disk.
		/// </summary>
		void Save();

		/// <summary>
		///		Delete properties from disk.
		/// </summary>
		void Delete();
	}
}
