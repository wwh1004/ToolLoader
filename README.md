# ToolLoader
A loader for the tools which depends on given framework version and platform

## Usages
### Code
Apply OptionAttribute to property

#### Attribute
OptionAttribute.cs:
``` cs
/// <summary>
/// Represents a command line option. The property which <see cref="OptionAttribute"/> is applied to must be an instance property and one of the following types: <see cref="bool"/>, <see cref="char"/>, <see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>, <see cref="DateTime"/>, <see cref="string"/>, <see cref="Enum"/> or array of them.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionAttribute : Attribute {
	/// <summary>
	/// Option name
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Whether it is a default option
	/// </summary>
	public bool IsDefault => string.IsNullOrEmpty(Name);

	/// <summary>
	/// Whether it is a required option
	/// </summary>
	public bool IsRequired { get; set; }

	/// <summary>
	/// Array separator, which is used to split value when option type is array
	/// </summary>
	public char Separator { get; set; } = ',';

	/// <summary>
	/// By default, when <see cref="IsRequired"/> is <see langword="true"/>, <see cref="DefaultValue"/> must be <see langword="null"/>
	/// </summary>
	public object? DefaultValue { get; set; }

	/// <summary>
	/// Option description, which is used to describe this option
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Constructor
	/// </summary>
	public OptionAttribute() {
		Name = string.Empty;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="name">Option name</param>
	public OptionAttribute(string name) {
		Name = name;
	}
}
```

ChildOptionsAttribute.cs:
``` cs
/// <summary>
/// Marks current type as child of <see cref="Parent"/>. <see cref="Parent"/> type must have instance property 'ChildOptions' with return type 'IDictionary&lt;Type, object&gt;'
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ChildOptionsAttribute : Attribute {
	/// <summary>
	/// Parent options type
	/// </summary>
	public Type Parent { get; }

	/// <summary>
	/// Option prefix (can be empty)
	/// </summary>
	public string Prefix { get; set; } = string.Empty;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="parent">Parent options type</param>
	/// <exception cref="ArgumentNullException"></exception>
	public ChildOptionsAttribute(Type parent) {
		Parent = parent ?? throw new ArgumentNullException(nameof(parent));
	}
}
```

#### Example:
``` cs
enum ToolEnum {
	EnumA,
	EnumB
}

sealed class ToolOptions {
	public IDictionary<Type, object> ChildOptions { get; } = new Dictionary<Type, object>();

	[Option(Description = "A string value, this is default option")]
	public string DefaultOption { get; set; }

	[Option("-r", IsRequired = true, Description = "A string value")]
	public string RequiredOption { get; set; }

	[Option("-o", DefaultValue = ToolEnum.EnumB, Description = "An enum value")]
	public ToolEnum OptionalOption { get; set; }

	[Option("-o2", DefaultValue = new ToolEnum[] { ToolEnum.EnumA, ToolEnum.EnumB }, Description = "Some enum values")]
	public ToolEnum[] OptionalOption2 { get; set; }

	public T ChildOption<T>() {
		return (T)ChildOptions[typeof(T)];
	}
}

[ChildOptions(typeof(ToolOptions))]
sealed class ChildToolOptions1 {
	[Option("-child1-o1", Description = "A child option", DefaultValue = "My default value")]
	public string ChildOption1 { get; set; }
}

[ChildOptions(typeof(ToolOptions), Prefix = "-child2")]
sealed class ChildToolOptions2 {
	[Option("-o2", Description = "Another child option")]
	public string ChildOption2 { get; set; }
}

sealed class Tool : ITool<ToolOptions> {
	public string Title => null;

	public void Execute(ToolOptions options) {
		Logger.Level = LogLevel.Verbose1;
		Logger.Info($"LogLevel: {Logger.Level}");
		Logger.Info("Info");
		Logger.Warning("Warning");
		Logger.Error("Error");
		Logger.Verbose1($"Verbose1, options.ChildOptions.Count: {options.ChildOptions.Count:X8}");
		Logger.Verbose2("Verbose2");
		Logger.Verbose3("Verbose3");
		Logger.Verbose3($"Verbose{3:X} with InterpolatedStringHandler: {typeof(Logger.Verbose3InterpolatedStringHandler)}");

		var separator = new string('*', options.RequiredOption.Length);
		Logger.Info(separator);
		Logger.Info($"DefaultOption: {options.DefaultOption}");
		Logger.Info($"RequiredOption: {options.RequiredOption}");
		Logger.Info($"OptionalOption: {options.OptionalOption}");
		Logger.Info($"OptionalOption2: {string.Join(", ", options.OptionalOption2)}");
		Logger.Info($"ChildOption1: {options.ChildOption<ChildToolOptions1>().ChildOption1}");
		Logger.Info($"ChildOption2: {options.ChildOption<ChildToolOptions2>().ChildOption2}");
		Logger.Info(separator);

		var lockedLogger = Logger.EnterLock();
		lockedLogger.Info($"Lock mode | IsLocked: {lockedLogger.IsLocked}");
		new Thread(() => Logger.Warning($"I'm blocked | IsLocked: {lockedLogger.IsLocked}")).Start();
		Thread.Sleep(2000);
		lockedLogger.ExitLock();
		Logger.Info($"No lock mode | IsLocked: {lockedLogger.IsLocked}");

		Logger.Info("Exception test");
		try {
			throw new ApplicationException("test");
		}
		catch (Exception ex) {
			Logger.Exception(ex);
		}

		Logger.Flush();
	}
}
```

### CLI
#### Syntax:
Command:
```
Tool.Loader.*.exe <tool-name.dll> <arguments>
```

Show usage:
```
Tool.Loader.*.exe <tool-name.dll> -h
```

#### Example:
Command:
```
Tool.Loader.CLR40.x64.exe TestTool.dll -r TestTool.dll1 Test2 -o EnumA -o2 EnumA,EnumA,EnumB -child2-o2 test
```

Output:
```
LogLevel: Verbose1
Info
Warning
Error
Verbose1, options.ChildOptions.Count: 00000002
*************
DefaultOption: Test2
RequiredOption: TestTool.dll1
OptionalOption: EnumA
OptionalOption2: EnumA, EnumA, EnumB
ChildOption1: My default value
ChildOption2: test
*************
Lock mode | IsLocked: True
I'm blocked | IsLocked: True
No lock mode | IsLocked: False
Exception test
Type:
System.ApplicationException
Message:
test
Source:
TestTool
StackTrace:
   at TestTool.Tool.Execute(ToolOptions options) in D:\Projects\ToolLoader\TestTool\Tool.cs:line 41
TargetSite:
Void Execute(TestTool.ToolOptions)
----------------------------------------

Press any key to exit...
```

Show usage:
```
Tool.Loader.CLR40.x64.exe TestTool.dll -h
```

Output:
```
Use -h --h /h -help --help /help to show these usage tips.
Options:
  <default>   String      A string value, this is default option  (Optional)
  -r          String      A string value
  -o          ToolEnum    An enum value                           (Optional)
  -o2         ToolEnum[]  Some enum values                        (Optional)
  -child1-o1  String      A child option                          (Optional)
  -child2-o2  String      Another child option                    (Optional)

Press any key to exit...
```

## Downloads
GitHub: [Latest release](https://github.com/wwh1004/ToolLoader/releases/latest/download/ToolLoader.zip)

AppVeyor: [![Build status](https://ci.appveyor.com/api/projects/status/wxe8omc4iqb0v7ye?svg=true)](https://ci.appveyor.com/project/wwh1004/toolloader)
