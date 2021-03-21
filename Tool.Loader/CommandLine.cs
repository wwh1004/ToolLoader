using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.Cli {
	internal static class CommandLine {
		public static T Parse<T>(string[] args, out bool showUsage) where T : class, new() {
			if (args is null)
				throw new ArgumentNullException(nameof(args));

			if (!TryParse(args, out T result, out showUsage))
				throw new FormatException($"Invalid {nameof(args)} or generic argument {typeof(T)}");
			if (showUsage && !ShowUsage<T>())
				throw new FormatException($"Can't generate usage for {typeof(T)}");
			return result;
		}

		public static bool TryParse<T>(string[] args, out T result, out bool showUsage) where T : class, new() {
			if (args is null) {
				Console.WriteLine($"Parameter '{nameof(args)}' is null");
				goto fail;
			}
			if (args.Any(t => string.IsNullOrEmpty(t))) {
				Console.WriteLine($"Contains arg in '{nameof(args)}' is null");
				goto fail;
			}
			if (!TryGetOptionInfos(typeof(T), out var optionInfos, out var defaultOptionInfo)) {
				goto fail;
			}

			showUsage = false;
			for (int i = 0; i < args.Length; i++) {
				string arg = args[i].Trim();
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
				return true;
			}

			result = new T();
			for (int i = 0; i < args.Length; i++) {
				if (!optionInfos.TryGetValue(args[i], out var optionInfo)) {
					if (!(defaultOptionInfo is null) && !defaultOptionInfo.HasSetValue) {
						// 默认选项
						if (!defaultOptionInfo.TrySetValue(result, args[i]))
							goto fail;
						continue;
					}
					else {
						// 不是有效选项名
						Console.WriteLine($"'{args[i]}' is not a valid option");
						goto fail;
					}
				}

				if (optionInfo.IsBoolean) {
					// 是 bool 类型，所以不需要其它判断，直接赋值 true
					if (!optionInfo.TrySetValue(result, true))
						goto fail;
					continue;
				}

				if (i == args.Length - 1) {
					// 需要提供值但是到末尾了，未提供值
					Console.WriteLine($"No value is provided for {OptionNameOrDefault(optionInfo.Option)}");
					goto fail;
				}

				if (!optionInfo.TrySetValue(result, args[++i])) {
					goto fail;
				}
			}

			foreach (var optionInfo in optionInfos.Values) {
				if (optionInfo.HasSetValue)
					continue;
				// 选项已设置值

				if (optionInfo.IsRequired) {
					// 必选选项
					Console.WriteLine($"Required {OptionNameOrDefault(optionInfo.Option)} must have a value");
					goto fail;
				}
				else if (optionInfo.IsBoolean) {
					// bool 选项
					if (!optionInfo.TrySetValue(result, false))
						goto fail;
				}
				else {
					// 可选选项
					if (!optionInfo.TrySetValue(result, optionInfo.DefaultValue))
						goto fail;
				}
			}
			return true;

		fail:
			result = null;
			showUsage = false;
			return false;
		}

		private static bool TryGetOptionInfos(Type type, out Dictionary<string, OptionInfo> optionInfos, out OptionInfo defaultOptionInfo) {
			optionInfos = new Dictionary<string, OptionInfo>();
			defaultOptionInfo = null;
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var property in properties) {
				if (!VerifyProperty(property, out var option))
					goto fail;
				if (option is null)
					continue;

				if (!option.IsDefault) {
					optionInfos.Add(option.Name, new OptionInfo(option, property));
				}
				else if (defaultOptionInfo is null) {
					defaultOptionInfo = new OptionInfo(option, property);
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

		private static bool VerifyProperty(PropertyInfo property, out OptionAttribute option) {
			option = null;
			object[] options = property.GetCustomAttributes(typeof(OptionAttribute), false);
			if (options is null || options.Length == 0) {
				// 排除未应用 OptionAttribute 的属性
				return true;
			}
			if (options.Length != 1) {
				// OptionAttribute 不应该被应用多次
				Console.WriteLine($"Duplicated '{nameof(OptionAttribute)}' in property '{property.Name}'");
				goto fail;
			}
			var optionType = property.PropertyType;
			if (!IsSupportedType(optionType)) {
				// 检查返回类型
				Console.WriteLine($"Unsupported type '{optionType}' in property '{property.Name}'");
				goto fail;
			}
			option = (OptionAttribute)options[0];
			if (option.IsDefault && optionType == typeof(bool)) {
				// 默认选项不能为 bool 类型
				Console.WriteLine($"Property '{property.Name}' is default option but option type is '{typeof(bool)}'");
				goto fail;
			}
			if (!IsValidOptionName(option.Name, out char c)) {
				// 检查选项名是否合法
				Console.WriteLine($"Invalid name char '{c}' in {OptionNameOrDefault(option)}");
				goto fail;
			}
			object defaultValue = option.DefaultValue;
			if (defaultValue is null) {
				return true;
			}
			// 下面是默认值检查
			if (option.IsRequired) {
				// 有默认值但选项是必选选项
				Console.WriteLine($"{OptionNameOrDefault(option, true)} is required but has default value '{defaultValue}'");
				goto fail;
			}
			if (optionType == typeof(bool)) {
				// 有默认值但选项类型为 bool
				Console.WriteLine($"Type of {OptionNameOrDefault(option, true)} is '{typeof(bool)}' but option has default value '{defaultValue}'");
				goto fail;
			}
			var defaultValueType = defaultValue.GetType();
			if (defaultValueType != optionType && defaultValueType != typeof(string)) {
				// 有默认值但默认值的类型与属性的类型不相同
				Console.WriteLine($"Type of default value ({defaultValueType}) is neither same as type of {OptionNameOrDefault(option)} ({optionType}) nor '{typeof(string)}'");
				goto fail;
			}
			return true;

		fail:
			option = null;
			return false;
		}

		private static bool IsValidOptionName(string name, out char invalidChar) {
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

		private static bool IsValidOptionNameChar(char c) {
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-' || c == '/';
		}

		private static bool IsSupportedType(Type type) {
			if (type.IsArray) {
				if (type.GetArrayRank() != 1)
					return false;
				return IsSupportedTypeCore(type.GetElementType());
			}

			return IsSupportedTypeCore(type);
		}

		private static bool IsSupportedTypeCore(Type type) {
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

		private static string OptionNameOrDefault(OptionAttribute option, bool firstCharUpper = false) {
			if (!firstCharUpper)
				return !option.IsDefault ? $"option '{option.Name}'" : "default option";
			else
				return !option.IsDefault ? $"Option '{option.Name}'" : "Default option";
		}

		public static bool ShowUsage<T>() {
			if (!TryGetOptionInfos(typeof(T), out var optionInfoMap, out var defaultOptionInfo))
				return false;
			var optionInfos = optionInfoMap.Values.ToList();
			if (!(defaultOptionInfo is null))
				optionInfos.Add(defaultOptionInfo);
			if (optionInfos.Count == 0) {
				Console.WriteLine("No option available");
				return true;
			}

			int maxNameLength = optionInfos.Max(t => t.DisplayName.Length);
			int maxTypeNameLength = optionInfos.Max(t => t.Type.Name.Length);
			int maxDescriptionLength = optionInfos.Max(t => t.Description.Length);
			var sb = new StringBuilder();
			sb.AppendLine("Use -h --h /h -help --help /help to show these usage tips.");
			sb.AppendLine("Options:");
			foreach (var optionInfo in optionInfos) {
				sb.Append($"  {optionInfo.DisplayName.PadRight(maxNameLength)}  {optionInfo.Type.Name.PadRight(maxTypeNameLength)}  {optionInfo.Description.PadRight(maxDescriptionLength)}");
				if (!optionInfo.IsRequired)
					sb.Append("  (Optional)");
				sb.AppendLine();
			}

			Console.WriteLine(sb.ToString());
			return true;
		}

		private sealed class OptionInfo {
			private delegate T Parser<T>(string s, NumberStyles style);

			private readonly OptionAttribute _option;
			private readonly PropertyInfo _property;
			private bool _hasSetValue;

			public OptionAttribute Option => _option;

			public PropertyInfo Property => _property;

			public string Name => _option.Name ?? string.Empty;

			public string DisplayName => !string.IsNullOrEmpty(_option.Name) ? _option.Name : "<default>";

			public bool IsDefault => _option.IsDefault;

			public bool IsRequired => _option.IsRequired;

			public char Separator => _option.Separator;

			public object DefaultValue => _option.DefaultValue;

			public string Description => _option.Description ?? string.Empty;

			public Type Type => _property.PropertyType;

			public bool IsBoolean => Type == typeof(bool);

			public bool IsArray => Type.IsArray;

			public bool HasSetValue => _hasSetValue;

			public OptionInfo(OptionAttribute option, PropertyInfo property) {
				_option = option;
				_property = property;
			}

			public bool TrySetValue(object instance, object value) {
				if (_hasSetValue) {
					Console.WriteLine($"{OptionNameOrDefault(_option, true)} has been set");
					return false;
				}

				_hasSetValue = true;
				try {
					SetValue(instance, value);
					return true;
				}
				catch (Exception ex) {
					if (ex is TargetInvocationException ex2)
						ex = ex2.InnerException;
					Console.WriteLine($"Fail to set {OptionNameOrDefault(_option)} , {ex.GetType()}: {ex.Message}");
					return false;
				}
			}

			private void SetValue(object instance, object value) {
				if (IsBoolean) {
					// bool 类型，value 一定是装箱的 bool 值
					_property.SetValue(instance, value, null);
					return;
				}
				if (value is null || value.GetType() == Type) {
					// value 是 null 或者 value 的类型等于选项类型，直接设置，不需要转换
					_property.SetValue(instance, value, null);
					return;
				}

				string s = (string)value;
				if (IsArray) {
					string[] elements = s.Split(_option.Separator).Select(t => t.Trim()).Where(t => t.Length != 0).ToArray();
					var elementType = Type.GetElementType();
					var values = Array.CreateInstance(elementType, elements.Length);
					for (int i = 0; i < elements.Length; i++)
						values.SetValue(Parse(elements[i], elementType), i);
					value = values;
				}
				else {
					value = Parse(s.Trim(), Type);
				}
				_property.SetValue(instance, value, null);
			}

			private static object Parse(string s, Type type) {
				if (type.IsEnum)
					return Enum.Parse(type, s);

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

			private static T ParseInteger<T>(string s, Parser<T> parser) {
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
}
