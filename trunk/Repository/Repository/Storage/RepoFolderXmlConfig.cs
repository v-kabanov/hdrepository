//-----------------------------------------------------------------------------
// <created>3/23/2010 10:13:27 AM</created>
// <author>Vasily Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using System.Xml.Serialization;
using System.Xml;
using System.IO;

using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	public class NameValuePair
	{
		public string Name
		{ get; set; }

		public string Value
		{ get; set; }
	}

	public class FolderXmlConfigV1
	{
		private int _desiredItemsPerFile;

		public FolderXmlConfigV1()
		{
			this.CustomParameters = new List<NameValuePair>();
		}

		[XmlElement(IsNullable = false)]
		public string DisplayName
		{ get; set; }

		[XmlElement(IsNullable = false)]
		public string Description
		{ get; set; }

		[XmlElement(IsNullable = false)]
		public string Compressor
		{ get; set; }

		[XmlElement(IsNullable = false)]
		public string Encryptor
		{ get; set; }

		[XmlElement(IsNullable = false)]
		public bool EnableEncryption
		{ get; set; }

		[XmlElement(IsNullable = false)]
		public bool EnableEncryptionSet
		{ get; set; }

		/// <summary>
		///		Value of 0 will indicate "not set, use default"
		/// </summary>
		[XmlElement]
		public int DesiredItemsPerFile
		{
			get { return _desiredItemsPerFile;}
			set
			{
				if (value < 0)
				{
					_desiredItemsPerFile = 0;
				}
				else
				{
					_desiredItemsPerFile = value;
				}
			}
		}

		[XmlIgnore]
		public bool HasData
		{
			get
			{
				return !string.IsNullOrEmpty(this.DisplayName)
					|| !string.IsNullOrEmpty(this.Description)
					|| !string.IsNullOrEmpty(this.Compressor)
					|| !string.IsNullOrEmpty(this.Encryptor)
					|| this.EnableEncryptionSet
					|| (this.CustomParameters != null && this.CustomParameters.Count > 0)
					|| this.DesiredItemsPerFile != 0;
			}
		}

		/// <summary>
		///		<see cref="XmlSerizlizer"/> does not support nullable types.
		///		Therefore here wrapping a pair of related flags (<see cref="EnableEncryption"/> and
		///		<see cref="EnableEncryptionSet"/>).
		/// </summary>
		[XmlIgnore]
		public bool? EnableEncryptionNullable
		{
			get
			{
				return EnableEncryptionSet ? (bool?)EnableEncryption : null;
			}
			set
			{
				EnableEncryptionSet = value.HasValue;
				if (value.HasValue)
				{
					EnableEncryption = value.Value;
				}
			}
		}

		[XmlArray("CustomParameters")]
		[XmlArrayItem(ElementName="Parameter")]
		public List<NameValuePair> CustomParameters
		{ get; set; }
	}

	[XmlType(IncludeInSchema = false)]
	public enum FolderVersion
	{
		v1
	}

	[XmlRoot("folder", IsNullable = false)]
	public class RepoFolderXmlConfig
	{
		public RepoFolderXmlConfig()
		{
			this.Version = FolderVersion.v1;
			this.Config = new FolderXmlConfigV1();
		}

		[XmlAttribute]
		public FolderVersion Version
		{ get; set; }

		[XmlChoiceIdentifier("Version")]
		[XmlElement("v1", typeof(FolderXmlConfigV1))]
		public FolderXmlConfigV1 Config
		{ get; set; }

		public void Save(string filePath, IFileProvider fileProvider)
		{
			if (this.Config == null || !this.Config.HasData)
			{
				// empty config, removing file
				if (fileProvider.Exists(filePath))
				{
					fileProvider.Delete(filePath);
				}
			}
			else
			{
				XmlSerializer serialiser = new XmlSerializer(typeof(RepoFolderXmlConfig));

				using (XmlTextWriter stream = new XmlTextWriter(
					fileProvider.Open(filePath, FileMode.Create, FileAccess.Write)
					, Encoding.UTF8))
				{
					stream.Indentation = 1;
					stream.IndentChar = '\t';
					stream.Formatting = Formatting.Indented;
					serialiser.Serialize(stream, this);
				}
			}
		}

		/// <summary>
		///		Load folder configuration from config file
		/// </summary>
		/// <param name="filePath">
		///		Configuration file full path
		/// </param>
		/// <returns>
		///		null if file does not exist
		/// </returns>
		public static RepoFolderXmlConfig Load(string filePath, IFileProvider fileProvider)
		{
			RepoFolderXmlConfig retval;
			if (fileProvider.Exists(filePath))
			{
				XmlSerializer serialiser = new XmlSerializer(typeof(RepoFolderXmlConfig));
				using (FileStream stream = fileProvider.Open(filePath, FileMode.Open, FileAccess.Read))
				{
					retval = (RepoFolderXmlConfig)serialiser.Deserialize(stream);
				}
			}
			else
			{
				// empty config
				retval = new RepoFolderXmlConfig();
			}
			return retval;
		}
	}
}
