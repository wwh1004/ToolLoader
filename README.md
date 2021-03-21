# ToolLoader
A loader for the tools which depends on given framework version and platform

## Usages
### Code
Apply OptionAttribute to property

#### Attribute
OptionAttribute.cs:
``` cs
namespace System.Cli {
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
		public object DefaultValue { get; set; }

		/// <summary>
		/// Option description, which is used to describe this option
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public OptionAttribute() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Option name</param>
		public OptionAttribute(string name) {
			Name = name;
		}
	}
}
```

#### Example:
``` cs
using System;
using System.Threading;
using Tool;
using Tool.Interface;

namespace TestTool {
	internal enum ToolEnum {
		EnumA,
		EnumB
	}

	internal sealed class ToolOptions {
		[Option(Description = "A string value, this is default option")]
		public string DefaultOption { get; set; }

		[Option("-r", IsRequired = true, Description = "A string value")]
		public string RequiredOption { get; set; }

		[Option("-o", DefaultValue = ToolEnum.EnumB, Description = "An enum value")]
		public ToolEnum OptionalOption { get; set; }

		[Option("-o2", DefaultValue = new ToolEnum[] { ToolEnum.EnumA, ToolEnum.EnumB }, Description = "Some enum values")]
		public ToolEnum[] OptionalOption2 { get; set; }
	}

	internal sealed class Tool : ITool<ToolOptions> {
		public string Title => "Test";

		public void Execute(ToolOptions settings) {
			Logger.Level = LogLevel.Verbose1;
			Logger.Info($"LogLevel: {Logger.Level}");
			Logger.Info("Info");
			Logger.Warning("Warning");
			Logger.Error("Error");
			Logger.Verbose1("Verbose1");
			Logger.Verbose2("Verbose2");
			Logger.Verbose2("Verbose3");

			string separator = new string('*', settings.RequiredOption.Length);
			Logger.Info(separator);
			Logger.Info($"DefaultOption: {settings.DefaultOption}");
			Logger.Info($"RequiredOption: {settings.RequiredOption}");
			Logger.Info($"OptionalOption: {settings.OptionalOption}");
			Logger.Info($"OptionalOption2: {string.Join(", ", settings.OptionalOption2)}");
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
Tool.Loader.CLR40.x64.exe TestTool.dll -r TestTool.dll1 Test2 -o EnumA -o2 EnumA,EnumA,EnumB
```

Output:
```
LogLevel: Verbose1
Info
Warning
Error
Verbose1
*************
DefaultOption: Test2
RequiredOption: TestTool.dll1
OptionalOption: EnumA
OptionalOption2: EnumA, EnumA, EnumB
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
   at TestTool.Tool.Execute(ToolOptions settings) in D:\Projects\ToolLoader\TestTool\Tool.cs:line 37
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
  -r         String      A string value
  -o         ToolEnum    An enum value                           (Optional)
  -o2        ToolEnum[]  Some enum values                        (Optional)
  <default>  String      A string value, this is default option  (Optional)

Press any key to exit...
```

## Downloads
GitHub: [Latest release](https://github.com/wwh1004/ToolLoader/releases/latest/download/ToolLoader.zip)

AppVeyor: [![Build status](https://ci.appveyor.com/api/projects/status/wxe8omc4iqb0v7ye?svg=true)](https://ci.appveyor.com/project/wwh1004/toolloader)
