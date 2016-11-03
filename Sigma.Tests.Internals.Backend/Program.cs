﻿using Sigma.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;
using Sigma.Core;
using Sigma.Core.Utils;

namespace Sigma.Tests.Internals.Backend
{
	class Program
	{
		static void Main(string[] args)
		{
			SigmaEnvironment sigma = SigmaEnvironment.Create("test");

			Registry trainer1 = new Registry(sigma.Registry, new string[] { "trainer" });
			Registry trainer2 = new Registry(sigma.Registry, new string[] { "trainer" });
			Registry weirdtrainer = new Registry(sigma.Registry, new string[] { "trainer" });
			Registry trainer1architecture = new Registry(trainer1, new string[] { "architecture" });
			Registry weirdarchitecture = new Registry(weirdtrainer, new string[] { "architecture" });

			sigma.Registry["trainer1"] = trainer1;
			sigma.Registry["trainer2"] = trainer2;
			sigma.Registry["weirdtrainer"] = weirdtrainer;

			trainer1["architecture"] = trainer1architecture;
			weirdtrainer["architecture"] = weirdarchitecture;

			trainer1architecture["complexity"] = 2;
			weirdarchitecture["complexity"] = 3;

			trainer1["accuracy"] = 0.0f;
			trainer2["accuracy"] = 0.0f;

			sigma.RegistryResolver.ResolveSet<float>("*.accuracy", 1.0f, typeof(float));
			sigma.RegistryResolver.ResolveSet<int>("*<trainer>.architecture.complexity", 5, typeof(int));
			sigma.RegistryResolver.ResolveSet<int>("*<trainer>.architecture.complexity", 6, typeof(int));
			sigma.RegistryResolver.ResolveSet<int>("trainer*<trainer>.*<architecture>.complexity", 9, typeof(int));

			Registry differentTrainer1Architecture = new Registry(trainer1, "architecture");

			trainer1["architecture"] = differentTrainer1Architecture;
			differentTrainer1Architecture["complexity"] = 2;

			sigma.RegistryResolver.ResolveSet<int>("*<trainer>.architecture.complexity", 11, typeof(int));

			float[] result = null;

			Console.WriteLine(string.Join(",", sigma.RegistryResolver.ResolveRetrieve<float>("*.accuracy", ref result)));
			Console.WriteLine(string.Join(",", result));

			Console.WriteLine(sigma.Registry);
			Console.ReadKey();
		}
	}
}
