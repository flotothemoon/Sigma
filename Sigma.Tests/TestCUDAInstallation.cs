﻿/* 
MIT License

Copyright (c) 2016 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using ManagedCuda;
using ManagedCuda.CudaBlas;
using NUnit.Framework;

namespace Sigma.Tests
{
	public class TestCUDAInstallation
	{
		static bool cudaInstalled;
		static bool checkedCudaInstalled;

		public static void AssertIgnoreIfCudaUnavailable()
		{
			if (!checkedCudaInstalled)
			{
				try
				{
					new CudaContext();

					cudaInstalled = true;
				}
				catch
				{
					cudaInstalled = false;
				}

				checkedCudaInstalled = true;
			}

			if (!cudaInstalled)
			{
				Assert.Ignore("CUDA installation not found or not working. As CUDA is optional, this test will be ignored.");
			}
		}

		[TestCase]
		public void TestCreateDefaultCUDAContext()
		{
			AssertIgnoreIfCudaUnavailable();

			CudaContext context = new CudaContext();
		}

		[TestCase]
		public void TestCreateCudaBlas()
		{
			AssertIgnoreIfCudaUnavailable();

			CudaBlas cublas = new CudaBlas();
		}
	}
}
