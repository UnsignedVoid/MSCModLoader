using System;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Application
{
	class MethodName{
		public string Type {
			get => this.methodType;
		}

		public string Name {
			get => this.methodName;
		}
		public MethodName(string FullName){
			int lastDot = FullName.LastIndexOf(".");
			if (lastDot <= 0) throw new ArgumentException("Invalid method name " + FullName);
			this.methodType = FullName.Substring(0, lastDot);
			this.methodName = FullName.Substring(lastDot+1, FullName.Length - lastDot - 1);
		}

		private string methodType;
		private string methodName;

	}
	class Program
	{

		static void Main(string[] args){
			
			string[] arguments = args[0].Split(',');
			if (arguments.Length < 4) {
				throw new ArgumentException("Not ehough args");
			}
			string GameDirectory = arguments[0];
			string AssembliesPath = Path.Combine(Path.Combine(GameDirectory, "mysummercar_Data"), "Managed");

			string TargetAssemblyPath = Path.Combine(AssembliesPath, "Assembly-CSharp.dll");

			string SourceAssemblyPath = arguments[1];

			string SorceMethodFullName = arguments[2];
			string TargetMethodFullName = arguments[3];

			MethodName TargetMethod = new MethodName(TargetMethodFullName);
			MethodName SourceMethod = new MethodName(SorceMethodFullName);

			Console.WriteLine("Inejecting " + TargetMethod.Name + " From " + SourceAssemblyPath + " " + SourceMethod.Type + " " + SourceMethod.Name + " Into " + TargetAssemblyPath + " " + TargetMethod.Type + " " + TargetMethod.Name);

			DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
			resolver.AddSearchDirectory(AssembliesPath);

			ReaderParameters ParamsWritableAssenbly = new ReaderParameters {ReadWrite = true, AssemblyResolver = resolver};
			ModuleDefinition TargetAssembly = ModuleDefinition.ReadModule(TargetAssemblyPath, ParamsWritableAssenbly) ?? throw new ArgumentException("Cannot Load " + TargetAssemblyPath);
			TypeDefinition TypeToHookIn = TargetAssembly.GetType(TargetMethod.Type) ?? throw new ArgumentException("Cannot find " + TargetMethod.Type + " in  " + TargetAssemblyPath);
			MethodDefinition MethodToHookIn = TypeToHookIn.Methods.First(x => x.Name == TargetMethod.Name) ?? throw new ArgumentException("Cannot find " + TargetMethod.Name + " in  " + TargetMethod.Type);

			ModuleDefinition SourceAssembly = ModuleDefinition.ReadModule(SourceAssemblyPath) ?? throw new ArgumentException("Cannot Load " + SourceAssemblyPath);
			TypeDefinition TypeToInject = SourceAssembly.GetType(SourceMethod.Type) ?? throw new ArgumentException("Cannot find " + SourceMethod.Type + " in " + SourceAssemblyPath);
			MethodDefinition MethodToInject = TypeToInject.Methods.Single(x => x.Name == SourceMethod.Name) ?? throw new ArgumentException("Cannot find " + SourceMethod.Name + " in " + SourceMethod.Type);


			Instruction LoaderInit = Instruction.Create(OpCodes.Call, TargetAssembly.ImportReference(MethodToInject));
			ILProcessor Processor = MethodToHookIn.Body.GetILProcessor();
			Processor.InsertBefore(MethodToHookIn.Body.Instructions[0], LoaderInit);
			TargetAssembly.Write();
			TargetAssembly.Dispose();
			SourceAssembly.Dispose();		
		}
	}
}


