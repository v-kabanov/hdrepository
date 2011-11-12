using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using System.IO;
using bfs.Repository.Util;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Class implements access to repository folder persistent metadata.
	/// </summary>
	/// <remarks>
	///		Folder properties are designed for reliability as the priority. Any changes are immediately flushed to disk. As a result they are not suitable
	///		for large volumes of data or frequent updates.
	/// </remarks>
	internal class FolderProperties : IFolderProperties
	{
		public const string folderConfigFileName = "config.xml";

		private RepoFolderXmlConfig _folderConfig;
		private IFolder _folder;

		internal FolderProperties(IFolder folder)
		{
			_folder = folder;
		}

		/// <summary>
		///		Get or set folder description.
		/// </summary>
		public string Description
		{
			get
			{
				RepositoryFolder.CheckNotDetached(_folder);

				return _folderConfig.Config.Description;
			}
			set
			{
				RepositoryFolder.CheckNotDetached(_folder);

				_folderConfig.Config.Description = value;
				Save();
			}
		}

		/// <summary>
		///		Get or set user-friendly name.
		/// </summary>
		public string DisplayName
		{
			get
			{
				RepositoryFolder.CheckNotDetached(_folder);

				return _folderConfig.Config.DisplayName;
			}
			set
			{
				RepositoryFolder.CheckNotDetached(_folder);

				_folderConfig.Config.DisplayName = value;
				Save();
			}
		}

		/// <summary>
		///		Get or set code of the compressor (<see cref="ICoder.KeyCode"/>) configured for use when writing data into this folder.
		/// </summary>
		/// <remarks>
		///		If the property is not set for the folder, the value will be inherited from the closest ancestor which has this property set.
		///		Must return <code>string.Empty</code> if not set. This property must return valid compressor code to be able to save data into the folder.
		///		The compressor must be registered beforehand with <see cref="IObjectFactory.AddCompressor(ICoder, bool)"/>.
		///		The code is case-insensitive.
		///		Cannot be null.
		/// </remarks>
		public string Compressor
		{
			get
			{
				RepositoryFolder.CheckNotDetached(_folder);

				if (string.IsNullOrEmpty(_folderConfig.Config.Compressor) && _folder.ParentFolder != null)
				{
					return _folder.ParentFolder.Properties.Compressor;
				}
				return _folderConfig.Config.Compressor == null
					? string.Empty
					: _folderConfig.Config.Compressor;
			}
			set
			{
				RepositoryFolder.CheckNotDetached(_folder);

				if (!string.IsNullOrEmpty(value))
				{
					CheckHelper.CheckExistingCompressor(value, _folder.Repository.ObjectFactory);
				}

				_folderConfig.Config.Compressor = value;
				Save();
			}
		}

		/// <summary>
		///		Get or set optional flag enabling or disabling encryption in this and descendand folders (unless overridden).
		/// </summary>
		/// <remarks>
		///		The property can be inherited or overridden. Set null value to clear explicit setting for this folder and only inherit it from
		///		closest ancestor which has this property set.
		/// </remarks>
		public bool? EnableEncryption
		{
			get
			{
				RepositoryFolder.CheckNotDetached(_folder);

				if (!_folderConfig.Config.EnableEncryptionSet && _folder.ParentFolder != null)
				{
					return _folder.ParentFolder.Properties.EnableEncryption;
				}
				return _folderConfig.Config.EnableEncryptionNullable;
			}
			set
			{
				RepositoryFolder.CheckNotDetached(_folder);

				_folderConfig.Config.EnableEncryptionNullable = value;
				Save();
			}
		}


		/// <summary>
		///		Get or set code of the encryptor configured for use when writing data into this folder.
		/// </summary>
		/// <remarks>
		///		If the property is not set for the folder, the value will be inherited from the closest ancestor which has this property set.
		///		Must return <code>string.Empty</code> if not set.
		///		If <see cref="EnableEncryption"/> is true and this property returns string.Empty the default encryptor will be used
		///		(<see cref="IObjectFactory."/>
		///		The encryptor must be registered beforehand with <see cref="IObjectFactory.AddEncryptor(ICoder, bool)"/>.
		///		The code is case-insensitive.
		///		Cannot be null.
		/// </remarks>
		public string Encryptor
		{
			get
			{
				RepositoryFolder.CheckNotDetached(_folder);

				if (string.IsNullOrEmpty(_folderConfig.Config.Encryptor) && _folder.ParentFolder != null)
				{
					return _folder.ParentFolder.Properties.Encryptor;
				}
				return _folderConfig.Config.Encryptor == null
					? string.Empty
					: _folderConfig.Config.Encryptor;
			}
			set
			{
				RepositoryFolder.CheckNotDetached(_folder);

				if (!string.IsNullOrEmpty(value))
				{
					CheckHelper.CheckExistingEncryptor(value, _folder.Repository.ObjectFactory);
				}

				_folderConfig.Config.Encryptor = value;
				Save();
			}
		}

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
		public int DesiredItemsPerFile
		{
			get
			{
				RepositoryFolder.CheckNotDetached(_folder);

				if (0 >= _folderConfig.Config.DesiredItemsPerFile && _folder.ParentFolder != null)
				{
					return _folder.ParentFolder.Properties.DesiredItemsPerFile;
				}
				return _folderConfig.Config.DesiredItemsPerFile;
			}
			set
			{
				RepositoryFolder.CheckNotDetached(_folder);

				_folderConfig.Config.DesiredItemsPerFile = value;

				Save();
			}
		}

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
		public string GetCustomProperty(string name)
		{
			RepositoryFolder.CheckNotDetached(_folder);

			string retval = null;
			NameValuePair pair = FindCustomProperty(name);
			if (pair != null)
			{
				retval = pair.Value;
			}
			return retval;
		}

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
		public void SetCustomProperty(string name, string value)
		{
			RepositoryFolder.CheckNotDetached(_folder);

			Check.RequireArgumentNotNull(name, "name");

			NameValuePair pair = FindCustomProperty(name);
			if (pair != null)
			{
				if (value == null)
				{
					_folderConfig.Config.CustomParameters.Remove(pair);
				}
				else
				{
					pair.Value = value;
				}
			}
			else
			{
				_folderConfig.Config.CustomParameters.Add(new NameValuePair() { Name = name, Value = value });
			}

			Save();
		}

		/// <summary>
		///		Load properties from disk.
		/// </summary>
		public void Load()
		{
			RepositoryFolder.CheckNotDetached(_folder);

			_folderConfig = RepoFolderXmlConfig.Load(this.ConfigFilePath, this.FileProvider);
		}

		/// <summary>
		///		Force save to disk.
		/// </summary>
		public void Save()
		{
			RepositoryFolder.CheckNotDetached(_folder);

			using (var scope = StorageTransactionScope.Create(_folder.Repository))
			{
				_folderConfig.Save(this.ConfigFilePath, this.FileProvider);
				scope.Complete();
			}
		}

		/// <summary>
		///		Delete properties from disk.
		/// </summary>
		public void Delete()
		{
			RepositoryFolder.CheckNotDetached(_folder);

			using (var scope = StorageTransactionScope.Create(_folder.Repository))
			{
				if (ConfigFileExists)
				{
					this.FileProvider.Delete(this.ConfigFilePath);
				}
				scope.Complete();
			}
		}

		/// <summary>
		///		Convenience property returning repository's file system file provider
		/// </summary>
		internal IFileProvider FileProvider
		{
			get { return _folder.Repository.ObjectFactory.FileSystemProvider.FileProvider; }
		}

		protected string ConfigFilePath
		{
			get
			{
				// Combine works fine with long names
				return Path.Combine(_folder.FullPath, folderConfigFileName);
			}
		}

		private NameValuePair FindCustomProperty(string name)
		{
			return _folderConfig.Config.CustomParameters.FirstOrDefault((p) => p.Name == name);
		}

		protected bool ConfigFileExists
		{
			get { return this.FileProvider.Exists(this.ConfigFilePath); }
		}
	}
}
