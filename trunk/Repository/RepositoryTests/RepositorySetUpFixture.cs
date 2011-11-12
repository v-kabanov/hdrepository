using System;
using System.IO;
using System.Reflection;

using bfs.Repository.Storage;
using bfs.Repository.Interfaces;

using NUnit.Framework;


[SetUpFixture]
public class RepositorySetUpFixture
{
	public const string LoggingConfigFile = "logging.config";
	private const string _logFileExtension = "log";

	[SetUp]
	public static void AssemblyInit()
	{
		if (Manager == null)
		{
			string binaryDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName);

			string runDirName = DateTime.Now.ToString("MM-dd_HH_mm_ss");
			string deploymentDir = Path.Combine(binaryDir, "../../TestResults");
			deploymentDir = Path.Combine(deploymentDir, runDirName);
			deploymentDir = Path.GetFullPath(deploymentDir);

			if (!Directory.Exists(deploymentDir))
			{
				Directory.CreateDirectory(deploymentDir);
			}
			DeploymentPath = deploymentDir;
			foreach (string filePath in Directory.GetFiles(binaryDir))
			{
				if (_logFileExtension.Equals(Path.GetExtension(filePath).Substring(1), StringComparison.InvariantCultureIgnoreCase))
				{
					// deleting all old logs
					File.Delete(filePath);
				}
				else
				{
					File.Copy(filePath, Path.Combine(deploymentDir, Path.GetFileName(filePath)));
				}
			}

			FileInfo configFile = new FileInfo(Path.Combine(deploymentDir, LoggingConfigFile));
			log4net.Config.XmlConfigurator.Configure(configFile);
			//log4net.LogManager.GetRepository().Properties.

			string rootPath = Path.Combine(deploymentDir, "Repo1");
			Directory.CreateDirectory(rootPath);
			Manager = new RepositoryManager(rootPath);

			Assert.IsNotNull(Manager.RootFolder);

			Assert.IsTrue(Directory.Exists(Manager.RootFolder.FullPath));
		}
	}
	
	[TearDown]
	public static void AssemblyTearDown()
	{
		log4net.LogManager.Shutdown();

		string binaryDir = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName));
		if (!string.Equals(binaryDir, DeploymentPath))
		{
			// moving all logs to the deployment directory
			foreach (string filePath in Directory.GetFiles(binaryDir, "*." + _logFileExtension))
			{
				File.Move(filePath, Path.Combine(DeploymentPath, Path.GetFileName(filePath)));
			}
			foreach (string filePath in Directory.GetFiles(binaryDir, "*." + _logFileExtension + ".*"))
			{
				File.Move(filePath, Path.Combine(DeploymentPath, Path.GetFileName(filePath)));
			}
		}
	}

	/// <summary>
	///		Get absolute deployment path.
	/// </summary>
	public static string DeploymentPath
	{ get; private set; }

	internal static RepositoryManager Manager
	{
		get;
		private set;
	}

}
