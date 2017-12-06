using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb;
using FG.ServiceFabric.Services.Runtime.StateSession.FileSystem;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using FG.ServiceFabric.Utils;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace PersistancePerformanceTestBench
{
	class Program
	{
		static void Main(string[] args)
		{
			RunTest5().GetAwaiter().GetResult();
		}

		/// <summary>
		/// Filesystem 
		/// </summary>
		/// <returns></returns>
		public static async Task RunTest0()
		{
			var keepRunning = true;
			Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
				e.Cancel = true;
				keepRunning = false;
			};

			var settingsProvider = new SettingsProvider();
			var stateSessionManager = new FileSystemStateSessionManager("sample-service", Guid.NewGuid(), "range-0", @"c:\temp\storage\performance-test");

			var cancellationTokenSource = new CancellationTokenSource();
			var cancellationToken = cancellationTokenSource.Token;

			var rate = 100;
			var delay = 1000;
			var stopwatch = new Stopwatch();
			while (keepRunning)
			{
				Console.WriteLine($"Running test with {rate} items. Delaying for {delay} ms.");
				var people = await stateSessionManager.OpenDictionary<Person>("people", cancellationToken);

				stopwatch.Reset();
				stopwatch.Start();
				var random = new Random(Environment.TickCount);
				using (var session = stateSessionManager.CreateSession(people))
				{
					for (var i = 0; i < rate; i++)
					{
						var lastName = ObjectMother.LastNames[random.Next(0, ObjectMother.LastNames.Length - 1)];
						var firstName = ObjectMother.FirstNames[random.Next(0, ObjectMother.FirstNames.Length - 1)];
						var description = ObjectMother.Descriptions[random.Next(0, ObjectMother.Descriptions.Length - 1)];
						var title = ObjectMother.Titles[random.Next(0, ObjectMother.Titles.Length - 1)];

						var key = $"[{title}] {firstName} {lastName.Substring(0, 1).ToUpper()}{lastName.Substring(1)}";
						var person = new Person() { Description = description, Title = title, Name = $"{firstName} {lastName}" };

						await people.SetValueAsync(key, person, cancellationToken);
						Console.Write(".");
					}
					await session.CommitAsync();
				}
				Console.WriteLine();
				stopwatch.Stop();
				var average = (float)stopwatch.ElapsedMilliseconds / (float)rate;
				Console.WriteLine($"Done in {stopwatch.ElapsedMilliseconds} ms. {average} ms/item");
				await Task.Delay(1000, cancellationToken);
				Console.WriteLine();

				rate = rate * 110 / 100;
			}
		}

		/// <summary>
		/// With DocumentDbStateSessionManagerWithTransactions
		/// </summary>
		/// <returns></returns>
		public static async Task RunTest1()
		{
			var keepRunning = true;
			Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
				e.Cancel = true;
				keepRunning = false;
			};

			var settingsProvider = new SettingsProvider();
			var stateSessionManager = new DocumentDbStateSessionManagerWithTransactions("sample-service", Guid.NewGuid(), "range-0", settingsProvider);

			var cancellationTokenSource = new CancellationTokenSource();
			var cancellationToken = cancellationTokenSource.Token;

			var rate = 100;
			var delay = 1000;
			var stopwatch = new Stopwatch();
			while (keepRunning)
			{
				Console.WriteLine($"Running test with {rate} items. Delaying for {delay} ms.");
				var people = await stateSessionManager.OpenDictionary<Person>("people", cancellationToken);

				stopwatch.Reset();
				stopwatch.Start();
				var random = new Random(Environment.TickCount);
				using (var session = stateSessionManager.CreateSession(people))
				{
					for (var i = 0; i < rate; i++)
					{
						var lastName = ObjectMother.LastNames[random.Next(0, ObjectMother.LastNames.Length - 1)];
						var firstName = ObjectMother.FirstNames[random.Next(0, ObjectMother.FirstNames.Length - 1)];
						var description = ObjectMother.Descriptions[random.Next(0, ObjectMother.Descriptions.Length - 1)];
						var title = ObjectMother.Titles[random.Next(0, ObjectMother.Titles.Length - 1)];

						var key = $"[{title}] {firstName} {lastName.Substring(0, 1).ToUpper()}{lastName.Substring(1)}";
						var person = new Person() { Description = description, Title = title, Name = $"{firstName} {lastName}" };

						await people.SetValueAsync(key, person, cancellationToken);
						Console.Write(".");
					}
					await session.CommitAsync();
				}
				Console.WriteLine();
				stopwatch.Stop();
				var average = (float)stopwatch.ElapsedMilliseconds / (float)rate;
				Console.WriteLine($"Done in {stopwatch.ElapsedMilliseconds} ms. {average} ms/item");
				await Task.Delay(1000, cancellationToken);
				Console.WriteLine();

				rate = rate * 110 / 100;
			}
		}

		/// <summary>
		/// Direct DocDb with Wrappers
		/// </summary>
		/// <returns></returns>
		public static async Task RunTest2()
		{
			var keepRunning = true;
			Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
				e.Cancel = true;
				keepRunning = false;
			};

			var settingsProvider = new SettingsProvider();
			var factory = new CosmosDbClientFactory();
			var collection = settingsProvider.Collection();
			var databaseName = settingsProvider.DatabaseName();
			var endpointUri = settingsProvider.EndpointUri();
			var collectionPrimaryKey = settingsProvider.PrimaryKey();
			var connectionPolicySetting = ConnectionPolicySetting.GatewayHttps;
			var client = await factory.OpenAsync(
				databaseName: databaseName,
				collection: new CosmosDbCollectionDefinition(collection, $"/partitionKey"),
				endpointUri: new Uri(endpointUri),
				primaryKey: collectionPrimaryKey,
				connectionPolicySetting: connectionPolicySetting
			);

			var cancellationTokenSource = new CancellationTokenSource();
			var cancellationToken = cancellationTokenSource.Token;

			var rate = 100;
			var delay = 1000;
			var stopwatch = new Stopwatch();
			while (keepRunning)
			{
				Console.WriteLine($"Running test with {rate} items. Delaying for {delay} ms.");				

				stopwatch.Reset();
				stopwatch.Start();
				var random = new Random(Environment.TickCount);
				for (var i = 0; i < rate; i++)
				{
					var lastName = ObjectMother.LastNames[random.Next(0, ObjectMother.LastNames.Length - 1)];
					var firstName = ObjectMother.FirstNames[random.Next(0, ObjectMother.FirstNames.Length - 1)];
					var description = ObjectMother.Descriptions[random.Next(0, ObjectMother.Descriptions.Length - 1)];
					var title = ObjectMother.Titles[random.Next(0, ObjectMother.Titles.Length - 1)];

					var key = $"[{title}] {firstName} {lastName.Substring(0, 1).ToUpper()}{lastName.Substring(1)}";
					var person = new Person() {Description = description, Title = title, Name = $"{firstName} {lastName}"};
					var metadata = new ValueMetadata(StateWrapperType.ReliableQueueItem){ Schema = "people", Key = key};
					var value = new FG.ServiceFabric.Services.Runtime.StateSession.StateWrapper<Person>(key, person,
						new ServiceMetadata() {ServiceName = "sample-service2", PartitionKey = "range-0"}, metadata);

					try
					{
						await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collection),
							value,
							new RequestOptions { PartitionKey = new PartitionKey("range-0") });
					}
					catch (DocumentClientException dcex)
					{
						throw new StateSessionException($"SetValueAsync for {key} failed, {dcex.Message}", dcex);
					}
					catch (Exception ex)
					{
						throw new StateSessionException($"SetValueAsync for {key} failed, {ex.Message}", ex);
					}
					
					Console.Write(".");
				}

				Console.WriteLine();
				stopwatch.Stop();
				var average = (float)rate / (float)stopwatch.ElapsedMilliseconds;
				Console.WriteLine($"Done in {stopwatch.ElapsedMilliseconds} ms. {average}/ms");
				await Task.Delay(1000, cancellationToken);
				Console.WriteLine();

				rate = rate * 110 / 100;
			}
		}

		/// <summary>
		/// Direct DocDb no wrappers
		/// </summary>
		/// <returns></returns>
		public static async Task RunTest3()
		{
			var keepRunning = true;
			Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
				e.Cancel = true;
				keepRunning = false;
			};

			var settingsProvider = new SettingsProvider();
			var factory = new CosmosDbClientFactory();
			var collection = settingsProvider.Collection();
			var databaseName = settingsProvider.DatabaseName();
			var endpointUri = settingsProvider.EndpointUri();
			var collectionPrimaryKey = settingsProvider.PrimaryKey();
			var connectionPolicySetting = ConnectionPolicySetting.GatewayHttps;
			var client = await factory.OpenAsync(
				databaseName: databaseName,
				collection: new CosmosDbCollectionDefinition(collection, $"/partitionKey"),
				endpointUri: new Uri(endpointUri),
				primaryKey: collectionPrimaryKey,
				connectionPolicySetting: connectionPolicySetting
			);

			var cancellationTokenSource = new CancellationTokenSource();
			var cancellationToken = cancellationTokenSource.Token;

			var rate = 100;
			var delay = 1000;
			var stopwatch = new Stopwatch();
			while (keepRunning)
			{
				Console.WriteLine($"Running test with {rate} items. Delaying for {delay} ms.");

				stopwatch.Reset();
				stopwatch.Start();
				var random = new Random(Environment.TickCount);
				for (var i = 0; i < rate; i++)
				{
					var lastName = ObjectMother.LastNames[random.Next(0, ObjectMother.LastNames.Length - 1)];
					var firstName = ObjectMother.FirstNames[random.Next(0, ObjectMother.FirstNames.Length - 1)];
					var description = ObjectMother.Descriptions[random.Next(0, ObjectMother.Descriptions.Length - 1)];
					var title = ObjectMother.Titles[random.Next(0, ObjectMother.Titles.Length - 1)];

					var key = $"[{title}] {firstName} {lastName.Substring(0, 1).ToUpper()}{lastName.Substring(1)}";
					var person = new Person() {Id = key, Description = description, Title = title, Name = $"{firstName} {lastName}", PartitionKey = "range-0" };
					var value = person;

					try
					{
						await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collection),
							value,
							new RequestOptions { PartitionKey = new PartitionKey("range-0") });
					}
					catch (DocumentClientException dcex)
					{
						throw new StateSessionException($"SetValueAsync for {key} failed, {dcex.Message}", dcex);
					}
					catch (Exception ex)
					{
						throw new StateSessionException($"SetValueAsync for {key} failed, {ex.Message}", ex);
					}

					Console.Write(".");
				}

				Console.WriteLine();
				stopwatch.Stop();
				var average = (float)rate / (float)stopwatch.ElapsedMilliseconds;
				Console.WriteLine($"Done in {stopwatch.ElapsedMilliseconds} ms. {average}/ms");
				await Task.Delay(1000, cancellationToken);
				Console.WriteLine();

				rate = rate * 110 / 100;
			}
		}

		/// <summary>
		/// Direct DocDb no wrappers, single partition
		/// </summary>
		/// <returns></returns>
		public static async Task RunTest4()
		{
			var keepRunning = true;
			Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
				e.Cancel = true;
				keepRunning = false;
			};

			var settingsProvider = new SettingsProvider("dev-col-1");
			var factory = new CosmosDbClientFactory();
			var collection = settingsProvider.Collection();
			var databaseName = settingsProvider.DatabaseName();
			var endpointUri = settingsProvider.EndpointUri();
			var collectionPrimaryKey = settingsProvider.PrimaryKey();
			var connectionPolicySetting = ConnectionPolicySetting.GatewayHttps;
			var client = await factory.OpenAsync(
				databaseName: databaseName,
				collection: new CosmosDbCollectionDefinition(collection),
				endpointUri: new Uri(endpointUri),
				primaryKey: collectionPrimaryKey,
				connectionPolicySetting: connectionPolicySetting
			);

			var cancellationTokenSource = new CancellationTokenSource();
			var cancellationToken = cancellationTokenSource.Token;

			var rate = 100;
			var delay = 1000;
			var stopwatch = new Stopwatch();
			while (keepRunning)
			{
				Console.WriteLine($"Running test with {rate} items. Delaying for {delay} ms.");

				stopwatch.Reset();
				stopwatch.Start();
				var random = new Random(Environment.TickCount);
				for (var i = 0; i < rate; i++)
				{
					var lastName = ObjectMother.LastNames[random.Next(0, ObjectMother.LastNames.Length - 1)];
					var firstName = ObjectMother.FirstNames[random.Next(0, ObjectMother.FirstNames.Length - 1)];
					var description = ObjectMother.Descriptions[random.Next(0, ObjectMother.Descriptions.Length - 1)];
					var title = ObjectMother.Titles[random.Next(0, ObjectMother.Titles.Length - 1)];

					var key = $"[{title}] {firstName} {lastName.Substring(0, 1).ToUpper()}{lastName.Substring(1)}";
					var person = new Person() { Id = key, Description = description, Title = title, Name = $"{firstName} {lastName}", PartitionKey = "range-0" };
					var value = person;

					try
					{
						await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collection),
							value,
							new RequestOptions {  });
					}
					catch (DocumentClientException dcex)
					{
						throw new StateSessionException($"SetValueAsync for {key} failed, {dcex.Message}", dcex);
					}
					catch (Exception ex)
					{
						throw new StateSessionException($"SetValueAsync for {key} failed, {ex.Message}", ex);
					}

					Console.Write(".");
				}

				Console.WriteLine();
				stopwatch.Stop();
				var average = (float)rate / (float)stopwatch.ElapsedMilliseconds;
				Console.WriteLine($"Done in {stopwatch.ElapsedMilliseconds} ms. {average}/ms");
				await Task.Delay(1000, cancellationToken);
				Console.WriteLine();

				rate = rate * 110 / 100;
			}
		}

		/// <summary>
		/// Direct DocDb no wrappers, TCP Direct
		/// </summary>
		/// <returns></returns>
		public static async Task RunTest5()
		{
			var keepRunning = true;
			Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
				e.Cancel = true;
				keepRunning = false;
			};

			var settingsProvider = new SettingsProvider();
			var factory = new CosmosDbClientFactory();
			var collection = settingsProvider.Collection();
			var databaseName = settingsProvider.DatabaseName();
			var endpointUri = settingsProvider.EndpointUri();
			var collectionPrimaryKey = settingsProvider.PrimaryKey();
			var connectionPolicySetting = ConnectionPolicySetting.DirectTcp;
			var client = await factory.OpenAsync(
				databaseName: databaseName,
				collection: new CosmosDbCollectionDefinition(collection, $"/partitionKey"),
				endpointUri: new Uri(endpointUri),
				primaryKey: collectionPrimaryKey,
				connectionPolicySetting: connectionPolicySetting
			);

			var cancellationTokenSource = new CancellationTokenSource();
			var cancellationToken = cancellationTokenSource.Token;

			var rate = 100;
			var delay = 1000;
			var stopwatch = new Stopwatch();
			while (keepRunning)
			{
				Console.WriteLine($"Running test with {rate} items. Delaying for {delay} ms.");

				stopwatch.Reset();
				stopwatch.Start();
				var random = new Random(Environment.TickCount);
				for (var i = 0; i < rate; i++)
				{
					var lastName = ObjectMother.LastNames[random.Next(0, ObjectMother.LastNames.Length - 1)];
					var firstName = ObjectMother.FirstNames[random.Next(0, ObjectMother.FirstNames.Length - 1)];
					var description = ObjectMother.Descriptions[random.Next(0, ObjectMother.Descriptions.Length - 1)];
					var title = ObjectMother.Titles[random.Next(0, ObjectMother.Titles.Length - 1)];

					var key = $"[{title}] {firstName} {lastName.Substring(0, 1).ToUpper()}{lastName.Substring(1)}";
					var person = new Person() { Id = key, Description = description, Title = title, Name = $"{firstName} {lastName}", PartitionKey = "range-0" };
					var value = person;

					try
					{
						await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collection),
							value,
							new RequestOptions { PartitionKey = new PartitionKey("range-0") });
					}
					catch (DocumentClientException dcex)
					{
						throw new StateSessionException($"SetValueAsync for {key} failed, {dcex.Message}", dcex);
					}
					catch (Exception ex)
					{
						throw new StateSessionException($"SetValueAsync for {key} failed, {ex.Message}", ex);
					}

					Console.Write(".");
				}

				Console.WriteLine();
				stopwatch.Stop();
				var average = (float)stopwatch.ElapsedMilliseconds / (float)rate;
				Console.WriteLine($"Done in {stopwatch.ElapsedMilliseconds} ms. {average} ms/item");
				await Task.Delay(1000, cancellationToken);
				Console.WriteLine();

				rate = rate * 110 / 100;
			}
		}
	}

	public class Person
	{
		[JsonProperty("partitionKey")]
		public string PartitionKey { get; set; }
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Title { get; set; }
	}

	public class SettingsProvider : ISettingsProvider
	{
		private readonly IDictionary<string, string> _settings;

		public SettingsProvider(string collection = "dev-col-0")
		{
			_settings = new Dictionary<string, string>()
			{
				{$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyEndpointUri}" , "https://ce-labs-dev.documents.azure.com:443/"/* "https://172.27.88.224:8081/"*/},
				{$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyPrimaryKey}" ,  "F3YcC30pRvxLfp8pVebpfZQN4C4BdRB1ppJ0tiCt4clKwuMyS6cJ7M1XReCa4EJOGuaj8Kt6wwtdt5H6I0gt9Q=="/*"C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="*/},
				{$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyDatabaseName}" ,  "dummy"},
				{$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyCollection}" , collection},
			};
		}

		public bool Contains(string key) { return _settings.ContainsKey(key); }

		public string this[string key] => _settings.ContainsKey(key) ? _settings[key] : string.Empty;

		public string[] Keys => _settings.Keys.ToArray();
	}

	public static class ObjectMother
	{
		public static string[] Titles = new string[]
		{
			"Doctor",
			"Overlord",
			"Mister",
			"Fraulein",
		};
		public static string[] LastNames => new string[] { "albattani", "allen", "almeida", "agnesi", "archimedes", "ardinghelli", "aryabhata", "austin", "babbage", "banach", "bardeen", "bartik", "bassi", "beaver", "bell", "bhabha", "bhaskara", "blackwell", "bohr", "booth", "borg", "bose", "boyd", "brahmagupta", "brattain", "brown", "carson", "chandrasekhar", "shannon", "clarke", "colden", "cori", "cray", "curran", "curie", "darwin", "davinci", "dijkstra", "dubinsky", "easley", "edison", "einstein", "elion", "engelbart", "euclid", "euler", "fermat", "fermi", "feynman", "franklin", "galileo", "gates", "goldberg", "goldstine", "goldwasser", "golick", "goodall", "haibt", "hamilton", "hawking", "heisenberg", "heyrovsky", "hodgkin", "hoover", "hopper", "hugle", "hypatia", "jang", "jennings", "jepsen", "joliot", "jones", "kalam", "kare", "keller", "khorana", "kilby", "kirch", "knuth", "kowalevski", "lalande", "lamarr", "lamport", "leakey", "leavitt", "lewin", "lichterman", "liskov", "lovelace", "lumiere", "mahavira", "mayer", "mccarthy", "mcclintock", "mclean", "mcnulty", "meitner", "meninsky", "mestorf", "minsky", "mirzakhani", "morse", "murdock", "newton", "nightingale", "nobel", "noether", "northcutt", "noyce", "panini", "pare", "pasteur", "payne", "perlman", "pike", "poincare", "poitras", "ptolemy", "raman", "ramanujan", "ride", "montalcini", "ritchie", "roentgen", "rosalind", "saha", "sammet", "shaw", "shirley", "shockley", "sinoussi", "snyder", "spence", "stallman", "stonebraker", "swanson", "swartz", "swirles", "tesla", "thompson", "torvalds", "turing", "varahamihira", "visvesvaraya", "volhard", "wescoff", "wiles", "williams", "wilson", "wing", "wozniak", "wright", "yalow", "yonath" };

		public static string[] Descriptions => new string[]
		{
			"Muhammad ibn Jābir al-Ḥarrānī al-Battānī was a founding father of astronomy. ",
			" Frances E. Allen, became the first female IBM Fellow in 1989. In 2006, she became the first female recipient of the ACM's Turing Award. ",
			" June Almeida - Scottish virologist who took the first pictures of the rubella virus - ",
			" Maria Gaetana Agnesi - Italian mathematician, philosopher, theologian and humanitarian. She was the first woman to write a mathematics handbook and the first woman appointed as a Mathematics Professor at a University. ",
			" Archimedes was a physicist, engineer and mathematician who invented too many things to list them here. ",
			" Maria Ardinghelli - Italian translator, mathematician and physicist - ",
			" Aryabhata - Ancient Indian mathematician-astronomer during 476-550 CE ",
			" Wanda Austin - Wanda Austin is the President and CEO of The Aerospace Corporation, a leading architect for the US security space programs. ",
			" Charles Babbage invented the concept of a programmable computer. ",
			" Stefan Banach - Polish mathematician, was one of the founders of modern functional analysis. ",
			" John Bardeen co-invented the transistor - ",
			" Jean Bartik, born Betty Jean Jennings, was one of the original programmers for the ENIAC computer. ",
			" Laura Bassi, the world's first female professor ",
			" Hugh Beaver, British engineer, founder of the Guinness Book of World Records ",
			" Alexander Graham Bell - an eminent Scottish-born scientist, inventor, engineer and innovator who is credited with inventing the first practical telephone - ",
			" Homi J Bhabha - was an Indian nuclear physicist, founding director, and professor of physics at the Tata Institute of Fundamental Research. Colloquially known as 'father of Indian nuclear programme'- ",
			" Bhaskara II - Ancient Indian mathematician-astronomer whose work on calculus predates Newton and Leibniz by over half a millennium - ",
			" Elizabeth Blackwell - American doctor and first American woman to receive a medical degree - ",
			" Niels Bohr is the father of quantum theory. ",
			" Kathleen Booth, she's credited with writing the first assembly language. ",
			" Anita Borg - Anita Borg was the founding director of the Institute for Women and Technology (IWT). ",
			" Satyendra Nath Bose - He provided the foundation for Bose–Einstein statistics and the theory of the Bose–Einstein condensate. - ",
			" Evelyn Boyd Granville - She was one of the first African-American woman to receive a Ph.D. in mathematics; she earned it in 1949 from Yale University. ",
			" Brahmagupta - Ancient Indian mathematician during 598-670 CE who gave rules to compute with zero - ",
			" Walter Houser Brattain co-invented the transistor - ", " Emmett Brown invented time travel. ",
			" Rachel Carson - American marine biologist and conservationist, her book Silent Spring and other writings are credited with advancing the global environmental movement. ",
			" Subrahmanyan Chandrasekhar - Astrophysicist known for his mathematical theory on different stages and evolution in structures of the stars. He has won nobel prize for physics - ",
			"Claude Shannon - The father of information theory and founder of digital circuit design theory. (",
			" Joan Clarke - Bletchley Park code breaker during the Second World War who pioneered techniques that remained top secret for decades. Also an accomplished numismatist ",
			" Jane Colden - American botanist widely considered the first female American botanist - ",
			" Gerty Theresa Cori - American biochemist who became the third woman—and first American woman—to win a Nobel Prize in science, and the first woman to be awarded the Nobel Prize in Physiology or Medicine. Cori was born in Prague. ",
			" Seymour Roger Cray was an American electrical engineer and supercomputer architect who designed a series of computers that were the fastest in the world for decades. ",
			" Samuel Curran was an Irish physicist who worked alongside his wife during WWII and invented the proximity fuse. ",
			" Marie Curie discovered radioactivity. ", " Charles Darwin established the principles of natural evolution. ",
			" Leonardo Da Vinci invented too many things to list here. ",
			" Edsger Wybe Dijkstra was a Dutch computer scientist and mathematical scientist. ",
			" Donna Dubinsky - played an integral role in the development of personal digital assistants (PDAs) serving as CEO of Palm, Inc. and co-founding Handspring. ",
			" Annie Easley - She was a leading member of the team which developed software for the Centaur rocket stage and one of the first African-Americans in her field. ",
			" Thomas Alva Edison, prolific inventor ", " Albert Einstein invented the general theory of relativity. ",
			" Gertrude Elion - American biochemist, pharmacologist and the 1988 recipient of the Nobel Prize in Medicine - ",
			" Douglas Engelbart gave the mother of all demos: ", " Euclid invented geometry. ",
			" Leonhard Euler invented large parts of modern mathematics. ",
			" Pierre de Fermat pioneered several aspects of modern mathematics. ",
			" Enrico Fermi invented the first nuclear reactor. ",
			" Richard Feynman was a key contributor to quantum mechanics and particle physics. ",
			"Benjamin Franklin is famous for his experiments in electricity and the invention of the lightning rod.",
			" Galileo was a founding father of modern astronomy, and faced politics and obscurantism to establish scientific truth.  ",
			" William Henry 'Bill' Gates III is an American business magnate, philanthropist, investor, computer programmer, and inventor. ",
			" Adele Goldberg, was one of the designers and developers of the Smalltalk language. ",
			" Adele Goldstine, born Adele Katz, wrote the complete technical description for the first electronic digital computer, ENIAC. ",
			" Shafi Goldwasser is a computer scientist known for creating theoretical foundations of modern cryptography. Winner of 2012 ACM Turing Award. ",
			"James Golick, all around gangster.",
			" Jane Goodall - British primatologist, ethologist, and anthropologist who is considered to be the world's foremost expert on chimpanzees - ",
			" Lois Haibt - American computer scientist, part of the team at IBM that developed FORTRAN - ",
			" Margaret Hamilton - Director of the Software Engineering Division of the MIT Instrumentation Laboratory, which developed on-board flight software for the Apollo space program. ",
			" Stephen Hawking pioneered the field of cosmology by combining general relativity and quantum mechanics. ",
			" Werner Heisenberg was a founding father of quantum mechanics. ",
			" Jaroslav Heyrovský was the inventor of the polarographic method, father of the electroanalytical method, and recipient of the Nobel Prize in 1959. His main field of work was polarography. ",
			" Dorothy Hodgkin was a British biochemist, credited with the development of protein crystallography. She was awarded the Nobel Prize in Chemistry in 1964. ",
			" Erna Schneider Hoover revolutionized modern communication by inventing a computerized telephone switching method. ",
			" Grace Hopper developed the first compiler for a computer programming language and  is credited with popularizing the term 'debugging' for fixing computer glitches. ",
			" Frances Hugle, she was an American scientist, engineer, and inventor who contributed to the understanding of semiconductors, integrated circuitry, and the unique electrical principles of microscopic materials. ",
			" Hypatia - Greek Alexandrine Neoplatonist philosopher in Egypt who was one of the earliest mothers of mathematics - ",
			" Yeong-Sil Jang was a Korean scientist and astronomer during the Joseon Dynasty; he invented the first metal printing press and water gauge. ",
			" Betty Jennings - one of the original programmers of the ENIAC. https://en.wikipedia.org/wiki/ENIAC - ",
			" Mary Lou Jepsen, was the founder and chief technology officer of One Laptop Per Child (OLPC), and the founder of Pixel Qi. ",
			" Irène Joliot-Curie - French scientist who was awarded the Nobel Prize for Chemistry in 1935. Daughter of Marie and Pierre Curie. ",
			" Karen Spärck Jones came up with the concept of inverse document frequency, which is used in most search engines today. ",
			" A. P. J. Abdul Kalam - is an Indian scientist aka Missile Man of India for his work on the development of ballistic missile and launch vehicle technology - ",
			" Susan Kare, created the icons and many of the interface elements for the original Apple Macintosh in the 1980s, and was an original employee of NeXT, working as the Creative Director. ",
			" Mary Kenneth Keller, Sister Mary Kenneth Keller became the first American woman to earn a PhD in Computer Science in 1965. ",
			" Har Gobind Khorana - Indian-American biochemist who shared the 1968 Nobel Prize for Physiology - ",
			" Jack Kilby invented silicone integrated circuits and gave Silicon Valley its name. - ",
			" Maria Kirch - German astronomer and first woman to discover a comet - ",
			" Donald Knuth - American computer scientist, author of 'The Art of Computer Programming' and creator of the TeX typesetting system. ",
			" Sophie Kowalevski - Russian mathematician responsible for important original contributions to analysis, differential equations and mechanics - ",
			" Marie-Jeanne de Lalande - French astronomer, mathematician and cataloguer of stars - ",
			" Hedy Lamarr - Actress and inventor. The principles of her work are now incorporated into modern Wi-Fi, CDMA and Bluetooth technology. ",
			" Leslie B. Lamport - American computer scientist. Lamport is best known for his seminal work in distributed systems and was the winner of the 2013 Turing Award. ",
			" Mary Leakey - British paleoanthropologist who discovered the first fossilized Proconsul skull - ",
			" Henrietta Swan Leavitt - she was an American astronomer who discovered the relation between the luminosity and the period of Cepheid variable stars. ",
			"Daniel Lewin -  Mathematician, Akamai co-founder, soldier, 9/11 victim-- Developed optimization techniques for routing traffic on the internet. Died attempting to stop the 9-11 hijackers. ",
			" Ruth Lichterman - one of the original programmers of the ENIAC. https://en.wikipedia.org/wiki/ENIAC - ",
			" Barbara Liskov - co-developed the Liskov substitution principle. Liskov was also the winner of the Turing Prize in 2008. - ",
			" Ada Lovelace invented the first algorithm. ", " Auguste and Louis Lumière - the first filmmakers in history - ",
			" Mahavira - Ancient Indian mathematician during 9th century AD who discovered basic algebraic identities - ",
			" Maria Mayer - American theoretical physicist and Nobel laureate in Physics for proposing the nuclear shell model of the atomic nucleus - ",
			" John McCarthy invented LISP: ",
			" Barbara McClintock - a distinguished American cytogeneticist, 1983 Nobel Laureate in Physiology or Medicine for discovering transposons. ",
			" Malcolm McLean invented the modern shipping container: ",
			" Kay McNulty - one of the original programmers of the ENIAC. https://en.wikipedia.org/wiki/ENIAC - ",
			" Lise Meitner - Austrian/Swedish physicist who was involved in the discovery of nuclear fission. The element meitnerium is named after her - ",
			" Carla Meninsky, was the game designer and programmer for Atari 2600 games Dodge 'Em and Warlords. ",
			" Johanna Mestorf - German prehistoric archaeologist and first female museum director in Germany - ",
			" Marvin Minsky - Pioneer in Artificial Intelligence, co-founder of the MIT's AI Lab, won the Turing Award in 1969. ",
			" Maryam Mirzakhani - an Iranian mathematician and the first woman to win the Fields Medal. ",
			" Samuel Morse - contributed to the invention of a single-wire telegraph system based on European telegraphs and was a co-developer of the Morse code - ",
			" Ian Murdock - founder of the Debian project - ", " Isaac Newton invented classic mechanics and modern optics. ",
			" Florence Nightingale, more prominently known as a nurse, was also the first female member of the Royal Statistical Society and a pioneer in statistical graphics ",
			" Alfred Nobel - a Swedish chemist, engineer, innovator, and armaments manufacturer (inventor of dynamite) - ",
			" Emmy Noether, German mathematician. Noether's Theorem is named after her. ",
			"Poppy Northcutt. Poppy Northcutt was the first woman to work as part of NASA’s Mission Control.",
			" Robert Noyce invented silicone integrated circuits and gave Silicon Valley its name. - ",
			" Panini - Ancient Indian linguist and grammarian from 4th century CE who worked on the world's first formal system - ",
			" Ambroise Pare invented modern surgery. ",
			" Louis Pasteur discovered vaccination, fermentation and pasteurization. ",
			" Cecilia Payne-Gaposchkin was an astronomer and astrophysicist who, in 1925, proposed in her Ph.D. thesis an explanation for the composition of stars in terms of the relative abundances of hydrogen and helium. ",
			" Radia Perlman is a software designer and network engineer and most famous for her invention of the spanning-tree protocol (STP). ",
			" Rob Pike was a key contributor to Unix, Plan 9, the X graphic system, utf-8, and the Go programming language. ",
			" Henri Poincaré made fundamental contributions in several fields of mathematics. ",
			" Laura Poitras is a director and producer whose work, made possible by open source crypto tools, advances the causes of truth and freedom of information by reporting disclosures by whistleblowers such as Edward Snowden. ",
			" Claudius Ptolemy - a Greco-Egyptian writer of Alexandria, known as a mathematician, astronomer, geographer, astrologer, and poet of a single epigram in the Greek Anthology - ",
			" C. V. Raman - Indian physicist who won the Nobel Prize in 1930 for proposing the Raman effect. - ",
			" Srinivasa Ramanujan - Indian mathematician and autodidact who made extraordinary contributions to mathematical analysis, number theory, infinite series, and continued fractions. - ",
			" Sally Kristen Ride was an American physicist and astronaut. She was the first American woman in space, and the youngest American astronaut. ",
			" Rita Levi-Montalcini - Won Nobel Prize in Physiology or Medicine jointly with colleague Stanley Cohen for the discovery of nerve growth factor (",
			" Dennis Ritchie - co-creator of UNIX and the C programming language. - ",
			" Wilhelm Conrad Röntgen - German physicist who was awarded the first Nobel Prize in Physics in 1901 for the discovery of X-rays (Röntgen rays). ",
			" Rosalind Franklin - British biophysicist and X-ray crystallographer whose research was critical to the understanding of DNA - ",
			" Meghnad Saha - Indian astrophysicist best known for his development of the Saha equation, used to describe chemical and physical conditions in stars - ",
			" Jean E. Sammet developed FORMAC, the first widely used computer language for symbolic manipulation of mathematical formulas. ",
			" Carol Shaw - Originally an Atari employee, Carol Shaw is said to be the first female video game designer. ",
			" Dame Stephanie 'Steve' Shirley - Founded a software company in 1962 employing women working from home. ",
			" William Shockley co-invented the transistor - ",
			" Françoise Barré-Sinoussi - French virologist and Nobel Prize Laureate in Physiology or Medicine; her work was fundamental in identifying HIV as the cause of AIDS. ",
			" Betty Snyder - one of the original programmers of the ENIAC. https://en.wikipedia.org/wiki/ENIAC - ",
			" Frances Spence - one of the original programmers of the ENIAC. https://en.wikipedia.org/wiki/ENIAC - ",
			" Richard Matthew Stallman - the founder of the Free Software movement, the GNU project, the Free Software Foundation, and the League for Programming Freedom. He also invented the concept of copyleft to protect the ideals of this movement, and enshrined this concept in the widely-used GPL (General Public License) for software. ",
			" Michael Stonebraker is a database research pioneer and architect of Ingres, Postgres, VoltDB and SciDB. Winner of 2014 ACM Turing Award. ",
			" Janese Swanson (with others) developed the first of the Carmen Sandiego games. She went on to found Girl Tech. ",
			" Aaron Swartz was influential in creating RSS, Markdown, Creative Commons, Reddit, and much of the internet as we know it today. He was devoted to freedom of information on the web. ",
			" Bertha Swirles was a theoretical physicist who made a number of contributions to early quantum theory. ",
			" Nikola Tesla invented the AC electric system and every gadget ever used by a James Bond villain. ",
			" Ken Thompson - co-creator of UNIX and the C programming language - ", " Linus Torvalds invented Linux and Git. ",
			" Alan Turing was a founding father of computer science. ",
			" Varahamihira - Ancient Indian mathematician who discovered trigonometric formulae during 505-587 CE - ",
			" Sir Mokshagundam Visvesvaraya - is a notable Indian engineer.  He is a recipient of the Indian Republic's highest honour, the Bharat Ratna, in 1955. On his birthday, 15 September is celebrated as Engineer's Day in India in his memory - ",
			" Christiane Nüsslein-Volhard - German biologist, won Nobel Prize in Physiology or Medicine in 1995 for research on the genetic control of embryonic development. ",
			" Marlyn Wescoff - one of the original programmers of the ENIAC. https://en.wikipedia.org/wiki/ENIAC - ",
			" Andrew Wiles - Notable British mathematician who proved the enigmatic Fermat's Last Theorem - ",
			" Roberta Williams, did pioneering work in graphical adventure games for personal computers, particularly the King's Quest series. ",
			" Sophie Wilson designed the first Acorn Micro-Computer and the instruction set for ARM processors. ",
			" Jeannette Wing - co-developed the Liskov substitution principle. - ",
			" Steve Wozniak invented the Apple I and Apple II. ",
			" The Wright brothers, Orville and Wilbur - credited with inventing and building the world's first successful airplane and making the first controlled, powered and sustained heavier-than-air human flight - ",
			" Rosalyn Sussman Yalow - Rosalyn Sussman Yalow was an American medical physicist, and a co-winner of the 1977 Nobel Prize in Physiology or Medicine for development of the radioimmunoassay technique. ",
			" Ada Yonath - an Israeli crystallographer, the first woman from the Middle East to win a Nobel prize in the sciences. "
		};
		public static string[] FirstNames => new string[]
		{
			"Carley",
			"Bradley",
			"Harriette",
			"Rosalia",
			"Erline",
			"Todd",
			"Vesta",
			"Ivey",
			"Kori",
			"Harlan",
			"Ernestina",
			"Shelba",
			"Tommy",
			"Bari",
			"Delila",
			"Hosea",
			"Imogene",
			"Marsha",
			"Craig",
			"Mayola",
			"Carli",
			"Jacelyn",
			"Aleta",
			"Jeanice",
			"Ignacio",
			"Murray",
			"George",
			"Missy",
			"Lang",
			"Elvina",
			"Arron",
			"Jene",
			"Fallon",
			"Kaitlin",
			"Joshua",
			"Germaine",
			"Vickey",
			"Winifred",
			"Jenelle",
			"Lora",
			"Daina",
			"Josef",
			"Rosie",
			"Jeanie",
			"Opal",
			"Petrina",
			"Heide",
			"Felicita",
			"Alfredia",
			"Sherlyn",
			"Sherill",
			"Leland",
			"Genevieve",
			"Emery",
			"Chante",
			"Aiko",
			"Grisel",
			"Erinn",
			"Hermelinda",
			"Toi",
			"Winona",
			"Florentino",
			"Lai",
			"Santina",
			"Darryl",
			"Arleen",
			"Chau",
			"Yoko",
			"Fletcher",
			"Loni",
			"Georgiann",
			"Sima",
			"Kenton",
			"Ninfa",
			"Shawna",
			"Ophelia",
			"Cecil",
			"Zoraida",
			"Yessenia",
			"Dolores",
			"Kalyn",
			"Marth",
			"Desiree",
			"Charles",
			"Christal",
			"Jacquelyn",
			"Lavina",
			"Patty",
			"Denese",
			"Latrice",
			"Nancy",
			"Hazel",
			"Violette",
			"Lakeisha",
			"Alma",
			"Vikki",
			"Shaunna",
			"Noma",
			"Rhett",
			"Kai",
			"Kimbra",
			"Hedy",
			"Kanesha",
			"Teresia",
			"Cristy",
			"Miyoko",
			"Warner",
			"Darwin",
			"Valrie",
			"Beckie",
			"Benito",
			"Marth",
			"Stasia",
			"Patrick",
			"Valeria",
			"Yun",
			"Alexia",
			"Daniele",
			"Hettie",
			"Ivette",
			"Elinor",
			"Migdalia",
			"Ginette",
			"Hilary",
			"Ariane",
			"Yung",
			"Penney",
			"Ethyl",
			"Milda",
			"Devin",
			"Nelda",
			"Carli",
			"Marchelle",
			"Heidi",
			"Lennie",
			"Lyn",
			"Cinthia",
			"Maile",
			"Lina",
			"Iesha",
			"Lakiesha",
			"Alysha",
			"Lara",
			"Donya",
			"Raymundo",
			"Tequila",
			"Valentina",
			"Stephenie",
			"Rhoda",
			"Catarina",
			"Concha",
			"Melba",
			"Grazyna",
			"Lavonna",
			"Whitney",
			"Crissy",
			"Lakisha",
			"Idalia",
			"Nancey",
			"Aurore",
			"Janna",
			"Louise",
			"Larraine",
			"Evon",
			"Kristle",
			"Coleman",
			"Joanie",
			"Alida",
			"Arlen",
			"Chu",
			"Bertie",
			"Eliana",
			"Katharyn",
			"Kimberlie",
			"Deandra",
			"Coretta",
			"Karan",
			"Bruno",
			"Vesta",
			"Kathe",
			"Qiana",
			"Melodie",
			"Shyla",
			"Wiley",
			"Malka",
			"Emile",
			"Ervin",
			"Neda",
			"Ashlea",
			"Latricia",
			"Jessia",
			"Dena",
			"Adelle",
			"Taisha",
			"Rachael",
			"Myrl",
			"Opal",
			"Leonie",
			"Kandy",
			"Jeffry"
		};
	}
}
