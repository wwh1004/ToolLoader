using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System;

static class CommandLine {
	public static T? Parse<T>(string[] args, out bool showUsage) where T : class, new() {
		if (args is null)
			throw new ArgumentNullException(nameof(args));

		if (!TryParse(args, out T? result, out showUsage) && !showUsage)
			throw new FormatException($"Invalid {nameof(args)} or generic argument {typeof(T)}");
		if (showUsage && !ShowUsage<T>())
			throw new FormatException($"Can't generate usage for {typeof(T)}");
		return result;
	}

	public static bool TryParse<T>(string[] args, out T? result, out bool showUsage) where T : class, new() {
		if (args is null) {
			Console.WriteLine($"Parameter '{nameof(args)}' is null");
			goto fail;
		}
		if (args.Any(t => t is null)) {
			Console.WriteLine($"Contains arg in '{nameof(args)}' is null");
			goto fail;
		}
		if (!TryGetOptionInfos(typeof(T), out var optionInfos, out var defaultOptionInfo, out var childOptionsProperty, out var childTypes)) {
			goto fail;
		}

		showUsage = false;
		for (int i = 0; i < args.Length; i++) {
			var arg = args[i].Trim();
			if (arg.StartsWith("-", StringComparison.Ordinal))
				arg = arg.TrimStart('-');
			else if (arg.StartsWith("/", StringComparison.Ordinal))
				arg = arg.Substring(1);
			else
				continue;

			if (!string.Equals(arg, "h", StringComparison.OrdinalIgnoreCase) && !string.Equals(arg, "help", StringComparison.OrdinalIgnoreCase))
				continue;

			result = null;
			showUsage = true;
			return false;
		}

		try {
			result = new T();
		}
		catch (Exception ex) {
			Console.WriteLine($"Can't create instance of type '{typeof(T)}': {ex.Message}");
			goto fail;
		}

		var instances = new Dictionary<Type, object> {
			[typeof(T)] = result
		};
		IDictionary<Type, object>? childOptionsMap = null;
		if (childOptionsProperty is not null) {
			try {
				childOptionsMap = (IDictionary<Type, object>?)childOptionsProperty.GetValue(result, null);
				if (childOptionsMap is null)
					throw new NullReferenceException();
			}
			catch (Exception ex) {
				Console.WriteLine($"Can't get value of property '{childOptionsProperty}': {ex.Message}");
				goto fail;
			}
			foreach (var childType in childTypes) {
				object? instance;
				try {
					instance = Activator.CreateInstance(childType);
					if (instance is null)
						throw new NullReferenceException();
					childOptionsMap.Add(childType, instance);
					instances.Add(childType, instance);
				}
				catch (Exception ex) {
					Console.WriteLine($"Can't create instance of type '{childType}': {ex.Message}");
					goto fail;
				}
			}
		}

		for (int i = 0; i < args.Length; i++) {
			if (!optionInfos.TryGetValue(args[i], out var optionInfo)) {
				if (defaultOptionInfo is not null && !defaultOptionInfo.HasSetValue) {
					// default option
					if (!defaultOptionInfo.TrySetValue(instances, args[i]))
						goto fail;
					continue;
				}
				else {
					// invalid option name
					Console.WriteLine($"'{args[i]}' is not a valid option");
					goto fail;
				}
			}

			if (optionInfo.IsBoolean) {
				// bool type, don't need other checks, set true
				if (!optionInfo.TrySetValue(instances, true))
					goto fail;
				continue;
			}

			if (i == args.Length - 1) {
				// no value is provided for option
				Console.WriteLine($"No value is provided for {OptionNameOrDefault(optionInfo.Option)}");
				goto fail;
			}

			if (!optionInfo.TrySetValue(instances, args[++i])) {
				goto fail;
			}
		}

		foreach (var optionInfo in optionInfos.Values) {
			if (optionInfo.HasSetValue)
				continue;
			// option value has been set

			if (optionInfo.IsRequired) {
				// required option
				Console.WriteLine($"Required {OptionNameOrDefault(optionInfo.Option)} must have a value");
				goto fail;
			}
			else if (optionInfo.IsBoolean) {
				// bool option
				if (!optionInfo.TrySetValue(instances, false))
					goto fail;
			}
			else {
				// optional option
				if (!optionInfo.TrySetValue(instances, optionInfo.DefaultValue))
					goto fail;
			}
		}
		return true;

	fail:
		result = null;
		showUsage = false;
		return false;
	}

	static bool TryGetOptionInfos(Type type, [NotNullWhen(true)] out Dictionary<string, OptionInfo>? optionInfos, out OptionInfo? defaultOptionInfo, out PropertyInfo? childOptionsProperty, [NotNullWhen(true)] out List<Type>? childTypes) {
		if (!TryGetOptionInfosCore(type, false, out optionInfos, out defaultOptionInfo))
			goto fail;

		childTypes = new List<Type>();
		childOptionsProperty = type.GetProperty("ChildOptions", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		if (childOptionsProperty is null || childOptionsProperty.PropertyType != typeof(IDictionary<Type, object>) || !childOptionsProperty.CanRead)
			return true;

		foreach (var childType in type.Module.GetTypes()) {
			var childOptionsAttrs = (ChildOptionsAttribute[])childType.GetCustomAttributes(typeof(ChildOptionsAttribute), false);
			if (childOptionsAttrs is null || childOptionsAttrs.Length == 0)
				continue;

			var childOptionsAttr = childOptionsAttrs.FirstOrDefault(t => t.Parent == type);
			if (childOptionsAttr is null)
				continue;

			if (!IsValidOptionName(childOptionsAttr.Prefix, out char c)) {
				// check child option prefix
				Console.WriteLine($"Invalid name char '{c}' in child option prefix '{childOptionsAttr.Prefix}'");
				goto fail;
			}

			if (!TryGetOptionInfosCore(childType, true, out var childOptionInfos, out _)) {
				Console.WriteLine($"Can't get option infos for child options type '{childType}'");
				goto fail;
			}

			foreach (var childOptionInfo in childOptionInfos.Values) {
				childOptionInfo.Prefix = childOptionsAttr.Prefix;
				if (optionInfos.ContainsKey(childOptionInfo.Name)) {
					Console.WriteLine($"Duplicated option name '{childOptionInfo.Name}' in type '{type}' and '{childType}'");
					goto fail;
				}
				optionInfos.Add(childOptionInfo.Name, childOptionInfo);
			}
			childTypes.Add(childType);
		}
		return true;

	fail:
		optionInfos = null;
		defaultOptionInfo = null;
		childOptionsProperty = null;
		childTypes = null;
		return false;
	}

	static bool TryGetOptionInfosCore(Type type, bool isChild, [NotNullWhen(true)] out Dictionary<string, OptionInfo>? optionInfos, out OptionInfo? defaultOptionInfo) {
		optionInfos = new Dictionary<string, OptionInfo>(StringComparer.OrdinalIgnoreCase);
		defaultOptionInfo = null;
		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		foreach (var property in properties) {
			if (!TryGetOptionAttribute(property, isChild, out var option, out bool isExcluded))
				goto fail;
			if (isExcluded)
				continue;

			var optionInfo = new OptionInfo(option!, type, property);
			if (optionInfos.ContainsKey(optionInfo.Name)) {
				Console.WriteLine($"Duplicated option name '{optionInfo.Name}' in type '{type}'");
				goto fail;
			}

			if (!optionInfo.IsDefault) {
				optionInfos.Add(optionInfo.Name, optionInfo);
			}
			else if (defaultOptionInfo is null) {
				defaultOptionInfo = optionInfo;
			}
			else {
				Console.WriteLine($"Both property '{defaultOptionInfo.Property.Name}' and property '{property.Name}' are default options");
				goto fail;
			}
		}
		return true;

	fail:
		optionInfos = null;
		defaultOptionInfo = null;
		return false;
	}

	static bool TryGetOptionAttribute(PropertyInfo property, bool isChild, out OptionAttribute? option, out bool isExcluded) {
		option = null;
		var options = property.GetCustomAttributes(typeof(OptionAttribute), false);
		if (options is null || options.Length == 0) {
			// exclude properties without OptionAttribute
			isExcluded = true;
			return true;
		}
		isExcluded = false;
		if (options.Length != 1) {
			// OptionAttribute shouldn't be applied more than one times
			Console.WriteLine($"Duplicated '{nameof(OptionAttribute)}' in property '{property.Name}'");
			goto fail;
		}
		var optionType = property.PropertyType;
		if (!IsSupportedType(optionType)) {
			// check option type
			Console.WriteLine($"Unsupported type '{optionType}' in property '{property.Name}'");
			goto fail;
		}
		option = (OptionAttribute)options[0];
		if (isChild && option.IsDefault) {
			// don't allow default option in child options
			Console.WriteLine($"Property '{property.Name}' is default option but default option is not allowed in type with '{nameof(ChildOptionsAttribute)}'");
			goto fail;
		}
		if (option.IsDefault && optionType == typeof(bool)) {
			// default option shouldn't bool type
			Console.WriteLine($"Property '{property.Name}' is default option but option type is '{typeof(bool)}'");
			goto fail;
		}
		if (!IsValidOptionName(option.Name, out char c)) {
			// check option name
			Console.WriteLine($"Invalid name char '{c}' in {OptionNameOrDefault(option)}");
			goto fail;
		}
		var defaultValue = option.DefaultValue;
		if (defaultValue is null) {
			return true;
		}
		// the following code is the default value check
		if (option.IsRequired) {
			// option has default value, but option is required
			Console.WriteLine($"{OptionNameOrDefault(option, true)} is required but has default value '{defaultValue}'");
			goto fail;
		}
		if (optionType == typeof(bool)) {
			// option has default value, but option type is bool
			Console.WriteLine($"Type of {OptionNameOrDefault(option, true)} is '{typeof(bool)}' but option has default value '{defaultValue}'");
			goto fail;
		}
		var defaultValueType = defaultValue.GetType();
		if (defaultValueType != optionType && defaultValueType != typeof(string)) {
			// option has default value, but type of default value is not supported
			Console.WriteLine($"Type of default value ({defaultValueType}) is neither same as type of {OptionNameOrDefault(option)} ({optionType}) nor '{typeof(string)}'");
			goto fail;
		}
		return true;

	fail:
		option = null;
		return false;
	}

	static bool IsValidOptionName(string name, out char invalidChar) {
		invalidChar = default;
		if (string.IsNullOrEmpty(name))
			return true;
		foreach (char c in name) {
			if (!IsValidOptionNameChar(c)) {
				invalidChar = c;
				return false;
			}
		}
		return true;
	}

	static bool IsValidOptionNameChar(char c) {
		return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-' || c == '/';
	}

	static bool IsSupportedType(Type type) {
		if (type.IsArray) {
			if (type.GetArrayRank() != 1)
				return false;
			return IsSupportedTypeCore(type.GetElementType()!);
		}

		return IsSupportedTypeCore(type);
	}

	static bool IsSupportedTypeCore(Type type) {
		switch (Type.GetTypeCode(type)) {
		case TypeCode.Boolean:
		case TypeCode.Char:
		case TypeCode.SByte:
		case TypeCode.Byte:
		case TypeCode.Int16:
		case TypeCode.UInt16:
		case TypeCode.Int32:
		case TypeCode.UInt32:
		case TypeCode.Int64:
		case TypeCode.UInt64:
		case TypeCode.Single:
		case TypeCode.Double:
		case TypeCode.Decimal:
		case TypeCode.DateTime:
		case TypeCode.String: return true;
		default: return false;
		}
	}

	static string OptionNameOrDefault(OptionAttribute option, bool firstCharUpper = false) {
		if (!firstCharUpper)
			return !option.IsDefault ? $"option '{option.Name}'" : "default option";
		else
			return !option.IsDefault ? $"Option '{option.Name}'" : "Default option";
	}

	public static bool ShowUsage<T>() {
		if (!TryGetOptionInfos(typeof(T), out var optionInfoMap, out var defaultOptionInfo, out _, out _))
			return false;
		var optionInfos = new List<OptionInfo>(optionInfoMap.Count + 1);
		if (defaultOptionInfo is not null)
			optionInfos.Add(defaultOptionInfo);
		optionInfos.AddRange(optionInfoMap.Values);
		if (optionInfos.Count == 0) {
			Console.WriteLine("No option available");
			return true;
		}

		var lines = new List<StringBuilder>(optionInfos.Count);
		for (int i = 0; i < optionInfos.Count; i++)
			lines.Add(new StringBuilder());
		AppendGroup(lines, _ => "  ");
		AppendPadRightGroup(lines, i => optionInfos[i].DisplayName);
		AppendGroup(lines, _ => "  ");
		AppendPadRightGroup(lines, i => optionInfos[i].Type.Name);
		AppendGroup(lines, _ => "  ");
		AppendPadRightGroup(lines, i => optionInfos[i].Description);
		AppendGroup(lines, i => !optionInfos[i].IsRequired ? "  (Optional)" : string.Empty);

		var sb = new StringBuilder();
		sb.AppendLine("Use -h --h /h -help --help /help to show these usage tips.");
		sb.AppendLine("Options:");
		foreach (var line in lines)
			sb.AppendLine(line.ToString());

		Console.WriteLine(sb.ToString());
		return true;
	}

	static void AppendGroup(List<StringBuilder> lines, Func<int, string> s) {
		for (int i = 0; i < lines.Count; i++)
			lines[i].Append(s(i));
	}

	static void AppendPadRightGroup(List<StringBuilder> lines, Func<int, string> s) {
		int maxWidth = Enumerable.Range(0, lines.Count).Max(i => CountHalfWidth(s(i)));
		for (int i = 0; i < lines.Count; i++) {
			var v = s(i);
			lines[i].Append(v);
			int delta = maxWidth - CountHalfWidth(s(i));
			if (delta != 0)
				lines[i].Append(new string(' ', delta));
		}
	}

	static int CountHalfWidth(string s) {
		int count = 0;
		foreach (char c in s)
			count += IsHalfWidth(c) ? 1 : 2;
		return count;
	}

	static bool IsHalfWidth(char c) {
		return ('\u0000' <= c && c <= '\u00FF') || ('\uFF61' <= c && c <= '\uFFDC') || ('\uFFE8' <= c && c <= '\uFFEE');
	}

	sealed class OptionInfo {
		delegate T Parser<T>(string s, NumberStyles style);

		readonly OptionAttribute option;
		readonly Type declaringType;
		readonly PropertyInfo property;
		bool hasSetValue;

		public OptionAttribute Option => option;

		public Type DeclaringType => declaringType;

		public PropertyInfo Property => property;

		public string Name => Prefix + (option.Name ?? string.Empty);

		public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : "<default>";

		public bool IsDefault => option.IsDefault;

		public bool IsRequired => option.IsRequired;

		public char Separator => option.Separator;

		public object? DefaultValue => option.DefaultValue;

		public string Description => option.Description ?? string.Empty;

		public Type Type => property.PropertyType;

		public bool IsBoolean => Type == typeof(bool);

		public bool IsArray => Type.IsArray;

		public bool HasSetValue => hasSetValue;

		public string Prefix { get; set; } = string.Empty;

		public OptionInfo(OptionAttribute option, Type declaringType, PropertyInfo property) {
			this.option = option;
			this.declaringType = declaringType;
			this.property = property;
		}

		public bool TrySetValue(Dictionary<Type, object> instances, object? value) {
			if (hasSetValue) {
				Console.WriteLine($"{OptionNameOrDefault(option, true)} has been set");
				return false;
			}

			hasSetValue = true;
			try {
				SetValue(instances[DeclaringType], value);
				return true;
			}
			catch (Exception ex) {
				if (ex is TargetInvocationException ex2)
					ex = ex2.InnerException!;
				Console.WriteLine($"Fail to set {OptionNameOrDefault(option)} , {ex.GetType()}: {ex.Message}");
				return false;
			}
		}

		void SetValue(object instance, object? value) {
			if (IsBoolean) {
				// bool type, value should be a boxed bool object
				property.SetValue(instance, value, null);
				return;
			}
			if (value is null || value.GetType() == Type) {
				// value is null or type of value equals option type, just set and don't convert
				property.SetValue(instance, value, null);
				return;
			}

			var s = (string)value;
			if (IsArray) {
				var elements = s.Split(Separator).Select(t => t.Trim()).Where(t => t.Length != 0).ToArray();
				var elementType = Type.GetElementType()!;
				var values = Array.CreateInstance(elementType, elements.Length);
				for (int i = 0; i < elements.Length; i++)
					values.SetValue(Parse(elements[i], elementType), i);
				value = values;
			}
			else {
				value = Parse(s.Trim(), Type);
			}
			property.SetValue(instance, value, null);
		}

		static object Parse(string s, Type type) {
			if (type.IsEnum)
				return Enum.Parse(type, s, true);

			switch (Type.GetTypeCode(type)) {
			case TypeCode.Boolean: return bool.Parse(s);
			case TypeCode.Char: return char.Parse(s);
			case TypeCode.SByte: return ParseInteger(s, sbyte.Parse);
			case TypeCode.Byte: return ParseInteger(s, byte.Parse);
			case TypeCode.Int16: return ParseInteger(s, short.Parse);
			case TypeCode.UInt16: return ParseInteger(s, ushort.Parse);
			case TypeCode.Int32: return ParseInteger(s, int.Parse);
			case TypeCode.UInt32: return ParseInteger(s, uint.Parse);
			case TypeCode.Int64: return ParseInteger(s, long.Parse);
			case TypeCode.UInt64: return ParseInteger(s, ulong.Parse);
			case TypeCode.Single: return float.Parse(s);
			case TypeCode.Double: return double.Parse(s);
			case TypeCode.Decimal: return decimal.Parse(s);
			case TypeCode.DateTime: return DateTime.Parse(s);
			case TypeCode.String: return s;
			default: throw new ArgumentOutOfRangeException(nameof(type));
			}
		}

		static T ParseInteger<T>(string s, Parser<T> parser) {
			bool isHex = false;
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				s = s.Substring(2);
				isHex = true;
			}
			else if (s.EndsWith("h", StringComparison.OrdinalIgnoreCase)) {
				s = s.Substring(0, s.Length - 1);
				isHex = true;
			}
			return parser(s, isHex ? NumberStyles.HexNumber : NumberStyles.Integer);
		}
	}
}
