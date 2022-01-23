namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
sealed class InterpolatedStringHandlerAttribute : Attribute {
	public InterpolatedStringHandlerAttribute() {
	}
}
