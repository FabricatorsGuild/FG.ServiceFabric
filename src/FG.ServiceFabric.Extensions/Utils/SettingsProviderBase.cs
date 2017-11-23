using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;

namespace FG.ServiceFabric.Utils
{
	public abstract class SettingsProviderBase : ISettingsProvider
	{
		private readonly ServiceContext _context;
		private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

		protected SettingsProviderBase(ServiceContext context)
		{
			_context = context;
		}

		public string this[string key]
		{
			get
			{
				if (!_values.TryGetValue(key, out string value))
				{
					throw new IndexOutOfRangeException(
						$"Setting not found. Check your service configuration, configuration overloads and make sure to configure your {nameof(GetType)} with a setting named {key}.");
				}

				return value;
			}
		}

		public string[] Keys => _values.Keys.ToArray();

		public bool Contains(string key)
		{
			return _values.ContainsKey(key);
		}

		protected RegistrationBuilder Configure()
		{
			return new RegistrationBuilder(this);
		}

		private IEnumerable<KeyValuePair<string, string>> GetValuesFromSettingsFile(string section)
		{
			var settingsFile = _context.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings;
			if (settingsFile.Sections.Contains(section))
			{
				var configSection = settingsFile.Sections[section];
				foreach (var parameter in configSection.Parameters)
				{
					yield return new KeyValuePair<string, string>(parameter.Name, parameter.Value);
				}
			}
		}

		private string GetValueFromSettingsFile(string section, string key)
		{
			var settingsFile = _context.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings;
			if (settingsFile.Sections.Contains(section))
			{
				var configSection = settingsFile.Sections[section];
				if (configSection.Parameters.Contains(key))
				{
					return configSection.Parameters[key].Value;
				}
			}

			throw new ArgumentException($"Key {key} not found in section {section}.");
		}

		public ISettingsProvider With(ISettingsProvider combine)
		{
			foreach (var key in combine.Keys)
			{
				this._values[key] = combine[key];
			}
			return this;
		}

		public class RegistrationBuilder
		{
			private readonly SettingsProviderBase _settingsProvider;

			public RegistrationBuilder(SettingsProviderBase settingsProvider)
			{
				_settingsProvider = settingsProvider;
			}

			public RegistrationBuilder FromSettings(IEnumerable<string> sections, KeyNameBuilder keyNameBuilder = null)
			{
				foreach (var section in sections)
				{
					FromSettings(section, keyNameBuilder);
				}
				return this;
			}

			public RegistrationBuilder FromSettings(string section, KeyNameBuilder keyNameBuilder = null)
			{
				var parameters = _settingsProvider.GetValuesFromSettingsFile(section);
				foreach (var parameter in parameters)
				{
					var namedKey = (keyNameBuilder ?? KeyNameBuilder.KeyNameOnly).GetKeyName(section, parameter.Key);
					_settingsProvider._values.Add(namedKey, parameter.Value);
				}

				return this;
			}

			public RegistrationBuilder FromSettings(string section, string key, KeyNameBuilder keyNameBuilder = null)
			{
				var namedKey = (keyNameBuilder ?? KeyNameBuilder.KeyNameOnly).GetKeyName(section, key);
				_settingsProvider._values.Add(namedKey, _settingsProvider.GetValueFromSettingsFile(section, key));
				return this;
			}			

			public class KeyNameBuilder
			{
				private readonly Func<string, string, string> _builder;

				protected KeyNameBuilder(Func<string, string, string> builder)
				{
					this._builder = builder;
				}

				public static KeyNameBuilder Default => KeyNameOnly;
				public static KeyNameBuilder SectionAndKeyName { get; } = new KeyNameBuilder((section, key) => $"{section}.{key}");

				public static KeyNameBuilder KeyNameOnly { get; } = new KeyNameBuilder((section, key) => key);

				public string GetKeyName(string section, string key)
				{
					return _builder?.Invoke(section, key) ?? key;
				}
			}
		}
	}
}