using System;
using System.Threading;
using Tool.Interface;
using Tool.Logging;

namespace TestTool;

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
