using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Text;

namespace PusherClient.Tests.Utilities
{
    /// <summary>
    /// Loads test configuration from a json file.
    /// </summary>
    public class JsonFileConfigLoader : IApplicationConfigLoader
    {
        private const string DefaultFileName = "AppConfig.test.json";

        /// <summary>
        /// Instantiates an instance of a <see cref="JsonFileConfigLoader"/> using the default file name.
        /// </summary>
        public JsonFileConfigLoader()
            : this(Path.Combine(Assembly.GetExecutingAssembly().Location, $"../../../../{DefaultFileName}"))
        {
        }

        /// <summary>
        /// Instantiates an instance of a <see cref="JsonFileConfigLoader"/> using the specified <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">The full path to the Json config file.</param>
        public JsonFileConfigLoader(string fileName)
        {
            this.FileName = fileName;
        }

        /// <summary>
        /// Gets a static default instance of this class.
        /// </summary>
        public static IApplicationConfigLoader Default { get; } = new JsonFileConfigLoader();

        /// <summary>
        /// Gets or sets the Json config file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Loads test configuration from a Json file.
        /// </summary>
        /// <returns>An <see cref="IApplicationConfig"/> instance.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the Json settings file does not exist.</exception>
        public IApplicationConfig Load()
        {
            FileInfo fileInfo = new FileInfo(this.FileName);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"The test application config file was not found at location '{fileInfo.FullName}'.", fileName: this.FileName);
            }

            string content = File.ReadAllText(fileInfo.FullName, Encoding.UTF8);
            ApplicationConfig result = JsonConvert.DeserializeObject<ApplicationConfig>(content);
            return result;
        }
    }
}
